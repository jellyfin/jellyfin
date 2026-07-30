using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.CustomNetflix.Tests;

public sealed class PostgreSqlCustomNetflixRepositoryTests
{
    [Fact]
    public async Task ConcurrentDefaultProfileCreation_IsIdempotent()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();

        try
        {
            var profiles = await Task.WhenAll(
                repository.CreateProfileAsync(userId, "First", null, false, true, 5, CancellationToken.None),
                repository.CreateProfileAsync(userId, "Second", null, false, true, 5, CancellationToken.None));

            Assert.Equal(profiles[0].Id, profiles[1].Id);
            var persistedProfiles = await repository.GetProfilesAsync(userId, CancellationToken.None);
            Assert.Single(persistedProfiles);
            Assert.True(persistedProfiles[0].IsDefault);
        }
        finally
        {
            await DeleteProfilesAsync(dataSource, userId);
        }
    }

    [Fact]
    public async Task ConcurrentProfileCreation_EnforcesPerAccountLimit()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();

        try
        {
            await repository.CreateProfileAsync(userId, "Default", null, false, true, 5, CancellationToken.None);
            var results = await Task.WhenAll(
                Enumerable.Range(1, 8)
                    .Select(index => TryCreateProfileAsync(repository, userId, $"Profile {index}")));

            Assert.Equal(4, results.Count(result => result));
            Assert.Equal(5, (await repository.GetProfilesAsync(userId, CancellationToken.None)).Count);
        }
        finally
        {
            await repository.PurgeUserDataAsync(userId, CancellationToken.None);
        }
    }

    [Fact]
    public async Task ConcurrentProfileDeletes_PreserveOneDefaultProfile()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var firstProfile = await repository.CreateProfileAsync(userId, "First", null, false, true, 5, CancellationToken.None);
        var secondProfile = await repository.CreateProfileAsync(userId, "Second", null, false, false, 5, CancellationToken.None);

        try
        {
            var deleteResults = await Task.WhenAll(
                TryDeleteProfileAsync(repository, firstProfile.Id),
                TryDeleteProfileAsync(repository, secondProfile.Id));

            Assert.Single(deleteResults, result => result);
            var persistedProfiles = await repository.GetProfilesAsync(userId, CancellationToken.None);
            Assert.Single(persistedProfiles);
            Assert.True(persistedProfiles[0].IsDefault);
        }
        finally
        {
            await DeleteProfilesAsync(dataSource, userId);
        }
    }

    [Fact]
    public async Task ActiveProfiles_ArePersistedPerTokenAndRepairedAfterDelete()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var firstProfile = await repository.CreateProfileAsync(userId, "First", null, false, true, 5, CancellationToken.None);
        var secondProfile = await repository.CreateProfileAsync(userId, "Second", null, false, false, 5, CancellationToken.None);

        try
        {
            await repository.SetActiveProfileAsync(userId, "legacy", firstProfile.Id, CancellationToken.None);
            Assert.Equal(firstProfile.Id, await repository.GetActiveProfileAsync(userId, "token-hash-a", CancellationToken.None));

            await repository.SetActiveProfileAsync(userId, "token-hash-a", firstProfile.Id, CancellationToken.None);
            await repository.SetActiveProfileAsync(userId, "token-hash-b", secondProfile.Id, CancellationToken.None);

            Assert.Equal(firstProfile.Id, await repository.GetActiveProfileAsync(userId, "token-hash-a", CancellationToken.None));
            Assert.Equal(secondProfile.Id, await repository.GetActiveProfileAsync(userId, "token-hash-b", CancellationToken.None));

            Assert.True(await repository.SoftDeleteProfileAsync(firstProfile.Id, CancellationToken.None));
            Assert.Equal(secondProfile.Id, await repository.GetActiveProfileAsync(userId, "token-hash-a", CancellationToken.None));
            Assert.Equal(secondProfile.Id, await repository.GetActiveProfileAsync(userId, "token-hash-b", CancellationToken.None));
        }
        finally
        {
            await DeleteProfilesAsync(dataSource, userId);
        }
    }

    [Fact]
    public async Task PlaybackPreferences_RoundTripAllProfileFields()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var profile = await repository.CreateProfileAsync(userId, "Preferences", null, false, true, 5, CancellationToken.None);
        var preferences = new PlaybackPreferencesRow(
            profile.Id,
            false,
            true,
            false,
            true,
            false,
            20_000_000,
            "fr-FR",
            "en",
            true,
            true,
            false,
            true);

        try
        {
            var updated = await repository.UpdateProfileAsync(
                profile.Id,
                null,
                null,
                null,
                preferences,
                CancellationToken.None);
            var reloaded = await repository.GetProfileAsync(profile.Id, CancellationToken.None);

            Assert.NotNull(updated);
            Assert.NotNull(reloaded);
            Assert.Equal(preferences, updated!.PlaybackPreferences);
            Assert.Equal(preferences, reloaded!.PlaybackPreferences);
        }
        finally
        {
            await repository.PurgeUserDataAsync(userId, CancellationToken.None);
        }
    }

    [Fact]
    public async Task AutoplayState_RequiresConfirmationAfterThreeDistinctCompletedItems()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var profile = await repository.CreateProfileAsync(userId, "Autoplay", null, false, true, 5, CancellationToken.None);
        var itemIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        try
        {
            var first = await repository.TrackAutoplayAsync(profile.Id, itemIds[0], true, CancellationToken.None);
            var second = await repository.TrackAutoplayAsync(profile.Id, itemIds[1], true, CancellationToken.None);
            var third = await repository.TrackAutoplayAsync(profile.Id, itemIds[2], true, CancellationToken.None);
            var repeated = await repository.TrackAutoplayAsync(profile.Id, itemIds[2], true, CancellationToken.None);

            Assert.Equal(1, first.ConsecutiveCount);
            Assert.False(first.StillWatchingRequired);
            Assert.Equal(2, second.ConsecutiveCount);
            Assert.False(second.StillWatchingRequired);
            Assert.Equal(3, third.ConsecutiveCount);
            Assert.True(third.StillWatchingRequired);
            Assert.Equal(3, repeated.ConsecutiveCount);
            Assert.True(repeated.StillWatchingRequired);

            var confirmedAt = await repository.ConfirmStillWatchingAsync(profile.Id, CancellationToken.None);
            var sameAfterConfirmation = await repository.TrackAutoplayAsync(
                profile.Id,
                itemIds[2],
                true,
                CancellationToken.None);
            var nextAfterConfirmation = await repository.TrackAutoplayAsync(
                profile.Id,
                itemIds[3],
                true,
                CancellationToken.None);

            Assert.True(confirmedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.Equal(0, sameAfterConfirmation.ConsecutiveCount);
            Assert.False(sameAfterConfirmation.StillWatchingRequired);
            Assert.NotNull(sameAfterConfirmation.ConfirmedAt);
            Assert.Equal(1, nextAfterConfirmation.ConsecutiveCount);
            Assert.False(nextAfterConfirmation.StillWatchingRequired);
        }
        finally
        {
            await repository.PurgeUserDataAsync(userId, CancellationToken.None);
        }
    }

    [Fact]
    public async Task BatchedWrites_PersistProgressAndEventsIdempotently()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var profile = await repository.CreateProfileAsync(userId, "Batch test", null, false, false, 5, CancellationToken.None);
        var itemIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var playedAt = DateTime.UtcNow;

        try
        {
            await repository.UpsertProgressRowsAsync(
                [
                    new WatchProgressRow(profile.Id, itemIds[0], null, 10, 100, 10, false, 0, playedAt),
                    new WatchProgressRow(profile.Id, itemIds[1], Guid.NewGuid(), 100, 100, 100, true, 1, playedAt)
                ],
                CancellationToken.None);
            WatchEventRow[] watchEvents =
            [
                new(Guid.NewGuid(), profile.Id, userId, itemIds[0], "Movie", "progress", 10, 100, null, null),
                new(Guid.NewGuid(), profile.Id, userId, itemIds[1], "Movie", "stop", 100, 100, "session", "client")
            ];
            await repository.InsertWatchEventsAsync(watchEvents, CancellationToken.None);
            await repository.InsertWatchEventsAsync(watchEvents, CancellationToken.None);

            var progress = await repository.GetProgressForItemsAsync(profile.Id, itemIds, CancellationToken.None);
            Assert.Equal(2, progress.Count);
            Assert.Null(progress.Single(row => row.ItemId.Equals(itemIds[0])).MediaSourceId);

            await using var countCommand = dataSource.CreateCommand("select count(*) from cnx_watch_events where profile_id = @profile_id");
            countCommand.Parameters.AddWithValue("profile_id", profile.Id);
            Assert.Equal(2L, await countCommand.ExecuteScalarAsync(CancellationToken.None));
        }
        finally
        {
            await using var cleanup = dataSource.CreateCommand(
                """
                delete from cnx_watch_events where profile_id = @profile_id;
                delete from cnx_watch_history where profile_id = @profile_id;
                delete from cnx_watch_progress where profile_id = @profile_id;
                delete from cnx_playback_preferences where profile_id = @profile_id;
                delete from cnx_profile_settings where profile_id = @profile_id;
                delete from cnx_profiles where id = @profile_id;
                """);
            cleanup.Parameters.AddWithValue("profile_id", profile.Id);
            await cleanup.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task PurgeUserData_RemovesAllProfileDataAndIsIdempotent()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var rankingId = $"purge-test-{Guid.NewGuid():N}";
        var profile = await repository.CreateProfileAsync(userId, "Purge test", null, false, true, 5, CancellationToken.None);
        var now = DateTime.UtcNow;

        try
        {
            await repository.SetActiveProfileAsync(userId, "purge-token", profile.Id, CancellationToken.None);
            await repository.UpsertProgressRowsAsync(
                [new WatchProgressRow(profile.Id, itemId, null, 60, 120, 50, false, 0, now)],
                CancellationToken.None);
            await repository.InsertWatchEventsAsync(
                [new WatchEventRow(Guid.NewGuid(), profile.Id, userId, itemId, "Movie", "progress", 60, 120, null, null)],
                CancellationToken.None);
            await repository.AddToMyListAsync(profile.Id, itemId, CancellationToken.None);
            await repository.HideFromContinueWatchingAsync(profile.Id, itemId, CancellationToken.None);
            await repository.SaveHomeSnapshotAsync(profile.Id, "purge-test", "{}", now, now.AddMinutes(5), CancellationToken.None);
            await repository.SaveRankingSnapshotAsync(
                rankingId,
                [new RankedItemRow(itemId, 1, 1)],
                now,
                now.AddMinutes(5),
                CancellationToken.None);
            await using (var preferenceCommand = dataSource.CreateCommand(
                "insert into cnx_playback_preferences (profile_id) values (@profile_id)"))
            {
                preferenceCommand.Parameters.AddWithValue("profile_id", profile.Id);
                await preferenceCommand.ExecuteNonQueryAsync(CancellationToken.None);
            }

            var firstPurge = await repository.PurgeUserDataAsync(userId, CancellationToken.None);
            var secondPurge = await repository.PurgeUserDataAsync(userId, CancellationToken.None);

            Assert.Equal(new[] { profile.Id }, firstPurge.ProfileIds);
            Assert.Equal(new[] { "purge-token" }, firstPurge.ActiveProfileTokenHashes);
            Assert.Empty(secondPurge.ProfileIds);
            Assert.Empty(secondPurge.ActiveProfileTokenHashes);
            Assert.Empty(await repository.GetProfilesAsync(userId, CancellationToken.None));
            Assert.Null(await repository.GetActiveProfileAsync(userId, "purge-token", CancellationToken.None));

            await using var countCommand = dataSource.CreateCommand(
                """
                select
                    (select count(*) from cnx_profiles where jellyfin_user_id = @jellyfin_user_id)
                    + (select count(*) from cnx_active_profiles where jellyfin_user_id = @jellyfin_user_id)
                    + (select count(*) from cnx_profile_settings where profile_id = @profile_id)
                    + (select count(*) from cnx_playback_preferences where profile_id = @profile_id)
                    + (select count(*) from cnx_watch_progress where profile_id = @profile_id)
                    + (select count(*) from cnx_watch_events where jellyfin_user_id = @jellyfin_user_id)
                    + (select count(*) from cnx_watch_history where profile_id = @profile_id)
                    + (select count(*) from cnx_profile_hidden_items where profile_id = @profile_id)
                    + (select count(*) from cnx_profile_my_list where profile_id = @profile_id)
                    + (select count(*) from cnx_home_row_snapshots where profile_id = @profile_id)
                    + (select count(*) from cnx_ranking_snapshots where ranking_id = @ranking_id)
                """);
            countCommand.Parameters.AddWithValue("jellyfin_user_id", userId);
            countCommand.Parameters.AddWithValue("profile_id", profile.Id);
            countCommand.Parameters.AddWithValue("ranking_id", rankingId);
            Assert.Equal(0L, await countCommand.ExecuteScalarAsync(CancellationToken.None));
        }
        finally
        {
            await repository.PurgeUserDataAsync(userId, CancellationToken.None);
        }
    }

    [Fact]
    public async Task PurgeWatchEvents_DeletesOnlyOneExpiredBatch()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(connectionString), "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var profile = await repository.CreateProfileAsync(userId, "Retention test", null, false, false, 5, CancellationToken.None);
        var oldItemIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var freshItemId = Guid.NewGuid();

        try
        {
            await repository.InsertWatchEventsAsync(
                oldItemIds
                    .Append(freshItemId)
                    .Select(itemId => new WatchEventRow(Guid.NewGuid(), profile.Id, userId, itemId, "Movie", "progress", 10, 100, null, null))
                    .ToArray(),
                CancellationToken.None);
            await using (var ageCommand = dataSource.CreateCommand(
                "update cnx_watch_events set created_at = @created_at where profile_id = @profile_id and item_id = any(@item_ids)"))
            {
                ageCommand.Parameters.AddWithValue("created_at", DateTime.UtcNow.AddDays(-32));
                ageCommand.Parameters.AddWithValue("profile_id", profile.Id);
                ageCommand.Parameters.AddWithValue("item_ids", oldItemIds);
                await ageCommand.ExecuteNonQueryAsync(CancellationToken.None);
            }

            var cutoff = DateTime.UtcNow.AddDays(-31);
            var purged = await repository.PurgeWatchEventsAsync(cutoff, 2, CancellationToken.None);

            Assert.Equal(2, purged);
            await using var countCommand = dataSource.CreateCommand(
                "select count(*) from cnx_watch_events where profile_id = @profile_id and created_at < @cutoff");
            countCommand.Parameters.AddWithValue("profile_id", profile.Id);
            countCommand.Parameters.AddWithValue("cutoff", cutoff);
            Assert.Equal(1L, await countCommand.ExecuteScalarAsync(CancellationToken.None));
        }
        finally
        {
            await using var cleanup = dataSource.CreateCommand(
                """
                delete from cnx_watch_events where profile_id = @profile_id;
                delete from cnx_profile_settings where profile_id = @profile_id;
                delete from cnx_profiles where id = @profile_id;
                """);
            cleanup.Parameters.AddWithValue("profile_id", profile.Id);
            await cleanup.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    private static async Task<bool> TryDeleteProfileAsync(PostgreSqlCustomNetflixRepository repository, Guid profileId)
    {
        try
        {
            return await repository.SoftDeleteProfileAsync(profileId, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task<bool> TryCreateProfileAsync(
        PostgreSqlCustomNetflixRepository repository,
        Guid userId,
        string name)
    {
        try
        {
            await repository.CreateProfileAsync(userId, name, null, false, false, 5, CancellationToken.None);
            return true;
        }
        catch (CustomNetflixProfileLimitExceededException)
        {
            return false;
        }
    }

    private static async Task DeleteProfilesAsync(NpgsqlDataSource dataSource, Guid userId)
    {
        await using var cleanup = dataSource.CreateCommand(
            """
            delete from cnx_active_profiles where jellyfin_user_id = @jellyfin_user_id;
            delete from cnx_watch_events where jellyfin_user_id = @jellyfin_user_id;
            delete from cnx_watch_history where profile_id in (select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);
            delete from cnx_watch_progress where profile_id in (select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);
            delete from cnx_playback_preferences where profile_id in (select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);
            delete from cnx_profile_settings where profile_id in (select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);
            delete from cnx_profiles where jellyfin_user_id = @jellyfin_user_id;
            """);
        cleanup.Parameters.AddWithValue("jellyfin_user_id", userId);
        await cleanup.ExecuteNonQueryAsync(CancellationToken.None);
    }
}
