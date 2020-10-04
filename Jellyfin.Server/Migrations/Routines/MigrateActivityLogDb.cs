#pragma warning disable CS1591

using System;
using System.IO;
using Emby.Server.Implementations.Data;
using Jellyfin.Data;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    public class MigrateActivityLogDb : IMigrationRoutine
    {
        public Guid Id => Guid.Parse("{3793eb59-bc8c-456c-8b9f-bd5a62a42978}");

        public string Name => "MigrateActivityLogDatabase";

        public void Perform(CoreAppHost host, ILogger logger)
        {
            var dataPath = host.ServerConfigurationManager.ApplicationPaths.DataPath;
            using (var connection = SQLite3.Open(
                Path.Combine(dataPath, "activitylog.db"),
                ConnectionFlags.ReadOnly,
                null))
            {
                logger.LogInformation("This may take a while, do not stop Jellyfin.");
                using var dbContext = host.DatabaseProvider.GetConnection();

                var queryResult = connection.Query("SELECT * FROM ActivityLog ORDER BY Id");

                // Make sure that the database is empty in case of failed migration due to power outages, etc.
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();

                foreach (var entry in queryResult)
                {
                    var newEntry = new ActivityLog(
                        entry[1].ToString(),
                        entry[4].ToString(),
                        entry[6].SQLiteType == SQLiteType.Null ? Guid.Empty : Guid.Parse(entry[6].ToString()),
                        entry[7].ReadDateTime(),
                        ParseLogLevel(entry[8].ToString()));

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

                    dbContext.ActivityLogs.Add(newEntry);
                    dbContext.SaveChanges();
                }
            }

            try
            {
                File.Move(Path.Combine(dataPath, "activitylog.db"), Path.Combine(dataPath, "activitylog.db.old"));
            }
            catch (IOException e)
            {
                logger.LogError("Error renaming file: ", e);
            }
        }

        private LogLevel ParseLogLevel(string entry)
        {
            if (string.Equals(entry, "Debug", StringComparison.OrdinalIgnoreCase))
            {
                return LogLevel.Debug;
            }

            if (string.Equals(entry, "Information", StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry, "Info", StringComparison.OrdinalIgnoreCase))
            {
                return LogLevel.Information;
            }

            if (string.Equals(entry, "Warning", StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry, "Warn", StringComparison.OrdinalIgnoreCase))
            {
                return LogLevel.Warning;
            }

            if (string.Equals(entry, "Error", StringComparison.OrdinalIgnoreCase))
            {
                return LogLevel.Error;
            }

            return LogLevel.Trace;
        }
    }
}
