using System;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Session;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class SessionManagerTests
{
    private const string PlaybackOwnerId = "authenticated-playback-owner";

    [Theory]
    [InlineData("", typeof(ArgumentException))]
    [InlineData(null, typeof(ArgumentNullException))]
    public async Task GetAuthorizationToken_Should_ThrowException(string? deviceId, Type exceptionType)
    {
        await using var sessionManager = new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>(),
            Mock.Of<IVideoPlaybackCoordinator>());

        await Assert.ThrowsAsync(exceptionType, () => sessionManager.GetAuthorizationToken(
            new User("test", "default", "default"),
            deviceId,
            "app_name",
            "0.0.0",
            "device_name"));
    }

    [Theory]
    [MemberData(nameof(AuthenticateNewSessionInternal_Exception_TestData))]
    public async Task AuthenticateNewSessionInternal_Should_ThrowException(AuthenticationRequest authenticationRequest, Type exceptionType)
    {
        await using var sessionManager = new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>(),
            Mock.Of<IVideoPlaybackCoordinator>());

        await Assert.ThrowsAsync(exceptionType, () => sessionManager.AuthenticateNewSessionInternal(authenticationRequest, false));
    }

    public static TheoryData<AuthenticationRequest, Type> AuthenticateNewSessionInternal_Exception_TestData()
    {
        var data = new TheoryData<AuthenticationRequest, Type>
        {
            {
                new AuthenticationRequest { App = string.Empty, DeviceId = "device_id", DeviceName = "device_name", AppVersion = "app_version" },
                typeof(ArgumentException)
            },
            {
                new AuthenticationRequest { App = null, DeviceId = "device_id", DeviceName = "device_name", AppVersion = "app_version" },
                typeof(ArgumentNullException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = string.Empty, DeviceName = "device_name", AppVersion = "app_version" },
                typeof(ArgumentException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = null, DeviceName = "device_name", AppVersion = "app_version" },
                typeof(ArgumentNullException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = "device_id", DeviceName = string.Empty, AppVersion = "app_version" },
                typeof(ArgumentException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = "device_id", DeviceName = null, AppVersion = "app_version" },
                typeof(ArgumentNullException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = "device_id", DeviceName = "device_name", AppVersion = string.Empty },
                typeof(ArgumentException)
            },
            {
                new AuthenticationRequest { App = "app_name", DeviceId = "device_id", DeviceName = "device_name", AppVersion = null },
                typeof(ArgumentNullException)
            }
        };

        return data;
    }

    [Fact]
    public async Task SendMessageCommand_Should_ThrowSecurityException_WhenControllingAnotherUsersSession()
    {
        var victim = new User("victim", "default", "default");
        var attacker = new User("attacker", "default", "default");
        await using var sessionManager = CreateSessionManager(victim, attacker);

        var victimSession = await LogSessionActivity(sessionManager, victim);
        var attackerSession = await LogSessionActivity(sessionManager, attacker);

        await Assert.ThrowsAsync<SecurityException>(() => sessionManager.SendMessageCommand(
            attackerSession.Id,
            victimSession.Id,
            new MessageCommand { Header = "Custom Message", Text = "test exploit!" },
            CancellationToken.None));
    }

    [Fact]
    public async Task SendMessageCommand_Should_Succeed_WhenAllowedToControlOtherUsers()
    {
        var victim = new User("victim", "default", "default");
        var attacker = new User("controller", "default", "default");
        attacker.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, true);
        await using var sessionManager = CreateSessionManager(victim, attacker);

        var victimSession = await LogSessionActivity(sessionManager, victim);
        var controllingSession = await LogSessionActivity(sessionManager, attacker);

        await sessionManager.SendMessageCommand(
            controllingSession.Id,
            victimSession.Id,
            new MessageCommand { Header = "Custom Message", Text = "hello" },
            CancellationToken.None);
    }

    [Fact]
    public async Task LogSessionActivity_Should_NotReuseAnotherUsersSession()
    {
        var victim = new User("victim", "default", "default");
        var attacker = new User("attacker", "default", "default");
        await using var sessionManager = CreateSessionManager(victim, attacker);

        // Client name and device id are attacker controlled, so they must not identify a session on their own.
        var victimSession = await LogSessionActivity(sessionManager, victim);
        var attackerSession = await LogSessionActivity(sessionManager, attacker);

        Assert.NotEqual(victimSession.Id, attackerSession.Id);
        Assert.Equal(victim.Id, victimSession.UserId);
    }

    [Fact]
    public async Task AddAdditionalUser_Should_ThrowSecurityException_WhenAttachingAnotherUser()
    {
        var attacker = new User("attacker", "default", "default");
        var victim = new User("victim", "default", "default");
        await using var sessionManager = CreateSessionManager(victim, attacker);

        var attackerSession = await LogSessionActivity(sessionManager, attacker);

        Assert.Throws<SecurityException>(() => sessionManager.AddAdditionalUser(attackerSession.Id, attackerSession.Id, victim.Id));
    }

    [Fact]
    public async Task AddAdditionalUser_Should_Succeed_WhenCallerIsAdministrator()
    {
        var admin = new User("admin", "default", "default");
        admin.SetPermission(PermissionKind.IsAdministrator, true);
        var guest = new User("guest", "default", "default");
        await using var sessionManager = CreateSessionManager(admin, guest);

        var adminSession = await LogSessionActivity(sessionManager, admin);

        sessionManager.AddAdditionalUser(adminSession.Id, adminSession.Id, guest.Id);

        Assert.Contains(adminSession.AdditionalUsers, i => i.UserId.Equals(guest.Id));
    }

    [Fact]
    public async Task RemoveAdditionalUser_Should_ThrowSecurityException_WhenModifyingAnotherUsersSession()
    {
        var victim = new User("victim", "default", "default");
        var attacker = new User("attacker", "default", "default");
        await using var sessionManager = CreateSessionManager(victim, attacker);

        var victimSession = await LogSessionActivity(sessionManager, victim);
        var attackerSession = await LogSessionActivity(sessionManager, attacker);

        Assert.Throws<SecurityException>(() => sessionManager.RemoveAdditionalUser(attackerSession.Id, victimSession.Id, attacker.Id));
    }

    [Fact]
    public async Task ReportCapabilities_Should_ThrowSecurityException_WhenReportingForAnotherUsersSession()
    {
        var victim = new User("victim", "default", "default");
        var attacker = new User("attacker", "default", "default");
        await using var sessionManager = CreateSessionManager(victim, attacker);

        var victimSession = await LogSessionActivity(sessionManager, victim);
        var attackerSession = await LogSessionActivity(sessionManager, attacker);

        Assert.Throws<SecurityException>(() => sessionManager.ReportCapabilities(attackerSession.Id, victimSession.Id, new ClientCapabilities()));
    }

    [Theory]
    [InlineData("start", VideoPlaybackStatus.LimitReached)]
    [InlineData("progress", VideoPlaybackStatus.LimitReached)]
    [InlineData("progress-no-item", VideoPlaybackStatus.LimitReached)]
    [InlineData("automated", VideoPlaybackStatus.Success)]
    [InlineData("untrusted", VideoPlaybackStatus.Success)]
    [InlineData("wrong-owner", VideoPlaybackStatus.Success)]
    public async Task PlaybackReports_RenewOnlyRealClientActivity(string reportKind, VideoPlaybackStatus expectedAdmission)
    {
        var clock = new ManualTimeProvider();
        var timeout = TimeSpan.FromMinutes(10);
        var coordinator = new VideoPlaybackCoordinator(clock, timeout);
        var user = new User("user", "default", "default");
        await using var sessionManager = CreateSessionManager(coordinator, user);
        var session = await LogSessionActivity(sessionManager, user);
        var itemId = Guid.NewGuid();
        var playback = new VideoPlaybackContext(user.Id, PlaybackOwnerId, itemId, "playback");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, "encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(playback, "encoding", 1, out var operation));
        using (var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(operation))
        {
            request.Acknowledge();
        }

        clock.Advance(timeout / 2);
        var info = CreatePlaybackInfo(session.Id, itemId, "encoding");
        if (reportKind == "progress-no-item")
        {
            info.ItemId = Guid.Empty;
            info.Item = null;
        }

        if (reportKind == "start")
        {
            await sessionManager.OnPlaybackStart(info, PlaybackOwnerId);
        }
        else
        {
            var ownerId = reportKind switch
            {
                "untrusted" => null,
                "wrong-owner" => "other-token-owner",
                _ => PlaybackOwnerId
            };
            await sessionManager.OnPlaybackProgress(info, reportKind == "automated", ownerId);
        }

        clock.Advance((timeout / 2) + TimeSpan.FromTicks(1));
        coordinator.RemoveExpired();
        var next = playback with { PlaybackInstanceId = "next" };
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(next, "next-encoding"));
        Assert.Equal(expectedAdmission, coordinator.TryAcquire(next, "next-encoding", 1, out var nextOperation));
        nextOperation?.Dispose();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task OnPlaybackStopped_ReleasesExactOrCanonicalLegacyAdmission(bool legacy, bool omitItemId)
    {
        var coordinator = new VideoPlaybackCoordinator(new ManualTimeProvider(), TimeSpan.FromMinutes(10));
        var user = new User("user", "default", "default");
        await using var sessionManager = CreateSessionManager(coordinator, user);
        var session = await LogSessionActivity(sessionManager, user);
        var itemId = Guid.NewGuid();
        var playSessionId = Guid.NewGuid().ToString("N");
        var instanceId = legacy ? VideoPlaybackContext.CreateLegacyId(user.Id, PlaybackOwnerId, itemId) : "playback";
        var playback = new VideoPlaybackContext(user.Id, PlaybackOwnerId, itemId, instanceId);
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, playSessionId));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(playback, playSessionId, 1, out var operation));
        using (var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(operation))
        {
            request.Acknowledge();
        }

        await sessionManager.OnPlaybackStopped(
            new PlaybackStopInfo
            {
                SessionId = session.Id,
                ItemId = omitItemId ? Guid.Empty : itemId,
                PlaySessionId = legacy ? null : playSessionId,
                Item = omitItemId ? null : new BaseItemDto { Id = itemId, MediaType = MediaType.Video }
            },
            PlaybackOwnerId);

        var next = playback with { PlaybackInstanceId = "next" };
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(next, "next-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(next, "next-encoding", 1, out var nextOperation));
        nextOperation?.Dispose();
    }

    [Fact]
    public async Task OnPlaybackStopped_WithoutIdentifierDoesNotReleaseModernAdmission()
    {
        var coordinator = new VideoPlaybackCoordinator(new ManualTimeProvider(), TimeSpan.FromMinutes(10));
        var user = new User("user", "default", "default");
        await using var sessionManager = CreateSessionManager(coordinator, user);
        var session = await LogSessionActivity(sessionManager, user);
        var itemId = Guid.NewGuid();
        var playback = new VideoPlaybackContext(user.Id, PlaybackOwnerId, itemId, "modern");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, "encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(playback, "encoding", 1, out var operation));
        using (var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(operation))
        {
            request.Acknowledge();
        }

        await sessionManager.OnPlaybackStopped(new PlaybackStopInfo { SessionId = session.Id, ItemId = itemId }, PlaybackOwnerId);

        var next = playback with { PlaybackInstanceId = "next" };
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(next, "next-encoding"));
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(next, "next-encoding", 1, out _));
    }

    [Fact]
    public async Task OnPlaybackStopped_MetadataOnlyCancellationCleansItsLiveInput()
    {
        var coordinator = new VideoPlaybackCoordinator(new ManualTimeProvider(), TimeSpan.FromMinutes(10));
        var sources = new Mock<IMediaSourceManager>();
        sources.Setup(source => source.CloseLiveStream("metadata-live")).Returns(Task.CompletedTask);
        var user = new User("user", "default", "default");
        await using var sessionManager = CreateSessionManager(coordinator, sources.Object, user);
        var session = await LogSessionActivity(sessionManager, user);
        var itemId = Guid.NewGuid();
        var playback = new VideoPlaybackContext(user.Id, PlaybackOwnerId, itemId, "metadata");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, "encoding"));

        await sessionManager.OnPlaybackStopped(
            new PlaybackStopInfo
            {
                SessionId = session.Id,
                ItemId = itemId,
                PlaySessionId = "encoding",
                LiveStreamId = "metadata-live"
            },
            PlaybackOwnerId);

        sources.Verify(source => source.CloseLiveStream("metadata-live"), Times.Once);
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.ResolvePlaySession(user.Id, PlaybackOwnerId, itemId, "encoding", out _));
    }

    [Fact]
    public async Task OnPlaybackStopped_NegativeCurrentVideoPositionDoesNotRevokeOrCloseInput()
    {
        var coordinator = new VideoPlaybackCoordinator(new ManualTimeProvider(), TimeSpan.FromMinutes(10));
        var sources = new Mock<IMediaSourceManager>();
        var user = new User("user", "default", "default");
        await using var sessionManager = CreateSessionManager(coordinator, sources.Object, user);
        var session = await LogSessionActivity(sessionManager, user);
        var itemId = Guid.NewGuid();
        var playback = new VideoPlaybackContext(user.Id, PlaybackOwnerId, itemId, "playback");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, "encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(playback, "encoding", 1, out var operation));
        using (var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(operation))
        {
            request.Acknowledge();
        }

        var info = CreatePlaybackInfo(session.Id, itemId, "encoding");
        info.LiveStreamId = "shared-live";
        await sessionManager.OnPlaybackStart(info, PlaybackOwnerId);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sessionManager.OnPlaybackStopped(
            new PlaybackStopInfo
            {
                SessionId = session.Id,
                ItemId = itemId,
                PlaySessionId = "encoding",
                PositionTicks = -1,
                LiveStreamId = "shared-live"
            },
            PlaybackOwnerId));

        Assert.Same(info.Item, session.NowPlayingItem);
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(playback, "encoding"));
        sources.Verify(source => source.CloseLiveStream(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(100L)]
    [InlineData(-1L)]
    public async Task OnPlaybackStopped_LateGenerationDoesNotClearOrCloseCurrentVideo(long? positionTicks)
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, TimeSpan.FromMinutes(10));
        var sources = new Mock<IMediaSourceManager>();
        var user = new User("user", "default", "default");
        await using var sessionManager = CreateSessionManager(coordinator, sources.Object, user);
        var session = await LogSessionActivity(sessionManager, user);
        var itemId = Guid.NewGuid();
        var playback = new VideoPlaybackContext(user.Id, PlaybackOwnerId, itemId, "playback");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, "old-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(playback, "old-encoding", 1, out var originalOperation));
        using (var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(originalOperation))
        {
            request.Acknowledge();
        }

        var original = CreatePlaybackInfo(session.Id, itemId, "old-encoding");
        original.LiveStreamId = "shared-live";
        await sessionManager.OnPlaybackStart(original, PlaybackOwnerId);
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, "new-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(playback, "new-encoding", 1, out var replacementOperation));
        using (var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(replacementOperation))
        {
            request.Acknowledge();
        }

        var replacement = CreatePlaybackInfo(session.Id, itemId, "new-encoding");
        replacement.LiveStreamId = "shared-live";
        await sessionManager.OnPlaybackStart(replacement, PlaybackOwnerId);
        var currentState = session.PlayState;
        clock.Advance(TimeSpan.FromMinutes(1));

        await sessionManager.OnPlaybackStopped(
            new PlaybackStopInfo
            {
                SessionId = session.Id,
                ItemId = itemId,
                PlaySessionId = "old-encoding",
                PositionTicks = positionTicks,
                LiveStreamId = "shared-live"
            },
            PlaybackOwnerId);

        Assert.Same(replacement.Item, session.NowPlayingItem);
        Assert.Same(currentState, session.PlayState);
        Assert.Equal("shared-live", session.PlayState.LiveStreamId);
        sources.Verify(source => source.CloseLiveStream(It.IsAny<string>()), Times.Never);
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(playback, "new-encoding"));
        var other = playback with { PlaybackInstanceId = "other" };
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(other, "other-encoding"));
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(other, "other-encoding", 1, out _));
    }

    [Fact]
    public async Task OnPlaybackStopped_NegativeUnregisteredPositionStillClosesLiveInput()
    {
        var coordinator = new VideoPlaybackCoordinator(new ManualTimeProvider(), TimeSpan.FromMinutes(10));
        var sources = new Mock<IMediaSourceManager>();
        sources.Setup(source => source.CloseLiveStream("audio-live")).Returns(Task.CompletedTask);
        var user = new User("user", "default", "default");
        await using var sessionManager = CreateSessionManager(coordinator, sources.Object, user);
        var session = await LogSessionActivity(sessionManager, user);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sessionManager.OnPlaybackStopped(
            new PlaybackStopInfo
            {
                SessionId = session.Id,
                ItemId = Guid.NewGuid(),
                PlaySessionId = "unregistered-audio",
                PositionTicks = -1,
                LiveStreamId = "audio-live"
            },
            PlaybackOwnerId));

        sources.Verify(source => source.CloseLiveStream("audio-live"), Times.Once);
    }

    [Fact]
    public async Task OnPlaybackStart_DoesNotAdmitVideoFromClientReportAlone()
    {
        var coordinator = new VideoPlaybackCoordinator(new ManualTimeProvider(), TimeSpan.FromMinutes(10));
        var user = new User("user", "default", "default");
        await using var sessionManager = CreateSessionManager(coordinator, user);
        var session = await LogSessionActivity(sessionManager, user);
        var itemId = Guid.NewGuid();
        var playback = new VideoPlaybackContext(user.Id, PlaybackOwnerId, itemId, "reported");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, "encoding"));

        await sessionManager.OnPlaybackStart(CreatePlaybackInfo(session.Id, itemId, "encoding"), PlaybackOwnerId);

        var next = playback with { PlaybackInstanceId = "next" };
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(next, "next-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(next, "next-encoding", 1, out var operation));
        operation?.Dispose();
    }

    private static PlaybackStartInfo CreatePlaybackInfo(string sessionId, Guid itemId, string playSessionId)
        => new()
        {
            SessionId = sessionId,
            ItemId = itemId,
            PlaySessionId = playSessionId,
            Item = new BaseItemDto { Id = itemId, MediaType = MediaType.Video },
            IsPaused = true
        };

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager(params User[] users)
        => CreateSessionManager(Mock.Of<IVideoPlaybackCoordinator>(), users);

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager(IVideoPlaybackCoordinator coordinator, params User[] users)
        => CreateSessionManager(coordinator, Mock.Of<IMediaSourceManager>(), users);

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager(IVideoPlaybackCoordinator coordinator, IMediaSourceManager mediaSourceManager, params User[] users)
    {
        var userManager = new Mock<IUserManager>();
        foreach (var user in users)
        {
            userManager.Setup(i => i.GetUserById(user.Id)).Returns(user);
        }

        return new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(config => config.Configuration == new ServerConfiguration()),
            Mock.Of<ILibraryManager>(),
            userManager.Object,
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            mediaSourceManager,
            Mock.Of<IHostApplicationLifetime>(),
            coordinator);
    }

    // All sessions are logged with the same client and device id on purpose, those values are taken
    // from the request headers and are not bound to the access token of the calling user.
    private static Task<SessionInfo> LogSessionActivity(ISessionManager sessionManager, User user)
        => sessionManager.LogSessionActivity("Jellyfin Web", "1.0.0", "victim-tv-01", "device_name", "127.0.0.1", user);

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
    }
}
