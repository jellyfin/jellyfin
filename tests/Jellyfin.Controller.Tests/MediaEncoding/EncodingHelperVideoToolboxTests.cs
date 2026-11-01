using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperVideoToolboxTests
{
    [Fact]
    public void GetProgressiveVideoFullCommandLine_SoftwareDecodeHardwareDeinterlace_DisablesAutoscale()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "VideoToolbox filter selection requires macOS");
        var helper = CreateHelper();
        var state = CreateState();
        var options = CreateOptions();

        var args = helper.GetProgressiveVideoFullCommandLine(state, options, EncoderPreset.auto);

        Assert.Contains("-codec:v:0 h264_videotoolbox", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-hwaccel videotoolbox", args, StringComparison.Ordinal);
        Assert.Contains("format=nv12|p010le|videotoolbox_vld,hwupload,bwdif_videotoolbox=0:-1:0", args, StringComparison.Ordinal);
        Assert.DoesNotContain("hwdownload", args, StringComparison.Ordinal);
        Assert.Contains(" -noautoscale", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetProgressiveVideoFullCommandLine_LegacySoftwareFilters_PreservesAutoscale()
    {
        var args = CreateHelper("scale_vt").GetProgressiveVideoFullCommandLine(CreateState(), CreateOptions(), EncoderPreset.auto);

        Assert.Contains("-codec:v:0 h264_videotoolbox", args, StringComparison.Ordinal);
        Assert.Contains("bwdif=0:-1:0", args, StringComparison.Ordinal);
        Assert.DoesNotContain("hwupload", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-noautoscale", args, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, null, SubtitleDeliveryMethod.Drop, false)]
    [InlineData(720, null, SubtitleDeliveryMethod.Drop, true)]
    [InlineData(null, "srt", SubtitleDeliveryMethod.Encode, true)]
    [InlineData(null, "pgssub", SubtitleDeliveryMethod.Encode, true)]
    public void GetProgressiveVideoFullCommandLine_ProgressiveInput_ProtectsOnlyUploadedHardwareOutput(
        int? maxWidth,
        string? subtitleCodec,
        SubtitleDeliveryMethod subtitleDeliveryMethod,
        bool expectHardwareOutput)
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "VideoToolbox filter selection requires macOS");
        var state = CreateState();
        state.VideoStream.IsInterlaced = false;
        state.BaseRequest.MaxWidth = maxWidth;
        if (subtitleCodec is not null)
        {
            state.SubtitleStream = new MediaStream
            {
                Index = 1,
                Type = MediaStreamType.Subtitle,
                Codec = subtitleCodec,
                IsExternal = true,
                Path = "/media/subtitles." + subtitleCodec,
                Width = 1440,
                Height = 1080,
            };
            state.MediaSource.MediaStreams = [state.VideoStream, state.SubtitleStream];
            state.SubtitleDeliveryMethod = subtitleDeliveryMethod;
            state.SupportedSubtitleCodecs = [subtitleCodec];
        }

        var args = CreateHelper().GetProgressiveVideoFullCommandLine(state, CreateOptions(), EncoderPreset.auto);

        Assert.DoesNotContain("bwdif", args, StringComparison.Ordinal);
        Assert.Equal(expectHardwareOutput, args.Contains("hwupload", StringComparison.Ordinal));
        Assert.Equal(expectHardwareOutput, args.Contains(" -noautoscale", StringComparison.Ordinal));
        Assert.Equal(maxWidth.HasValue, args.Contains("scale_vt=w=", StringComparison.Ordinal));
        Assert.Equal(subtitleCodec is not null && subtitleDeliveryMethod == SubtitleDeliveryMethod.Encode, args.Contains("overlay_videotoolbox", StringComparison.Ordinal));
    }

    [Fact]
    public void GetProgressiveVideoFullCommandLine_HardwareDecode_PreservesExistingProtection()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "VideoToolbox decoder selection requires macOS");
        var state = CreateState();
        state.VideoStream.Codec = "h264";
        var options = CreateOptions();
        options.HardwareDecodingCodecs = ["h264"];
        var helper = CreateHelper();

        var inputArgs = helper.GetInputArgument(state, options, null);
        var args = helper.GetProgressiveVideoFullCommandLine(state, options, EncoderPreset.auto);

        Assert.Contains("-hwaccel videotoolbox", inputArgs, StringComparison.Ordinal);
        Assert.Contains(" -noautoscale", inputArgs, StringComparison.Ordinal);
        Assert.Equal(1, args.Split(" -noautoscale", StringSplitOptions.None).Length - 1);
    }

    private static EncodingOptions CreateOptions() => new()
    {
        HardwareAccelerationType = HardwareAccelerationType.videotoolbox,
        HardwareDecodingCodecs = [],
        DeinterlaceMethod = DeinterlaceMethod.bwdif,
    };

    private static EncodingJobInfo CreateState()
    {
        var video = new MediaStream
        {
            Index = 0,
            Type = MediaStreamType.Video,
            Codec = "mpeg2video",
            PixelFormat = "yuv420p",
            Width = 1440,
            Height = 1080,
            IsInterlaced = true,
            RealFrameRate = 30000f / 1001,
            AverageFrameRate = 30000f / 1001,
        };

        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "mpeg",
                Path = "/media/input.m2v",
                Protocol = MediaProtocol.File,
                MediaStreams = [video],
            },
            VideoType = VideoType.VideoFile,
            VideoStream = video,
            OutputVideoCodec = "h264",
            OutputContainer = "mkv",
            OutputFilePath = "/transcodes/output.mkv",
            BaseRequest = new VideoRequestDto(),
            IsVideoRequest = true,
            IsInputVideo = true,
        };
    }

    private static EncodingHelper CreateHelper(string? unsupportedCapability = null)
    {
        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.SetupGet(encoder => encoder.EncoderVersion).Returns(new Version(8, 1, 3));
        mediaEncoder.Setup(encoder => encoder.SupportsHwaccel("videotoolbox")).Returns(true);
        mediaEncoder.Setup(encoder => encoder.SupportsEncoder(It.IsAny<string>())).Returns(true);
        mediaEncoder.Setup(encoder => encoder.SupportsFilter(It.IsAny<string>()))
            .Returns((string filter) => filter != unsupportedCapability);
        mediaEncoder.Setup(encoder => encoder.GetInputPathArgument(It.IsAny<EncodingJobInfo>()))
            .Returns("\"/media/input.m2v\"");

        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            mediaEncoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }
}
