using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.TV;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class SeriesResolverTests
    {
        private static readonly NamingOptions _namingOptions = new();
        private readonly SeriesResolver _resolver;
        private readonly Mock<ILibraryManager> _libraryManagerMock;

        public SeriesResolverTests()
        {
            _libraryManagerMock = new Mock<ILibraryManager>();
            // Return null so that configuredContentType != CollectionType.tvshows, allowing series resolution.
            _libraryManagerMock
                .Setup(m => m.GetConfiguredContentType(It.IsAny<string>()))
                .Returns((CollectionType?)null);

            _resolver = new SeriesResolver(Mock.Of<ILogger<SeriesResolver>>(), _namingOptions);
        }

        private MediaBrowser.Controller.Library.ItemResolveArgs MakeTvArgs(string path) =>
            new(Mock.Of<IServerApplicationPaths>(), _libraryManagerMock.Object)
            {
                CollectionType = CollectionType.tvshows,
                FileSystemChildren = [],
                FileInfo = new FileSystemMetadata
                {
                    FullName = path,
                    IsDirectory = true
                }
            };

        [Theory]
        [InlineData("/media/Show [tvdbid=12345]", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show [tvdbid-12345]", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show (tvdbid=12345)", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show [tvmazeid=67890]", MetadataProvider.TvMaze, "67890")]
        [InlineData("/media/Show [tvmazeid-67890]", MetadataProvider.TvMaze, "67890")]
        [InlineData("/media/Show [tmdbid=99999]", MetadataProvider.Tmdb, "99999")]
        [InlineData("/media/Show [tmdbid-99999]", MetadataProvider.Tmdb, "99999")]
        [InlineData("/media/Show [imdbid=tt1234567]", MetadataProvider.Imdb, "tt1234567")]
        [InlineData("/media/Show [imdbid-tt1234567]", MetadataProvider.Imdb, "tt1234567")]
        public void ResolvePath_SeriesFolderWithProviderId_SetsProviderId(string path, MetadataProvider provider, string expectedId)
        {
            var series = _resolver.ResolvePath(MakeTvArgs(path)) as Series;

            Assert.NotNull(series);
            Assert.True(series.TryGetProviderId(provider, out var actualId));
            Assert.Equal(expectedId, actualId);
        }

        [Theory]
        [InlineData("/media/Show [anidbid=11111]", "AniDB", "11111")]
        [InlineData("/media/Show [anilistid=22222]", "AniList", "22222")]
        [InlineData("/media/Show [anisearchid=33333]", "AniSearch", "33333")]
        public void ResolvePath_SeriesFolderWithAniProviderId_SetsProviderId(string path, string providerKey, string expectedId)
        {
            var series = _resolver.ResolvePath(MakeTvArgs(path)) as Series;

            Assert.NotNull(series);
            Assert.True(series.TryGetProviderId(providerKey, out var actualId));
            Assert.Equal(expectedId, actualId);
        }

        [Fact]
        public void ResolvePath_SeriesFolderWithMultipleProviderIds_SetsAll()
        {
            var series = _resolver.ResolvePath(MakeTvArgs("/media/Show [tvdbid=12345][tmdbid=99999]")) as Series;

            Assert.NotNull(series);
            Assert.True(series.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbId));
            Assert.Equal("12345", tvdbId);
            Assert.True(series.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbId));
            Assert.Equal("99999", tmdbId);
        }

        [Fact]
        public void ResolvePath_SeriesFolderWithNoProviderId_HasNoProviderIds()
        {
            var series = _resolver.ResolvePath(MakeTvArgs("/media/Show")) as Series;

            Assert.NotNull(series);
            Assert.False(series.TryGetProviderId(MetadataProvider.Tvdb, out _));
            Assert.False(series.TryGetProviderId(MetadataProvider.TvMaze, out _));
            Assert.False(series.TryGetProviderId(MetadataProvider.Tmdb, out _));
            Assert.False(series.TryGetProviderId(MetadataProvider.Imdb, out _));
            Assert.False(series.TryGetProviderId("AniDB", out _));
            Assert.False(series.TryGetProviderId("AniList", out _));
            Assert.False(series.TryGetProviderId("AniSearch", out _));
        }

        [Fact]
        public void ResolvePath_SeriesFolderNotInTvShowsCollection_DoesNotResolve()
        {
            // Without CollectionType.tvshows, a plain folder with no tvshow.nfo and
            // no season/episode children should not resolve as a Series.
            var args = new MediaBrowser.Controller.Library.ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                _libraryManagerMock.Object)
            {
                CollectionType = null,
                FileSystemChildren = [],
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/Show [tvdbid=12345]",
                    IsDirectory = true
                }
            };

            Assert.Null(_resolver.ResolvePath(args));
        }
    }
}
