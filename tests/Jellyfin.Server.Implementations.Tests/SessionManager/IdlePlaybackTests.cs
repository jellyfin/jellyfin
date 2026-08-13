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
    [InlineData(null, 0)]
    [InlineData(123456789L, 123456789L)]
    public async Task CheckForIdlePlayback_StopsAtLastClientReportedPosition(long? clientPositionTicks, long expectedPositionTicks)
    {
        var playbackStopped = new TaskCompletionSource<long?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventManager = new Mock<IEventManager>();
        eventManager
            .Setup(manager => manager.PublishAsync(It.IsAny<PlaybackStopEventArgs>()))
            .Callback<PlaybackStopEventArgs>(eventArgs => playbackStopped.TrySetResult(eventArgs.PlaybackPositionTicks))
            .Returns(Task.CompletedTask);
        await using var sessionManager = new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            eventManager.Object,
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
            Mock.Of<IHostApplicationLifetime>());
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

        var idlePlaybackCallback = typeof(Emby.Server.Implementations.Session.SessionManager)
            .GetMethod("CheckForIdlePlayback", BindingFlags.Instance | BindingFlags.NonPublic)!;
        idlePlaybackCallback.Invoke(sessionManager, new object?[] { null });

        var stoppedPositionTicks = await playbackStopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(expectedPositionTicks, stoppedPositionTicks);
    }
}
