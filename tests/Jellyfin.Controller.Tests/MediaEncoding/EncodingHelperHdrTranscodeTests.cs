using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding;

/// <summary>
/// The filter chains and encoder arguments used when HDR is carried through a transcode.
/// These are the graphs handed to ffmpeg, so a wrong format here means a broken stream.
/// </summary>
public class EncodingHelperHdrTranscodeTests
{
    [Theory]
    [InlineData("libx265")]
    [InlineData("hevc_nvenc")]
    [InlineData("hevc_qsv")]
    [InlineData("hevc_vaapi")]
    [InlineData("hevc_amf")]
    public void HdrTranscode_FeedsTenBitWithHdrColour(string encoder)
    {
        var filters = BuildChain(encoder, hdrTranscoding: true);

        Assert.Contains("p010", filters, StringComparison.Ordinal);
        Assert.Contains("color_primaries=bt2020", filters, StringComparison.Ordinal);
        Assert.DoesNotContain("tonemap", filters, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("libx265")]
    [InlineData("hevc_nvenc")]
    [InlineData("hevc_qsv")]
    [InlineData("hevc_vaapi")]
    [InlineData("hevc_amf")]
    public void HdrTranscode_Off_KeepsTheSdrPath(string encoder)
    {
        var filters = BuildChain(encoder, hdrTranscoding: false);

        // Without the option the chain still carries the source's HDR tags for the tone-mapper
        // to read, and the tone-mapping filter is what turns the output into SDR.
        Assert.Contains("tonemap", filters, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("hevc_nvenc", "scale_cuda")]
    [InlineData("hevc_qsv", "qsv")]
    [InlineData("hevc_vaapi", "scale_vaapi")]
    [InlineData("hevc_amf", "scale_opencl")]
    public void HdrTranscode_UsesItsOwnChain(string encoder, string backendFilter)
    {
        var filters = BuildChain(encoder, hdrTranscoding: true);

        Assert.Contains(backendFilter, filters, StringComparison.Ordinal);
    }

    [Fact]
    public void HdrTranscode_EncoderGetsMain10() =>
        Assert.Contains(
            "-profile:v:0 main10",
            BuildQualityParam("hevc_nvenc", hdrTranscoding: true),
            StringComparison.Ordinal);

    [Fact]
    public void HdrTranscode_Off_EncoderStaysMain() =>
        Assert.DoesNotContain(
            "main10",
            BuildQualityParam("hevc_nvenc", hdrTranscoding: false),
            StringComparison.Ordinal);

    private static string BuildQualityParam(string encoder, bool hdrTranscoding)
    {
        var helper = CreateHelper();
        var state = CreateState();
        var options = CreateOptions(hdrTranscoding, encoder);

        return helper.GetVideoQualityParam(state, encoder, options, EncoderPreset.medium);
    }

    private static string BuildChain(string encoder, bool hdrTranscoding)
    {
        var helper = CreateHelper();
        var state = CreateState();
        var options = CreateOptions(hdrTranscoding, encoder);
        var vidDecoder = encoder == "libx265" ? string.Empty : HwDecoderFor(encoder);

        var (main, sub, overlay) = encoder switch
        {
            "libx265" => helper.GetSwVidFilterChain(state, options, encoder),
            "hevc_nvenc" => helper.GetNvidiaVidFiltersPrefered(state, options, vidDecoder, encoder),
            "hevc_qsv" => helper.GetIntelQsvVaapiVidFiltersPrefered(state, options, vidDecoder, encoder),
            "hevc_vaapi" => helper.GetIntelVaapiFullVidFiltersPrefered(state, options, vidDecoder, encoder),
            "hevc_amf" => helper.GetAmdDx11VidFiltersPrefered(state, options, vidDecoder, encoder),
            _ => throw new ArgumentOutOfRangeException(nameof(encoder))
        };

        return string.Join(',', (main ?? []).Concat(sub ?? []).Concat(overlay ?? []));
    }

    private static string HwDecoderFor(string encoder) => encoder switch
    {
        "hevc_nvenc" => "cuda",
        "hevc_qsv" => "qsv",
        "hevc_vaapi" => "vaapi",
        "hevc_amf" => "d3d11va",
        _ => string.Empty
    };

    private static EncodingOptions CreateOptions(bool hdrTranscoding, string encoder) => new EncodingOptions
    {
        EnableHdrTranscoding = hdrTranscoding,
        EnableTonemapping = true,
        EnableHardwareEncoding = encoder != "libx265",
        AllowHevcEncoding = true,
        HardwareAccelerationType = encoder switch
        {
            "hevc_nvenc" => HardwareAccelerationType.nvenc,
            "hevc_qsv" => HardwareAccelerationType.qsv,
            "hevc_vaapi" => HardwareAccelerationType.vaapi,
            "hevc_amf" => HardwareAccelerationType.amf,
            _ => HardwareAccelerationType.none
        }
    };

    private static EncodingJobInfo CreateState()
    {
        var stream = new MediaStream
        {
            Type = MediaStreamType.Video,
            Codec = "av1",
            Width = 3840,
            Height = 1600,
            BitDepth = 10,
            ColorTransfer = "smpte2084",
            ColorPrimaries = "bt2020",
            ColorSpace = "bt2020nc",
            PixelFormat = "yuv420p10le"
        };

        return new EncodingJobInfo(TranscodingJobType.Hls)
        {
            VideoStream = stream,
            MediaSource = new MediaSourceInfo { Container = "mkv", MediaStreams = [stream] },
            BaseRequest = new VideoRequestDto { VideoRangeType = "HDR10" },
            OutputVideoCodec = "hevc",
            VideoType = VideoType.VideoFile,
            IsVideoRequest = true,
            IsInputVideo = true
        };
    }

    private static EncodingHelper CreateHelper()
    {
        var encoder = new Mock<IMediaEncoder>();
        encoder.Setup(x => x.SupportsFilter(It.IsAny<string>())).Returns(true);
        encoder.Setup(x => x.SupportsFilterWithOption(It.IsAny<FilterOptionType>())).Returns(true);
        encoder.Setup(x => x.SupportsEncoder(It.IsAny<string>())).Returns(true);
        encoder.Setup(x => x.SupportsHwaccel(It.IsAny<string>())).Returns(true);
        encoder.SetupGet(x => x.EncoderVersion).Returns(new Version(8, 1));
        encoder.SetupGet(x => x.IsVaapiDeviceInteliHD).Returns(true);

        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            encoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }
}
