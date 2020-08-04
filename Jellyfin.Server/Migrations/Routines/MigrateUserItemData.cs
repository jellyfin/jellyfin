using System;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// The migration routine for migrating user item data to EF Core.
    /// </summary>
    public class MigrateUserItemData : IMigrationRoutine
    {
        private const string DbFilename = "library.db";

        private readonly JellyfinDbProvider _dbProvider;
        private readonly IServerApplicationPaths _paths;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrateUserItemData"/> class.
        /// </summary>
        /// <param name="provider">The db provider.</param>
        /// <param name="paths">The server application paths.</param>
        public MigrateUserItemData(JellyfinDbProvider provider, IServerApplicationPaths paths)
        {
            _dbProvider = provider;
            _paths = paths;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("69CF729C-E956-4E68-B3BD-1E1129A87F3E");

        /// <inheritdoc/>
        public string Name => "MigrateUserItemData";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

        /// <inheritdoc/>
        public void Perform()
        {
            var dbFilePath = Path.Combine(_paths.DataPath, DbFilename);
            using var connection = SQLite3.Open(dbFilePath, ConnectionFlags.ReadOnly, null);
            using var dbContext = _dbProvider.CreateContext();

            var idDict = dbContext.Users.ToDictionary(user => user.InternalId, user => user.Id);

            var result = connection.Query("SELECT * FROM UserDatas");
            foreach (var row in result)
            {
                if (!Guid.TryParse(row[0].ToString(), out var guid))
                {
                    // Poetry
                    continue;
                }

                var userItemData = new UserItemData
                {
                    UserId = idDict[row[1].ToInt64()],
                    ItemId = guid,
                    Rating = row[2].SQLiteType == SQLiteType.Null ? (float?)null : row[2].ToFloat(),
                    IsPlayed = row[3].ToBool(),
                    PlayCount = row[4].ToInt(),
                    IsFavorite = row[5].ToBool(),
                    PlaybackPositionTicks = row[6].ToInt64(),
                    LastPlayedDate = row[7].TryReadDateTime(),
                    VideoStreamIndex = 1,
                    AudioStreamIndex = row[8].SQLiteType == SQLiteType.Null ? (int?)null : row[8].ToInt(),
                    SubtitleStreamIndex = row[9].SQLiteType == SQLiteType.Null ? (int?)null : row[9].ToInt()
                };

                dbContext.UserItemData.Add(userItemData);
            }

            dbContext.SaveChanges();
        }
    }
}
