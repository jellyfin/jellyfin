#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

[JellyfinMigration("2025-06-18T01:00:00", nameof(MigrateLibraryUserData))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class MigrateLibraryUserData : IAsyncMigrationRoutine
{
    private const string DbFilename = "library.db.old";

    private readonly IStartupLogger _logger;
    private readonly IServerApplicationPaths _paths;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;

    public MigrateLibraryUserData(
            IStartupLogger<MigrateLibraryDb> startupLogger,
            IDbContextFactory<JellyfinDbContext> provider,
            IServerApplicationPaths paths)
    {
        _logger = startupLogger;
        _provider = provider;
        _paths = paths;
    }

    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Migrating the userdata from library.db.old may take a while, do not stop Jellyfin.");

        var dataPath = _paths.DataPath;
        var libraryDbPath = Path.Combine(dataPath, DbFilename);
        if (!File.Exists(libraryDbPath))
        {
            _logger.LogError("Cannot migrate userdata from {LibraryDb} as it does not exist. This migration expects the MigrateLibraryDb to run first.", libraryDbPath);
            return;
        }

        var dbContext = await _provider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            if (!await dbContext.BaseItems.AnyAsync(e => e.Id == BaseItemRepository.PlaceholderId, cancellationToken).ConfigureAwait(false))
            {
                // the placeholder baseitem has been deleted by the librarydb migration so we need to readd it.
                await dbContext.BaseItems.AddAsync(
                    new Database.Implementations.Entities.BaseItemEntity()
                    {
                        Id = BaseItemRepository.PlaceholderId,
                        Type = "PLACEHOLDER",
                        Name = "This is a placeholder item for UserData that has been detacted from its original item"
                    },
                    cancellationToken)
                    .ConfigureAwait(false);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var users = dbContext.Users.AsNoTracking().ToArray();
            var userIdBlacklist = new HashSet<int>();
            using var connection = new SqliteConnection($"Filename={libraryDbPath};Mode=ReadOnly");
            var retentionDate = DateTime.UtcNow;

            var queryResult = connection.Query(
"""
    SELECT key, userId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex FROM UserDatas

    WHERE NOT EXISTS(SELECT 1 FROM TypedBaseItems WHERE TypedBaseItems.UserDataKey = UserDatas.key)
""");
            foreach (var entity in queryResult)
            {
                var userData = MigrateLibraryDb.GetUserData(users, entity, userIdBlacklist, _logger);
                if (userData is null)
                {
                    var userDataId = entity.GetString(0);
                    var internalUserId = entity.GetInt32(1);

                    if (!userIdBlacklist.Contains(internalUserId))
                    {
                        _logger.LogError("Was not able to migrate user data with key {0} because its id {InternalId} does not match any existing user.", userDataId, internalUserId);
                        userIdBlacklist.Add(internalUserId);
                    }

                    continue;
                }

                userData.ItemId = BaseItemRepository.PlaceholderId;
                userData.RetentionDate = retentionDate;
                dbContext.UserData.Add(userData);
            }

            _logger.LogInformation("Try saving {NewSaved} UserData entries.", dbContext.UserData.Local.Count);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
