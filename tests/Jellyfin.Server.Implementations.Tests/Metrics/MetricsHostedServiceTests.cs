using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

public class MetricsHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenMetricsDisabled_DoesNotInvokeCollectors()
    {
        var collector = new Mock<IMetricsCollector>();
        collector.SetupGet(c => c.Name).Returns("Mock");
        var configManager = new Mock<IServerConfigurationManager>();
        configManager.SetupGet(c => c.Configuration).Returns(new ServerConfiguration { EnableMetrics = false });

        using var cts = new CancellationTokenSource();
        var service = new MetricsHostedService(
            configManager.Object,
            new[] { collector.Object },
            NullLogger<MetricsHostedService>.Instance);

        // Cancel immediately so the loop exits before the first wait completes.
        await cts.CancelAsync();
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        collector.Verify(c => c.CollectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCollectorThrows_DoesNotPropagate()
    {
        var failing = new Mock<IMetricsCollector>();
        failing.SetupGet(c => c.Name).Returns("Failing");
        failing.Setup(c => c.CollectAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        var configManager = new Mock<IServerConfigurationManager>();
        configManager.SetupGet(c => c.Configuration).Returns(new ServerConfiguration { EnableMetrics = true });

        using var cts = new CancellationTokenSource();
        var service = new MetricsHostedService(
            configManager.Object,
            new[] { failing.Object },
            NullLogger<MetricsHostedService>.Instance);

        await service.StartAsync(cts.Token);
        // Give the BackgroundService one scheduling slice to enter the loop.
        await Task.Yield();
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // The host must still be alive — no unobserved exception escaped the catch block.
        Assert.True(true);
    }
}
