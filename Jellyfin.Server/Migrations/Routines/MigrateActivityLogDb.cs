#pragma warning disable CS1591

using System;
using System.IO;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities;
using Jellyfin.Server.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    public class MigrateActivityLogDb : IMigrationRoutine
    {
        private const string DbFilename = "activitylog.db";

        public Guid Id => Guid.Parse("3793eb59-bc8c-456c-8b9f-bd5a62a42978");

        public string Name => "MigrateActivityLogDatabase";

        public void Perform(CoreAppHost host, ILogger logger)
        {
            var dataPath = host.ServerConfigurationManager.ApplicationPaths.DataPath;
            using (var connection = SQLite3.Open(
                Path.Combine(dataPath, DbFilename),
                ConnectionFlags.ReadOnly,
                null))
            {
                logger.LogInformation("Migrating the database may take a while, do not stop Jellyfin.");
                using var dbContext = host.ServiceProvider.GetService<JellyfinDb>();

                var queryResult = connection.Query("SELECT * FROM ActivityLog ORDER BY Id ASC");

                // Make sure that the database is empty in case of failed migration due to power outages, etc.
                dbContext.ActivityLogs.RemoveRange(dbContext.ActivityLogs);
                dbContext.SaveChanges();
                // Reset the autoincrement counter
                dbContext.Database.ExecuteSqlRaw("UPDATE sqlite_sequence SET seq = 0 WHERE name = 'ActivityLog';");
                dbContext.SaveChanges();

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
                File.Move(Path.Combine(dataPath, DbFilename), Path.Combine(dataPath, DbFilename + ".old"));
            }
            catch (IOException e)
            {
                logger.LogError(e, "Error renaming legacy activity log database to 'activitylog.db.old'");
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
