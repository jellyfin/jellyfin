using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Configuration;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Listings;

public class ListingsManagerTests
{
    private readonly IConfigurationManager _config;
    private readonly IListingsProvider[] _listingsProviders;
    private readonly ILogger<ListingsManager> _logger;
    private readonly ITaskManager _taskManager;
    private readonly ITunerHostManager _tunerHostManager;

    public ListingsManagerTests()
    {
        _logger = Mock.Of<ILogger<ListingsManager>>();
        _config = Mock.Of<IConfigurationManager>();
        _taskManager = Mock.Of<ITaskManager>();
        _tunerHostManager = Mock.Of<ITunerHostManager>();
        _listingsProviders = new[] { Mock.Of<IListingsProvider>() };
    }

    [Fact]
    public void DeleteListingsProvider_DeletesProvider()
    {
        // Arrange
        var id = "MockId";
        var manager = new ListingsManager(_logger, _config, _taskManager, _tunerHostManager, _listingsProviders);

        Mock.Get(_config)
            .Setup(x => x.GetConfiguration(It.IsAny<string>()))
            .Returns(new LiveTvOptions { ListingProviders = [new ListingsProviderInfo { Id = id }] });

        // Act
        manager.DeleteListingsProvider(id);

        // Assert
        Assert.DoesNotContain(
            _config.GetLiveTvConfiguration().ListingProviders,
            p => p.Id.Equals(id, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetProgramsAsync_FirstProviderCannotListChannels_FallsBackToTheNext()
    {
        // Arrange
        var failing = new Mock<IListingsProvider>();
        failing.SetupGet(x => x.Type).Returns("failing");
        failing.Setup(x => x.GetChannels(It.IsAny<ListingsProviderInfo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("offline"));

        var working = new Mock<IListingsProvider>();
        working.SetupGet(x => x.Type).Returns("working");
        working.Setup(x => x.GetChannels(It.IsAny<ListingsProviderInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChannelInfo> { new() { Id = "5", Number = "5" } });
        working.Setup(x => x.GetProgramsAsync(It.IsAny<ListingsProviderInfo>(), "5", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ProgramInfo { Id = "p1" } });

        Mock.Get(_config)
            .Setup(x => x.GetConfiguration(It.IsAny<string>()))
            .Returns(new LiveTvOptions
            {
                ListingProviders =
                [
                    new ListingsProviderInfo { Id = "a", Type = "failing" },
                    new ListingsProviderInfo { Id = "b", Type = "working" }
                ]
            });

        var manager = new ListingsManager(_logger, _config, _taskManager, _tunerHostManager, [failing.Object, working.Object]);

        // Act
        var programs = await manager.GetProgramsAsync(
            new ChannelInfo { Id = "5", Number = "5", TunerHostId = "tuner" },
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        // Assert
        Assert.Single(programs);
    }
}
