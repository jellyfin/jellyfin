using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Controller.Tests;

public class EncodingHelperTests
{
    [Fact]
    public void GetVideoQualityParam_VaapiEncoder_WithVbrSupport_KeepsVbrRateControl()
    {
        var encodingHelper = CreateEncodingHelper(mediaEncoder =>
        {
            mediaEncoder.Setup(encoder => encoder.SupportsVaapiRateControlMode("hevc_vaapi", "VBR")).Returns(true);
        });
        var state = CreateState("hevc");

        var result = encodingHelper.GetVideoQualityParam(state, "hevc_vaapi", new EncodingOptions(), EncoderPreset.superfast);

        Assert.Contains(" -rc_mode VBR -b:v 5616000 -maxrate 5616000 -bufsize 11232000", result, StringComparison.Ordinal);
        Assert.DoesNotContain("CQP", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("h264_vaapi", "h264", 23)]
    [InlineData("hevc_vaapi", "hevc", 28)]
    [InlineData("av1_vaapi", "av1", 28)]
    public void GetVideoQualityParam_VaapiEncoder_WithCqpOnlySupport_UsesCqpRateControl(string videoEncoder, string outputCodec, int expectedQp)
    {
        var encodingHelper = CreateEncodingHelper(mediaEncoder =>
        {
            mediaEncoder.Setup(encoder => encoder.SupportsVaapiRateControlMode(videoEncoder, "VBR")).Returns(false);
            mediaEncoder.Setup(encoder => encoder.SupportsVaapiRateControlMode(videoEncoder, "CQP")).Returns(true);
        });
        var state = CreateState(outputCodec);

        var result = encodingHelper.GetVideoQualityParam(state, videoEncoder, new EncodingOptions(), EncoderPreset.superfast);

        Assert.Contains($" -rc_mode CQP -qp {expectedQp}", result, StringComparison.Ordinal);
        Assert.DoesNotContain("CBR", result, StringComparison.Ordinal);
        Assert.DoesNotContain("VBR", result, StringComparison.Ordinal);
        Assert.DoesNotContain(" -b:v ", result, StringComparison.Ordinal);
        Assert.DoesNotContain("maxrate", result, StringComparison.Ordinal);
        Assert.DoesNotContain("bufsize", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoQualityParam_VaapiEncoder_WithInteli965_KeepsCbrRateControl()
    {
        var encodingHelper = CreateEncodingHelper(mediaEncoder =>
        {
            mediaEncoder.Setup(encoder => encoder.IsVaapiDeviceInteli965).Returns(true);
            mediaEncoder.Setup(encoder => encoder.SupportsVaapiRateControlMode("h264_vaapi", "VBR")).Returns(false);
            mediaEncoder.Setup(encoder => encoder.SupportsVaapiRateControlMode("h264_vaapi", "CQP")).Returns(true);
        });
        var state = CreateState("h264");

        var result = encodingHelper.GetVideoQualityParam(state, "h264_vaapi", new EncodingOptions(), EncoderPreset.superfast);

        Assert.Contains(" -rc_mode CBR -b:v 5616000 -maxrate 5616000 -bufsize 11232000", result, StringComparison.Ordinal);
        Assert.DoesNotContain("CQP", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("h264_vaapi", "h264", 99, 28, 23)]
    [InlineData("hevc_vaapi", "hevc", 23, -1, 28)]
    [InlineData("av1_vaapi", "av1", 23, 52, 28)]
    public void GetVideoQualityParam_VaapiEncoder_InvalidConfiguredQp_UsesDefault(
        string videoEncoder,
        string outputCodec,
        int h264Crf,
        int h265Crf,
        int expectedQp)
    {
        var encodingHelper = CreateEncodingHelper(mediaEncoder =>
        {
            mediaEncoder.Setup(encoder => encoder.SupportsVaapiRateControlMode(videoEncoder, "VBR")).Returns(false);
            mediaEncoder.Setup(encoder => encoder.SupportsVaapiRateControlMode(videoEncoder, "CQP")).Returns(true);
        });
        var state = CreateState(outputCodec);
        var encodingOptions = new EncodingOptions
        {
            H264Crf = h264Crf,
            H265Crf = h265Crf
        };

        var result = encodingHelper.GetVideoQualityParam(state, videoEncoder, encodingOptions, EncoderPreset.superfast);

        Assert.Contains($" -rc_mode CQP -qp {expectedQp}", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoQualityParam_NonVaapiEncoder_KeepsBitrateControl()
    {
        var encodingHelper = CreateEncodingHelper();
        var state = CreateState("h264");

        var result = encodingHelper.GetVideoQualityParam(state, "libx264", new EncodingOptions(), EncoderPreset.superfast);

        Assert.Contains(" -maxrate 5616000 -bufsize 11232000", result, StringComparison.Ordinal);
        Assert.DoesNotContain("rc_mode", result, StringComparison.Ordinal);
    }

    private static EncodingHelper CreateEncodingHelper(Action<Mock<IMediaEncoder>>? configureMediaEncoder = null)
    {
        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.Setup(encoder => encoder.EncoderVersion).Returns(new Version(5, 0));
        configureMediaEncoder?.Invoke(mediaEncoder);

        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            mediaEncoder.Object,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    private static EncodingJobInfo CreateState(string outputVideoCodec)
    {
        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            BaseRequest = new BaseEncodingJobOptions(),
            OutputVideoBitrate = 5616000,
            OutputVideoCodec = outputVideoCodec,
            VideoStream = new MediaStream
            {
                Codec = outputVideoCodec
            }
        };
    }
}
