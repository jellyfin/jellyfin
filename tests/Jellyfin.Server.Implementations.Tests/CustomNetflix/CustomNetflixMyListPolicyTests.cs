using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixMyListPolicyTests
{
    [Theory]
    [InlineData(0, CustomNetflixMyListPolicy.DefaultLimit)]
    [InlineData(-1, CustomNetflixMyListPolicy.DefaultLimit)]
    [InlineData(12, 12)]
    [InlineData(500, CustomNetflixMyListPolicy.MaxLimit)]
    public void NormalizeLimit_UsesDefaultAndBounds(int limit, int expected)
    {
        Assert.Equal(expected, CustomNetflixMyListPolicy.NormalizeLimit(limit));
    }

    [Theory]
    [InlineData("Movie", true)]
    [InlineData("Series", true)]
    [InlineData("Episode", false)]
    [InlineData("Audio", false)]
    public void SupportsItemType_AllowsNetflixTitleTypes(string itemType, bool expected)
    {
        Assert.Equal(expected, CustomNetflixMyListPolicy.SupportsItemType(itemType));
    }
}
