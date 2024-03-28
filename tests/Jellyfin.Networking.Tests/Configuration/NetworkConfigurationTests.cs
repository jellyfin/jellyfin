using MediaBrowser.Common.Net;
using Xunit;

namespace Jellyfin.Networking.Tests.Configuration;

public static class NetworkConfigurationTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("/Test", "/Test")]
    [InlineData("/Test", "Test")]
    [InlineData("/Test", "Test/")]
    [InlineData("/Test", "/Test/")]
    [InlineData("/Test/2", "/Test/2")]
    [InlineData("/Test/2", "Test/2")]
    [InlineData("/Test/2", "Test/2/")]
    [InlineData("/Test/2", "/Test/2/")]
    public static void BaseUrl_ReturnsNormalized(string expected, string input)
    {
        var config = new NetworkConfiguration()
        {
            BaseUrl = input
        };

        Assert.Equal(expected, config.BaseUrl);
    }
}
