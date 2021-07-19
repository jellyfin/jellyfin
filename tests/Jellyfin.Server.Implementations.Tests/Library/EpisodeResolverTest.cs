using System;
using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.TV;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class EpisodeResolverTest
    {
        [Fact]
        public void Resolve_GivenVideoInExtrasFolder_DoesNotResolveToEpisode()
        {
            var season = new Season { Name = "Season 1" };
            var parent = new Folder { Name = "extras" };
            var libraryManagerMock = new Mock<ILibraryManager>();
            libraryManagerMock.Setup(x => x.GetItemById(It.IsAny<Guid>())).Returns(season);

            var episodeResolver = new EpisodeResolver(libraryManagerMock.Object);
            var itemResolveArgs = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                Mock.Of<IDirectoryService>())
            {
                Parent = parent,
                CollectionType = CollectionType.TvShows,
                FileInfo = new FileSystemMetadata()
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
            var episodeResolver = new EpisodeResolverMock(Mock.Of<ILibraryManager>());
            var itemResolveArgs = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                Mock.Of<IDirectoryService>())
            {
                Parent = series,
                CollectionType = CollectionType.TvShows,
                FileInfo = new FileSystemMetadata()
                {
                    FullName = "Extras/Extras S01E01.mkv"
                }
            };
            Assert.NotNull(episodeResolver.Resolve(itemResolveArgs));
        }

        [Fact]
        public void Resolve_GivenMultiVersionEpisode_ResolvesToEpisode()
        {
            var season = new Season { Name = "Season 1" };
            var parent = new Folder { Name = "Episode S01E01" };
            var libraryManagerMock = new Mock<ILibraryManager>();
            libraryManagerMock.Setup(x => x.GetItemById(It.IsAny<Guid>())).Returns(season);
            libraryManagerMock.Setup(x => x.GetNamingOptions()).Returns(new NamingOptions());

            var episodeResolver = new EpisodeResolver(libraryManagerMock.Object);
            var itemResolveArgs = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                Mock.Of<IDirectoryService>())
            {
                Parent = parent,
                CollectionType = CollectionType.TvShows,
                FileSystemChildren = new FileSystemMetadata[]
                {
                    new FileSystemMetadata()
                    {
                        FullName = "All My Children/Season 01/Episode S01E01/Episode S01E01 - version1.strm"
                    },
                    new FileSystemMetadata()
                    {
                        FullName = "All My Children/Season 01/Episode S01E01/Episode S01E01 - version2.strm"
                    },
                },
                FileInfo = new FileSystemMetadata()
                {
                    FullName = "All My Children/Season 01/Episode S01E01",
                    IsDirectory = true,
                }
            };

            var episode = episodeResolver.Resolve(itemResolveArgs);

            Assert.NotNull(episode);
            Assert.True(episode.IsShortcut);
            Assert.Single(episode.LocalAlternateVersions);
            Assert.Equal("All My Children/Season 01/Episode S01E01/Episode S01E01 - version2.strm", episode.LocalAlternateVersions[0]);
            Assert.Empty(episode.AdditionalParts);
        }

        private class EpisodeResolverMock : EpisodeResolver
        {
            public EpisodeResolverMock(ILibraryManager libraryManager) : base(libraryManager)
            {
            }

            protected override TVideoType ResolveVideo<TVideoType>(ItemResolveArgs args, bool parseName) => new ();
        }
    }
}
