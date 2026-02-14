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
    public BackupService(
        ILogger<BackupService> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost applicationHost,
        IServerApplicationPaths applicationPaths,
        IJellyfinDatabaseProvider jellyfinDatabaseProvider,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _dbProvider = dbProvider;
        _applicationHost = applicationHost;
        _applicationPaths = applicationPaths;
        _jellyfinDatabaseProvider = jellyfinDatabaseProvider;
        _hostApplicationLifetime = applicationLifetime;
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
                manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false);
            }

            if (manifest!.ServerVersion > _applicationHost.ApplicationVersion) // newer versions of Jellyfin should be able to load older versions as we have migrations.
            {
                throw new NotSupportedException($"The loaded archive '{archivePath}' is made for a newer version of Jellyfin ({manifest.ServerVersion}) and cannot be loaded in this version.");
            }

            if (!TestBackupVersionCompatibility(manifest.BackupEngineVersion))
            {
                throw new NotSupportedException($"The loaded archive '{archivePath}' is made for a newer version of Jellyfin ({manifest.ServerVersion}) and cannot be loaded in this version.");
            }

            void CopyDirectory(string source, string target)
            {
                var fullSourcePath = NormalizePathSeparator(Path.GetFullPath(source) + Path.DirectorySeparatorChar);
                var fullTargetRoot = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
                foreach (var item in zipArchive.Entries)
                {
                    var sourcePath = NormalizePathSeparator(Path.GetFullPath(item.FullName));
                    var targetPath = Path.GetFullPath(Path.Combine(target, Path.GetRelativePath(source, item.FullName)));

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

            CopyDirectory("Config", _applicationPaths.ConfigurationDirectoryPath);
            CopyDirectory("Data", _applicationPaths.DataPath);
            CopyDirectory("Root", _applicationPaths.RootFolderPath);

            if (manifest.Options.Database)
            {
                _logger.LogInformation("Begin restoring Database");
                var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
                await using (dbContext.ConfigureAwait(false))
                {
                    // restore migration history manually
                    var historyEntry = zipArchive.GetEntry(NormalizePathSeparator(Path.Combine("Database", $"{nameof(HistoryRow)}.json")));
                    if (historyEntry is null)
                    {
                        _logger.LogInformation("No backup of the history table in archive. This is required for Jellyfin operation");
                        throw new InvalidOperationException("Cannot restore backup that has no History data.");
                    }

                    HistoryRow[] historyEntries;
                    var historyArchive = await historyEntry.OpenAsync().ConfigureAwait(false);
                    await using (historyArchive.ConfigureAwait(false))
                    {
                        historyEntries = await JsonSerializer.DeserializeAsync<HistoryRow[]>(historyArchive).ConfigureAwait(false) ??
                            throw new InvalidOperationException("Cannot restore backup that has no History data.");
                    }

                    var historyRepository = dbContext.GetService<IHistoryRepository>();
                    await historyRepository.CreateIfNotExistsAsync().ConfigureAwait(false);

                    foreach (var item in await historyRepository.GetAppliedMigrationsAsync(CancellationToken.None).ConfigureAwait(false))
                    {
                        var insertScript = historyRepository.GetDeleteScript(item.MigrationId);
                        await dbContext.Database.ExecuteSqlRawAsync(insertScript).ConfigureAwait(false);
                    }

                    foreach (var item in historyEntries)
                    {
                        var insertScript = historyRepository.GetInsertScript(item);
                        await dbContext.Database.ExecuteSqlRawAsync(insertScript).ConfigureAwait(false);
                    }

                    dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                    var entityTypes = typeof(JellyfinDbContext).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                        .Where(e => e.PropertyType.IsAssignableTo(typeof(IQueryable)))
                        .Select(e => (Type: e, Set: e.GetValue(dbContext) as IQueryable))
                        .ToArray();

                    var tableNames = entityTypes.Select(f => dbContext.Model.FindEntityType(f.Type.PropertyType.GetGenericArguments()[0])!.GetSchemaQualifiedTableName()!);
                    _logger.LogInformation("Begin purging database");
                    await _jellyfinDatabaseProvider.PurgeDatabase(dbContext, tableNames).ConfigureAwait(false);
                    _logger.LogInformation("Database Purged");

                    foreach (var entityType in entityTypes)
                    {
                        _logger.LogInformation("Read backup of {Table}", entityType.Type.Name);

                        var zipEntry = zipArchive.GetEntry(NormalizePathSeparator(Path.Combine("Database", $"{entityType.Type.Name}.json")));
                        if (zipEntry is null)
                        {
                            _logger.LogInformation("No backup of expected table {Table} is present in backup, continuing anyway", entityType.Type.Name);
                            continue;
                        }

                        var zipEntryStream = await zipEntry.OpenAsync().ConfigureAwait(false);
                        await using (zipEntryStream.ConfigureAwait(false))
                        {
                            _logger.LogInformation("Restore backup of {Table}", entityType.Type.Name);
                            var records = 0;
                            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<JsonObject>(zipEntryStream, _serializerSettings).ConfigureAwait(false))
                            {
                                var entity = item.Deserialize(entityType.Type.PropertyType.GetGenericArguments()[0]);
                                if (entity is null)
                                {
                                    throw new InvalidOperationException($"Cannot deserialize entity '{item}'");
                                }

                                try
                                {
                                    records++;
                                    dbContext.Add(entity);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Could not store entity {Entity}, continuing anyway", item);
                                }
                            }

                            _logger.LogInformation("Prepared to restore {Number} entries for {Table}", records, entityType.Type.Name);
                        }
                    }

                    _logger.LogInformation("Try restore Database");
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    _logger.LogInformation("Restored database");
                }
            }

            _logger.LogInformation("Restored Jellyfin system from {Date}", manifest.DateCreated);
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

                    ICollection<(Type Type, string SourceName, Func<IAsyncEnumerable<object>> ValueFactory)> entityTypes =
                    [
                        .. typeof(JellyfinDbContext)
                            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                            .Where(e => e.PropertyType.IsAssignableTo(typeof(IQueryable)))
                            .Select(e => (Type: e.PropertyType, dbContext.Model.FindEntityType(e.PropertyType.GetGenericArguments()[0])!.GetSchemaQualifiedTableName()!, ValueFactory: new Func<IAsyncEnumerable<object>>(() => GetValues((IQueryable)e.GetValue(dbContext)!)))),
                        (Type: typeof(HistoryRow), SourceName: nameof(HistoryRow), ValueFactory: () => migrations.ToAsyncEnumerable())
                    ];
                    manifest.DatabaseTables = entityTypes.Select(e => e.Type.Name).ToArray();
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
                                    await foreach (var item in set.ConfigureAwait(false))
                                    {
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
