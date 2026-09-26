using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

public class SessionMetricsTests
{
    [Fact]
    public async Task CollectAsync_WithMixedSessions_CompletesWithoutError()
    {
        var sessionManager = new Mock<ISessionManager>();
        var playing = new SessionInfo(sessionManager.Object, NullLogger.Instance) { Client = "Web", NowPlayingItem = new BaseItemDto() };
        playing.PlayState!.PlayMethod = PlayMethod.DirectPlay;
        var idle = new SessionInfo(sessionManager.Object, NullLogger.Instance) { Client = "Android" };
        var transcoding = new SessionInfo(sessionManager.Object, NullLogger.Instance) { Client = "AndroidTV", NowPlayingItem = new BaseItemDto(), TranscodingInfo = new TranscodingInfo() };
        transcoding.PlayState!.PlayMethod = PlayMethod.Transcode;
        sessionManager.SetupGet(m => m.Sessions).Returns(new[] { playing, idle, transcoding });

        var collector = new SessionMetrics(sessionManager.Object);
        await collector.CollectAsync(CancellationToken.None);

        sessionManager.VerifyGet(m => m.Sessions, Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithNoSessions_DoesNotThrow()
    {
        var sessionManager = new Mock<ISessionManager>();
        sessionManager.SetupGet(m => m.Sessions).Returns(new List<SessionInfo>());

        var collector = new SessionMetrics(sessionManager.Object);
        await collector.CollectAsync(CancellationToken.None);

        sessionManager.VerifyGet(m => m.Sessions, Times.Once);
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        var collector = new SessionMetrics(Mock.Of<ISessionManager>());

        Assert.Equal(nameof(SessionMetrics), collector.Name);
    }
}
