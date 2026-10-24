using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Images;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Images;

/// <summary>
/// A music library is collaged from its artists' backdrops. Artists are by-name items with no
/// library of their own, so they have to be asked for through the by-name listing, which reaches
/// them through the tracks that credit them; an item query for them ignores the library scope and
/// hands back the artists of every music library.
/// </summary>
public sealed class CollectionFolderImageProviderTests
{
    [Fact]
    public void GetItemsWithImages_MusicLibrary_AsksForTheArtistsOfThatLibraryOnly()
    {
        var view = new CollectionFolder { Id = Guid.NewGuid(), CollectionType = CollectionType.music };
        var artist = new MusicArtist { Id = Guid.NewGuid(), Name = "Artist" };

        InternalItemsQuery? query = null;
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(l => l.GetAllArtists(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => query = q)
            .Returns(new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>([(artist, new ItemCounts())]));

        var items = CreateProvider(libraryManager.Object).GetItems(view);

        Assert.Equal([artist], items);
        Assert.NotNull(query);
        Assert.Equal([view.Id], query.AncestorIds);
        Assert.Equal([ImageType.Primary], query.ImageTypes);
        Assert.Equal(8, query.Limit);
    }

    private static TestableCollectionFolderImageProvider CreateProvider(ILibraryManager libraryManager)
    {
        return new TestableCollectionFolderImageProvider(
            Mock.Of<IFileSystem>(),
            Mock.Of<IProviderManager>(),
            Mock.Of<IApplicationPaths>(),
            Mock.Of<IImageProcessor>(),
            libraryManager);
    }

    private sealed class TestableCollectionFolderImageProvider : CollectionFolderImageProvider
    {
        public TestableCollectionFolderImageProvider(
            IFileSystem fileSystem,
            IProviderManager providerManager,
            IApplicationPaths applicationPaths,
            IImageProcessor imageProcessor,
            ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
        {
        }

        public IReadOnlyList<BaseItem> GetItems(BaseItem item) => GetItemsWithImages(item);
    }
}
