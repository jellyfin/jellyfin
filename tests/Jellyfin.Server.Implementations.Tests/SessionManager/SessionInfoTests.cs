using System;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class SessionInfoTests
{
    [Fact]
    public async Task StartAutomaticProgress_SnapshotsClientReportedPosition()
    {
        await using var session = new SessionInfo(Mock.Of<ISessionManager>(), NullLogger.Instance);
        var progressInfo = new PlaybackProgressInfo
        {
            IsPaused = true,
            PositionTicks = 123456789
        };

        session.StartAutomaticProgress(progressInfo);

        Assert.Equal(progressInfo.PositionTicks, session.LastPlaybackCheckInPositionTicks);
    }

    [Fact]
    public async Task AutomaticProgress_AdvancesEstimatedPositionWithoutAdvancingSnapshot()
    {
        var sessionManager = new Mock<ISessionManager>();
        await using var session = new SessionInfo(sessionManager.Object, NullLogger.Instance);
        var automaticProgress = new TaskCompletionSource<long?>(TaskCreationOptions.RunContinuationsAsynchronously);
        const long reportedPositionTicks = 123456789;

        sessionManager
            .Setup(manager => manager.OnPlaybackProgress(It.IsAny<PlaybackProgressInfo>(), true))
            .Callback<PlaybackProgressInfo, bool>((info, _) =>
            {
                session.PlayState.PositionTicks = info.PositionTicks;
                automaticProgress.TrySetResult(info.PositionTicks);
            })
            .Returns(Task.CompletedTask);
        session.PlayState.PositionTicks = reportedPositionTicks;

        session.StartAutomaticProgress(new PlaybackProgressInfo
        {
            PositionTicks = reportedPositionTicks
        });

        var estimatedPositionTicks = await automaticProgress.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        session.StopAutomaticProgress();

        Assert.Equal(reportedPositionTicks + TimeSpan.TicksPerSecond, estimatedPositionTicks);
        Assert.Equal(estimatedPositionTicks, session.PlayState.PositionTicks);
        Assert.Equal(reportedPositionTicks, session.LastPlaybackCheckInPositionTicks);
    }

    [Fact]
    public async Task StartAutomaticProgress_ReplacesSnapshotOnLaterClientReport()
    {
        await using var session = new SessionInfo(Mock.Of<ISessionManager>(), NullLogger.Instance);
        session.StartAutomaticProgress(new PlaybackProgressInfo
        {
            IsPaused = true,
            PositionTicks = 123456789
        });

        session.StartAutomaticProgress(new PlaybackProgressInfo
        {
            IsPaused = true,
            PositionTicks = 987654321
        });

        Assert.Equal(987654321, session.LastPlaybackCheckInPositionTicks);
    }

    [Fact]
    public async Task StartAutomaticProgress_PreservesExactPausedPosition()
    {
        await using var session = new SessionInfo(Mock.Of<ISessionManager>(), NullLogger.Instance);
        var pausedProgress = new PlaybackProgressInfo
        {
            IsPaused = true,
            PositionTicks = 314159265
        };

        session.StartAutomaticProgress(pausedProgress);

        Assert.Equal(pausedProgress.PositionTicks, session.LastPlaybackCheckInPositionTicks);
    }
}
