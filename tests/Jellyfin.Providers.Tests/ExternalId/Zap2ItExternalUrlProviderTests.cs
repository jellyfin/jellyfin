using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.TV;
using Xunit;

namespace Jellyfin.Providers.Tests.ExternalId
{
    public sealed class Zap2ItExternalUrlProviderTests
    {
        private readonly Zap2ItExternalUrlProvider _provider = new();

        [Fact]
        public void GetExternalUrls_ItemWithZap2ItId_ReturnsCorrectUrl()
        {
            var series = new Series();
            series.SetProviderId(MetadataProvider.Zap2It, "EP012345678901");

            var urls = _provider.GetExternalUrls(series);

            Assert.Contains("http://tvlistings.zap2it.com/overview.html?programSeriesId=EP012345678901", urls);
        }

        [Fact]
        public void GetExternalUrls_ItemWithNoZap2ItId_ReturnsNoUrl()
        {
            var series = new Series();

            var urls = _provider.GetExternalUrls(series);

            Assert.Empty(urls);
        }
    }
}
