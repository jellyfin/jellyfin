using System;
using System.Collections.Generic;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixRecommendationPolicyTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 1)]
    [InlineData(80, 50)]
    public void NormalizeLimit_UsesNetflixHomeBounds(int requested, int expected)
    {
        Assert.Equal(expected, CustomNetflixRecommendationPolicy.NormalizeLimit(requested));
    }

    [Fact]
    public void RankCandidates_PrefersProfileGenresOverGlobalRating()
    {
        var watchedId = Guid.NewGuid();
        var scienceFictionId = Guid.NewGuid();
        var comedyId = Guid.NewGuid();
        var signals = new[]
        {
            Signal(watchedId, "Movie", ["Science-fiction"], UtcNow.AddDays(-2), completed: true, playCount: 2)
        };
        var candidates = new[]
        {
            Candidate(comedyId, "Movie", ["Comedie"], rating: 9.8),
            Candidate(scienceFictionId, "Movie", ["Science-fiction"], rating: 6.5)
        };

        var result = CustomNetflixRecommendationPolicy.RankCandidates(signals, candidates, UtcNow, 10);

        Assert.Equal(scienceFictionId, result[0]);
    }

    [Fact]
    public void RankCandidates_ExcludesAnythingAlreadyWatchedByTheProfile()
    {
        var watchedId = Guid.NewGuid();
        var unwatchedId = Guid.NewGuid();
        var signals = new[]
        {
            Signal(watchedId, "Series", ["Drame"], UtcNow.AddDays(-1), completed: false, playCount: 1)
        };
        var candidates = new[]
        {
            Candidate(watchedId, "Series", ["Drame"], rating: 10),
            Candidate(unwatchedId, "Series", ["Drame"], rating: 7)
        };

        var result = CustomNetflixRecommendationPolicy.RankCandidates(signals, candidates, UtcNow, 10);

        Assert.Equal(new[] { unwatchedId }, result);
    }

    [Fact]
    public void GetTopGenres_GivesMoreWeightToRecentViewing()
    {
        var signals = new[]
        {
            Signal(Guid.NewGuid(), "Movie", ["Horreur"], UtcNow.AddDays(-240), completed: true, playCount: 1),
            Signal(Guid.NewGuid(), "Movie", ["Drame"], UtcNow.AddDays(-2), completed: true, playCount: 1)
        };

        var result = CustomNetflixRecommendationPolicy.GetTopGenres(signals, UtcNow);

        Assert.Equal("Drame", result[0]);
    }

    [Fact]
    public void RankCandidates_UsesRatingAsStableFallbackForNewProfile()
    {
        var highRatingId = Guid.NewGuid();
        var lowRatingId = Guid.NewGuid();
        var releaseDate = UtcNow.AddYears(-1);
        var candidates = new[]
        {
            Candidate(lowRatingId, "Movie", ["Drame"], rating: 6, releaseDate: releaseDate),
            Candidate(highRatingId, "Movie", ["Action"], rating: 9, releaseDate: releaseDate)
        };

        var result = CustomNetflixRecommendationPolicy.RankCandidates(
            Array.Empty<CustomNetflixRecommendationSignal>(),
            candidates,
            UtcNow,
            10);

        Assert.Equal(highRatingId, result[0]);
        Assert.False(CustomNetflixRecommendationPolicy.HasPersonalizationSignals(Array.Empty<CustomNetflixRecommendationSignal>()));
    }

    [Fact]
    public void RankCandidates_ExcludesNegativeFeedback()
    {
        var excludedId = Guid.NewGuid();
        var retainedId = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(excludedId, "Movie", ["Drame"], rating: 10),
            Candidate(retainedId, "Movie", ["Drame"], rating: 7)
        };

        var result = CustomNetflixRecommendationPolicy.RankCandidates(
            Array.Empty<CustomNetflixRecommendationSignal>(),
            candidates,
            UtcNow,
            10,
            new HashSet<Guid> { excludedId });

        Assert.Equal(new[] { retainedId }, result);
    }

    [Theory]
    [InlineData(true, true, CustomNetflixRecommendationPolicy.LikedItemsReason)]
    [InlineData(false, true, CustomNetflixRecommendationPolicy.WatchHistoryReason)]
    [InlineData(false, false, CustomNetflixRecommendationPolicy.PopularFallbackReason)]
    public void GetStableReason_ReturnsApiCodes(bool hasLikedItems, bool personalized, string expected)
        => Assert.Equal(expected, CustomNetflixRecommendationPolicy.GetStableReason(hasLikedItems, personalized));

    private static CustomNetflixRecommendationSignal Signal(
        Guid itemId,
        string itemType,
        IReadOnlyList<string> genres,
        DateTime lastPlayedAt,
        bool completed,
        int playCount)
        => new(itemId, itemType, genres, lastPlayedAt, completed, playCount);

    private static CustomNetflixRecommendationCandidate Candidate(
        Guid itemId,
        string itemType,
        IReadOnlyList<string> genres,
        double rating,
        DateTime? releaseDate = null)
        => new(itemId, itemType, genres, rating, releaseDate ?? UtcNow.AddYears(-1), UtcNow.AddYears(-1));
}
