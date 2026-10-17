using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Books.Isbn;
using Xunit;

namespace Jellyfin.Providers.Tests.ExternalId
{
    public sealed class IsbnExternalUrlProviderTests
    {
        private readonly IsbnExternalUrlProvider _provider = new();

        [Fact]
        public void GetExternalUrls_BookWithIsbnId_ReturnsCorrectUrl()
        {
            var book = new Book();
            book.SetProviderId("ISBN", "9780306406157");

            var urls = _provider.GetExternalUrls(book);

            Assert.Contains("https://search.worldcat.org/search?q=bn:9780306406157", urls);
        }

        [Fact]
        public void GetExternalUrls_BookWithNoIsbnId_ReturnsNoUrl()
        {
            var book = new Book();

            var urls = _provider.GetExternalUrls(book);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_NonBookWithIsbnId_ReturnsNoUrl()
        {
            var series = new Series();
            series.SetProviderId("ISBN", "9780306406157");

            var urls = _provider.GetExternalUrls(series);

            Assert.Empty(urls);
        }
    }
}
