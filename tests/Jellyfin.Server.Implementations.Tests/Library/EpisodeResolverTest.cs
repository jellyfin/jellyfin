using System;
using Emby.Server.Implementations.Library.Resolvers.TV;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
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
                Path = "All My Children/Season 01/Extras/All My Children S01E01 - Behind The Scenes.mkv"
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
                Path = "Extras/Extras S01E01.mkv"
            };
            Assert.NotNull(episodeResolver.Resolve(itemResolveArgs));
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
