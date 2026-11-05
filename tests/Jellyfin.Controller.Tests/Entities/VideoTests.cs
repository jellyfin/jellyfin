using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public class VideoTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateToRepositoryAsync_LocalAlternate_CopiesOnlyReadCollections(bool ownerReadCollections)
    {
        var previousLibrary = BaseItem.LibraryManager;
        var library = new Mock<ILibraryManager>();
        try
        {
            BaseItem.LibraryManager = library.Object;

            var alternateImages = new[] { new ItemImageInfo { Path = "/media/alternate.jpg", Type = ImageType.Primary } };
            var alternate = new Video
            {
                Id = Guid.NewGuid(),
                ImageInfos = alternateImages,
                ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Tmdb"] = "1" }
            };

            var owner = new Video
            {
                Id = Guid.NewGuid(),
                ImageInfos = [new ItemImageInfo { Path = "/media/owner.jpg", Type = ImageType.Primary }],
                ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Tmdb"] = "2" }
            };
            if (ownerReadCollections)
            {
                owner.MarkOwnedRowsRead(OwnedItemRows.All);
            }

            library.Setup(l => l.GetLocalAlternateVersionIds(owner)).Returns([alternate.Id]);
            library.Setup(l => l.GetItemById(alternate.Id)).Returns(alternate);

            await owner.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, TestContext.Current.CancellationToken).ConfigureAwait(true);

            if (ownerReadCollections)
            {
                Assert.Same(owner.ImageInfos, alternate.ImageInfos);
                Assert.Same(owner.ProviderIds, alternate.ProviderIds);
            }
            else
            {
                Assert.Same(alternateImages, alternate.ImageInfos);
                Assert.Equal("1", alternate.ProviderIds["Tmdb"]);
            }

            library.Verify(l => l.UpdateItemAsync(alternate, It.IsAny<BaseItem>(), ItemUpdateType.MetadataDownload, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            BaseItem.LibraryManager = previousLibrary;
        }
    }
}
