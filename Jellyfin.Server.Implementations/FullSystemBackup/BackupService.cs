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
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Backup;

/// <summary>
/// Contains methods for creating and restoring backups.
/// </summary>
public class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _applicationHost;
    private readonly IApplicationPaths _applicationPaths;
    private readonly IJellyfinDatabaseProvider _jellyfinDatabaseProvider;
    private static string _manifestEntryName = "manifest.json";
    private readonly JsonSerializerOptions _serializerSettings = new JsonSerializerOptions()
    {
        AllowTrailingCommas = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    private readonly Version _backupEngineVersion = Version.Parse("0.1.0");

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="dbProvider">A Database Factory.</param>
    /// <param name="applicationHost">The Application host.</param>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="jellyfinDatabaseProvider">The Jellyfin database Provider in use.</param>
    public BackupService(
        ILogger<BackupService> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost applicationHost,
        IApplicationPaths applicationPaths,
        IJellyfinDatabaseProvider jellyfinDatabaseProvider)
    {
        _logger = logger;
        _dbProvider = dbProvider;
        _applicationHost = applicationHost;
        _applicationPaths = applicationPaths;
        _jellyfinDatabaseProvider = jellyfinDatabaseProvider;
    }

    /// <inheritdoc/>
    public void ScheduleRestoreAndRestartServer(string archivePath)
    {
        _applicationHost.RestoreBackup = archivePath;
        _applicationHost.ShouldRestart = true;
        _applicationHost.NotifyPendingRestart();
    }

    /// <inheritdoc/>
    public async Task RestoreBackupAsync(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Requested backup file '{archivePath}' does not exist.");
        }

        using var fileStream = File.OpenRead(archivePath);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, false);
        var zipArchiveEntry = zipArchive.GetEntry(_manifestEntryName);

        if (zipArchiveEntry is null)
        {
            throw new NotSupportedException($"The loaded archive '{archivePath}' does not appear to be a jellyfin backup as its missing the '{_manifestEntryName}'.");
        }

        using var manifestStream = zipArchiveEntry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false);

        if (manifest!.JellyfinVersion > _applicationHost.ApplicationVersion) // newer versions of jellyfin should be able to load older versions as we have migrations.
        {
            throw new NotSupportedException($"The loaded archive '{archivePath}' is made for a newer version of jellyfin ({manifest.JellyfinVersion}) and cannot be loaded in this version.");
        }

        if (!TestBackupVersionCompatibility(manifest.BackupEngineVersion))
        {
            throw new NotSupportedException($"The loaded archive '{archivePath}' is made for a newer version of jellyfin ({manifest.JellyfinVersion}) and cannot be loaded in this version.");
        }

        static async Task CopyOverride(ZipArchiveEntry item, string targetPath)
        {
            using var targetStream = File.Create(targetPath);
            using var sourceStream = item.Open();
            await sourceStream.CopyToAsync(targetStream).ConfigureAwait(false);
        }

        async Task CopyDirectory(string source, string target)
        {
            if (!Directory.Exists(source))
            {
                Directory.CreateDirectory(source);
            }

            var configFiles = zipArchive.Entries.Where(e => e.FullName.StartsWith(target, StringComparison.InvariantCultureIgnoreCase));

            foreach (var item in configFiles)
            {
                var targetPath = Path.Combine(source, item.FullName[target.Length..].Trim('/'));
                _logger.LogInformation("Restore and override {File}", targetPath);
                await CopyOverride(item, targetPath).ConfigureAwait(false);
            }
        }

        await CopyDirectory(_applicationPaths.ConfigurationDirectoryPath, "Config/").ConfigureAwait(false);
        await CopyDirectory(_applicationPaths.DataPath, "Data/").ConfigureAwait(false);

        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var entityTypes = typeof(JellyfinDbContext).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(e => e.PropertyType.IsAssignableTo(typeof(IQueryable)))
                .Select(e => (Type: e, Set: e.GetValue(dbContext) as IQueryable))
                .ToArray();

            var tableNames = entityTypes.Select(f => dbContext.Model.FindEntityType(f.Type.PropertyType.GetGenericArguments()[0])!.GetSchemaQualifiedTableName()!);
            await _jellyfinDatabaseProvider.PurgeDatabase(dbContext, tableNames).ConfigureAwait(false);

            foreach (var entityType in entityTypes)
            {
                _logger.LogInformation("Read backup of {Table}", entityType.Type.Name);

                var zipEntry = zipArchive.GetEntry($"Database\\{entityType.Type.Name}.json");
                if (zipEntry is null)
                {
                    _logger.LogInformation("No backup of expected table {Table} is present in backup. Continue anyway.", entityType.Type.Name);
                    continue;
                }

                using var zipEntryStream = zipEntry.Open();
                {
                    _logger.LogInformation("Restore backup of {Table}", entityType.Type.Name);
                    var records = 0;
                    await foreach (JsonObject item in JsonSerializer.DeserializeAsyncEnumerable<JsonObject>(zipEntryStream, _serializerSettings).ConfigureAwait(false)!)
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
                            _logger.LogError(ex, "Could not store entity {Entity} continue anyway.", item);
                        }
                    }

                    _logger.LogInformation("Prepared to restore {Number} entries for {Table}", records, entityType.Type.Name);
                }
            }

            _logger.LogInformation("Try restore Database");
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Restored database.");
        }

        _logger.LogInformation("Restored Jellyfin system from {Date}.", manifest.DateOfCreation);
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
    public async Task<BackupManifestDto> CreateBackupAsync()
    {
        var manifest = new BackupManifest()
        {
            DateOfCreation = DateTime.UtcNow,
            JellyfinVersion = _applicationHost.ApplicationVersion,
            DatabaseTables = null!,
            BackupEngineVersion = _backupEngineVersion,
        };

        await _jellyfinDatabaseProvider.RunScheduledOptimisation(CancellationToken.None).ConfigureAwait(false);

        var backupFolder = Path.Combine(_applicationPaths.BackupPath);

        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        var backupPath = Path.Combine(backupFolder, $"jfBackup-{DateTime.Now:yyyyMMddHHmmss}.zip");
        _logger.LogInformation("Attempt to create a new backup at {BackupPath}", backupPath);
        using var fileStream = File.OpenWrite(backupPath);
        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
        {
            _logger.LogInformation("Start backup process.");
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var entityTypes = typeof(JellyfinDbContext).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(e => e.PropertyType.IsAssignableTo(typeof(IQueryable)))
                    .Select(e => (Type: e, Set: e.GetValue(dbContext) as IQueryable))
                    .ToArray();
                manifest.DatabaseTables = entityTypes.Select(e => e.Type.Name).ToArray();
                var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);

                await using (transaction.ConfigureAwait(false))
                {
                    _logger.LogInformation("Begin Database backup");
                    static IAsyncEnumerable<object> GetValues(IQueryable dbSet, Type type)
                    {
                        var method = dbSet.GetType().GetMethod(nameof(DbSet<object>.AsAsyncEnumerable))!;
                        var enumerable = method.Invoke(dbSet, null)!;
                        return (IAsyncEnumerable<object>)enumerable;
                    }

                    foreach (var entityType in entityTypes)
                    {
                        _logger.LogInformation("Begin backup of entity {Table}", entityType.Type.Name);
                        var zipEntry = zipArchive.CreateEntry($"Database\\{entityType.Type.Name}.json");
                        var entities = 0;
                        using var zipEntryStream = zipEntry.Open();
                        {
                            using var jsonSerializer = new Utf8JsonWriter(zipEntryStream);
                            jsonSerializer.WriteStartArray();

                            var set = GetValues(entityType.Set!, entityType.Type.PropertyType).ConfigureAwait(false);
                            await foreach (var item in set)
                            {
                                entities++;
                                try
                                {
                                    JsonSerializer.SerializeToDocument(item, _serializerSettings).WriteTo(jsonSerializer);
                                }
                                catch (System.Exception ex)
                                {
                                    _logger.LogError(ex, "Could not load entity {Entity}", item);
                                    throw;
                                }
                            }

                            jsonSerializer.WriteEndArray();
                        }

                        _logger.LogInformation("backup of entity {Table} with {Number} created", entityType.Type.Name, entities);
                    }
                }
            }

            _logger.LogInformation("Backup of folder {Table}", _applicationPaths.ConfigurationDirectoryPath);
            foreach (var item in Directory.EnumerateFiles(_applicationPaths.ConfigurationDirectoryPath, "*.xml", SearchOption.TopDirectoryOnly)
              .Union(Directory.EnumerateFiles(_applicationPaths.ConfigurationDirectoryPath, "*.json", SearchOption.TopDirectoryOnly)))
            {
                zipArchive.CreateEntryFromFile(item, Path.Combine("Config", Path.GetFileName(item)));
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
                    zipArchive.CreateEntryFromFile(item, Path.Combine(target, item[..source.Length].Trim('\\')));
                }
            }

            CopyDirectory(Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "users"), Path.Combine("Config", "users"));
            CopyDirectory(Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "ScheduledTasks"), Path.Combine("Config", "ScheduledTasks"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "collections"), Path.Combine("Data", "collections"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "playlists"), Path.Combine("Data", "playlists"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "ScheduledTasks"), Path.Combine("Data", "ScheduledTasks"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "subtitles"), Path.Combine("Data", "subtitles"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "trickplay"), Path.Combine("Data", "trickplay"));

            using var manifestEntry = zipArchive.CreateEntry(_manifestEntryName).Open();
            await JsonSerializer.SerializeAsync(manifestEntry, manifest).ConfigureAwait(false);
        }

        _logger.LogInformation("Backup created");
        return Map(manifest, backupPath);
    }

    /// <inheritdoc/>
    public async Task<BackupManifestDto[]> EnumerateBackups()
    {
        var archives = Directory.EnumerateFiles(_applicationPaths.BackupPath, "*.zip");
        var manifests = new List<BackupManifestDto>();
        foreach (var item in archives)
        {
            try
            {
                using var archiveStream = File.OpenRead(item);
                using var zipStream = new ZipArchive(archiveStream, ZipArchiveMode.Read);
                var manifestEntry = zipStream.GetEntry(_manifestEntryName);
                if (manifestEntry is null)
                {
                    continue;
                }

                using var manifestStream = manifestEntry.Open();
                var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false);

                if (manifest is null)
                {
                    continue;
                }

                manifests.Add(Map(manifest, item));
            }
            catch
            {
                continue;
            }
        }

        return manifests.ToArray();
    }

    private static BackupManifestDto Map(BackupManifest manifest, string path)
    {
        return new BackupManifestDto()
        {
            BackupEngineVersion = manifest.BackupEngineVersion,
            DateOfCreation = manifest.DateOfCreation,
            JellyfinVersion = manifest.JellyfinVersion,
            Path = path
        };
    }
}
