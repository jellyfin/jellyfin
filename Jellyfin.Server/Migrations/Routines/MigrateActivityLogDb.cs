using System;
using System.Collections.Generic;
using System.IO;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// The migration routine for migrating the activity log database to EF Core.
    /// </summary>
    public class MigrateActivityLogDb : IMigrationRoutine
    {
        private const string DbFilename = "activitylog.db";

        private readonly ILogger<MigrateActivityLogDb> _logger;
        private readonly IDbContextFactory<JellyfinDbContext> _provider;
        private readonly IServerApplicationPaths _paths;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrateActivityLogDb"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="paths">The server application paths.</param>
        /// <param name="provider">The database provider.</param>
        public MigrateActivityLogDb(ILogger<MigrateActivityLogDb> logger, IServerApplicationPaths paths, IDbContextFactory<JellyfinDbContext> provider)
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
        public bool PerformOnNewInstall => false;

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
            using (var connection = new SqliteConnection($"Filename={Path.Combine(dataPath, DbFilename)}"))
            {
                connection.Open();

                using var userDbConnection = new SqliteConnection($"Filename={Path.Combine(dataPath, "users.db")}");
                userDbConnection.Open();
                _logger.LogWarning("Migrating the activity database may take a while, do not stop Jellyfin.");
                using var dbContext = _provider.CreateDbContext();

                // Make sure that the database is empty in case of failed migration due to power outages, etc.
                dbContext.ActivityLogs.RemoveRange(dbContext.ActivityLogs);
                dbContext.SaveChanges();
                // Reset the autoincrement counter
                dbContext.Database.ExecuteSqlRaw("UPDATE sqlite_sequence SET seq = 0 WHERE name = 'ActivityLog';");
                dbContext.SaveChanges();

                var newEntries = new List<ActivityLog>();

                var queryResult = connection.Query("SELECT * FROM ActivityLog ORDER BY Id");

                foreach (var entry in queryResult)
                {
                    if (!logLevelDictionary.TryGetValue(entry.GetString(8), out var severity))
                    {
                        severity = LogLevel.Trace;
                    }

                    var guid = Guid.Empty;
                    if (!entry.IsDBNull(6) && !entry.TryGetGuid(6, out guid))
                    {
                        var id = entry.GetString(6);
                        // This is not a valid Guid, see if it is an internal ID from an old Emby schema
                        _logger.LogWarning("Invalid Guid in UserId column: {Guid}", id);

                        using var statement = userDbConnection.PrepareStatement("SELECT guid FROM LocalUsersv2 WHERE Id=@Id");
                        statement.TryBind("@Id", id);

                        using var reader = statement.ExecuteReader();
                        if (reader.HasRows && reader.Read() && reader.TryGetGuid(0, out guid))
                        {
                            // Successfully parsed a Guid from the user table.
                            break;
                        }
                    }

                    var newEntry = new ActivityLog(entry.GetString(1), entry.GetString(4), guid)
                    {
                        DateCreated = entry.GetDateTime(7),
                        LogSeverity = severity
                    };

                    if (entry.TryGetString(2, out var result))
                    {
                        newEntry.Overview = result;
                    }

                    if (entry.TryGetString(3, out result))
                    {
                        newEntry.ShortOverview = result;
                    }

                    if (entry.TryGetString(5, out result))
                    {
                        newEntry.ItemId = result;
                    }

                    newEntries.Add(newEntry);
                }

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
