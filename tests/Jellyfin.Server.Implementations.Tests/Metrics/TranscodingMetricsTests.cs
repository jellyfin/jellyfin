using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

public class TranscodingMetricsTests
{
    [Fact]
    public async Task CollectAsync_WithHardwareAndSoftwareTranscodes_CompletesWithoutError()
    {
        var sessionManager = new Mock<ISessionManager>();
        var hardware = new SessionInfo(sessionManager.Object, NullLogger.Instance)
        {
            TranscodingInfo = new TranscodingInfo
            {
                HardwareAccelerationType = HardwareAccelerationType.nvenc,
                Bitrate = 4_000_000,
            },
        };
        var software = new SessionInfo(sessionManager.Object, NullLogger.Instance)
        {
            TranscodingInfo = new TranscodingInfo
            {
                HardwareAccelerationType = null,
                Bitrate = 2_000_000,
            },
        };
        var notTranscoding = new SessionInfo(sessionManager.Object, NullLogger.Instance);
        sessionManager.SetupGet(m => m.Sessions).Returns(new[] { hardware, software, notTranscoding });

        var collector = new TranscodingMetrics(sessionManager.Object);
        await collector.CollectAsync(CancellationToken.None);

        sessionManager.VerifyGet(m => m.Sessions, Times.Once);
    }

    [Fact]
    public async Task CollectAsync_WithNoActiveTranscodes_DoesNotThrow()
    {
        var sessionManager = new Mock<ISessionManager>();
        sessionManager.SetupGet(m => m.Sessions).Returns(new List<SessionInfo>());

        var collector = new TranscodingMetrics(sessionManager.Object);
        await collector.CollectAsync(CancellationToken.None);
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        var collector = new TranscodingMetrics(Mock.Of<ISessionManager>());

        Assert.Equal(nameof(TranscodingMetrics), collector.Name);
    }
}
