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

        [Theory]
        [InlineData("")]
        [InlineData("originalAirDate")]
        public void GetExternalUrls_SeasonWithOwnTmdbId_ReturnsIdUrl(string displayOrder)
        {
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = displayOrder };
            series.SetProviderId(MetadataProvider.Tmdb, "1399");

            var season = new Season { IndexNumber = 3, SeriesId = series.Id };
            season.SetProviderId(MetadataProvider.Tmdb, "3627");
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Equal([TmdbUtils.BaseTmdbUrl + "tv/season/3627"], urls);
        }

        [Theory]
        [InlineData("")]
        [InlineData("originalAirDate")]
        [InlineData("OriginalAirDate")]
        public void GetExternalUrls_SeasonInAirDateOrderWithoutOwnTmdbId_ReturnsNumberedUrl(string displayOrder)
        {
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = displayOrder };
            series.SetProviderId(MetadataProvider.Tmdb, "1399");

            var season = new Season { IndexNumber = 3, SeriesId = series.Id };
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Equal([TmdbUtils.BaseTmdbUrl + "tv/1399/season/3"], urls);
        }

        [Theory]
        [InlineData("absolute")]
        [InlineData("dvd")]
        [InlineData("digital")]
        [InlineData("storyArc")]
        [InlineData("production")]
        [InlineData("tv")]
        public void GetExternalUrls_SeasonInGroupOrderWithGroupIds_ReturnsGroupUrl(string displayOrder)
        {
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = displayOrder };
            series.SetProviderId(MetadataProvider.Tmdb, "46260");
            series.SetProviderId(TmdbUtils.EpisodeGroupProviderKey, "5f24dcdc869e75003658360e");

            var season = new Season { IndexNumber = 2, SeriesId = series.Id };
            season.SetProviderId(TmdbUtils.EpisodeGroupProviderKey, "652da673024ec800aeccf025");
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Equal(
                [TmdbUtils.BaseTmdbUrl + "tv/46260/episode_group/5f24dcdc869e75003658360e/group/652da673024ec800aeccf025"],
                urls);
        }

        [Fact]
        public void GetExternalUrls_SeasonInGroupOrderWithoutGroupIds_ReturnsNoUrl()
        {
            // The season number belongs to the episode group, so it cannot be used against the series.
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = "tv" };
            series.SetProviderId(MetadataProvider.Tmdb, "46260");

            var season = new Season { IndexNumber = 3, SeriesId = series.Id };
            season.SetProviderId(MetadataProvider.Tmdb, "3627");
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Empty(urls);
        }

        [Fact]
        public void GetExternalUrls_SeasonWithStaleGroupIdAfterSwitchingToAiredOrder_ReturnsSeasonUrl()
        {
            // Provider ids are only ever merged, so a group id from a previous order can outlive it.
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = string.Empty };
            series.SetProviderId(MetadataProvider.Tmdb, "46260");
            series.SetProviderId(TmdbUtils.EpisodeGroupProviderKey, "5f24dcdc869e75003658360e");

            var season = new Season { IndexNumber = 2, SeriesId = series.Id };
            season.SetProviderId(MetadataProvider.Tmdb, "3627");
            season.SetProviderId(TmdbUtils.EpisodeGroupProviderKey, "652da673024ec800aeccf025");
            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);

            var urls = _provider.GetExternalUrls(season);

            Assert.Equal([TmdbUtils.BaseTmdbUrl + "tv/season/3627"], urls);
        }

        [Fact]
        public void GetExternalUrls_SeriesInGroupOrder_ReturnsSeriesAndGroupUrl()
        {
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = "tv" };
            series.SetProviderId(MetadataProvider.Tmdb, "46260");
            series.SetProviderId(TmdbUtils.EpisodeGroupProviderKey, "5f24dcdc869e75003658360e");

            var urls = _provider.GetExternalUrls(series);

            Assert.Equal(
                [
                    TmdbUtils.BaseTmdbUrl + "tv/46260",
                    TmdbUtils.BaseTmdbUrl + "tv/46260/episode_group/5f24dcdc869e75003658360e"
                ],
                urls);
        }

        [Theory]
        [InlineData("")]
        [InlineData("originalAirDate")]
        [InlineData("tv")]
        [InlineData("digital")]
        public void GetExternalUrls_EpisodeWithOwnTmdbId_ReturnsIdUrl(string displayOrder)
        {
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = displayOrder };
            series.SetProviderId(MetadataProvider.Tmdb, "46260");

            var season = new Season { Id = Guid.NewGuid(), IndexNumber = 3, SeriesId = series.Id };

            var episode = new Episode { IndexNumber = 1, SeasonId = season.Id, SeriesId = series.Id };
            episode.SetProviderId(MetadataProvider.Tmdb, "1104330");

            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);
            _libraryManagerMock.Setup(m => m.GetItemById(season.Id)).Returns(season);

            var urls = _provider.GetExternalUrls(episode);

            Assert.Equal([TmdbUtils.BaseTmdbUrl + "tv/episode/1104330"], urls);
        }

        [Theory]
        [InlineData("")]
        [InlineData("originalAirDate")]
        [InlineData("OriginalAirDate")]
        public void GetExternalUrls_EpisodeInAirDateOrderWithoutOwnTmdbId_ReturnsNumberedUrl(string displayOrder)
        {
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = displayOrder };
            series.SetProviderId(MetadataProvider.Tmdb, "1399");

            var season = new Season { Id = Guid.NewGuid(), IndexNumber = 2, SeriesId = series.Id };
            var episode = new Episode { IndexNumber = 5, SeasonId = season.Id, SeriesId = series.Id };

            _libraryManagerMock.Setup(m => m.GetItemById(series.Id)).Returns(series);
            _libraryManagerMock.Setup(m => m.GetItemById(season.Id)).Returns(season);

            var urls = _provider.GetExternalUrls(episode);

            Assert.Equal([TmdbUtils.BaseTmdbUrl + "tv/1399/season/2/episode/5"], urls);
        }

        [Theory]
        [InlineData("absolute")]
        [InlineData("dvd")]
        [InlineData("digital")]
        [InlineData("storyArc")]
        [InlineData("production")]
        [InlineData("tv")]
        public void GetExternalUrls_EpisodeInGroupOrderWithoutOwnTmdbId_ReturnsNoUrl(string displayOrder)
        {
            var series = new Series { Id = Guid.NewGuid(), DisplayOrder = displayOrder };
            series.SetProviderId(MetadataProvider.Tmdb, "46260");

            var season = new Season { Id = Guid.NewGuid(), IndexNumber = 3, SeriesId = series.Id };
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
