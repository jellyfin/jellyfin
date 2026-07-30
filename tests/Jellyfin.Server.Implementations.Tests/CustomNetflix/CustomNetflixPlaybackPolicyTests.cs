using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixPlaybackPolicyTests
{
    [Theory]
    [InlineData("Episode", 0, 0, 0, false)]
    [InlineData("Episode", 1200, 1080, 90, true)]
    [InlineData("Episode", 1200, 1019, 84.916, false)]
    [InlineData("Episode", 1200, 1020, 85, true)]
    [InlineData("Movie", 2000, 1699, 84.95, false)]
    [InlineData("Movie", 2000, 1700, 85, true)]
    public void IsCompleted_AppliesPercentAndRemainingThresholds(
        string itemType,
        double durationSeconds,
        double positionSeconds,
        double percentViewed,
        bool expected)
    {
        var result = CustomNetflixPlaybackPolicy.IsCompleted(itemType, positionSeconds, durationSeconds, percentViewed);

        Assert.Equal(expected, result);
    }
}
