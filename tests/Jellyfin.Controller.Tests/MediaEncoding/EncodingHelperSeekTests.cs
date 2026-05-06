using System;
using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperSeekTests
{
    private readonly EncodingHelper _helper;

    public EncodingHelperSeekTests()
    {
        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder
            .Setup(i => i.GetTimeParameter(It.IsAny<long>()))
            .Returns<long>(ticks => TimeSpan.FromTicks(ticks).ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture));

        _helper = new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            mediaEncoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    [Theory]
    [InlineData("mp4")]
    [InlineData("ts")]
    public void HlsTranscodeVideoCopyAudio_AddsBsfDrop(string segmentContainer)
    {
        var state = CreateState(TranscodingJobType.Hls, "libx264", "copy", TimeSpan.FromSeconds(63.063).Ticks);

        var result = _helper.GetFastSeekCommandLineParameter(state, new EncodingOptions(), segmentContainer);

        Assert.Contains("-ss 00:01:03.063", result, StringComparison.Ordinal);
        Assert.Contains("-bsf:a noise=drop='lt(pts*tb\\,63.063)'", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-noaccurate_seek", result, StringComparison.Ordinal);
    }

    [Fact]
    public void HlsRemuxFmp4_KeepsNoaccurateSeek()
    {
        var state = CreateState(TranscodingJobType.Hls, "copy", "copy", TimeSpan.FromSeconds(63).Ticks);

        var result = _helper.GetFastSeekCommandLineParameter(state, new EncodingOptions(), "mp4");

        Assert.Contains("-ss 00:01:03.500", result, StringComparison.Ordinal);
        Assert.Contains("-noaccurate_seek", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-bsf:a noise=drop=", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TranscodingJobType.Hls, "libx264", "aac", "mp4", "mkv")]
    [InlineData(TranscodingJobType.Progressive, "libx264", "copy", "mp4", "mkv")]
    [InlineData(TranscodingJobType.Hls, "libx264", "copy", "mp4", "wtv")]
    public void OtherSeekPaths_DoNotAddBsfDrop(
        TranscodingJobType jobType,
        string videoCodec,
        string audioCodec,
        string segmentContainer,
        string inputContainer)
    {
        var state = CreateState(jobType, videoCodec, audioCodec, TimeSpan.FromSeconds(63).Ticks, inputContainer);

        var result = _helper.GetFastSeekCommandLineParameter(state, new EncodingOptions(), segmentContainer);

        Assert.Contains("-ss 00:01:03.000", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-bsf:a noise=drop=", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-noaccurate_seek", result, StringComparison.Ordinal);
    }

    private static EncodingJobInfo CreateState(
        TranscodingJobType jobType,
        string videoCodec,
        string audioCodec,
        long? startTimeTicks,
        string inputContainer = "mkv")
    {
        return new EncodingJobInfo(jobType)
        {
            BaseRequest = new BaseEncodingJobOptions
            {
                StartTimeTicks = startTimeTicks
            },
            InputContainer = inputContainer,
            IsVideoRequest = true,
            OutputAudioCodec = audioCodec,
            OutputVideoCodec = videoCodec,
            RunTimeTicks = TimeSpan.FromMinutes(5).Ticks
        };
    }
}
