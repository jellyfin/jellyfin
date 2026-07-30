using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixHistoryPolicyTests
{
    [Theory]
    [InlineData(0, CustomNetflixHistoryPolicy.DefaultLimit)]
    [InlineData(-4, CustomNetflixHistoryPolicy.DefaultLimit)]
    [InlineData(1, CustomNetflixHistoryPolicy.MinLimit)]
    [InlineData(42, 42)]
    [InlineData(500, CustomNetflixHistoryPolicy.MaxLimit)]
    public void NormalizeLimit_UsesDefaultAndBounds(int limit, int expected)
    {
        var result = CustomNetflixHistoryPolicy.NormalizeLimit(limit);

        Assert.Equal(expected, result);
    }
}
