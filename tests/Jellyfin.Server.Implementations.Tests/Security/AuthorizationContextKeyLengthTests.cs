using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Security;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Security;

/// <summary>
/// The web client builds its device id from its user agent, so the id of the authorization header can be longer
/// than the column it is compared with. It is shortened where the header is read, so that every use of it finds
/// the device stored for it.
/// </summary>
public sealed class AuthorizationContextKeyLengthTests : JellyfinDbTestFixture
{
    private readonly AuthorizationContext _authorizationContext;

    public AuthorizationContextKeyLengthTests()
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _authorizationContext = new AuthorizationContext(
            CreateDbContextFactory(),
            new Mock<IUserManager>().Object,
            new Mock<IDeviceManager>().Object,
            new Mock<IServerApplicationHost>().Object,
            configurationManager.Object);
    }

    [Fact]
    public async Task GetAuthorizationInfo_DeviceIdOverTheBound_IsShortened()
    {
        var deviceId = new string('d', KeyNormalizer.DeviceIdMaxLength + 100);

        var info = await _authorizationContext.GetAuthorizationInfo(CreateRequest($"Client=\"app\", Device=\"name\", DeviceId=\"{deviceId}\", Version=\"1.0\""));

        Assert.Equal(KeyNormalizer.NormalizeDeviceId(deviceId), info.DeviceId);
        Assert.Equal(KeyNormalizer.DeviceIdMaxLength, info.DeviceId?.Length);
    }

    [Fact]
    public async Task GetAuthorizationInfo_DeviceIdAtTheBound_IsUnchanged()
    {
        var deviceId = new string('d', KeyNormalizer.DeviceIdMaxLength);

        var info = await _authorizationContext.GetAuthorizationInfo(CreateRequest($"Client=\"app\", Device=\"name\", DeviceId=\"{deviceId}\", Version=\"1.0\""));

        Assert.Equal(deviceId, info.DeviceId);
    }

    private static HttpRequest CreateRequest(string authorization)
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.Authorization = "MediaBrowser " + authorization;
        return request;
    }
}
