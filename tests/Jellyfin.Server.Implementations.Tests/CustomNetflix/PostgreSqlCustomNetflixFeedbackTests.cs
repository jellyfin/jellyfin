using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.CustomNetflix.Tests;

public sealed class PostgreSqlCustomNetflixFeedbackTests
{
    [Fact]
    public async Task ItemFeedback_RoundTripsAndFiltersForRecommendations()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_POSTGRES_CONNECTION_STRING");
        Assert.SkipUnless(
            !string.IsNullOrWhiteSpace(connectionString),
            "Set JELLYFIN_TEST_POSTGRES_CONNECTION_STRING to run this integration test.");
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgreSqlCustomNetflixRepository(dataSource);
        await repository.EnsureSchemaAsync(CancellationToken.None);
        var userId = Guid.NewGuid();
        var profile = await repository.CreateProfileAsync(
            userId,
            "Feedback",
            null,
            false,
            true,
            5,
            CancellationToken.None);
        var likedItemId = Guid.NewGuid();
        var hiddenItemId = Guid.NewGuid();

        try
        {
            await repository.UpsertItemFeedbackAsync(
                profile.Id,
                likedItemId,
                CustomNetflixFeedbackPolicy.Like,
                CancellationToken.None);
            await repository.UpsertItemFeedbackAsync(
                profile.Id,
                hiddenItemId,
                CustomNetflixFeedbackPolicy.NotInterested,
                CancellationToken.None);

            var liked = await repository.GetLikedItemFeedbacksAsync(profile.Id, 10, CancellationToken.None);
            var candidates = await repository.GetItemFeedbacksForItemsAsync(
                profile.Id,
                [likedItemId, hiddenItemId],
                CancellationToken.None);

            Assert.Single(liked, row => row.ItemId.Equals(likedItemId));
            Assert.Equal(2, candidates.Count);
            Assert.Equal(
                CustomNetflixFeedbackPolicy.NotInterested,
                (await repository.GetItemFeedbackAsync(profile.Id, hiddenItemId, CancellationToken.None))?.Feedback);

            Assert.True(await repository.DeleteItemFeedbackAsync(profile.Id, hiddenItemId, CancellationToken.None));
            Assert.Null(await repository.GetItemFeedbackAsync(profile.Id, hiddenItemId, CancellationToken.None));
        }
        finally
        {
            await repository.PurgeUserDataAsync(userId, CancellationToken.None);
        }
    }
}
