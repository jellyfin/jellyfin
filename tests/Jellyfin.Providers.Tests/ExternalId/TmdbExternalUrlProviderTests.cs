using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.Tmdb;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.ExternalId
{
    // put tests that mock the static LibraryManager in the same collection to avoid test interference
    [Collection("LibraryManagerTests")]
    public sealed class TmdbExternalUrlProviderTests : IDisposable
    {
        private readonly TmdbExternalUrlProvider _provider = new();
        private readonly Mock<ILibraryManager> _libraryManagerMock = new();
        private readonly ILibraryManager? _previousLibraryManager;

        public TmdbExternalUrlProviderTests()
        {
            _previousLibraryManager = BaseItem.LibraryManager;
            BaseItem.LibraryManager = _libraryManagerMock.Object;
        }

        public void Dispose()
        {
            BaseItem.LibraryManager = _previousLibraryManager;
        }

        [Fact]
        public void GetExternalUrls_SeriesWithTmdbId_ReturnsCorrectUrl()
        {
            var series = new Series();
            series.SetProviderId(MetadataProvider.Tmdb, "1399");

            var urls = _provider.GetExternalUrls(series);

            Assert.Contains(TmdbUtils.BaseTmdbUrl + "tv/1399", urls);
        }

        [Fact]
        public void GetExternalUrls_SeriesWithNoTmdbId_ReturnsNoUrl()
        {
            var series = new Series();

            var urls = _provider.GetExternalUrls(series);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_SeasonWithSeriesTmdbId_ReturnsCorrectUrl()
        {
            var series = new Series { Id = Guid.NewGuid() };
            series.SetProviderId(MetadataProvider.Tmdb, "1399");

            var season = new Season { IndexNumber = 3, SeriesId = series.Id };
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Contains(TmdbUtils.BaseTmdbUrl + "tv/1399/season/3", urls);
        }

        [Fact]
        public void GetExternalUrls_SeasonWithNoSeriesTmdbId_ReturnsNoUrl()
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
            series.SetProviderId(MetadataProvider.Tmdb, "1399");
            var season = new Season { IndexNumber = null, SeriesId = series.Id };
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_EpisodeWithSeriesTmdbId_ReturnsCorrectUrl()
        {
            var series = new Series { Id = Guid.NewGuid() };
            series.SetProviderId(MetadataProvider.Tmdb, "1399");

            var season = new Season { Id = Guid.NewGuid(), IndexNumber = 2, SeriesId = series.Id };

            var episode = new Episode
            {
                IndexNumber = 5,
                SeasonId = season.Id,
                SeriesId = series.Id
            };

            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);
            _libraryManagerMock.Setup(m => m.GetItemById(season.Id)).Returns(season);

            var urls = _provider.GetExternalUrls(episode);

            Assert.Contains(TmdbUtils.BaseTmdbUrl + "tv/1399/season/2/episode/5", urls);
        }

        [Fact]
        public void GetExternalUrls_EpisodeWithNoSeriesTmdbId_ReturnsNoUrl()
        {
            var series = new Series { Id = Guid.NewGuid() };
            var season = new Season { Id = Guid.NewGuid(), IndexNumber = 1, SeriesId = series.Id };
            var episode = new Episode { IndexNumber = 1, SeasonId = season.Id, SeriesId = series.Id };

            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);
            _libraryManagerMock.Setup(m => m.GetItemById(season.Id)).Returns(season);

            var urls = _provider.GetExternalUrls(episode);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_MovieWithTmdbId_ReturnsCorrectUrl()
        {
            var movie = new Movie();
            movie.SetProviderId(MetadataProvider.Tmdb, "550");

            var urls = _provider.GetExternalUrls(movie);

            Assert.Contains(TmdbUtils.BaseTmdbUrl + "movie/550", urls);
        }

        [Fact]
        public void GetExternalUrls_MovieWithNoTmdbId_ReturnsNoUrl()
        {
            var movie = new Movie();

            var urls = _provider.GetExternalUrls(movie);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_PersonWithTmdbId_ReturnsCorrectUrl()
        {
            var person = new Person();
            person.SetProviderId(MetadataProvider.Tmdb, "6384");

            var urls = _provider.GetExternalUrls(person);

            Assert.Contains(TmdbUtils.BaseTmdbUrl + "person/6384", urls);
        }

        [Fact]
        public void GetExternalUrls_PersonWithNoTmdbId_ReturnsNoUrl()
        {
            var person = new Person();

            var urls = _provider.GetExternalUrls(person);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_BoxSetWithTmdbId_ReturnsCorrectUrl()
        {
            var boxSet = new BoxSet();
            boxSet.SetProviderId(MetadataProvider.Tmdb, "10");

            var urls = _provider.GetExternalUrls(boxSet);

            Assert.Contains(TmdbUtils.BaseTmdbUrl + "collection/10", urls);
        }

        [Fact]
        public void GetExternalUrls_BoxSetWithNoTmdbId_ReturnsNoUrl()
        {
            var boxSet = new BoxSet();

            var urls = _provider.GetExternalUrls(boxSet);

            Assert.Empty(urls);
        }
    }
}
