using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.StorageHelpers;
using Jellyfin.Server.Implementations.SystemBackupService;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Contains methods for creating and restoring backups.
/// </summary>
public class BackupService : IBackupService
{
    private const string ManifestEntryName = "manifest.json";
    private readonly ILogger<BackupService> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _applicationHost;
    private readonly IServerApplicationPaths _applicationPaths;
    private readonly IJellyfinDatabaseProvider _jellyfinDatabaseProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILibraryManager _libraryManager;
    private static readonly JsonSerializerOptions _serializerSettings = new JsonSerializerOptions(JsonSerializerDefaults.General)
    {
        AllowTrailingCommas = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    private readonly Version _backupEngineVersion = new Version(0, 2, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="dbProvider">A Database Factory.</param>
    /// <param name="applicationHost">The Application host.</param>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="jellyfinDatabaseProvider">The Jellyfin database Provider in use.</param>
    /// <param name="applicationLifetime">The SystemManager.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    public BackupService(
        ILogger<BackupService> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost applicationHost,
        IServerApplicationPaths applicationPaths,
        IJellyfinDatabaseProvider jellyfinDatabaseProvider,
        IHostApplicationLifetime applicationLifetime,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _dbProvider = dbProvider;
        _applicationHost = applicationHost;
        _applicationPaths = applicationPaths;
        _jellyfinDatabaseProvider = jellyfinDatabaseProvider;
        _hostApplicationLifetime = applicationLifetime;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc/>
    public void ScheduleRestoreAndRestartServer(string archivePath)
    {
        _applicationHost.RestoreBackupPath = archivePath;
        _applicationHost.ShouldRestart = true;
        _applicationHost.NotifyPendingRestart();
        _ = Task.Run(async () =>
        {
            await Task.Delay(500).ConfigureAwait(false);
            _hostApplicationLifetime.StopApplication();
        });
    }

    /// <inheritdoc/>
    public async Task RestoreBackupAsync(string archivePath)
    {
        _logger.LogWarning("Begin restoring system to {BackupArchive}", archivePath); // Info isn't cutting it
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Requested backup file '{archivePath}' does not exist.");
        }

        StorageHelper.TestCommonPathsForStorageCapacity(_applicationPaths, _logger);

        var fileStream = File.OpenRead(archivePath);
        await using (fileStream.ConfigureAwait(false))
        {
            using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, false);
            var zipArchiveEntry = zipArchive.GetEntry(ManifestEntryName);

            if (zipArchiveEntry is null)
            {
                throw new NotSupportedException($"The loaded archive '{archivePath}' does not appear to be a Jellyfin backup as its missing the '{ManifestEntryName}'.");
            }

            BackupManifest? manifest;
            var manifestStream = await zipArchiveEntry.OpenAsync().ConfigureAwait(false);
            await using (manifestStream.ConfigureAwait(false))
            {
                manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Cannot restore backup with an empty manifest.");
            }

            if (manifest.ServerVersion > _applicationHost.ApplicationVersion) // newer versions of Jellyfin should be able to load older versions as we have migrations.
            {
                throw new NotSupportedException($"The loaded archive '{archivePath}' is made for a newer version of Jellyfin ({manifest.ServerVersion}) and cannot be loaded in this version.");
            }

            if (!TestBackupVersionCompatibility(manifest.BackupEngineVersion))
            {
                throw new NotSupportedException($"The loaded archive '{archivePath}' is made for a newer version of Jellyfin ({manifest.ServerVersion}) and cannot be loaded in this version.");
            }

            void CopyDirectory(string source, string target, string[]? exclude = null)
            {
                var fullSourcePath = NormalizePathSeparator(Path.GetFullPath(source) + Path.DirectorySeparatorChar);
                var fullTargetRoot = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
                var excludePaths = exclude?.Select(e => $"{source}/{e}/").ToArray();
                foreach (var item in zipArchive.Entries)
                {
                    var sourcePath = NormalizePathSeparator(Path.GetFullPath(item.FullName));
                    var targetPath = Path.GetFullPath(Path.Combine(target, Path.GetRelativePath(source, item.FullName)));

                    if (excludePaths is not null && excludePaths.Any(e => item.FullName.StartsWith(e, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    if (!sourcePath.StartsWith(fullSourcePath, StringComparison.Ordinal)
                        || !targetPath.StartsWith(fullTargetRoot, StringComparison.Ordinal)
                        || Path.EndsInDirectorySeparator(item.FullName))
                    {
                        continue;
                    }

                    _logger.LogInformation("Restore and override {File}", targetPath);

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    item.ExtractToFile(targetPath, overwrite: true);
                }
            }

            void RestoreFiles()
            {
                CopyDirectory("Config", _applicationPaths.ConfigurationDirectoryPath);
                CopyDirectory("Data", _applicationPaths.DataPath, exclude: ["metadata", "metadata-default"]);
                CopyDirectory("Root", _applicationPaths.RootFolderPath);
                CopyDirectory("Data/metadata", _applicationPaths.InternalMetadataPath);
                CopyDirectory("Data/metadata-default", _applicationPaths.DefaultInternalMetadataPath);
            }

            if (manifest.Options.Database)
            {
                _logger.LogInformation("Begin restoring Database");
                var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
                await using (dbContext.ConfigureAwait(false))
                {
                    var entityTypes = typeof(JellyfinDbContext).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                        .Where(e => e.PropertyType.IsAssignableTo(typeof(IQueryable)))
                        .Select(e => (Type: e.PropertyType.GetGenericArguments()[0], SourceName: dbContext.Model.FindEntityType(e.PropertyType.GetGenericArguments()[0])!.GetSchemaQualifiedTableName()!))
                        .ToArray();
                    ValidateDatabaseEntries(zipArchive, manifest, entityTypes.Select(e => e.SourceName));

                    var historyEntry = zipArchive.GetEntry($"Database/{nameof(HistoryRow)}.json")!;
                    HistoryRow[] historyEntries;
                    var historyArchive = await historyEntry.OpenAsync().ConfigureAwait(false);
                    await using (historyArchive.ConfigureAwait(false))
                    {
                        historyEntries = await JsonSerializer.DeserializeAsync<HistoryRow[]>(historyArchive).ConfigureAwait(false)
                            ?? throw new InvalidOperationException("Cannot restore backup that has no History data.");
                    }

                    foreach (var entityType in entityTypes)
                    {
                        _logger.LogInformation("Read backup of {Table}", entityType.SourceName);

                        var zipEntry = zipArchive.GetEntry($"Database/{entityType.SourceName}.json");
                        if (zipEntry is null)
                        {
                            // Tables added since this backup was created have no rows to import.
                            continue;
                        }

                        var zipEntryStream = await zipEntry.OpenAsync().ConfigureAwait(false);
                        await using (zipEntryStream.ConfigureAwait(false))
                        {
                            _logger.LogInformation("Restore backup of {Table}", entityType.SourceName);
                            var records = 0;
                            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<JsonObject>(zipEntryStream, _serializerSettings).ConfigureAwait(false))
                            {
                                var entity = item?.Deserialize(entityType.Type);
                                if (entity is null)
                                {
                                    throw new InvalidOperationException($"Cannot deserialize entity '{item}'");
                                }

                                dbContext.Add(entity);
                                records++;
                            }

                            _logger.LogInformation("Prepared to restore {Number} entries for {Table}", records, entityType.SourceName);
                        }
                    }

                    RestoreFiles();
                    var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);
                    await using (transaction.ConfigureAwait(false))
                    {
                        var historyRepository = dbContext.GetService<IHistoryRepository>();
                        await historyRepository.CreateIfNotExistsAsync().ConfigureAwait(false);
                        foreach (var item in await historyRepository.GetAppliedMigrationsAsync().ConfigureAwait(false))
                        {
                            await dbContext.Database.ExecuteSqlRawAsync(historyRepository.GetDeleteScript(item.MigrationId)).ConfigureAwait(false);
                        }

                        foreach (var item in historyEntries)
                        {
                            await dbContext.Database.ExecuteSqlRawAsync(historyRepository.GetInsertScript(item)).ConfigureAwait(false);
                        }

                        _logger.LogInformation("Begin purging database");
                        await _jellyfinDatabaseProvider.PurgeDatabase(dbContext, entityTypes.Select(e => e.SourceName)).ConfigureAwait(false);
                        _logger.LogInformation("Database Purged");
                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                        await transaction.CommitAsync().ConfigureAwait(false);
                        _logger.LogInformation("Restored database");
                    }
                }
            }
            else
            {
                RestoreFiles();
            }

            _logger.LogInformation("Restored Jellyfin system from {Date}", manifest.DateCreated);
        }
    }

    private static void ValidateDatabaseEntries(ZipArchive archive, BackupManifest manifest, IEnumerable<string> tableNames)
    {
        if (manifest.DatabaseTables is null || manifest.DatabaseTables.Length == 0)
        {
            throw new InvalidOperationException("Cannot restore backup with no database table manifest.");
        }

        var entries = archive.Entries
            .Where(e => e.FullName.StartsWith("Database/", StringComparison.Ordinal) && e.FullName.EndsWith(".json", StringComparison.Ordinal))
            .Select(e => e.FullName["Database/".Length..^".json".Length])
            .ToHashSet(StringComparer.Ordinal);
        var knownTables = tableNames.Append(nameof(HistoryRow)).ToHashSet(StringComparer.Ordinal);
        var legacyManifest = manifest.DatabaseTables.All(e => e == typeof(DbSet<>).Name || e == nameof(HistoryRow));

        // Older 0.2 archives recorded DbSet`1 instead of table names. Their table count still
        // identifies a missing entry, without requiring tables introduced by later versions.
        var expectedTables = legacyManifest ? entries : manifest.DatabaseTables.ToHashSet(StringComparer.Ordinal);
        if (entries.Count != manifest.DatabaseTables.Length
            || !entries.SetEquals(expectedTables)
            || !entries.Contains(nameof(HistoryRow))
            || !entries.IsSubsetOf(knownTables)
            || archive.Entries.Count(e => e.FullName.StartsWith("Database/", StringComparison.Ordinal) && e.FullName.EndsWith(".json", StringComparison.Ordinal)) != entries.Count)
        {
            throw new InvalidOperationException("Cannot restore backup with missing, duplicate or unsupported database table entries.");
        }
    }

    private bool TestBackupVersionCompatibility(Version backupEngineVersion)
    {
        if (backupEngineVersion == _backupEngineVersion)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<BackupManifestDto> CreateBackupAsync(BackupOptionsDto backupOptions)
    {
        // Creating a backup runs a database optimization and reads the entire database under a transaction, both of
        // which heavily contend with an active library scan and could capture an inconsistent database state.
        if (_libraryManager.IsScanRunning)
        {
            _logger.LogWarning("Cannot create a backup while a library scan is running.");
            throw new InvalidOperationException("Cannot create a backup while a library scan is running. Please try again once the scan has finished.");
        }

        var manifest = new BackupManifest()
        {
            DateCreated = DateTime.UtcNow,
            ServerVersion = _applicationHost.ApplicationVersion,
            DatabaseTables = null!,
            BackupEngineVersion = _backupEngineVersion,
            Options = Map(backupOptions)
        };

        _logger.LogInformation("Running database optimization before backup");

        await _jellyfinDatabaseProvider.RunScheduledOptimisation(CancellationToken.None).ConfigureAwait(false);

        var backupFolder = Path.Combine(_applicationPaths.BackupPath);

        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        var backupStorageSpace = StorageHelper.GetFreeSpaceOf(_applicationPaths.BackupPath);

        const long FiveGigabyte = 5_368_709_115;
        if (backupStorageSpace.FreeSpace < FiveGigabyte)
        {
            throw new InvalidOperationException($"The backup directory '{backupStorageSpace.Path}' does not have at least '{StorageHelper.HumanizeStorageSize(FiveGigabyte)}' free space. Cannot create backup.");
        }

        var backupPath = Path.Combine(backupFolder, $"jellyfin-backup-{manifest.DateCreated.ToLocalTime():yyyyMMddHHmmss}.zip");

        try
        {
            _logger.LogInformation("Attempting to create a new backup at {BackupPath}", backupPath);
            var fileStream = File.OpenWrite(backupPath);
            await using (fileStream.ConfigureAwait(false))
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
            {
                _logger.LogInformation("Starting backup process");
                var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
                await using (dbContext.ConfigureAwait(false))
                {
                    dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    static IAsyncEnumerable<object> GetValues(IQueryable dbSet)
                    {
                        var method = dbSet.GetType().GetMethod(nameof(DbSet<object>.AsAsyncEnumerable))!;
                        var enumerable = method.Invoke(dbSet, null)!;
                        return (IAsyncEnumerable<object>)enumerable;
                    }

                    // include the migration history as well
                    var historyRepository = dbContext.GetService<IHistoryRepository>();
                    var migrations = await historyRepository.GetAppliedMigrationsAsync().ConfigureAwait(false);

                    ICollection<(string SourceName, Func<IAsyncEnumerable<object>> ValueFactory)> entityTypes =
                    [
                        .. typeof(JellyfinDbContext)
                            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                            .Where(e => e.PropertyType.IsAssignableTo(typeof(IQueryable)))
                            .Select(e => (SourceName: dbContext.Model.FindEntityType(e.PropertyType.GetGenericArguments()[0])!.GetSchemaQualifiedTableName()!, ValueFactory: new Func<IAsyncEnumerable<object>>(() => GetValues((IQueryable)e.GetValue(dbContext)!)))),
                        (SourceName: nameof(HistoryRow), ValueFactory: () => migrations.ToAsyncEnumerable())
                    ];
                    manifest.DatabaseTables = entityTypes.Select(e => e.SourceName).ToArray();
                    var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);

                    await using (transaction.ConfigureAwait(false))
                    {
                        _logger.LogInformation("Begin Database backup");

                        foreach (var entityType in entityTypes)
                        {
                            _logger.LogInformation("Begin backup of entity {Table}", entityType.SourceName);
                            var zipEntry = zipArchive.CreateEntry(NormalizePathSeparator(Path.Combine("Database", $"{entityType.SourceName}.json")));
                            var entities = 0;
                            var zipEntryStream = await zipEntry.OpenAsync().ConfigureAwait(false);
                            await using (zipEntryStream.ConfigureAwait(false))
                            {
                                var jsonSerializer = new Utf8JsonWriter(zipEntryStream);
                                await using (jsonSerializer.ConfigureAwait(false))
                                {
                                    jsonSerializer.WriteStartArray();

                                    var set = entityType.ValueFactory().ConfigureAwait(false);
                                    var enumerator = set.GetAsyncEnumerator();
                                    await using (enumerator)
                                    {
                                        while (true)
                                        {
                                            bool hasNext;
                                            try
                                            {
                                                hasNext = await enumerator.MoveNextAsync();
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, "Could not read next entity of type {Table}, the underlying data appears to be corrupt. Skipping this row and continuing backup; the affected database row should be inspected and fixed manually", entityType.SourceName);
                                                continue;
                                            }

                                            if (!hasNext)
                                            {
                                                break;
                                            }

                                            var item = enumerator.Current;
                                            entities++;
                                            try
                                            {
                                                using var document = JsonSerializer.SerializeToDocument(item, _serializerSettings);
                                                document.WriteTo(jsonSerializer);
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, "Could not load entity {Entity}", item);
                                                throw;
                                            }
                                        }
                                    }

                                    jsonSerializer.WriteEndArray();
                                }
                            }

                            _logger.LogInformation("Backup of entity {Table} with {Number} created", entityType.SourceName, entities);
                        }
                    }
                }

                _logger.LogInformation("Backup of folder {Table}", _applicationPaths.ConfigurationDirectoryPath);
                foreach (var item in Directory.EnumerateFiles(_applicationPaths.ConfigurationDirectoryPath, "*.xml", SearchOption.TopDirectoryOnly)
                             .Union(Directory.EnumerateFiles(_applicationPaths.ConfigurationDirectoryPath, "*.json", SearchOption.TopDirectoryOnly)))
                {
                    await zipArchive.CreateEntryFromFileAsync(item, NormalizePathSeparator(Path.Combine("Config", Path.GetFileName(item)))).ConfigureAwait(false);
                }

                void CopyDirectory(string source, string target, string filter = "*")
                {
                    if (!Directory.Exists(source))
                    {
                        return;
                    }

                    _logger.LogInformation("Backup of folder {Table}", source);

                    foreach (var item in Directory.EnumerateFiles(source, filter, SearchOption.AllDirectories))
                    {
                        // TODO: @bond make async
                        zipArchive.CreateEntryFromFile(item, NormalizePathSeparator(Path.Combine(target, Path.GetRelativePath(source, item))));
                    }
                }

                CopyDirectory(Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "users"), Path.Combine("Config", "users"));
                CopyDirectory(Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "ScheduledTasks"), Path.Combine("Config", "ScheduledTasks"));
                CopyDirectory(Path.Combine(_applicationPaths.RootFolderPath), "Root");
                CopyDirectory(Path.Combine(_applicationPaths.DataPath, "collections"), Path.Combine("Data", "collections"));
                CopyDirectory(Path.Combine(_applicationPaths.DataPath, "playlists"), Path.Combine("Data", "playlists"));
                CopyDirectory(Path.Combine(_applicationPaths.DataPath, "ScheduledTasks"), Path.Combine("Data", "ScheduledTasks"));
                if (backupOptions.Subtitles)
                {
                    CopyDirectory(Path.Combine(_applicationPaths.DataPath, "subtitles"), Path.Combine("Data", "subtitles"));
                }

