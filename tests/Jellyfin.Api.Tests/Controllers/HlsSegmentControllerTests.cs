using System;
using System.IO;
using System.Security.Claims;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Results;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

// Legacy routes must reject paths outside the transcode directory and bind video files to an owned job.
public sealed class HlsSegmentControllerTests
{
    private readonly Mock<IServerConfigurationManager> _config = new();
    private readonly Mock<ITranscodeManager> _transcodeManager = new();
    private readonly Mock<IVideoPlaybackCoordinator> _coordinator = new();
    private readonly Mock<IVideoPlaybackRequest> _request = new();
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly VideoPlaybackContext _playback;
    private readonly VideoPlaybackHelper _videoPlaybackHelper;
    private readonly string _transcodePath;

    public HlsSegmentControllerTests()
    {
        _transcodePath = Path.Combine(Path.GetTempPath(), "jellyfin-hls-segment-tests");
        Directory.CreateDirectory(_transcodePath);

        _config.Setup(c => c.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = _transcodePath });
        _config.SetupGet(c => c.CommonApplicationPaths).Returns(Mock.Of<IApplicationPaths>());

        _playback = new VideoPlaybackContext(_userId, "verified-owner", _itemId, "logical-playback");
        var playback = _playback;
        _coordinator.Setup(value => value.ResolvePlaySession(_userId, "verified-owner", It.IsAny<Guid?>(), "encoding", out playback))
            .Returns(VideoPlaybackStatus.Success);
        IVideoPlaybackRequest? request = _request.Object;
        _coordinator.Setup(value => value.TryAcquire(_playback, "encoding", 1, out request))
            .Returns(VideoPlaybackStatus.Success);
        _request.Setup(value => value.Retain()).Returns(Mock.Of<IVideoPlaybackRequest>());
        var users = new Mock<IUserManager>();
        users.Setup(value => value.GetUserById(_userId))
            .Returns(new User("player", "authentication", "reset") { Id = _userId, MaxActiveVideoStreams = 1 });
        var library = new Mock<ILibraryManager>();
        library.Setup(value => value.GetItemById<BaseItem>(_itemId, _userId)).Returns(new Movie { Id = _itemId });
        _videoPlaybackHelper = new VideoPlaybackHelper(_coordinator.Object, users.Object, library.Object);
    }

    private HlsSegmentController CreateController(string requestPath)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = requestPath;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(InternalClaimTypes.UserId, _userId.ToString()),
                new Claim(InternalClaimTypes.PlaybackOwnerId, "verified-owner")
            },
            "test"));

        return new HlsSegmentController(_config.Object, _transcodeManager.Object, _videoPlaybackHelper)
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
        RegisterJob("list");
        var controller = CreateController("/Videos/abc/hls/list/stream.m3u8");

        var result = controller.GetHlsPlaylistLegacy(_itemId.ToString(), "list");

        Assert.IsType<VideoPlaybackResult>(result);
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
        RegisterJob("playlist123");
        var controller = CreateController("/Videos/abc/hls/playlist123/playlist1230.ts");

        var result = controller.GetHlsVideoSegmentLegacy(_itemId.ToString(), "playlist123", "playlist1230", "ts");

        Assert.IsType<VideoPlaybackResult>(result);
    }

    [Fact]
    public void GetHlsVideoSegmentLegacy_NoMatchingPlaylist_ReturnsNotFound()
    {
        var controller = CreateController("/Videos/abc/hls/playlist123/playlist1230.ts");

        var result = controller.GetHlsVideoSegmentLegacy(_itemId.ToString(), "playlist123", "playlist1230", "ts");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData("../../../../etc/passwd")]
    public void GetHlsVideoSegmentLegacy_TraversalOutsideTranscodePath_ReturnsBadRequest(string segmentId)
    {
        var controller = CreateController("/Videos/abc/hls/playlist123/seg1.ts");

        var result = controller.GetHlsVideoSegmentLegacy("abc", "playlist123", segmentId, "ts");

        Assert.IsType<BadRequestObjectResult>(result);
        _transcodeManager.Verify(value => value.GetTranscodingJob(It.IsAny<string>(), It.IsAny<TranscodingJobType>()), Times.Never);
    }

    [Fact]
    public void GetHlsPlaylistLegacy_CachedPlaylistWithoutJob_ReturnsNotFound()
    {
        var controller = CreateController("/Videos/abc/hls/list/stream.m3u8");

        Assert.IsType<NotFoundObjectResult>(controller.GetHlsPlaylistLegacy(_itemId.ToString(), "list"));
        _transcodeManager.Verify(value => value.OnTranscodeBeginRequest(It.IsAny<string>(), It.IsAny<TranscodingJobType>()), Times.Never);
    }

    [Fact]
    public void GetHlsVideoSegmentLegacy_UnrelatedSegmentCannotUseOwnedPlaylist()
    {
        RegisterJob("playlist123");
        var controller = CreateController("/Videos/abc/hls/playlist123/other0.ts");

        Assert.IsType<NotFoundObjectResult>(controller.GetHlsVideoSegmentLegacy(_itemId.ToString(), "playlist123", "other0", "ts"));
        _transcodeManager.Verify(value => value.OnTranscodeBeginRequest(It.IsAny<string>(), It.IsAny<TranscodingJobType>()), Times.Never);
    }

    [Fact]
    public void GetHlsVideoSegmentLegacy_QuotaRejectedBeforeTranscodeRequestBegins()
    {
        RegisterJob("playlist123");
        IVideoPlaybackRequest? denied = null;
        _coordinator.Setup(value => value.TryAcquire(_playback, "encoding", 1, out denied)).Returns(VideoPlaybackStatus.LimitReached);
        var controller = CreateController("/Videos/abc/hls/playlist123/playlist1230.ts");

        Assert.Throws<VideoPlaybackLimitException>(() => controller.GetHlsVideoSegmentLegacy(_itemId.ToString(), "playlist123", "playlist1230", "ts"));
        _transcodeManager.Verify(value => value.OnTranscodeBeginRequest(It.IsAny<string>(), It.IsAny<TranscodingJobType>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StopEncodingProcess_WrongOwnerCannotKillVideoOrUnknownJob(bool missingMediaSource)
    {
        var job = RegisterJob("playlist123");
        if (missingMediaSource)
        {
            job.MediaSource = null;
        }

        var playback = _playback;
        _coordinator.Setup(value => value.ResolvePlaySession(_userId, "verified-owner", It.IsAny<Guid?>(), "encoding", out playback))
            .Returns(VideoPlaybackStatus.OwnershipMismatch);
        var controller = CreateController("/Videos/ActiveEncodings");

        Assert.Throws<SecurityException>(() => controller.StopEncodingProcess("device", "encoding"));
        _transcodeManager.Verify(value => value.KillTranscodingJobs(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Func<string, bool>>()), Times.Never);
    }

    [Fact]
    public void StopEncodingProcess_OwnedVideoJobDoesNotReleaseLogicalPlayback()
    {
        RegisterJob("playlist123");
        var controller = CreateController("/Videos/ActiveEncodings");

        Assert.IsType<NoContentResult>(controller.StopEncodingProcess("device", "encoding"));
        _transcodeManager.Verify(value => value.KillTranscodingJobs("device", "encoding", It.IsAny<Func<string, bool>>()), Times.Once);
        _coordinator.Verify(value => value.Stop(It.IsAny<VideoPlaybackContext>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void StopEncodingProcess_AudioJobPreservesExistingBehavior()
    {
        RegisterJob("audio", MediaStreamType.Audio);
        var controller = CreateController("/Videos/ActiveEncodings");

        Assert.IsType<NoContentResult>(controller.StopEncodingProcess("device", "encoding"));
        _transcodeManager.Verify(value => value.KillTranscodingJobs("device", "encoding", It.IsAny<Func<string, bool>>()), Times.Once);
    }

    [Fact]
    public void StopEncodingProcess_AbsentJobDoesNotKillOtherJobs()
    {
        var controller = CreateController("/Videos/ActiveEncodings");

        Assert.IsType<NoContentResult>(controller.StopEncodingProcess("device", "encoding"));
        _transcodeManager.Verify(value => value.KillTranscodingJobs(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Func<string, bool>>()), Times.Never);
    }

    private TranscodingJob RegisterJob(string playlistId, MediaStreamType mediaType = MediaStreamType.Video)
    {
        var path = Path.Combine(_transcodePath, playlistId + ".m3u8");
        var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
        {
            PlaySessionId = "encoding",
            MediaSource = new MediaSourceInfo { MediaStreams = new[] { new MediaStream { Type = mediaType } } }
        };
        _transcodeManager.Setup(value => value.GetTranscodingJob(path, TranscodingJobType.Hls)).Returns(job);
        _transcodeManager.Setup(value => value.OnTranscodeBeginRequest(path, TranscodingJobType.Hls)).Returns(job);
        _transcodeManager.Setup(value => value.GetTranscodingJob("encoding")).Returns(job);
        return job;
    }
}
