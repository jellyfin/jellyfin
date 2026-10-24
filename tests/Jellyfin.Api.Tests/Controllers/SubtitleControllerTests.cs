using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class SubtitleControllerTests
{
    private const int SubtitleIndex = 2;

    private static readonly Guid _itemId = new("6e1f0a3b7c8d4e5f9a0b1c2d3e4f5a6b");

    [Theory]
    // Graphical subtitle tracks are negotiated as SubtitleDeliveryMethod.External and must be handed
    // to the client untouched, because neither format can be parsed or rewritten.
    [InlineData("pgssub", "pgssub", "/cache/subs/sub.sup")]
    // FFmpeg cannot mux VobSub back into an .idx/.sub pair, so extracted VobSub is exposed as .mks
    // while the negotiated profile format stays "vobsub".
    [InlineData("vobsub", "dvdsub", "/cache/subs/sub.mks")]
    public async Task GetSubtitle_GraphicalStream_ReturnsRawFileWithRangeProcessing(string format, string codec, string path)
    {
        var encoder = new Mock<ISubtitleEncoder>(MockBehavior.Strict);
        var controller = CreateController(codec, path, encoder, out var fileSystem);
        fileSystem.Setup(f => f.FileExists(path)).Returns(true);

        var result = await controller.GetSubtitle(_itemId, _itemId.ToString("N"), SubtitleIndex, format, null, null, null, null, null);

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal(path, fileResult.FileName);
        Assert.True(fileResult.EnableRangeProcessing);
        encoder.Verify(
            e => e.GetSubtitles(It.IsAny<BaseItem>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSubtitle_RemotelyHostedGraphicalStream_FallsBackToEncoder()
    {
        var encoder = new Mock<ISubtitleEncoder>(MockBehavior.Strict);
        var controller = CreateController("pgssub", "https://example.com/sub.sup", encoder, out var fileSystem);
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        encoder.Setup(e => e.GetSubtitles(It.IsAny<BaseItem>(), It.IsAny<string>(), SubtitleIndex, "pgssub", 0, 0, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var result = await controller.GetSubtitle(_itemId, _itemId.ToString("N"), SubtitleIndex, "pgssub", null, null, null, null, null);

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task GetSubtitle_VobSubIdxPair_FallsBackToEncoder()
    {
        // A .idx/.sub pair has no raw payload the client could render: the encoder converts it.
        var encoder = new Mock<ISubtitleEncoder>(MockBehavior.Strict);
        var controller = CreateController("dvdsub", "/media/sub.idx", encoder, out _);
        encoder.Setup(e => e.GetSubtitles(It.IsAny<BaseItem>(), It.IsAny<string>(), SubtitleIndex, "vobsub", 0, 0, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var result = await controller.GetSubtitle(_itemId, _itemId.ToString("N"), SubtitleIndex, "vobsub", null, null, null, null, null);

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task GetSubtitle_TextStream_IsEncoded()
    {
        var encoder = new Mock<ISubtitleEncoder>(MockBehavior.Strict);
        var controller = CreateController("subrip", "/media/sub.srt", encoder, out _);
        encoder.Setup(e => e.GetSubtitles(It.IsAny<BaseItem>(), It.IsAny<string>(), SubtitleIndex, "vtt", 0, 0, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var result = await controller.GetSubtitle(_itemId, _itemId.ToString("N"), SubtitleIndex, "vtt", null, null, null, null, null);

        Assert.IsType<FileStreamResult>(result);
    }

    private static SubtitleController CreateController(
        string codec,
        string subtitlePath,
        Mock<ISubtitleEncoder> subtitleEncoder,
        out Mock<IFileSystem> fileSystem)
    {
        var item = new Movie { Id = _itemId };
        var subtitleStream = new MediaStream
        {
            Type = MediaStreamType.Subtitle,
            Index = SubtitleIndex,
            Codec = codec,
            Path = subtitlePath,
            IsExternal = true
        };

        var mediaSource = new MediaSourceInfo
        {
            Id = _itemId.ToString("N"),
            MediaStreams = new List<MediaStream> { subtitleStream }
        };

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetItemById<BaseItem>(_itemId)).Returns(item);

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(m => m.GetStaticMediaSources(item, false, null)).Returns([mediaSource]);

        subtitleEncoder.Setup(e => e.GetSubtitleFilePath(subtitleStream, mediaSource, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subtitlePath);

        fileSystem = new Mock<IFileSystem>();

        return new SubtitleController(
            Mock.Of<IServerConfigurationManager>(),
            libraryManager.Object,
            Mock.Of<ISubtitleManager>(),
            subtitleEncoder.Object,
            mediaSourceManager.Object,
            Mock.Of<IProviderManager>(),
            fileSystem.Object,
            NullLogger<SubtitleController>.Instance);
    }
}