                if (backupOptions.Trickplay)
                {
                    CopyDirectory(Path.Combine(_applicationPaths.DataPath, "trickplay"), Path.Combine("Data", "trickplay"));
                }

                if (backupOptions.Metadata)
                {
                    CopyDirectory(Path.Combine(_applicationPaths.InternalMetadataPath), Path.Combine("Data", "metadata"));

                    // If a custom metadata path is configured, the default location may still contain data.
                    if (!string.Equals(
                            Path.GetFullPath(_applicationPaths.DefaultInternalMetadataPath),
                            Path.GetFullPath(_applicationPaths.InternalMetadataPath),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        CopyDirectory(Path.Combine(_applicationPaths.DefaultInternalMetadataPath), Path.Combine("Data", "metadata-default"));
                    }
                }

                var manifestStream = await zipArchive.CreateEntry(ManifestEntryName).OpenAsync().ConfigureAwait(false);
                await using (manifestStream.ConfigureAwait(false))
                {
                    await JsonSerializer.SerializeAsync(manifestStream, manifest).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Backup created");
            return Map(manifest, backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup, removing {BackupPath}", backupPath);
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogWarning(innerEx, "Unable to remove failed backup");
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<BackupManifestDto?> GetBackupManifest(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            return null;
        }

        BackupManifest? manifest;
        try
        {
            manifest = await GetManifest(archivePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tried to load manifest from archive {Path} but failed", archivePath);
            return null;
        }

        if (manifest is null)
        {
            return null;
        }

        return Map(manifest, archivePath);
    }

    /// <inheritdoc/>
    public async Task<BackupManifestDto[]> EnumerateBackups()
    {
        if (!Directory.Exists(_applicationPaths.BackupPath))
        {
            return [];
        }

        var archives = Directory.EnumerateFiles(_applicationPaths.BackupPath, "*.zip");
        var manifests = new List<BackupManifestDto>();
        foreach (var item in archives)
        {
            try
            {
                var manifest = await GetManifest(item).ConfigureAwait(false);

                if (manifest is null)
                {
                    continue;
                }

                manifests.Add(Map(manifest, item));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tried to load manifest from archive {Path} but failed", item);
            }
        }

        return manifests.ToArray();
    }

    private static async ValueTask<BackupManifest?> GetManifest(string archivePath)
    {
        var archiveStream = File.OpenRead(archivePath);
        await using (archiveStream.ConfigureAwait(false))
        {
            using var zipStream = new ZipArchive(archiveStream, ZipArchiveMode.Read);
            var manifestEntry = zipStream.GetEntry(ManifestEntryName);
            if (manifestEntry is null)
            {
                return null;
            }

            var manifestStream = await manifestEntry.OpenAsync().ConfigureAwait(false);
            await using (manifestStream.ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false);
            }
        }
    }

    private static BackupManifestDto Map(BackupManifest manifest, string path)
    {
        return new BackupManifestDto()
        {
            BackupEngineVersion = manifest.BackupEngineVersion,
            DateCreated = manifest.DateCreated,
            ServerVersion = manifest.ServerVersion,
            Path = path,
            Options = Map(manifest.Options)
        };
    }

    private static BackupOptionsDto Map(BackupOptions options)
    {
        return new BackupOptionsDto()
        {
            Metadata = options.Metadata,
            Subtitles = options.Subtitles,
            Trickplay = options.Trickplay,
            Database = options.Database
        };
    }

    private static BackupOptions Map(BackupOptionsDto options)
    {
        return new BackupOptions()
        {
            Metadata = options.Metadata,
            Subtitles = options.Subtitles,
            Trickplay = options.Trickplay,
            Database = options.Database
        };
    }

    /// <summary>
    /// Windows is able to handle '/' as a path seperator in zip files
    /// but linux isn't able to handle '\' as a path seperator in zip files,
    /// So normalize to '/'.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path. </returns>
    private static string NormalizePathSeparator(string path)
        => path.Replace('\\', '/');
}
