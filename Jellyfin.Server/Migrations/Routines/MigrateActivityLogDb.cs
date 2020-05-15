using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// The migration routine for migrating the activity log database to EF Core.
    /// </summary>
    public class MigrateActivityLogDb : IMigrationRoutine
    {
        private const string DbFilename = "activitylog.db";

        private readonly ILogger<MigrateActivityLogDb> _logger;
        private readonly JellyfinDbProvider _provider;
        private readonly IServerApplicationPaths _paths;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrateActivityLogDb"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="paths">The server application paths.</param>
        /// <param name="provider">The database provider.</param>
        public MigrateActivityLogDb(ILogger<MigrateActivityLogDb> logger, IServerApplicationPaths paths, JellyfinDbProvider provider)
        {
            _logger = logger;
            _provider = provider;
            _paths = paths;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("3793eb59-bc8c-456c-8b9f-bd5a62a42978");

        /// <inheritdoc/>
        public string Name => "MigrateActivityLogDatabase";

        /// <inheritdoc/>
        public void Perform()
        {
            var logLevelDictionary = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
            {
                { "None", LogLevel.None },
                { "Trace", LogLevel.Trace },
                { "Debug", LogLevel.Debug },
                { "Information", LogLevel.Information },
                { "Info", LogLevel.Information },
                { "Warn", LogLevel.Warning },
                { "Warning", LogLevel.Warning },
                { "Error", LogLevel.Error },
                { "Critical", LogLevel.Critical }
            };

            var dataPath = _paths.DataPath;
            using (var connection = SQLite3.Open(
                Path.Combine(dataPath, DbFilename),
                ConnectionFlags.ReadOnly,
                null))
            {
                _logger.LogWarning("Migrating the activity database may take a while, do not stop Jellyfin.");
                using var dbContext = _provider.CreateContext();

                var queryResult = connection.Query("SELECT * FROM ActivityLog ORDER BY Id ASC");

                // Make sure that the database is empty in case of failed migration due to power outages, etc.
                dbContext.ActivityLogs.RemoveRange(dbContext.ActivityLogs);
                dbContext.SaveChanges();
                // Reset the autoincrement counter
                dbContext.Database.ExecuteSqlRaw("UPDATE sqlite_sequence SET seq = 0 WHERE name = 'ActivityLog';");
                dbContext.SaveChanges();

                var newEntries = queryResult.Select(entry =>
                {
                    if (!logLevelDictionary.TryGetValue(entry[8].ToString(), out var severity))
                    {
                        severity = LogLevel.Trace;
                    }

                    var newEntry = new ActivityLog(
                        entry[1].ToString(),
                        entry[4].ToString(),
                        entry[6].SQLiteType == SQLiteType.Null ? Guid.Empty : Guid.Parse(entry[6].ToString()))
                    {
                        DateCreated = entry[7].ReadDateTime(),
                        LogSeverity = severity
                    };

                    if (entry[2].SQLiteType != SQLiteType.Null)
                    {
                        newEntry.Overview = entry[2].ToString();
                    }

                    if (entry[3].SQLiteType != SQLiteType.Null)
                    {
                        newEntry.ShortOverview = entry[3].ToString();
                    }

                    if (entry[5].SQLiteType != SQLiteType.Null)
                    {
                        newEntry.ItemId = entry[5].ToString();
                    }

                    return newEntry;
                });

                dbContext.ActivityLogs.AddRange(newEntries);
                dbContext.SaveChanges();
            }

            try
            {
                File.Move(Path.Combine(dataPath, DbFilename), Path.Combine(dataPath, DbFilename + ".old"));

                var journalPath = Path.Combine(dataPath, DbFilename + "-journal");
                if (File.Exists(journalPath))
                {
                    File.Move(journalPath, Path.Combine(dataPath, DbFilename + ".old-journal"));
                }
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Error renaming legacy activity log database to 'activitylog.db.old'");
            }
        }
    }
}
