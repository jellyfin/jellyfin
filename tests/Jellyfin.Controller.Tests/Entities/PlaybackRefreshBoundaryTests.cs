using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public class PlaybackRefreshBoundaryTests
{
    [Fact]
    public async Task TechnicalRefresh_DoesNotMaintainOwnedItemsOrImages()
    {
        var oldLibrary = BaseItem.LibraryManager;
        var oldProvider = BaseItem.ProviderManager;
        try
        {
            var library = new Mock<ILibraryManager>(MockBehavior.Strict);
            var provider = new Mock<IProviderManager>(MockBehavior.Strict);
            provider.Setup(p => p.RefreshSingleItem(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ItemUpdateType.None);
            BaseItem.LibraryManager = library.Object;
            BaseItem.ProviderManager = provider.Object;
            var directory = new Mock<IDirectoryService>(MockBehavior.Strict);
            var item = new OwnedItem { Id = Guid.NewGuid(), Path = "/media/movie.mkv" };
            await item.RefreshMetadata(new MetadataRefreshOptions(directory.Object) { RefreshMediaInfoOnly = true }, TestContext.Current.CancellationToken);
            Assert.False(item.OwnedItemsVisited);
            directory.VerifyNoOtherCalls();
            library.VerifyNoOtherCalls();
            provider.Verify(p => p.RefreshSingleItem(item, It.Is<MetadataRefreshOptions>(o => o.RefreshMediaInfoOnly), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            BaseItem.LibraryManager = oldLibrary;
            BaseItem.ProviderManager = oldProvider;
        }
    }

    [Fact]
    public async Task TechnicalSave_DoesNotCopyDescriptionsToAlternateVersions()
    {
        var oldLibrary = BaseItem.LibraryManager;
        try
        {
            var library = new Mock<ILibraryManager>();
            var member = new Movie { Id = Guid.NewGuid(), Genres = ["Edited"], Overview = "Edited" };
            library.Setup(l => l.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns([member.Id]);
            library.Setup(l => l.GetItemById(member.Id)).Returns(member);
            BaseItem.LibraryManager = library.Object;
            var primary = new Movie { Id = Guid.NewGuid(), Genres = ["Original"], Overview = "Original" };
            await primary.UpdateToRepositoryAsync(ItemUpdateType.MediaInfo, TestContext.Current.CancellationToken);
            Assert.Equal(["Edited"], member.Genres);
            Assert.Equal("Edited", member.Overview);
            library.Verify(l => l.GetLocalAlternateVersionIds(primary), Times.Never);
            library.Verify(l => l.UpdateItemAsync(member, It.IsAny<BaseItem>(), It.IsAny<ItemUpdateType>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            BaseItem.LibraryManager = oldLibrary;
        }
    }

    private sealed class OwnedItem : Movie
    {
        public bool OwnedItemsVisited { get; private set; }

        protected override bool SupportsOwnedItems => true;

        protected override Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, IReadOnlyList<FileSystemMetadata> children, CancellationToken cancellationToken)
        {
            OwnedItemsVisited = true;
            return Task.FromResult(false);
        }
    }
}
