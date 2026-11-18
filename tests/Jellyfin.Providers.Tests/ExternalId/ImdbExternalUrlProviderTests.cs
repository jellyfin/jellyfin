using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Movies;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.ExternalId
{
    // put tests that mock the static LibraryManager in the same collection to avoid test interference
    [Collection("LibraryManagerTests")]
    public sealed class ImdbExternalUrlProviderTests : IDisposable
    {
        private readonly ImdbExternalUrlProvider _provider = new();
        private readonly Mock<ILibraryManager> _libraryManagerMock = new();
        private readonly ILibraryManager? _previousLibraryManager;

        public ImdbExternalUrlProviderTests()
        {
            _previousLibraryManager = BaseItem.LibraryManager;
            BaseItem.LibraryManager = _libraryManagerMock.Object;
        }

        public void Dispose()
        {
            BaseItem.LibraryManager = _previousLibraryManager;
        }

        [Fact]
        public void GetExternalUrls_MovieWithImdbId_ReturnsCorrectUrl()
        {
            var movie = new Movie();
            movie.SetProviderId(MetadataProvider.Imdb, "tt1234567");

            var urls = _provider.GetExternalUrls(movie);

            Assert.Contains("https://www.imdb.com/title/tt1234567", urls);
        }

        [Fact]
        public void GetExternalUrls_SeriesWithImdbId_ReturnsCorrectUrl()
        {
            var series = new Series();
            series.SetProviderId(MetadataProvider.Imdb, "tt7654321");

            var urls = _provider.GetExternalUrls(series);

            Assert.Contains("https://www.imdb.com/title/tt7654321", urls);
        }

        [Fact]
        public void GetExternalUrls_EpisodeWithImdbId_ReturnsCorrectUrl()
        {
            var episode = new Episode();
            episode.SetProviderId(MetadataProvider.Imdb, "tt9999999");

            var urls = _provider.GetExternalUrls(episode);

            Assert.Contains("https://www.imdb.com/title/tt9999999", urls);
        }

        [Fact]
        public void GetExternalUrls_SeasonWithSeriesImdbId_ReturnsSeasonEpisodesUrl()
        {
            var series = new Series { Id = Guid.NewGuid() };
            series.SetProviderId(MetadataProvider.Imdb, "tt1234567");

            var season = new Season { IndexNumber = 2, SeriesId = series.Id };
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Contains("https://www.imdb.com/title/tt1234567/episodes/?season=2", urls);
        }

        [Fact]
        public void GetExternalUrls_SeasonWithNoSeriesImdbId_ReturnsNoUrl()
        {
            var series = new Series { Id = Guid.NewGuid() };
            var season = new Season { IndexNumber = 1, SeriesId = series.Id };
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_SeasonWithNoIndexNumber_ReturnsNoUrl()
        {
            var series = new Series { Id = Guid.NewGuid() };
            series.SetProviderId(MetadataProvider.Imdb, "tt1234567");
            var season = new Season { IndexNumber = null, SeriesId = series.Id };
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_SeasonWithUnknownSeriesId_ReturnsNoUrl()
        {
            var season = new Season { IndexNumber = 1, SeriesId = Guid.NewGuid() };
            _libraryManagerMock.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns((BaseItem?)null);

            var urls = _provider.GetExternalUrls(season);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_ItemWithNoImdbId_ReturnsNoUrl()
        {
            var movie = new Movie();

            var urls = _provider.GetExternalUrls(movie);

            Assert.Empty(urls);
        }
    }
}
