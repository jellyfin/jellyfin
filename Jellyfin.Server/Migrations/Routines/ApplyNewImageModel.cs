using System;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Data.Entities.Libraries;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// The migration routine for migrating the user database to EF Core.
    /// </summary>
    public class ApplyNewImageModel : IMigrationRoutine
    {
        private readonly ILogger<ApplyNewImageModel> _logger;
        private readonly IServerApplicationPaths _paths;
        private readonly JellyfinDbProvider _provider;
        private readonly MyXmlSerializer _xmlSerializer;

        private const string DbFilename = "jellyfin.db";
        private string dataPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyNewImageModel"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="paths">The server application paths.</param>
        /// <param name="provider">The database provider.</param>
        /// <param name="xmlSerializer">The xml serializer.</param>
        public ApplyNewImageModel(
            ILogger<ApplyNewImageModel> logger,
            IServerApplicationPaths paths,
            JellyfinDbProvider provider,
            MyXmlSerializer xmlSerializer)
        {
            _logger = logger;
            _paths = paths;
            _provider = provider;
            _xmlSerializer = xmlSerializer;
            dataPath = _paths.DataPath;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("107BD854-01E5-4EB3-A187-33ABF550B245");

        /// <inheritdoc/>
        public string Name => "ApplyNewImageModel";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => true;

        private void DropTable()
        {
            _logger.LogInformation("Dropping ImageInfos table...");
            using (var connection = SQLite3.Open(Path.Combine(dataPath, DbFilename), ConnectionFlags.ReadWrite, null))
            {
                connection.Execute("DROP TABLE ImageInfos");
            }
        }

        private void MoveToNewTable()
        {
            var dbContext = _provider.CreateContext();
            using (var connection = SQLite3.Open(Path.Combine(dataPath, DbFilename), ConnectionFlags.ReadOnly, null))
            {
                var queryResult = connection.Query("SELECT * FROM ImageInfos");

                foreach (var row in queryResult)
                {
                    var userId = new Guid(row[3].ToString());
                    var user = dbContext.Users.Single(u => u.Id == userId);
                    user.ProfileImage = new Image(row[2].ToString(), row[1].ReadDateTime());
                    _logger.LogInformation("User '" + user.Username + "' migrated to new image model successfully");
                }
            }

            dbContext.SaveChanges();
        }

        /// <inheritdoc/>
        public void Perform()
        {
            _logger.LogInformation("Applying the new EFCore Image model migration. This can take a while, do not stop Jellyfin");
            bool runMigration = true;
            using (var connection = SQLite3.Open(Path.Combine(dataPath, DbFilename), ConnectionFlags.ReadOnly, null))
            {
                var tableExists = connection.Query("SELECT COUNT(*) FROM ImageInfos");
                foreach (var row in tableExists)
                {
                    if (row[0].ToInt() == 0)
                    {
                        _logger.LogInformation("There is no information in ImageInfos table, skipping this migration and dropping ImageInfos");
                        runMigration = false;
                    }
                }
            }

            if (runMigration)
            {
                MoveToNewTable();
            }

            DropTable();
        }
    }
}
