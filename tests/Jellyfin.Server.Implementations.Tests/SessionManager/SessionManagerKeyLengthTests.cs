using System;
using System.Linq;
using System.Threading;
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
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

/// <summary>
/// The jellyfin-web device id is built from the user agent and a timestamp, and can be longer than the column of
/// the device. Logging in with it has to work every time and replace the device of the previous login, instead
/// of failing or adding a second device.
/// </summary>
public sealed class SessionManagerKeyLengthTests : JellyfinDbTestFixture
{
    private static readonly string _rawId = new('d', KeyNormalizer.DeviceIdMaxLength + 100);
    private static readonly string _storedId = KeyNormalizer.NormalizeDeviceId(_rawId);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly AuthorizationContext _authorizationContext;
    private readonly Emby.Server.Implementations.Session.SessionManager _sessionManager;

    public SessionManagerKeyLengthTests()
    {
        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset") { Id = _userId });
            context.SaveChanges();
        }

        var deviceManager = new DeviceManager(CreateDbContextFactory(), new Mock<IUserManager>().Object);

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _authorizationContext = new AuthorizationContext(
            CreateDbContextFactory(),
            new Mock<IUserManager>().Object,
            deviceManager,
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
            deviceManager,
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>());
    }

    [Fact]
    public async Task GetAuthorizationToken_DeviceIdOverTheBoundInTheHeader_LogsInAgainOnTheSameDevice()
    {
        var deviceId = Convert.ToBase64String(new byte[300]);
        var header = $"Client=\"Jellyfin Web\", Device=\"Chrome\", DeviceId=\"{deviceId}\", Version=\"10.11.0\"";

        var firstToken = await LoginAsync(header);
        var secondToken = await LoginAsync(header);

        Assert.NotEqual(firstToken, secondToken);
        using var context = CreateDbContext();
        var device = Assert.Single(context.Devices.AsNoTracking());
        Assert.Equal(secondToken, device.AccessToken);
        Assert.Equal(KeyNormalizer.NormalizeDeviceId(deviceId), device.DeviceId);
    }

    [Fact]
    public async Task GetAuthorizationToken_DeviceIdOverTheBoundPassedDirectly_LogsOutThePreviousLogin()
    {
        var deviceId = new string('d', KeyNormalizer.DeviceIdMaxLength + 100);
        var user = new User("user", "auth", "reset") { Id = _userId };

        var firstToken = await _sessionManager.GetAuthorizationToken(user, deviceId, "app", "1.0", "name");
        var secondToken = await _sessionManager.GetAuthorizationToken(user, deviceId, "app", "1.0", "name");

        Assert.NotEqual(firstToken, secondToken);
        using var context = CreateDbContext();
        var device = Assert.Single(context.Devices.AsNoTracking());
        Assert.Equal(secondToken, device.AccessToken);
        Assert.Equal(KeyNormalizer.NormalizeDeviceId(deviceId), device.DeviceId);
    }

    [Fact]
    public async Task LogSessionActivity_DeviceIdOverTheBound_HoldsTheShortenedId()
    {
        var session = await LogSessionActivityAsync(_rawId);

        Assert.Equal(KeyNormalizer.NormalizeDeviceId(_rawId), session.DeviceId);
        Assert.Same(session, await LogSessionActivityAsync(_rawId));
    }

    [Fact]
    public async Task GetSession_RawDeviceIdOverTheBound_FindsTheSession()
    {
        var session = await LogSessionActivityAsync(_storedId);

        Assert.Same(session, _sessionManager.GetSession(_rawId, "app", "1.0"));
    }

    [Fact]
    public async Task GetSessions_RawDeviceIdOverTheBound_FindsTheSession()
    {
        var session = await LogSessionActivityAsync(_storedId);

        var found = _sessionManager.GetSessions(Guid.Empty, _rawId, null, null, true);

        Assert.Equal(session.Id, Assert.Single(found).Id);
    }

    [Fact]
    public async Task ReportTranscodingInfo_RawDeviceIdOverTheBound_ReachesTheSession()
    {
        var session = await LogSessionActivityAsync(_storedId);
        var info = new TranscodingInfo();

        _sessionManager.ReportTranscodingInfo(_rawId, info);
        Assert.Same(info, session.TranscodingInfo);

        _sessionManager.ClearTranscodingInfo(_rawId);
        Assert.Null(session.TranscodingInfo);
    }

    [Fact]
    public async Task SendMessageToUserDeviceSessions_RawDeviceIdOverTheBound_ReachesTheSession()
    {
        var session = await LogSessionActivityAsync(_storedId);
        var controller = new Mock<ISessionController>();
        controller.Setup(c => c.SendMessage(It.IsAny<SessionMessageType>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        session.AddController(controller.Object);

        await _sessionManager.SendMessageToUserDeviceSessions(_rawId, SessionMessageType.KeepAlive, "data", CancellationToken.None);

        controller.Verify(
            c => c.SendMessage(SessionMessageType.KeepAlive, It.IsAny<Guid>(), "data", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private Task<SessionInfo> LogSessionActivityAsync(string deviceId)
        => _sessionManager.LogSessionActivity("app", "1.0", deviceId, "name", "127.0.0.1", new User("user", "auth", "reset") { Id = _userId });

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
