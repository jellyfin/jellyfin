using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Devices;
using Jellyfin.Server.Implementations.Security;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

/// <summary>
/// The device id of a login is read from the authorization header, and the device of the previous login on
/// the same device is found by it. A device id with a NUL character has to find the device stored for it,
/// so that the old token is logged out and no second device is added.
/// </summary>
public sealed class SessionManagerNulTests : JellyfinDbTestFixture
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DeviceManager _deviceManager;
    private readonly AuthorizationContext _authorizationContext;
    private readonly Emby.Server.Implementations.Session.SessionManager _sessionManager;

    public SessionManagerNulTests()
    {
        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset") { Id = _userId });
            context.SaveChanges();
        }

        _deviceManager = new DeviceManager(CreateDbContextFactory(), new Mock<IUserManager>().Object);

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _authorizationContext = new AuthorizationContext(
            CreateDbContextFactory(),
            new Mock<IUserManager>().Object,
            _deviceManager,
            new Mock<IServerApplicationHost>().Object,
            configurationManager.Object);

        _sessionManager = new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            configurationManager.Object,
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            _deviceManager,
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>());
    }

    [Fact]
    public async Task GetAuthorizationToken_DeviceIdWithNulInTheHeader_LogsOutTheTokenOfThePreviousLogin()
    {
        var firstToken = await LoginAsync("Client=\"app\", Device=\"name\", DeviceId=\"dev%00\", Version=\"1.0\"");
        var secondToken = await LoginAsync("Client=\"app\", Device=\"name\", DeviceId=\"dev%00\", Version=\"1.0\"");

        Assert.NotEqual(firstToken, secondToken);
        using var context = CreateDbContext();
        var device = Assert.Single(context.Devices.AsNoTracking());
        Assert.Equal(secondToken, device.AccessToken);
        Assert.Equal("dev", device.DeviceId);
    }

    private async Task<string> LoginAsync(string authorization)
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.Authorization = "MediaBrowser " + authorization;
        var info = await _authorizationContext.GetAuthorizationInfo(request);

        return await _sessionManager.GetAuthorizationToken(
            new User("user", "auth", "reset") { Id = _userId },
            info.DeviceId,
            info.Client,
            info.Version,
            info.Device);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sessionManager.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }
}
