using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.TV;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class EpisodeResolverTest
    {
        private static readonly NamingOptions _namingOptions = new();

        [Fact]
        public void Resolve_GivenVideoInExtrasFolder_DoesNotResolveToEpisode()
        {
            var parent = new Folder { Name = "extras" };

            var episodeResolver = new EpisodeResolver(Mock.Of<ILogger<EpisodeResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
            var itemResolveArgs = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = parent,
                CollectionType = CollectionType.tvshows,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "All My Children/Season 01/Extras/All My Children S01E01 - Behind The Scenes.mkv"
                }
            };

            Assert.Null(episodeResolver.Resolve(itemResolveArgs));
        }

        [Fact]
        public void Resolve_GivenVideoInExtrasSeriesFolder_ResolvesToEpisode()
        {
            var series = new Series { Name = "Extras" };

            // Have to create a mock because of moq proxies not being castable to a concrete implementation
            // https://github.com/jellyfin/jellyfin/blob/ab0cff8556403e123642dc9717ba778329554634/Emby.Server.Implementations/Library/Resolvers/BaseVideoResolver.cs#L48
            var episodeResolver = new EpisodeResolverMock(Mock.Of<ILogger<EpisodeResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
            var itemResolveArgs = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = series,
                CollectionType = CollectionType.tvshows,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "Extras/Extras S01E01.mkv"
                }
            };
            Assert.NotNull(episodeResolver.Resolve(itemResolveArgs));
        }

        [Theory]
        [InlineData("/media/Show/Season 01/Show S01E01 [tvdbid=12345].mkv", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show/Season 01/Show S01E01 [tvdbid-12345].mkv", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show/Season 01/Show S01E01 (tvdbid=12345).mkv", MetadataProvider.Tvdb, "12345")]
        [InlineData("/media/Show/Season 02/Show S02E03 [tvmazeid=67890].mkv", MetadataProvider.TvMaze, "67890")]
        [InlineData("/media/Show/Season 02/Show S02E03 [tvmazeid-67890].mkv", MetadataProvider.TvMaze, "67890")]
        [InlineData("/media/Show/Season 03/Show S03E04 [tmdbid=99999].mkv", MetadataProvider.Tmdb, "99999")]
        [InlineData("/media/Show/Season 03/Show S03E04 [tmdbid-99999].mkv", MetadataProvider.Tmdb, "99999")]
        [InlineData("/media/Show/Season 04/Show S04E05 [imdbid=tt1234567].mkv", MetadataProvider.Imdb, "tt1234567")]
        [InlineData("/media/Show/Season 04/Show S04E05 [imdbid-tt1234567].mkv", MetadataProvider.Imdb, "tt1234567")]
        public void Resolve_EpisodeFileWithProviderId_SetsProviderId(string path, MetadataProvider provider, string expectedId)
        {
            var series = new Series { Name = "Show" };
            var episodeResolver = new EpisodeResolverMock(Mock.Of<ILogger<EpisodeResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
            var itemResolveArgs = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = series,
                CollectionType = CollectionType.tvshows,
                FileInfo = new FileSystemMetadata
                {
                    FullName = path,
                    IsDirectory = false
                }
            };

            var episode = episodeResolver.Resolve(itemResolveArgs);

            Assert.NotNull(episode);
            Assert.True(episode.TryGetProviderId(provider, out var actualId));
            Assert.Equal(expectedId, actualId);
        }

        [Fact]
        public void Resolve_EpisodeFileWithProviderIdsOnAllLevels_OnlyUsesEpisodeLevelId()
        {
            // Series folder has tvdbid=11111, season folder has tvdbid=22222, episode file has tvdbid=33333.
            // The episode should only pick up its own ID, not the series- or season-level ones.
            var series = new Series { Name = "Show" };
            var episodeResolver = new EpisodeResolverMock(Mock.Of<ILogger<EpisodeResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
            var itemResolveArgs = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = series,
                CollectionType = CollectionType.tvshows,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/Show [tvdbid=11111]/Season 01 [tvdbid=22222]/Show S01E01 [tvdbid=33333].mkv",
                    IsDirectory = false
                }
            };

            var episode = episodeResolver.Resolve(itemResolveArgs);

            Assert.NotNull(episode);
            Assert.True(episode.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbId));
            Assert.Equal("33333", tvdbId);
        }

        [Fact]
        public void Resolve_EpisodeFileWithMultipleProviderIds_SetsAll()
        {
            var series = new Series { Name = "Show" };
            var episodeResolver = new EpisodeResolverMock(Mock.Of<ILogger<EpisodeResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
            var itemResolveArgs = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = series,
                CollectionType = CollectionType.tvshows,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/Show/Season 01/Show S01E01 [tvdbid=12345][tmdbid=99999].mkv",
                    IsDirectory = false
                }
            };

            var episode = episodeResolver.Resolve(itemResolveArgs);

            Assert.NotNull(episode);
            Assert.True(episode.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbId));
            Assert.Equal("12345", tvdbId);
            Assert.True(episode.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbId));
            Assert.Equal("99999", tmdbId);
        }

        private sealed class EpisodeResolverMock : EpisodeResolver
        {
            public EpisodeResolverMock(ILogger<EpisodeResolver> logger, NamingOptions namingOptions, IDirectoryService directoryService) : base(logger, namingOptions, directoryService)
            {
            }

            protected override TVideoType ResolveVideo<TVideoType>(ItemResolveArgs args, bool parseName) => new();
        }
    }
}
