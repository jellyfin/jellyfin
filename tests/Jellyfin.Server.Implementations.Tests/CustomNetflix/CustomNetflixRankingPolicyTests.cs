using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixRankingPolicyTests
{
    [Theory]
    [InlineData("complete", false, 5.0)]
    [InlineData("complete", true, 8.0)]
    [InlineData("mark_played", false, 4.0)]
    [InlineData("progress", true, 1.0)]
    public void EventWeight_ReturnsExpectedWeights(string eventType, bool topTen, double expected)
    {
        var result = CustomNetflixRankingPolicy.EventWeight(eventType, topTen);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProfileScore_CapsProgressSpamForTrending()
    {
        var normal = CustomNetflixRankingPolicy.ProfileScore(
            strongestEventScore: 1,
            eventCount: 10,
            lastEventAgeDays: 0,
            topTen: false);
        var spammed = CustomNetflixRankingPolicy.ProfileScore(
            strongestEventScore: 1,
            eventCount: 1000,
            lastEventAgeDays: 0,
            topTen: false);

        Assert.Equal(normal, spammed);
        Assert.Equal(1.8, spammed);
    }

    [Fact]
    public void RecencyMultiplier_HalvesAtHalfLife()
    {
        var result = CustomNetflixRankingPolicy.RecencyMultiplier(2, CustomNetflixRankingPolicy.TrendingRecencyHalfLifeDays);

        Assert.Equal(0.5, result, precision: 6);
    }

    [Fact]
    public void ItemScore_RewardsDistinctProfiles()
    {
        var singleProfile = CustomNetflixRankingPolicy.ItemScore(5, 1, topTen: false);
        var threeProfiles = CustomNetflixRankingPolicy.ItemScore(5, 3, topTen: false);

        Assert.Equal(7, singleProfile);
        Assert.Equal(11, threeProfiles);
    }
}
