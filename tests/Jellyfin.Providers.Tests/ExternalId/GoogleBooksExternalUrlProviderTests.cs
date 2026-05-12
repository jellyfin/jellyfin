using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.GoogleBooks;
using Xunit;

namespace Jellyfin.Providers.Tests.ExternalId
{
    public sealed class GoogleBooksExternalUrlProviderTests
    {
        private readonly GoogleBooksExternalUrlProvider _provider = new();

        [Fact]
        public void GetExternalUrls_BookWithGoogleBooksId_ReturnsCorrectUrl()
        {
            var book = new Book();
            book.SetProviderId("GoogleBooks", "buc0AAAAMAAJ");

            var urls = _provider.GetExternalUrls(book);

            Assert.Contains("https://books.google.com/books?id=buc0AAAAMAAJ", urls);
        }

        [Fact]
        public void GetExternalUrls_BookWithNoGoogleBooksId_ReturnsNoUrl()
        {
            var book = new Book();

            var urls = _provider.GetExternalUrls(book);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_NonBookWithGoogleBooksId_ReturnsNoUrl()
        {
            var series = new Series();
            series.SetProviderId("GoogleBooks", "buc0AAAAMAAJ");

            var urls = _provider.GetExternalUrls(series);

            Assert.Empty(urls);
        }
    }
}
