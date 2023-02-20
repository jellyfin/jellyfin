using System;
using System.Globalization;
using System.IO;

using Emby.Server.Implementations;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.PreStartupRoutines
{
    /// <summary>
    /// Migrate rating levels to new rating level system.
    /// </summary>
    internal class MigrateRatingLevels : IMigrationRoutine
    {
        private const string DbFilename = "library.db";
        private readonly ILogger<MigrateRatingLevels> _logger;
        private readonly IServerApplicationPaths _applicationPaths;

        public MigrateRatingLevels(ServerApplicationPaths applicationPaths, ILoggerFactory loggerFactory)
        {
            _applicationPaths = applicationPaths;
            _logger = loggerFactory.CreateLogger<MigrateRatingLevels>();
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{67445D54-B895-4B24-9F4C-35CE0690EA07}");

        /// <inheritdoc/>
        public string Name => "MigrateRatingLevels";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

        /// <inheritdoc/>
        public void Perform()
        {
            var dataPath = _applicationPaths.DataPath;
            var dbPath = Path.Combine(dataPath, DbFilename);
            using (var connection = SQLite3.Open(
                dbPath,
                ConnectionFlags.ReadWrite,
                null))
            {
                // Back up the database before deleting any entries
                for (int i = 1; ; i++)
                {
                    var bakPath = string.Format(CultureInfo.InvariantCulture, "{0}.bak{1}", dbPath, i);
                    if (!File.Exists(bakPath))
                    {
                        try
                        {
                            File.Copy(dbPath, bakPath);
                            _logger.LogInformation("Library database backed up to {BackupPath}", bakPath);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Cannot make a backup of {Library} at path {BackupPath}", DbFilename, bakPath);
                            throw;
                        }
                    }
                }

                // Migrate parental rating levels to new schema
                _logger.LogInformation("Migrating parental rating levels.");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = NULL WHERE OfficialRating = 'NR'");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = NULL WHERE InheritedParentalRatingValue = ''");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = NULL WHERE InheritedParentalRatingValue = 0");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 1000 WHERE InheritedParentalRatingValue = 100");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 1000 WHERE InheritedParentalRatingValue = 15");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 18 WHERE InheritedParentalRatingValue = 10");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 18 WHERE InheritedParentalRatingValue = 9");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 16 WHERE InheritedParentalRatingValue = 8");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 12 WHERE InheritedParentalRatingValue = 7");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 12 WHERE InheritedParentalRatingValue = 6");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 12 WHERE InheritedParentalRatingValue = 5");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 7 WHERE InheritedParentalRatingValue = 4");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 6 WHERE InheritedParentalRatingValue = 3");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 6 WHERE InheritedParentalRatingValue = 2");
                connection.Execute("UPDATE TypedBaseItems SET InheritedParentalRatingValue = 0 WHERE InheritedParentalRatingValue = 1");
            }
        }
    }
}
