using System;
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
    private readonly IWritableOptions<LiveTvOptions> _config;
    private readonly LiveTvOptions _options;
    private readonly IListingsProvider[] _listingsProviders;
    private readonly ILogger<ListingsManager> _logger;
    private readonly ITaskManager _taskManager;
    private readonly ITunerHostManager _tunerHostManager;

    public ListingsManagerTests()
    {
        _logger = Mock.Of<ILogger<ListingsManager>>();
        _options = new LiveTvOptions();
        var configMock = new Mock<IWritableOptions<LiveTvOptions>>();
        configMock.SetupGet(x => x.Value).Returns(_options);
        configMock.Setup(x => x.Update(It.IsAny<Action<LiveTvOptions>>()))
            .Callback<Action<LiveTvOptions>>(update => update(_options));
        _config = configMock.Object;
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
        _options.ListingProviders = [new ListingsProviderInfo { Id = id }];

        // Act
        manager.DeleteListingsProvider(id);

        // Assert
        Assert.DoesNotContain(
            _options.ListingProviders,
            p => p.Id.Equals(id, StringComparison.Ordinal));
    }
}
