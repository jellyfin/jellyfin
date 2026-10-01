using System;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.MediaEncoding.Transcoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Transcoding;

public class EnableSegmentCleaningTests
{
    private static StreamState LiveHlsState() => new StreamState(
        Mock.Of<IMediaSourceManager>(),
        TranscodingJobType.Hls,
        Mock.Of<ITranscodeManager>())
    {
        IsInputVideo = true,
        InputProtocol = MediaProtocol.Http,
        // RunTimeTicks = null → IsSegmentedLiveStream = true
    };

    private static StreamState VodFileState(long runTimeTicks) => new StreamState(
        Mock.Of<IMediaSourceManager>(),
        TranscodingJobType.Hls,
        Mock.Of<ITranscodeManager>())
    {
        IsInputVideo = true,
        InputProtocol = MediaProtocol.File,
        RunTimeTicks = runTimeTicks,
    };

    [Fact]
    public void EnableSegmentCleaning_LiveHlsStream_ReturnsTrue()
    {
        // IsSegmentedLiveStream = HLS + no RunTimeTicks → segment cleaning must activate.
        var state = LiveHlsState();
        Assert.True(TranscodeManager.EnableSegmentCleaning(state));
    }

    [Fact]
    public void EnableSegmentCleaning_LiveProgressiveStream_ReturnsFalse()
    {
        // Progressive type is not HLS — cleaning has no meaning here.
        var state = new StreamState(
            Mock.Of<IMediaSourceManager>(),
            TranscodingJobType.Progressive,
            Mock.Of<ITranscodeManager>())
        {
            IsInputVideo = true,
            InputProtocol = MediaProtocol.Http,
        };
        Assert.False(TranscodeManager.EnableSegmentCleaning(state));
    }

    [Fact]
    public void EnableSegmentCleaning_LiveHlsNonVideo_ReturnsFalse()
    {
        // Audio-only live streams don't carry video; cleaning should be skipped.
        var state = LiveHlsState();
        state.IsInputVideo = false;
        Assert.False(TranscodeManager.EnableSegmentCleaning(state));
    }

    [Fact]
    public void EnableSegmentCleaning_VodFileWithLongDuration_ReturnsTrue()
    {
        // File-protocol VOD with duration >= 5 minutes should trigger cleaning.
        var fiveMinutes = TimeSpan.FromMinutes(5).Ticks;
        var state = VodFileState(fiveMinutes);
        Assert.True(TranscodeManager.EnableSegmentCleaning(state));
    }

    [Fact]
    public void EnableSegmentCleaning_VodFileShortDuration_ReturnsFalse()
    {
        // Content shorter than 5 minutes is too short to bother cleaning up.
        var fourMinutes = TimeSpan.FromMinutes(4).Ticks;
        var state = VodFileState(fourMinutes);
        Assert.False(TranscodeManager.EnableSegmentCleaning(state));
    }

    [Fact]
    public void EnableSegmentCleaning_VodUdpProtocol_ReturnsFalse()
    {
        // UDP is neither File nor Http; cleaning has never applied.
        var state = new StreamState(
            Mock.Of<IMediaSourceManager>(),
            TranscodingJobType.Hls,
            Mock.Of<ITranscodeManager>())
        {
            IsInputVideo = true,
            InputProtocol = MediaProtocol.Udp,
            RunTimeTicks = TimeSpan.FromHours(1).Ticks,
        };
        Assert.False(TranscodeManager.EnableSegmentCleaning(state));
    }

    [Fact]
    public void EnableSegmentCleaning_VodHttpWithLongDuration_ReturnsTrue()
    {
        // Http-protocol VOD (e.g. remote file) with duration >= 5 minutes should also work.
        var state = new StreamState(
            Mock.Of<IMediaSourceManager>(),
            TranscodingJobType.Hls,
            Mock.Of<ITranscodeManager>())
        {
            IsInputVideo = true,
            InputProtocol = MediaProtocol.Http,
            RunTimeTicks = TimeSpan.FromHours(2).Ticks,
        };
        Assert.True(TranscodeManager.EnableSegmentCleaning(state));
    }
}
