using System;
using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class HlsPrerollTests
{
    [Theory]
    [InlineData(TranscodingJobType.Hls, "copy", "aac", "mp4", "mkv", true)]
    [InlineData(TranscodingJobType.Hls, "copy", "aac", "ts", "mkv", true)]
    [InlineData(TranscodingJobType.Hls, "copy", "opus", "mp4", "mkv", true)]
    [InlineData(TranscodingJobType.Hls, "copy", "mp3", "ts", "mkv", true)]
    [InlineData(TranscodingJobType.Hls, "copy", "copy", "mp4", "mkv", false)]
    [InlineData(TranscodingJobType.Hls, "libx264", "copy", "mp4", "mkv", false)]
    [InlineData(TranscodingJobType.Hls, "libx264", "aac", "mp4", "mkv", false)]
    [InlineData(TranscodingJobType.Progressive, "copy", "aac", "mp4", "mkv", false)]
    [InlineData(TranscodingJobType.Hls, "copy", "aac", "mp4", "wtv", false)]
    public void GetFastSeekCommandLineParameter_RetainsAudioPreroll(
        TranscodingJobType type,
        string video,
        string audio,
        string container,
        string input,
        bool retainsPreroll)
    {
        var state = CreateState(type);
        state.OutputVideoCodec = video;
        state.OutputAudioCodec = audio;
        state.InputContainer = input;
        state.BaseRequest.StartTimeTicks = 312_810_000L;
        var args = CreateHelper().GetFastSeekCommandLineParameter(state, new EncodingOptions(), container);
        Assert.Equal(retainsPreroll, args.Contains("-noaccurate_seek", StringComparison.Ordinal));
        Assert.StartsWith(type == TranscodingJobType.Hls && video == "copy" ? "-ss 00:00:31.781" : "-ss 00:00:31.281", args, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, "aac", 312_810_000L)]
    [InlineData(true, "", 312_810_000L)]
    [InlineData(true, "aac", 0L)]
    public void GetFastSeekCommandLineParameter_WithoutAudioOrSeek_OmitsNoAccurateSeek(bool hasAudio, string audioCodec, long seekTicks)
    {
        var state = CreateState();
        if (!hasAudio)
        {
            state.AudioStream = null;
        }

        state.OutputAudioCodec = audioCodec;
        state.BaseRequest.StartTimeTicks = seekTicks;
        var args = CreateHelper().GetFastSeekCommandLineParameter(state, new EncodingOptions(), "mp4");
        Assert.DoesNotContain("noaccurate_seek", args, StringComparison.Ordinal);
    }

    private static EncodingHelper CreateHelper()
    {
        var encoder = new Mock<IMediaEncoder>();
        encoder.Setup(e => e.GetTimeParameter(It.IsAny<long>()))
            .Returns((long ticks) => TimeSpan.FromTicks(ticks).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture));
        encoder.SetupGet(e => e.EncoderVersion).Returns(new Version(8, 1, 3));
        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            encoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    private static EncodingJobInfo CreateState(TranscodingJobType type = TranscodingJobType.Hls) => new(type)
    {
        IsVideoRequest = true,
        OutputVideoCodec = "copy",
        OutputAudioCodec = "aac",
        InputContainer = "mkv",
        RunTimeTicks = TimeSpan.FromSeconds(45).Ticks,
        VideoStream = new MediaStream { Codec = "h264", Type = MediaStreamType.Video, NalLengthSize = "4" },
        AudioStream = new MediaStream { Codec = "flac", Type = MediaStreamType.Audio, Channels = 1 },
        BaseRequest = new BaseEncodingJobOptions()
    };
}
