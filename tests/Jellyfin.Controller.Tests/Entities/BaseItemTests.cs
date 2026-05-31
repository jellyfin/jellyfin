using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class BaseItemTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("1", "0000000001")]
    [InlineData("t", "t")]
    [InlineData("test", "test")]
    [InlineData("test1", "test0000000001")]
    [InlineData("1test 2", "0000000001test 0000000002")]
    public void BaseItem_ModifySortChunks_Valid(string input, string expected)
        => Assert.Equal(expected, BaseItem.ModifySortChunks(input));

    [Fact]
    public async Task SwapImagesAsync_Backdrop_SwapsSortOrderAndPersistsImageUpdate()
    {
        var item = new Video
        {
            ImageInfos =
            [
                new ItemImageInfo { Path = "/media/backdrop-a.jpg", Type = ImageType.Backdrop, SortOrder = 0 },
                new ItemImageInfo { Path = "/media/backdrop-b.jpg", Type = ImageType.Backdrop, SortOrder = 1 },
                new ItemImageInfo { Path = "/media/backdrop-c.jpg", Type = ImageType.Backdrop, SortOrder = 2 }
            ]
        };

        var originalLibraryManager = BaseItem.LibraryManager;
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(x => x.UpdateItemAsync(item, It.IsAny<BaseItem>(), ItemUpdateType.ImageUpdate, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        try
        {
            BaseItem.LibraryManager = libraryManager.Object;

            await item.SwapImagesAsync(ImageType.Backdrop, 0, 2);
        }
        finally
        {
            BaseItem.LibraryManager = originalLibraryManager;
        }

        var orderedImages = item.GetImages(ImageType.Backdrop).ToArray();
        Assert.Equal(
            ["/media/backdrop-c.jpg", "/media/backdrop-b.jpg", "/media/backdrop-a.jpg"],
            orderedImages.Select(i => i.Path));
        Assert.Equal([0, 1, 2], orderedImages.Select(i => i.SortOrder));
        libraryManager.Verify(
            x => x.UpdateItemAsync(item, It.IsAny<BaseItem>(), ItemUpdateType.ImageUpdate, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("/Movies/Ted/Ted.mp4", "/Movies/Ted/Ted - Unrated Edition.mp4", "Ted", "Unrated Edition")]
    [InlineData("/Movies/Deadpool 2 (2018)/Deadpool 2 (2018).mkv", "/Movies/Deadpool 2 (2018)/Deadpool 2 (2018) - Super Duper Cut.mkv", "Deadpool 2 (2018)", "Super Duper Cut")]
    public void GetMediaSourceName_Valid(string primaryPath, string altPath, string name, string altName)
    {
        var video = new Video()
        {
            Path = primaryPath
        };

        var videoAlt = new Video()
        {
            Path = altPath,
        };

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>()))
                .Returns((string x) => MediaProtocol.File);
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>()))
                .Returns([Guid.Empty]);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        Assert.Equal(name, video.GetMediaSourceName(video));
        Assert.Equal(altName, video.GetMediaSourceName(videoAlt));
    }
}
