using System;
using System.IO;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// The migration routine for migrating the Authentication database to jellyfin.db.
    /// </summary>
    public class MigrateAuthenticationDb : IMigrationRoutine
    {
        private const string AuthDbFilename = "authentication.db";
        private const string DbFilename = "jellyfin.db";

        private readonly ILogger<MigrateAuthenticationDb> _logger;
        private readonly IServerApplicationPaths _paths;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrateAuthenticationDb"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="paths">The server application paths.</param>
        public MigrateAuthenticationDb(
            ILogger<MigrateAuthenticationDb> logger,
            IServerApplicationPaths paths)
        {
            _logger = logger;
            _paths = paths;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("01CA39A0-3E2A-463F-9B95-593041BB157E");

        /// <inheritdoc/>
        public string Name => "MigrateAuthenticationDatabase";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

        /// <inheritdoc/>
        public void Perform()
        {
            var dataPath = _paths.DataPath;
            _logger.LogInformation("Migrating authentication.db to jellyfin.db. This might take a while, please do not stop Jellyfin.");

            using (var connection = SQLite3.Open(Path.Combine(dataPath, DbFilename)))
            {
                connection.Execute("ATTACH DATABASE '" + Path.Combine(dataPath, AuthDbFilename) + "' AS auth;");
                // Authentication Repository is initialised first, so we remove the tables it initialised first.
                connection.Execute("DROP TABLE IF EXISTS main.Devices;");
                connection.Execute("DROP TABLE IF EXISTS main.Tokens;");
                connection.Execute("CREATE TABLE main.Devices AS SELECT * FROM auth.devices;");
                connection.Execute("CREATE TABLE main.Tokens AS SELECT * FROM auth.tokens;");
                connection.Execute("CREATE INDEX IF NOT EXISTS IX_Devices1 on Devices(Id);");
                connection.Execute("CREATE INDEX IF NOT EXISTS IX_Tokens3 on Tokens(AccessToken, DateLastActivity);");
                connection.Execute("CREATE INDEX IF NOT EXISTS IX_Tokens4 on Tokens (Id, DateLastActivity);");
            }
            

            try
            {
                File.Move(Path.Combine(dataPath, AuthDbFilename), Path.Combine(dataPath, AuthDbFilename + ".old"));

                var journalPath = Path.Combine(dataPath, AuthDbFilename + "-journal");
                if (File.Exists(journalPath))
                {
                    File.Move(journalPath, Path.Combine(dataPath, AuthDbFilename + ".old-journal"));
                }
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Error renaming legacy authentication database to 'authentication.db.old'");
            }
        }
    }
}
