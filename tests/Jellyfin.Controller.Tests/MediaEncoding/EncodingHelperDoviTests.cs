using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperDoviTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("bt709", false)]
    [InlineData("unknown", false)]
    [InlineData("bt2020-10", false)]
    [InlineData("smpte2084", true)]
    [InlineData("arib-std-b67", true)]
    public void GetSwVidFilterChain_InvalidDovi_OnlyTonemapsHdrBaseLayer(string? transfer, bool tonemap)
    {
        var state = CreateState("hevc", transfer);
        var helper = CreateHelper(true);

        var (filters, _, _) = helper.GetSwVidFilterChain(state, new EncodingOptions(), "libx264");
        var args = string.Join(',', filters);

        Assert.Equal(VideoRangeType.DOVIInvalid, state.VideoStream.VideoRangeType);
        Assert.Equal(tonemap, args.Contains("tonemapx=", StringComparison.Ordinal));
        Assert.Contains(tonemap ? "color_trc=" + transfer : "color_trc=bt709", args, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("bt709", false)]
    [InlineData("arib-std-b67", false)]
    [InlineData("smpte2084", true)]
    [InlineData("SMPTE2084", true)]
    public void IsDoviWithHdr10Bl_InvalidDovi_RequiresPq(string? transfer, bool expected)
    {
        var stream = CreateState("hevc", transfer).VideoStream;

        Assert.True(EncodingHelper.IsDovi(stream));
        Assert.Equal(expected, EncodingHelper.IsDoviWithHdr10Bl(stream));
    }

    [Theory]
    [InlineData("hevc", null, "hevc_metadata=remove_dovi=1")]
    [InlineData("hevc", "bt709", "hevc_metadata=remove_dovi=1")]
    [InlineData("hevc", "smpte2084", "hevc_metadata=remove_dovi=1")]
    [InlineData("hevc", "arib-std-b67", "hevc_metadata=remove_dovi=1")]
    [InlineData("av1", null, "av1_metadata=remove_dovi=1")]
    [InlineData("av1", "bt709", "av1_metadata=remove_dovi=1")]
    [InlineData("av1", "smpte2084", "av1_metadata=remove_dovi=1")]
    [InlineData("av1", "arib-std-b67", "av1_metadata=remove_dovi=1")]
    public void GetBitStreamArgs_InvalidDovi_PreservesClientDependentRemoval(string codec, string? transfer, string expected)
    {
        var state = CreateState(codec, transfer);
        var helper = CreateHelper(true);

        foreach (var (requestedRanges, removeDovi) in new[] { (null, false), ("SDR", false), ("HDR10", false), ("DOVIWithEL", false), ("DOVI", true), ("SDR,DOVI", true) })
        {
            state.BaseRequest.VideoRangeType = requestedRanges;

            Assert.Equal(removeDovi, helper.IsDoviRemoved(state));
            if (removeDovi)
            {
                Assert.Contains(expected, helper.GetBitStreamArgs(state, MediaStreamType.Video), StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(codec == "hevc" ? "-bsf:v hevc_mp4toannexb" : null, helper.GetBitStreamArgs(state, MediaStreamType.Video));
            }

            Assert.False(CreateHelper(false).IsDoviRemoved(state));
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("HDR10", true)]
    [InlineData("DOVI", false)]
    [InlineData("SDR,DOVI", false)]
    public void CanStreamCopyVideo_InvalidDovi_RequiresRemovalSupportOnlyForDoviClients(string? requestedRanges, bool copyWithoutRemovalSupport)
    {
        foreach (var codec in new[] { "hevc", "av1" })
        {
            foreach (var transfer in new[] { "bt709", "smpte2084" })
            {
                var state = CreateState(codec, transfer);
                state.BaseRequest.VideoRangeType = requestedRanges;

                Assert.True(CreateHelper(true).CanStreamCopyVideo(state, state.VideoStream));
                Assert.Equal(copyWithoutRemovalSupport, CreateHelper(false).CanStreamCopyVideo(state, state.VideoStream));
            }
        }
    }

    [Fact]
    public void GetBitStreamArgs_ValidDovi_PreservesMetadata()
    {
        var state = CreateState("hevc", "smpte2084");
        state.VideoStream.ColorSpace = "bt2020nc";
        state.VideoStream.ColorPrimaries = "bt2020";
        state.BaseRequest.VideoRangeType = "DOVIWithEL";
        var helper = CreateHelper(true);

        Assert.False(helper.IsDoviRemoved(state));
        Assert.Equal("-bsf:v hevc_mp4toannexb", helper.GetBitStreamArgs(state, MediaStreamType.Video));
    }

    [Theory]
    [InlineData(5, true, true)]
    [InlineData(7, true, true)]
    [InlineData(8, true, true)]
    [InlineData(null, true, false)]
    [InlineData(5, false, false)]
    public void RequiresLibplaceboDoviToneMapping_ProfilesAndOptions(int? profile, bool enableTonemapping, bool expected)
    {
        var options = new EncodingOptions { EnableTonemapping = enableTonemapping };
        var stream = new MediaStream
        {
            Type = MediaStreamType.Video,
            DvProfile = profile,
            DvBlSignalCompatibilityId = profile switch
            {
                5 => 0,
                7 => 6,
                8 => 1,
                _ => 0
            },
            RpuPresentFlag = profile.HasValue ? 1 : 0,
            BlPresentFlag = profile.HasValue ? 1 : 0,
            ColorTransfer = profile is 7 or 8 ? "smpte2084" : null,
            ColorSpace = profile is 7 or 8 ? "bt2020nc" : null,
            ColorPrimaries = profile is 7 or 8 ? "bt2020" : null
        };

        var result = EncodingHelper.RequiresLibplaceboDoviToneMapping(stream, options, isTranscodingToSdr: true);
        Assert.Equal(expected, result);

        // When not transcoding to SDR, should always be false
        var resultHdr = EncodingHelper.RequiresLibplaceboDoviToneMapping(stream, options, isTranscodingToSdr: false);
        Assert.False(resultHdr);
    }

    [Fact]
    public void RequiresLibplaceboDoviToneMapping_StateOverload_EvaluatesCorrectly()
    {
        var options = new EncodingOptions { EnableTonemapping = true };
        var state = CreateState("hevc", "smpte2084");
        state.OutputVideoCodec = "h264";
        state.VideoStream.DvProfile = 5;
        state.VideoStream.DvBlSignalCompatibilityId = 0;
        state.BaseRequest.VideoRangeType = "SDR";

        Assert.True(EncodingHelper.RequiresLibplaceboDoviToneMapping(state, options));

        // When target is HDR, should return false
        state.BaseRequest.VideoRangeType = "HDR10";
        Assert.False(EncodingHelper.RequiresLibplaceboDoviToneMapping(state, options));
    }

    [Fact]
    public void IsVulkanHwTonemapAvailable_ReturnsTrue_ForDolbyVision()
    {
        var helper = CreateHelper(true);
        var options = new EncodingOptions { EnableTonemapping = true };
        var state = CreateState("hevc", "smpte2084");
        state.OutputVideoCodec = "h264";
        state.VideoStream.DvProfile = 5;
        state.VideoStream.DvBlSignalCompatibilityId = 0;
        state.BaseRequest.VideoRangeType = "SDR";

        Assert.True(helper.IsVulkanHwTonemapAvailable(state, options));
    }

    [Fact]
    public void GetInputVideoHwaccelArgs_DolbyVisionVaapi_InitializesVulkanViaDrm()
    {
        var helper = CreateVaapiVulkanHelper(isInteliHD: true, supportsVulkanDrmInterop: true);
        var options = new EncodingOptions
        {
            HardwareAccelerationType = HardwareAccelerationType.vaapi,
            VaapiDevice = "/dev/dri/renderD128",
            EnableTonemapping = true
        };
        var state = CreateState("hevc", "smpte2084");
        state.VideoStream.DvProfile = 5;
        state.VideoStream.DvBlSignalCompatibilityId = 0;
        state.BaseRequest.VideoRangeType = "SDR";
        state.OutputVideoCodec = "h264_vaapi";

        var args = helper.GetInputVideoHwaccelArgs(state, options);

        Assert.Contains("-init_hw_device drm=dr:/dev/dri/renderD128", args, StringComparison.Ordinal);
        Assert.Contains("-init_hw_device vaapi=va@dr", args, StringComparison.Ordinal);
        Assert.Contains("-init_hw_device vulkan=vk@dr", args, StringComparison.Ordinal);
        Assert.Contains("-filter_hw_device vk", args, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVaapiVidFilterChain_DolbyVisionVaapi_UsesVulkanLibplacebo()
    {
        var helper = CreateVaapiVulkanHelper(isInteliHD: true, supportsVulkanDrmInterop: true);
        var options = new EncodingOptions
        {
            HardwareAccelerationType = HardwareAccelerationType.vaapi,
            VaapiDevice = "/dev/dri/renderD128",
            EnableTonemapping = true,
            TonemappingAlgorithm = TonemappingAlgorithm.bt2390,
            HardwareDecodingCodecs = ["h264", "hevc", "vc1"]
        };
        var state = CreateState("hevc", "smpte2084");
        state.VideoStream.PixelFormat = "yuv420p10le";
        state.VideoStream.DvProfile = 5;
        state.VideoStream.DvBlSignalCompatibilityId = 0;
        state.BaseRequest.VideoRangeType = "SDR";
        state.OutputVideoCodec = "h264_vaapi";

        var (mainFilters, _, _) = helper.GetVaapiVidFilterChain(state, options, "h264_vaapi");
        var filterChain = string.Join(',', mainFilters);

        Assert.Contains("libplacebo=", filterChain, StringComparison.Ordinal);
        Assert.Contains("tonemapping=bt.2390", filterChain, StringComparison.Ordinal);
        Assert.Contains("format=drm_prime", filterChain, StringComparison.Ordinal);
    }

    private static EncodingJobInfo CreateState(string codec, string? transfer)
    {
        var stream = new MediaStream
        {
            Type = MediaStreamType.Video,
            Codec = codec,
            Width = 1920,
            Height = 1080,
            BitDepth = 10,
            DvProfile = codec == "hevc" ? 7 : 10,
            DvBlSignalCompatibilityId = codec == "hevc" ? 6 : 1,
            RpuPresentFlag = 1,
            BlPresentFlag = 1,
            ColorSpace = "bt709",
            ColorPrimaries = "bt709",
            ColorTransfer = transfer
        };

        return new EncodingJobInfo(TranscodingJobType.Hls)
        {
            VideoStream = stream,
            MediaSource = new MediaSourceInfo { Container = "mkv", MediaStreams = [stream] },
            BaseRequest = new VideoRequestDto(),
            OutputVideoCodec = "copy",
            IsVideoRequest = true,
            IsInputVideo = true
        };
    }

    private static EncodingHelper CreateHelper(bool supportsRemoval)
    {
        var encoder = new Mock<IMediaEncoder>();
        encoder.Setup(x => x.SupportsBitStreamFilterWithOption(It.IsAny<BitStreamFilterOptionType>())).Returns(supportsRemoval);
        encoder.Setup(x => x.SupportsFilter("tonemapx")).Returns(true);
        encoder.SetupGet(x => x.EncoderVersion).Returns(new Version(8, 1));

        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            encoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    private static EncodingHelper CreateVaapiVulkanHelper(bool isInteliHD, bool supportsVulkanDrmInterop)
    {
        var encoder = new Mock<IMediaEncoder>();
        encoder.Setup(x => x.SupportsHwaccel("vaapi")).Returns(true);
        encoder.Setup(x => x.SupportsHwaccel("vulkan")).Returns(true);
        encoder.Setup(x => x.SupportsHwaccel("drm")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("libplacebo")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("scale_vulkan")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("transpose_vulkan")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("flip_vulkan")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("scale_vaapi")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("deinterlace_vaapi")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("tonemap_vaapi")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("procamp_vaapi")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("transpose_vaapi")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("hwupload_vaapi")).Returns(true);
        encoder.Setup(x => x.SupportsFilter("alphasrc")).Returns(true);
        encoder.Setup(x => x.SupportsFilterWithOption(FilterOptionType.OverlayVaapiFrameSync)).Returns(true);
        encoder.Setup(x => x.SupportsFilterWithOption(FilterOptionType.OverlayVulkanFrameSync)).Returns(true);
        encoder.Setup(x => x.SupportsDecoder("hevc_vaapi")).Returns(true);
        encoder.Setup(x => x.SupportsEncoder("h264_vaapi")).Returns(true);
        encoder.SetupGet(x => x.IsVaapiDeviceInteliHD).Returns(isInteliHD);
        encoder.SetupGet(x => x.IsVaapiDeviceSupportVulkanDrmInterop).Returns(supportsVulkanDrmInterop);
        encoder.SetupGet(x => x.IsVaapiDeviceSupportVulkanDrmModifier).Returns(true);
        encoder.SetupGet(x => x.EncoderVersion).Returns(new Version(8, 1));

        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            encoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }
}
