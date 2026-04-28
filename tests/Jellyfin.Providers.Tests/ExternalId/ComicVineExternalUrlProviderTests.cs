using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.ComicVine;
using Xunit;

namespace Jellyfin.Providers.Tests.ExternalId
{
    public sealed class ComicVineExternalUrlProviderTests
    {
        private readonly ComicVineExternalUrlProvider _provider = new();

        [Fact]
        public void GetExternalUrls_PersonWithComicVineId_ReturnsCorrectUrl()
        {
            var person = new Person();
            person.SetProviderId("ComicVine", "person/4005-1234");

            var urls = _provider.GetExternalUrls(person);

            Assert.Contains("https://comicvine.gamespot.com/person/4005-1234", urls);
        }

        [Fact]
        public void GetExternalUrls_BookWithComicVineId_ReturnsCorrectUrl()
        {
            var book = new Book();
            book.SetProviderId("ComicVine", "issue/4000-5678");

            var urls = _provider.GetExternalUrls(book);

            Assert.Contains("https://comicvine.gamespot.com/issue/4000-5678", urls);
        }

        [Fact]
        public void GetExternalUrls_PersonWithNoComicVineId_ReturnsNoUrl()
        {
            var person = new Person();

            var urls = _provider.GetExternalUrls(person);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_NonSupportedItemWithComicVineId_ReturnsNoUrl()
        {
            var series = new Series();
            series.SetProviderId("ComicVine", "volume/4050-9999");

            var urls = _provider.GetExternalUrls(series);

            Assert.Empty(urls);
        }
    }
}
