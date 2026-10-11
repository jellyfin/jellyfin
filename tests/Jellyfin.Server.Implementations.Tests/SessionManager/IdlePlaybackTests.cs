using System;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class IdlePlaybackTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(123456789L, 123456789L)]
    public async Task CheckForIdlePlayback_StopsAtLastClientReportedPosition(long? clientPositionTicks, long? expectedPositionTicks)
    {
        var playbackStopped = new TaskCompletionSource<long?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventManager = new Mock<IEventManager>();
        eventManager
            .Setup(manager => manager.PublishAsync(It.IsAny<PlaybackStopEventArgs>()))
            .Callback<PlaybackStopEventArgs>(eventArgs => playbackStopped.TrySetResult(eventArgs.PlaybackPositionTicks))
            .Returns(Task.CompletedTask);
        await using var sessionManager = CreateSessionManager(eventManager.Object, Mock.Of<IMediaSourceManager>());
        var session = await sessionManager.LogSessionActivity(
            "Test Client",
            "1.0.0",
            "test-device",
            "Test Device",
            "127.0.0.1",
            null);
        session.NowPlayingItem = new BaseItemDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Item"
        };
        session.PlayState.PositionTicks = 987654321;

        if (clientPositionTicks.HasValue)
        {
            session.StartAutomaticProgress(new PlaybackProgressInfo
            {
                IsPaused = true,
                PositionTicks = clientPositionTicks
            });
            session.StopAutomaticProgress();
        }

        InvokeCheckForIdlePlayback(sessionManager);

        var stoppedPositionTicks = await playbackStopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(expectedPositionTicks, stoppedPositionTicks);
    }

    [Fact]
    public async Task CheckForIdlePlayback_ReleasesSharedLiveStream()
    {
        const string LiveStreamId = "live-stream";
        var playbackStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventManager = new Mock<IEventManager>();
        eventManager
            .Setup(manager => manager.PublishAsync(It.IsAny<PlaybackStopEventArgs>()))
            .Callback(() => playbackStopped.TrySetResult())
            .Returns(Task.CompletedTask);
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        await using var sessionManager = CreateSessionManager(eventManager.Object, mediaSourceManager.Object);
        var idleSession = await sessionManager.LogSessionActivity(
            "Test Client",
            "1.0.0",
            "idle-device",
            "Idle Device",
            "127.0.0.1",
            null);
        idleSession.NowPlayingItem = new BaseItemDto
        {
            Id = Guid.NewGuid(),
            Name = "Live Channel"
        };
        idleSession.PlayState.LiveStreamId = LiveStreamId;
        MapLiveStreamSession(sessionManager, LiveStreamId, idleSession.Id, "idle-play-session");
        MapLiveStreamSession(sessionManager, LiveStreamId, "watching-session", "watching-play-session");

        InvokeCheckForIdlePlayback(sessionManager);

        await playbackStopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        mediaSourceManager.Verify(manager => manager.CloseLiveStream(LiveStreamId), Times.Once);
    }

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager(
        IEventManager eventManager,
        IMediaSourceManager mediaSourceManager)
        => new(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            eventManager,
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            mediaSourceManager,
            Mock.Of<IHostApplicationLifetime>());

    private static void InvokeCheckForIdlePlayback(Emby.Server.Implementations.Session.SessionManager sessionManager)
        => typeof(Emby.Server.Implementations.Session.SessionManager)
            .GetMethod("CheckForIdlePlayback", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(sessionManager, new object?[] { null });

    private static void MapLiveStreamSession(
        Emby.Server.Implementations.Session.SessionManager sessionManager,
        string liveStreamId,
        string sessionId,
        string playSessionId)
        => typeof(Emby.Server.Implementations.Session.SessionManager)
            .GetMethod("UpdateLiveStreamActiveSessionMappings", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(sessionManager, new object?[] { liveStreamId, sessionId, playSessionId });
}
