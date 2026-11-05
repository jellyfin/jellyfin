using System;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Guide;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Guide;

public sealed class GuideManagerTests : IDisposable
{
    private const string ImageUrl = "https://json.schedulesdirect.org/20141201/image/assets/p1_b.jpg?token=abc";
    private const string ImageNotFoundBody = "{\"response\":\"IMAGE_NOT_FOUND\",\"code\":5000,\"message\":\"test\"}";

    private readonly ILibraryManager? _previousLibraryManager = BaseItem.LibraryManager;
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<ISchedulesDirectService> _schedulesDirect = new();

    public GuideManagerTests()
    {
        // UpdateToRepositoryAsync goes through the static.
        BaseItem.LibraryManager = _libraryManager.Object;
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
    }

    [Fact]
    public async Task PreCacheImages_ImageNotFound_ReportsTheBodyAndRemovesTheImage()
    {
        var program = CreateProgram();
        _schedulesDirect.Setup(s => s.CanDownloadImage(ImageUrl)).Returns(true);
        _schedulesDirect.Setup(s => s.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.NotFound, ImageNotFoundBody))
            .Returns(ImageDownloadFailureAction.RemoveImage);

        // ConvertImageToLocal wraps the rejection, which is the only place the SD error body survives.
        _libraryManager.Setup(l => l.ConvertImageToLocal(program, It.IsAny<ItemImageInfo>(), 0, false))
            .ThrowsAsync(new InvalidOperationException(
                "test",
                new HttpResponseBodyException("test", HttpStatusCode.NotFound, ImageNotFoundBody)));

        await CreateGuideManager().PreCacheImages([program], DateTime.UtcNow.AddDays(GuideManager.MaxCacheDays));

        _schedulesDirect.Verify(s => s.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.NotFound, ImageNotFoundBody), Times.Once);
        Assert.Empty(program.ImageInfos);
    }

    [Fact]
    public async Task PreCacheImages_ImageBlocked_RemovesItWithoutDownloading()
    {
        // A blocked url left on the program would be fetched from SD when a client displays it.
        var program = CreateProgram();
        _schedulesDirect.Setup(s => s.CanDownloadImage(ImageUrl)).Returns(false);

        await CreateGuideManager().PreCacheImages([program], DateTime.UtcNow.AddDays(GuideManager.MaxCacheDays));

        _libraryManager.Verify(
            l => l.ConvertImageToLocal(It.IsAny<BaseItem>(), It.IsAny<ItemImageInfo>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
        Assert.Empty(program.ImageInfos);
    }

    private static LiveTvProgram CreateProgram()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            EndDate = DateTime.UtcNow.AddHours(1),
            ImageInfos = [new ItemImageInfo { Path = ImageUrl, Type = ImageType.Primary }]
        };

    private GuideManager CreateGuideManager()
        => new(
            NullLogger<GuideManager>.Instance,
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IFileSystem>(),
            Mock.Of<IItemRepository>(),
            _libraryManager.Object,
            Mock.Of<ILiveTvManager>(),
            Mock.Of<ITunerHostManager>(),
            Mock.Of<IRecordingsManager>(),
            _schedulesDirect.Object,
            null!);
}
