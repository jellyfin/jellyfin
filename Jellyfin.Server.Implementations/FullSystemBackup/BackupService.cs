using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Backup;

/// <summary>
/// Contains methods for creating and restoring backups.
/// </summary>
public class BackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _applicationHost;
    private readonly IApplicationPaths _applicationPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="dbProvider">A Database Factory.</param>
    /// <param name="applicationHost">The Application host.</param>
    /// <param name="applicationPaths">The application paths.</param>
    public BackupService(
        ILogger<BackupService> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost applicationHost,
        IApplicationPaths applicationPaths)
    {
        _logger = logger;
        _dbProvider = dbProvider;
        _applicationHost = applicationHost;
        _applicationPaths = applicationPaths;
    }

    /// <summary>
    /// Creates a new Backup zip file containing the current state of the application.
    /// </summary>
    /// <returns>A task.</returns>
    public async Task CreateBackupAsync()
    {
        var manifest = new BackupManifest()
        {
            DateOfCreation = DateTime.UtcNow,
            Version = _applicationHost.ApplicationVersion,
            DatabaseTables = null!
        };

        using var fileStream = File.OpenWrite(Path.Combine(_applicationPaths.TempDirectory, $"jfBackup{DateTime.Now:DDMMyyyyhhss}.zip"));
        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
        {
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
                    static IAsyncEnumerable<object> GetValues(IQueryable dbSet, Type type)
                    {
                        var method = dbSet.GetType().GetMethod(nameof(DbSet<object>.AsAsyncEnumerable))!;
                        var enumerable = method.Invoke(dbSet, null)!;
                        return (IAsyncEnumerable<object>)enumerable;
                    }

                    var serializerSettings = new JsonSerializerOptions()
                    {
                        AllowTrailingCommas = true,
                        ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    };

                    foreach (var entityType in entityTypes)
                    {
                        var zipEntry = zipArchive.CreateEntry($"Database\\{entityType.Type.Name}.json");
                        using var zipEntryStream = zipEntry.Open();
                        {
                            using var jsonSerializer = new Utf8JsonWriter(zipEntryStream);
                            jsonSerializer.WriteStartArray();

                            var set = GetValues(entityType.Set!, entityType.Type.PropertyType).ConfigureAwait(false);
                            await foreach (var item in set)
                            {
                                try
                                {
                                    JsonSerializer.SerializeToDocument(item, serializerSettings).WriteTo(jsonSerializer);
                                }
                                catch (System.Exception ex)
                                {
                                    _logger.LogError(ex, "Could not load entity {Entity}", item);
                                    throw;
                                }
                            }

                            jsonSerializer.WriteEndArray();
                        }
                    }
                }
            }

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

                foreach (var item in Directory.EnumerateFiles(source, filter, SearchOption.AllDirectories))
                {
                    zipArchive.CreateEntryFromFile(item, Path.Combine(target, item[..source.Length].Trim('\\')));
                }
            }

            CopyDirectory(Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "users"), Path.Combine("Config", "users"));
            CopyDirectory(Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "users"), Path.Combine("Config", "ScheduledTasks"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "collections"), Path.Combine("Data", "collections"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "playlists"), Path.Combine("Data", "playlists"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "ScheduledTasks"), Path.Combine("Data", "ScheduledTasks"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "subtitles"), Path.Combine("Data", "subtitles"));
            CopyDirectory(Path.Combine(_applicationPaths.DataPath, "trickplay"), Path.Combine("Data", "trickplay"));

            using var manifestEntry = zipArchive.CreateEntry("manifest.json").Open();
            await JsonSerializer.SerializeAsync(manifestEntry, manifest).ConfigureAwait(false);
        }
    }
}
