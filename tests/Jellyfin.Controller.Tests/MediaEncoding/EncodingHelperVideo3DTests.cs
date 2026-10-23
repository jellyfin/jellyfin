using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperVideo3DTests
{
    [Theory]
    [InlineData(null, 3840, 806)]
    [InlineData(Video3DFormat.FullSideBySide, 1920, 806)]
    [InlineData(Video3DFormat.HalfSideBySide, 3840, 806)]
    [InlineData(Video3DFormat.FullTopAndBottom, 3840, 402)]
    [InlineData(Video3DFormat.HalfTopAndBottom, 3840, 806)]
    public void GetSwVidFilterChain_GraphicalSubtitles_CanvasMatchesFlattenedVideo(Video3DFormat? format, int width, int height)
    {
        var state = CreateState(format, true);

        var (_, subFilters, _) = CreateHelper(false).GetSwVidFilterChain(state, new EncodingOptions(), "libx264");

        Assert.Equal(
            $@"scale,scale=-1:{height}:fast_bilinear,crop,pad=max({width}\,iw):max({height}\,ih):(ow-iw)/2:(oh-ih)/2:black@0,crop={width}:{height}",
            Assert.Single(subFilters));
    }

    [Theory]
    [InlineData(true, Video3DFormat.FullSideBySide, false, "libplacebo=upscaler=none:downscaler=none:w=1920:h=806:crop_w=1920:crop_h=806:crop_x=0:crop_y=0:format=nv12,setsar=sar*1920/3840,format=vulkan", 0, 0)]
    [InlineData(true, Video3DFormat.HalfSideBySide, false, "libplacebo=upscaler=none:downscaler=none:w=3840:h=806:crop_w=1920:crop_h=806:crop_x=0:crop_y=0:format=nv12,format=vulkan", 0, 0)]
    [InlineData(true, Video3DFormat.FullTopAndBottom, false, "libplacebo=upscaler=none:downscaler=none:w=3840:h=402:crop_w=3840:crop_h=402:crop_x=0:crop_y=0:format=nv12,setsar=sar*806/402,format=vulkan", 0, 0)]
    [InlineData(true, Video3DFormat.HalfTopAndBottom, false, "libplacebo=upscaler=none:downscaler=none:w=3840:h=806:crop_w=3840:crop_h=402:crop_x=0:crop_y=0:format=nv12,format=vulkan", 0, 0)]
    [InlineData(true, Video3DFormat.FullSideBySide, true, "libplacebo=upscaler=none:downscaler=none:w=1920:h=806:crop_w=1920:crop_h=806:crop_x=0:crop_y=0:format=bgra:range=pc,setsar=sar*1920/3840,format=vulkan", 1920, 806)]
    [InlineData(true, Video3DFormat.HalfSideBySide, true, "libplacebo=upscaler=none:downscaler=none:w=3840:h=806:crop_w=1920:crop_h=806:crop_x=0:crop_y=0:format=bgra:range=pc,format=vulkan", 3840, 806)]
    [InlineData(true, Video3DFormat.FullTopAndBottom, true, "libplacebo=upscaler=none:downscaler=none:w=3840:h=402:crop_w=3840:crop_h=402:crop_x=0:crop_y=0:format=bgra:range=pc,setsar=sar*806/402,format=vulkan", 3840, 402)]
    [InlineData(true, Video3DFormat.HalfTopAndBottom, true, "libplacebo=upscaler=none:downscaler=none:w=3840:h=806:crop_w=3840:crop_h=402:crop_x=0:crop_y=0:format=bgra:range=pc,format=vulkan", 3840, 806)]
    [InlineData(false, Video3DFormat.FullSideBySide, true, "vpp_qsv=w=1920:h=806:cw=1920:ch=806:cx=0:cy=0", 1920, 806)]
    [InlineData(false, Video3DFormat.HalfSideBySide, true, "vpp_qsv=w=3840:h=806:cw=1920:ch=806:cx=0:cy=0", 3840, 806)]
    [InlineData(false, Video3DFormat.FullTopAndBottom, true, "vpp_qsv=w=3840:h=402:cw=3840:ch=402:cx=0:cy=0", 3840, 402)]
    [InlineData(false, Video3DFormat.HalfTopAndBottom, true, "vpp_qsv=w=3840:h=806:cw=3840:ch=402:cx=0:cy=0", 3840, 806)]
    public void GetVideoProcessingFilterParam_HardwareCrop_KeepsOneViewWithItsOwnAspect(
        bool isAmdVaapi,
        Video3DFormat format,
        bool graphicalSubtitles,
        string expected,
        int canvasWidth,
        int canvasHeight)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() && Environment.OSVersion.Version >= new Version(5, 15), "These hardware pipelines are checked on Linux 5.15 or newer");

        var state = CreateState(format, graphicalSubtitles);
        var options = CreateOptions(isAmdVaapi ? HardwareAccelerationType.vaapi : HardwareAccelerationType.qsv, false);

        var args = CreateHelper(isAmdVaapi).GetVideoProcessingFilterParam(state, options, isAmdVaapi ? "h264_vaapi" : "h264_qsv");

        Assert.Contains(expected, args, StringComparison.Ordinal);
        Assert.DoesNotContain("crop=trunc", args, StringComparison.Ordinal);
        if (graphicalSubtitles)
        {
            Assert.Contains($@"pad=max({canvasWidth}\,iw):max({canvasHeight}\,ih):(ow-iw)/2:(oh-ih)/2:black@0,crop={canvasWidth}:{canvasHeight}", args, StringComparison.Ordinal);
        }
    }

    private static EncodingJobInfo CreateState(Video3DFormat? format, bool graphicalSubtitles)
    {
        // Issue #18047: two 1920x806 views packed into one 3840x806 frame with square pixels.
        var video = new MediaStream
        {
            Index = 0,
            Type = MediaStreamType.Video,
            Codec = "h264",
            Profile = "High",
            Level = 41,
            Width = 3840,
            Height = 806,
            BitDepth = 8,
            PixelFormat = "yuv420p",
            RealFrameRate = 24,
            AverageFrameRate = 24
        };
        var streams = new List<MediaStream> { video };
        MediaStream? subtitle = null;
        if (graphicalSubtitles)
        {
            subtitle = new MediaStream { Index = 1, Type = MediaStreamType.Subtitle, Codec = "PGSSUB", Width = 1920, Height = 1080 };
            streams.Add(subtitle);
        }

        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaPath = "/media/movie.mkv",
            MediaSource = new MediaSourceInfo { Container = "mkv", MediaStreams = streams, Video3DFormat = format },
            VideoStream = video,
            SubtitleStream = subtitle,
            SubtitleDeliveryMethod = graphicalSubtitles ? SubtitleDeliveryMethod.Encode : SubtitleDeliveryMethod.Drop,
            OutputVideoCodec = "h264",
            BaseRequest = new VideoRequestDto(),
            IsVideoRequest = true,
            IsInputVideo = true
        };
    }

    private static EncodingOptions CreateOptions(HardwareAccelerationType hardwareAcceleration, bool preferSystemNativeDecoder)
    {
        return new EncodingOptions
        {
            HardwareAccelerationType = hardwareAcceleration,
            EnableHardwareEncoding = true,
            HardwareDecodingCodecs = ["h264"],
            PreferSystemNativeHwDecoder = preferSystemNativeDecoder,
            VaapiDevice = "/dev/dri/renderD128"
        };
    }

    private static EncodingHelper CreateHelper(bool isAmd)
    {
        var encoder = new Mock<IMediaEncoder>();
        encoder.Setup(x => x.SupportsHwaccel(It.IsAny<string>())).Returns(true);
        encoder.Setup(x => x.SupportsFilter(It.IsAny<string>())).Returns(true);
        encoder.Setup(x => x.SupportsFilterWithOption(It.IsAny<FilterOptionType>())).Returns(true);
        encoder.Setup(x => x.SupportsEncoder(It.IsAny<string>())).Returns(true);
        encoder.Setup(x => x.SupportsDecoder(It.IsAny<string>())).Returns(true);
        encoder.SetupGet(x => x.EncoderVersion).Returns(new Version(8, 1));
        encoder.SetupGet(x => x.IsVaapiDeviceAmd).Returns(isAmd);
        encoder.SetupGet(x => x.IsVaapiDeviceSupportVulkanDrmModifier).Returns(isAmd);
        encoder.SetupGet(x => x.IsVaapiDeviceSupportVulkanDrmInterop).Returns(isAmd);

        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            encoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }
}
