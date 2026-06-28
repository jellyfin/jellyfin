using System;
using System.IO;
using Jellyfin.Api.Controllers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

// The legacy HLS endpoints build a file path from caller-supplied route values, and the audio
// and video segment endpoints are not authenticated. These tests pin down that requests escaping
// the transcode directory are rejected while legitimate ones still serve a file.
public sealed class HlsSegmentControllerTests
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<IServerConfigurationManager> _config = new();
    private readonly Mock<ITranscodeManager> _transcodeManager = new();
    private readonly string _transcodePath;

    public HlsSegmentControllerTests()
    {
        _transcodePath = Path.Combine(Path.GetTempPath(), "jellyfin-hls-segment-tests");
        Directory.CreateDirectory(_transcodePath);

        _config.Setup(c => c.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = _transcodePath });
        _config.SetupGet(c => c.CommonApplicationPaths).Returns(Mock.Of<IApplicationPaths>());
    }

    private HlsSegmentController CreateController(string requestPath)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = requestPath;

        return new HlsSegmentController(_fileSystem.Object, _config.Object, _transcodeManager.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public void GetHlsAudioSegmentLegacy_SegmentInsideTranscodePath_ReturnsFile()
    {
        var controller = CreateController("/Audio/abc/hls/segment/stream.mp3");

        var result = controller.GetHlsAudioSegmentLegacy("abc", "segment");

        Assert.IsType<PhysicalFileResult>(result);
    }

    [Theory]
    [InlineData("../../../../etc/passwd")]
    [InlineData("subdir/../../../../etc/passwd")]
    public void GetHlsAudioSegmentLegacy_TraversalOutsideTranscodePath_ReturnsBadRequest(string segmentId)
    {
        var controller = CreateController("/Audio/abc/hls/segment/stream.mp3");

        var result = controller.GetHlsAudioSegmentLegacy("abc", segmentId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetHlsAudioSegmentLegacy_AbsoluteRootedPath_ReturnsBadRequest()
    {
        var controller = CreateController("/Audio/abc/hls/segment/stream.mp3");

        // A rooted segment id makes Path.GetFullPath discard the transcode base.
        var rooted = OperatingSystem.IsWindows() ? "C:\\Windows\\win.ini" : "/etc/passwd";
        var result = controller.GetHlsAudioSegmentLegacy("abc", rooted);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetHlsAudioSegmentLegacy_SiblingPrefixDirectory_ReturnsBadRequest()
    {
        var controller = CreateController("/Audio/abc/hls/segment/stream.mp3");

        // Resolves to "<transcodePath>-evil/passwd", which shares the transcode path as a string prefix.
        var result = controller.GetHlsAudioSegmentLegacy("abc", "../jellyfin-hls-segment-tests-evil/passwd");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetHlsPlaylistLegacy_M3u8InsideTranscodePath_ReturnsFile()
    {
        var controller = CreateController("/Videos/abc/hls/list/stream.m3u8");

        var result = controller.GetHlsPlaylistLegacy("abc", "list");

        Assert.IsType<PhysicalFileResult>(result);
    }

    [Fact]
    public void GetHlsPlaylistLegacy_NonPlaylistExtension_ReturnsBadRequest()
    {
        // Playlist endpoint serves only .m3u8, even for a path inside the transcode dir.
        var controller = CreateController("/Videos/abc/hls/list/stream.mp4");

        var result = controller.GetHlsPlaylistLegacy("abc", "list");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("../../../../etc/passwd")]
    public void GetHlsPlaylistLegacy_TraversalOutsideTranscodePath_ReturnsBadRequest(string playlistId)
    {
        var controller = CreateController("/Videos/abc/hls/list/stream.m3u8");

        var result = controller.GetHlsPlaylistLegacy("abc", playlistId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetHlsVideoSegmentLegacy_SegmentInsideTranscodePath_ReturnsFile()
    {
        _fileSystem.Setup(f => f.GetFilePaths(_transcodePath, false))
            .Returns(new[] { Path.Combine(_transcodePath, "playlist123.ts") });

        var controller = CreateController("/Videos/abc/hls/playlist123/seg1.ts");

        var result = controller.GetHlsVideoSegmentLegacy("abc", "playlist123", "seg1", "ts");

        Assert.IsType<PhysicalFileResult>(result);
    }

    [Fact]
    public void GetHlsVideoSegmentLegacy_NoMatchingPlaylist_ReturnsNotFound()
    {
        _fileSystem.Setup(f => f.GetFilePaths(_transcodePath, false))
            .Returns(Array.Empty<string>());

        var controller = CreateController("/Videos/abc/hls/playlist123/seg1.ts");

        var result = controller.GetHlsVideoSegmentLegacy("abc", "playlist123", "seg1", "ts");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData("../../../../etc/passwd")]
    public void GetHlsVideoSegmentLegacy_TraversalOutsideTranscodePath_ReturnsBadRequest(string segmentId)
    {
        var controller = CreateController("/Videos/abc/hls/playlist123/seg1.ts");

        var result = controller.GetHlsVideoSegmentLegacy("abc", "playlist123", segmentId, "ts");

        Assert.IsType<BadRequestObjectResult>(result);
        _fileSystem.Verify(f => f.GetFilePaths(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }
}
