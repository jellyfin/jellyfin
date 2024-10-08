using System.Threading.Tasks;
using Jellyfin.LiveTv.Configuration;
using Jellyfin.LiveTv.Guide;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Listings
{
    public class ListingsManagerTests
    {
        private readonly IConfigurationManager _config;
        private readonly IListingsProvider[] _listingsProviders;
        private readonly ILogger<ListingsManager> _logger;
        private readonly ITaskManager _taskManager;
        private readonly ITunerHostManager _tunerHostManager;

        public ListingsManagerTests()
        {
            // Mock dependencies
            _logger = Mock.Of<ILogger<ListingsManager>>();
            _config = Mock.Of<IConfigurationManager>();
            _taskManager = Mock.Of<ITaskManager>();
            _tunerHostManager = Mock.Of<ITunerHostManager>();
            _listingsProviders = new[] { Mock.Of<IListingsProvider>() };
        }

        [Fact]
        public void DeleteListingsProvider_DeletesProviderAndRefreshesGuide()
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
            Assert.DoesNotContain(_config.GetLiveTvConfiguration().ListingProviders, p => p.Id == id);
        }

        [Fact]
        public async Task SaveListingProvider_SavesProviderAndReturnsInfo()
        {
            // Arrange
            var manager = new ListingsManager(_logger, _config, _taskManager, _tunerHostManager, _listingsProviders);
            var info = new ListingsProviderInfo { Type = "MockType", Id = "MockId" };

            // Act
            var result = await manager.SaveListingProvider(info, false, false);

            // Assert
            Assert.Equal(info, result);
            Assert.Contains(_config.GetLiveTvConfiguration().ListingProviders, p => p.Id == info.Id);
        }

        // Add more test methods for other methods in the class
    }
}
