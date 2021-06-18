using System;
using System.IO;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// A migration that moves data from the authentication database into the new schema.
    /// </summary>
    public class MigrateAuthenticationDb : IMigrationRoutine
    {
        private const string DbFilename = "authentication.db";

        private readonly ILogger<MigrateAuthenticationDb> _logger;
        private readonly JellyfinDbProvider _dbProvider;
        private readonly IServerApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrateAuthenticationDb"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="dbProvider">The database provider.</param>
        /// <param name="appPaths">The server application paths.</param>
        public MigrateAuthenticationDb(ILogger<MigrateAuthenticationDb> logger, JellyfinDbProvider dbProvider, IServerApplicationPaths appPaths)
        {
            _logger = logger;
            _dbProvider = dbProvider;
            _appPaths = appPaths;
        }

        /// <inheritdoc />
        public Guid Id => Guid.Parse("5BD72F41-E6F3-4F60-90AA-09869ABE0E22");

        /// <inheritdoc />
        public string Name => "MigrateAuthenticationDatabase";

        /// <inheritdoc />
        public bool PerformOnNewInstall => false;

        /// <inheritdoc />
        public void Perform()
        {
            var dataPath = _appPaths.DataPath;
            using (var connection = SQLite3.Open(
                Path.Combine(dataPath, DbFilename),
                ConnectionFlags.ReadOnly,
                null))
            {
                using var dbContext = _dbProvider.CreateContext();

                var queryResult = connection.Query("SELECT * FROM Tokens");

                foreach (var row in queryResult)
                {
                    if (row[6].IsDbNull())
                    {
                        dbContext.ApiKeys.Add(new ApiKey(row[3].ToString())
                        {
                            AccessToken = row[1].ToString(),
                            DateCreated = row[9].ToDateTime(),
                            DateLastActivity = row[10].ToDateTime()
                        });
                    }
                    else
                    {
                        dbContext.Devices.Add(new Device(
                            row[6].ToGuid(),
                            row[3].ToString(),
                            row[4].ToString(),
                            row[5].ToString(),
                            row[2].ToString())
                        {
                            AccessToken = row[1].ToString(),
                            IsActive = row[8].ToBool(),
                            DateCreated = row[9].ToDateTime(),
                            DateLastActivity = row[10].ToDateTime()
                        });
                    }
                }

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
                _logger.LogError(e, "Error renaming legacy activity log database to 'authentication.db.old'");
            }
        }
    }
}
