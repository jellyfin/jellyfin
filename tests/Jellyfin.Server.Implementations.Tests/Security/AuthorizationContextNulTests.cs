using System;
using System.Threading.Tasks;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Server.Implementations.Security;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Security;

/// <summary>
/// The device id, device name, client name and version of the authorization header are compared with the stored
/// device and used to look up the devices of a user, so they are cleaned of NUL characters where the header is read.
/// </summary>
public sealed class AuthorizationContextNulTests : SqliteDbTestFixture
{
    private readonly Mock<IDeviceManager> _deviceManager = new();
    private readonly AuthorizationContext _authorizationContext;

    public AuthorizationContextNulTests()
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _authorizationContext = new AuthorizationContext(
            CreateDbContextFactory(),
            new Mock<IUserManager>().Object,
            _deviceManager.Object,
            new Mock<IServerApplicationHost>().Object,
            configurationManager.Object);
    }

    [Fact]
    public async Task GetAuthorizationInfo_HeaderValuesWithNul_AreCleaned()
    {
        var request = CreateRequest("Client=\"ap%00p\", Device=\"na%00me\", DeviceId=\"dev%00\", Version=\"1.%000\"");

        var info = await _authorizationContext.GetAuthorizationInfo(request);

        Assert.Equal("app", info.Client);
        Assert.Equal("name", info.Device);
        Assert.Equal("dev", info.DeviceId);
        Assert.Equal("1.0", info.Version);
    }

    [Theory]
    [InlineData("Device=\"na%00me\", Version=\"1.0\"")]
    [InlineData("Device=\"name\", Version=\"1.%000\"")]
    [InlineData("Device=\"name%00\", Version=\"%001.0\"")]
    public async Task GetAuthorizationInfo_NameOrVersionDifferingOnlyByNul_DoesNotRewriteTheDevice(string header)
    {
        var device = new Device(Guid.NewGuid(), "app", "1.0", "name", "dev") { AccessToken = "token" };
        _deviceManager
            .Setup(m => m.GetDevices(It.Is<DeviceQuery>(q => q.AccessToken == "token")))
            .Returns(new QueryResult<Device>([device]));
        var request = CreateRequest(header + ", Client=\"app\", DeviceId=\"dev\", Token=\"token\"");

        await _authorizationContext.GetAuthorizationInfo(request);

        _deviceManager.Verify(m => m.UpdateDevice(It.IsAny<Device>()), Times.Never);
        Assert.Equal("name", device.DeviceName);
        Assert.Equal("1.0", device.AppVersion);
    }

    private static HttpRequest CreateRequest(string authorization)
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.Authorization = "MediaBrowser " + authorization;
        return request;
    }
}
