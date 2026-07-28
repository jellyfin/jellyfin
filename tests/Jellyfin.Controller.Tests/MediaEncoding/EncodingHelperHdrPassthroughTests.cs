using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
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

/// <summary>
/// Covers HDR passthrough, where a transcode keeps the BT.2020 primaries and the PQ or HLG
/// transfer of the source instead of tone mapping the picture down to BT.709 SDR.
/// </summary>
public class EncodingHelperHdrPassthroughTests
{
    [Fact]
    public void SwChain_PassthroughDisabled_TonemapsHdrToBt709()
    {
        var state = BuildHdr10State();
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: false), "hevc");

        // setparams tags the source as PQ so tonemapx knows what it is converting from. The
        // filter's own p/t/m arguments are what pin the result to BT.709, and the output
        // format stays 8-bit.
        Assert.Contains("tonemapx", filters, StringComparison.Ordinal);
        Assert.Contains("t=bt709:m=bt709:p=bt709", filters, StringComparison.Ordinal);
        Assert.DoesNotContain("yuv420p10le", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_PassthroughEnabled_KeepsPqAndBt2020AndDropsTonemap()
    {
        var state = BuildHdr10State();
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        Assert.DoesNotContain("tonemap", filters, StringComparison.Ordinal);
        Assert.Contains("color_trc=smpte2084", filters, StringComparison.Ordinal);
        Assert.Contains("color_primaries=bt2020", filters, StringComparison.Ordinal);
        Assert.Contains("colorspace=bt2020nc", filters, StringComparison.Ordinal);
        Assert.DoesNotContain("bt709", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_PassthroughEnabled_StaysTenBit()
    {
        var state = BuildHdr10State();
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        // Eight-bit output would quantise the HDR away even with the tags kept.
        Assert.Contains("format=yuv420p10le", filters, StringComparison.Ordinal);
        Assert.DoesNotContain("format=nv12", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_ClientDoesNotSupportHdr10_StillTonemaps()
    {
        // An SDR-only client must not be handed HDR, even with the option switched on.
        var state = BuildHdr10State(clientRangeTypes: "SDR");
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        Assert.Contains("tonemapx", filters, StringComparison.Ordinal);
        Assert.Contains("t=bt709:m=bt709:p=bt709", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_ClientDeclaresNothing_StillTonemaps()
    {
        // Unknown capabilities are not the same as HDR being safe to send.
        var state = BuildHdr10State(clientRangeTypes: null);
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        Assert.Contains("tonemapx", filters, StringComparison.Ordinal);
        Assert.Contains("t=bt709:m=bt709:p=bt709", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_H264Output_StillTonemaps()
    {
        // H.264 High 10 has no practical client support, so HDR cannot be delivered over it.
        var state = BuildHdr10State(outputCodec: "h264");
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "h264");

        Assert.Contains("tonemapx", filters, StringComparison.Ordinal);
        Assert.Contains("t=bt709:m=bt709:p=bt709", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_DolbyVisionProfile5_StillTonemaps()
    {
        // Profile 5 has no HDR10 base layer. Dropping the RPU would leave invalid colours,
        // so tone mapping remains the only correct option.
        var state = BuildDoviState(dvProfile: 5, dvBlCompatId: 0, hdr10Plus: false);
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        Assert.Equal(VideoRangeType.DOVI, state.VideoStream.VideoRangeType);
        Assert.Contains("tonemapx", filters, StringComparison.Ordinal);
        Assert.Contains("t=bt709:m=bt709:p=bt709", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_DolbyVisionProfile8WithHdr10Plus_KeepsPq()
    {
        // Profile 8.1 with HDR10+ keeps a static HDR10 base layer, so it degrades to HDR10
        // rather than breaking. This is the shape of a typical 4K WEB-DL.
        var state = BuildDoviState(dvProfile: 8, dvBlCompatId: 1, hdr10Plus: true);
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        Assert.Equal(VideoRangeType.DOVIWithHDR10Plus, state.VideoStream.VideoRangeType);
        Assert.DoesNotContain("tonemap", filters, StringComparison.Ordinal);
        Assert.Contains("color_trc=smpte2084", filters, StringComparison.Ordinal);
        Assert.Contains("format=yuv420p10le", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_HlgSource_KeepsHlgTransfer()
    {
        var state = BuildHdr10State(colorTransfer: "arib-std-b67", clientRangeTypes: "HLG");
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        Assert.Equal(VideoRangeType.HLG, state.VideoStream.VideoRangeType);
        Assert.DoesNotContain("tonemap", filters, StringComparison.Ordinal);
        Assert.Contains("color_trc=arib-std-b67", filters, StringComparison.Ordinal);
        Assert.Contains("color_primaries=bt2020", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_HlgSourceButClientOnlyDeclaresHdr10_StillTonemaps()
    {
        // HLG and HDR10 are not interchangeable transfers.
        var state = BuildHdr10State(colorTransfer: "arib-std-b67", clientRangeTypes: "HDR10");
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        Assert.Contains("tonemapx", filters, StringComparison.Ordinal);
        Assert.Contains("t=bt709:m=bt709:p=bt709", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void SwChain_SdrSource_IsUntouchedByThePassthroughOption()
    {
        var state = BuildHdr10State(colorTransfer: "bt709");
        var filters = CreateHelper().GetVideoProcessingFilterParam(state, SwOptions(passthrough: true), "hevc");

        Assert.Equal(VideoRange.SDR, state.VideoStream.VideoRange);
        Assert.Contains("color_trc=bt709", filters, StringComparison.Ordinal);
        Assert.DoesNotContain("yuv420p10le", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void Nvenc_PassthroughEnabled_SurvivesTheSoftwareFallback()
    {
        // The NVENC dispatch drops to the software chain when the full CUDA pipeline is not
        // available, which is what the mocked encoder reports. Passthrough has to survive that
        // fallback rather than quietly reverting to SDR.
        var options = SwOptions(passthrough: true);
        options.HardwareAccelerationType = HardwareAccelerationType.nvenc;
        var filters = CreateHelper().GetVideoProcessingFilterParam(BuildHdr10State(), options, "hevc");

        Assert.DoesNotContain("tonemap", filters, StringComparison.Ordinal);
        Assert.Contains("color_trc=smpte2084", filters, StringComparison.Ordinal);
        Assert.Contains("format=yuv420p10le", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void Vaapi_PassthroughEnabled_KeepsPqAndTenBit()
    {
        var options = SwOptions(passthrough: true);
        options.HardwareAccelerationType = HardwareAccelerationType.vaapi;
        var filters = CreateHelper().GetVideoProcessingFilterParam(BuildHdr10State(), options, "hevc");

        Assert.DoesNotContain("tonemap", filters, StringComparison.Ordinal);
        Assert.Contains("color_trc=smpte2084", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void EncoderMissing_StillTonemaps()
    {
        // Hardware that has no 10-bit encoder cannot carry HDR, whatever the option says.
        var options = SwOptions(passthrough: true);
        options.HardwareAccelerationType = HardwareAccelerationType.vaapi;
        var filters = CreateHelper(missingEncoder: "hevc_vaapi")
            .GetVideoProcessingFilterParam(BuildHdr10State(), options, "hevc");

        Assert.Contains("tonemapx", filters, StringComparison.Ordinal);
        Assert.Contains("t=bt709:m=bt709:p=bt709", filters, StringComparison.Ordinal);
    }

    [Fact]
    public void V4l2m2m_StillTonemaps()
    {
        // V4L2 M2M encoders are 8-bit only and have no 10-bit fallback.
        var options = SwOptions(passthrough: true);
        options.HardwareAccelerationType = HardwareAccelerationType.v4l2m2m;
        var filters = CreateHelper().GetVideoProcessingFilterParam(BuildHdr10State(), options, "hevc");

        Assert.Contains("tonemapx", filters, StringComparison.Ordinal);
    }

    private static EncodingOptions SwOptions(bool passthrough)
    {
        return new EncodingOptions
        {
            HardwareAccelerationType = HardwareAccelerationType.none,
            EnableTonemapping = true,
            EnableHdrPassthrough = passthrough,
        };
    }

    private static EncodingJobInfo BuildHdr10State(
        string colorTransfer = "smpte2084",
        string outputCodec = "hevc",
        string? clientRangeTypes = "HDR10")
    {
        var video = new MediaStream
        {
            Index = 0,
            Type = MediaStreamType.Video,
            Codec = "hevc",
            Profile = "Main 10",
            PixelFormat = "yuv420p10le",
            BitDepth = 10,
            Width = 3840,
            Height = 2160,
            ColorTransfer = colorTransfer,
            ColorPrimaries = "bt2020",
            ColorSpace = "bt2020nc",
        };

        return BuildState(video, outputCodec, clientRangeTypes);
    }

    private static EncodingJobInfo BuildDoviState(int dvProfile, int dvBlCompatId, bool hdr10Plus)
    {
        var video = new MediaStream
        {
            Index = 0,
            Type = MediaStreamType.Video,
            Codec = "hevc",
            Profile = "Main 10",
            PixelFormat = "yuv420p10le",
            BitDepth = 10,
            Width = 3840,
            Height = 2160,
            ColorTransfer = "smpte2084",
            ColorPrimaries = "bt2020",
            ColorSpace = "bt2020nc",
            DvProfile = dvProfile,
            DvBlSignalCompatibilityId = dvBlCompatId,
            RpuPresentFlag = 1,
            BlPresentFlag = 1,
            Hdr10PlusPresentFlag = hdr10Plus,
        };

        return BuildState(video, "hevc", "HDR10");
    }

    private static EncodingJobInfo BuildState(MediaStream video, string outputCodec, string? clientRangeTypes)
    {
        var audio = new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "eac3" };

        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            MediaSource = new MediaSourceInfo
            {
                Container = "mp4",
                MediaStreams = new List<MediaStream> { video, audio },
            },
            VideoStream = video,
            AudioStream = audio,
            SubtitleDeliveryMethod = SubtitleDeliveryMethod.Drop,
            BaseRequest = new VideoRequestDto { VideoRangeType = clientRangeTypes },
            OutputVideoCodec = outputCodec,
            IsVideoRequest = true,
            IsInputVideo = true,
        };
    }

    private static EncodingHelper CreateHelper(string? missingEncoder = null)
    {
        var appPaths = Mock.Of<IApplicationPaths>();
        var mediaEncoder = new Mock<IMediaEncoder>();
        var subtitleEncoder = new Mock<ISubtitleEncoder>();
        var config = new Mock<IConfiguration>();
        var configurationManager = new Mock<IConfigurationManager>();
        var pathManager = new Mock<IPathManager>();

        // The software tone mapping path is gated on this filter being present.
        mediaEncoder.Setup(e => e.SupportsFilter(It.IsAny<string>())).Returns(true);

        // Passthrough probes for the encoder that would actually run.
        mediaEncoder.Setup(e => e.SupportsEncoder(It.IsAny<string>()))
            .Returns((string enc) => !string.Equals(enc, missingEncoder, StringComparison.Ordinal));

        return new EncodingHelper(
            appPaths,
            mediaEncoder.Object,
            subtitleEncoder.Object,
            config.Object,
            configurationManager.Object,
            pathManager.Object);
    }
}
