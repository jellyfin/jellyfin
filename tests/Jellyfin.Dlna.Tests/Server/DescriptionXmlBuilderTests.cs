using Emby.Dlna.Server;
using MediaBrowser.Model.Dlna;
using Xunit;

namespace Jellyfin.Dlna.Server.Tests;

public class DescriptionXmlBuilderTests
{
    [Fact]
    public void GetFriendlyName_EmptyProfile_ReturnsServerName()
    {
        const string ServerName = "Test Server Name";
        var builder = new DescriptionXmlBuilder(new DeviceProfile(), "serverUdn", "localhost", ServerName, string.Empty);
        Assert.Equal(ServerName, builder.GetFriendlyName());
    }

    [Fact]
    public void GetFriendlyName_FriendlyName_ReturnsFriendlyName()
    {
        const string FriendlyName = "Friendly Neighborhood Test Server";
        var builder = new DescriptionXmlBuilder(
            new DeviceProfile()
            {
                FriendlyName = FriendlyName
            },
            "serverUdn",
            "localhost",
            "Test Server Name",
            string.Empty);
        Assert.Equal(FriendlyName, builder.GetFriendlyName());
    }

    [Fact]
    public void GetFriendlyName_FriendlyNameInterpolation_ReturnsFriendlyName()
    {
        var builder = new DescriptionXmlBuilder(
            new DeviceProfile()
            {
                FriendlyName = "Friendly Neighborhood ${HostName}"
            },
            "serverUdn",
            "localhost",
            "Test Server Name",
            string.Empty);
        Assert.Equal("Friendly Neighborhood TestServerName", builder.GetFriendlyName());
    }
}
