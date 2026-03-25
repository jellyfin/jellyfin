using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.TV;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class SeasonResolverTests
    {
        private static readonly NamingOptions _namingOptions = new();
        private readonly SeasonResolver _resolver;

        public SeasonResolverTests()
        {
            var localizationMock = new Mock<ILocalizationManager>();
            localizationMock
                .Setup(l => l.GetLocalizedString(It.IsAny<string>()))
                .Returns("Season {0}");

            _resolver = new SeasonResolver(
                _namingOptions,
                localizationMock.Object,
                Mock.Of<ILogger<SeasonResolver>>());
        }

        [Theory]
        [InlineData("/media/Show/Season 01 [tvdbid=12345]", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show/Season 01 [tvdbid-12345]", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show/Season 01 (tvdbid=12345)", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show/Season 02 [tvmazeid=67890]", MetadataProvider.TvMaze, "67890")]
        [InlineData("/media/Show/Season 02 [tvmazeid-67890]", MetadataProvider.TvMaze, "67890")]
        [InlineData("/media/Show/Season 03 [tmdbid=99999]", MetadataProvider.Tmdb, "99999")]
        [InlineData("/media/Show/Season 03 [tmdbid-99999]", MetadataProvider.Tmdb, "99999")]
        public void Resolve_SeasonFolderWithProviderId_SetsProviderId(string path, MetadataProvider provider, string expectedId)
        {
            var series = new Series { Path = "/media/Show" };

            var args = new MediaBrowser.Controller.Library.ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = series,
                LibraryOptions = new LibraryOptions(),
                FileInfo = new FileSystemMetadata
                {
                    FullName = path,
                    IsDirectory = true
                }
            };

            var season = _resolver.Resolve(args);

            Assert.NotNull(season);
            Assert.True(season.TryGetProviderId(provider, out var actualId));
            Assert.Equal(expectedId, actualId);
        }

        [Fact]
        public void Resolve_SeasonFolderWithMultipleProviderIds_SetsAll()
        {
            var series = new Series { Path = "/media/Show" };

            var args = new MediaBrowser.Controller.Library.ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = series,
                LibraryOptions = new LibraryOptions(),
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/Show/Season 01 [tvdbid=12345][tmdbid=99999]",
                    IsDirectory = true
                }
            };

            var season = _resolver.Resolve(args);

            Assert.NotNull(season);
            Assert.True(season.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbId));
            Assert.Equal("12345", tvdbId);
            Assert.True(season.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbId));
            Assert.Equal("99999", tmdbId);
        }

        [Fact]
        public void Resolve_SeasonFolderWithSeriesProviderIdInParentPath_DoesNotInheritSeriesId()
        {
            // Series folder has tvdbid=11111, season folder has tvdbid=22222.
            // The season should only pick up its own ID, not the series-level one.
            var series = new Series { Path = "/media/Show [tvdbid=11111]" };

            var args = new MediaBrowser.Controller.Library.ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = series,
                LibraryOptions = new LibraryOptions(),
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/Show [tvdbid=11111]/Season 01 [tvdbid=22222]",
                    IsDirectory = true
                }
            };

            var season = _resolver.Resolve(args);

            Assert.NotNull(season);
            Assert.True(season.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbId));
            Assert.Equal("22222", tvdbId);
        }

        [Fact]
        public void Resolve_SeasonFolderWithNoProviderId_HasNoProviderIds()
        {
            var series = new Series { Path = "/media/Show" };

            var args = new MediaBrowser.Controller.Library.ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = series,
                LibraryOptions = new LibraryOptions(),
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/Show/Season 01",
                    IsDirectory = true
                }
            };

            var season = _resolver.Resolve(args);

            Assert.NotNull(season);
            Assert.False(season.TryGetProviderId(MetadataProvider.Tvdb, out _));
            Assert.False(season.TryGetProviderId(MetadataProvider.TvMaze, out _));
            Assert.False(season.TryGetProviderId(MetadataProvider.Tmdb, out _));
        }
    }
}
