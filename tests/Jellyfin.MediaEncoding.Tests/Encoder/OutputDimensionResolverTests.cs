using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Encoder;

public class OutputDimensionResolverTests
{
    private const string X264 = "libx264";
    private const string Mjpeg = "mjpeg";
    private const string V4l2Enc = "h264_v4l2m2m";
    private const string VaapiEnc = "h264_vaapi";
    private const string Copy = "copy";

    [Theory]
    // canonical odd drift
    [InlineData(1920, 1080, null, 0, null, null, 720, null, null, X264, HardwareAccelerationType.none, false, 718, 404)]
    // no scale
    [InlineData(1920, 1080, null, 0, null, null, null, null, null, X264, HardwareAccelerationType.none, false, 1920, 1080)]
    // v4l2m2m mod-64 (always SW chain — no scale_v4l2m2m filter exists)
    [InlineData(1920, 1080, null, 0, null, null, 1000, null, null, V4l2Enc, HardwareAccelerationType.v4l2m2m, false, 960, 540)]
    // rotation
    [InlineData(1920, 1080, null, 180, null, null, 720, null, null, X264, HardwareAccelerationType.none, false, 718, 404)]
    [InlineData(1920, 1080, null, 90,  null, null, 720, null, null, X264, HardwareAccelerationType.none, false, 720, 1280)]
    [InlineData(1920, 1080, null, -90, null, null, 720, null, null, X264, HardwareAccelerationType.none, false, 720, 1280)]
    [InlineData(1920, 1080, null, 270, null, null, 720, null, null, X264, HardwareAccelerationType.none, false, 720, 1280)]
    // mjpeg anamorphic
    [InlineData(720, 576, "16:11", 0, null, null, 640, null, null, Mjpeg, HardwareAccelerationType.none, false, 636, 350)]
    // x264 anamorphic
    [InlineData(720, 576, "16:11", 0, null, null, 640, null, null, X264, HardwareAccelerationType.none, false, 931, 512)]
    // odd MaxW
    [InlineData(1280, 720,  null, 0, null, null, 999, null, null, X264, HardwareAccelerationType.none, false, 996, 560)]
    [InlineData(1920, 1080, null, 0, null, null, 999, null, null, X264, HardwareAccelerationType.none, false, 996, 560)]
    // fixed W+H
    [InlineData(1920, 1080, null, 0, 1280, 720, null, null, null, X264, HardwareAccelerationType.none, false, 1280, 720)]
    [InlineData(1920, 1080, null, 0, 640,  480, null, null, null, X264, HardwareAccelerationType.none, false, 640, 360)]
    // fixed W only / fixed H only
    [InlineData(1920, 1080, null, 0, 1280, null, null, null, null, X264, HardwareAccelerationType.none, false, 1280, 720)]
    [InlineData(1920, 1080, null, 0, null, 720,  null, null, null, X264, HardwareAccelerationType.none, false, 1280, 720)]
    // MaxW + MaxH
    [InlineData(1920, 1080, null, 0, null, null, 1280, 400, null, X264, HardwareAccelerationType.none, false, 711, 400)]
    // MaxH only
    [InlineData(1920, 1080, null, 0, null, null, null, 400, null, X264, HardwareAccelerationType.none, false, 711, 400)]
    // 3D formats (all collapse to MaxW=720 via EnforceResolutionLimit)
    [InlineData(1920, 1080, null, 0, 720, null, null, null, Video3DFormat.HalfSideBySide, X264, HardwareAccelerationType.none, false, 718, 404)]
    [InlineData(1920, 1080, null, 0, 720, null, null, null, Video3DFormat.HalfTopAndBottom, X264, HardwareAccelerationType.none, false, 718, 404)]
    [InlineData(1920, 1080, null, 0, 720, null, null, null, Video3DFormat.FullSideBySide, X264, HardwareAccelerationType.none, false, 718, 404)]
    [InlineData(1920, 1080, null, 0, 720, null, null, null, Video3DFormat.FullTopAndBottom, X264, HardwareAccelerationType.none, false, 718, 404)]
    [InlineData(1920, 1080, null, 0, 720, null, null, null, Video3DFormat.MVC, X264, HardwareAccelerationType.none, false, 718, 404)]
    // 3D + rotation
    [InlineData(1920, 1080, null, 90, 720, null, null, null, Video3DFormat.HalfSideBySide,   X264, HardwareAccelerationType.none, false, 720, 1280)]
    [InlineData(1920, 1080, null, 90, 720, null, null, null, Video3DFormat.FullTopAndBottom, X264, HardwareAccelerationType.none, false, 720, 1280)]
    // transcode + rotation + anamorphic (SAR rotates with frame: 16:11 → 11:16, 32:27 → 27:32)
    [InlineData(720, 576, "16:11", 90,  null, null, 640, null, null, X264, HardwareAccelerationType.none, false, 396, 720)]
    [InlineData(720, 480, "32:27", -90, 400, null, null, null, null, X264, HardwareAccelerationType.none, false, 338, 600)]
    // v4l2m2m fixed W+H
    [InlineData(1920, 1080, null, 0, 720, 400, null, null, null, V4l2Enc, HardwareAccelerationType.v4l2m2m, false, 711, 400)]
    // malformed SAR
    [InlineData(1920, 1080, "bad", 0, null, null, 720, null, null, X264, HardwareAccelerationType.none, false, 718, 404)]
    // null encoder
    [InlineData(1920, 1080, null, 0, null, null, 720, null, null, null, HardwareAccelerationType.none, false, 718, 404)]
    // float-precision quirk: trunc(ow/a/2)*2 drops 2 pixels; 7584x1862 → 7576x1860
    [InlineData(7584, 1862, null, 0, null, null, 7584, null, null, X264, HardwareAccelerationType.none, false, 7576, 1860)]
    // HW path with HW decoder: 8K capped to 4K
    [InlineData(7680, 4320, null, 0, null, null, null, null, null, VaapiEnc,            HardwareAccelerationType.vaapi,         true, 4096, 2304)]
    [InlineData(7680, 4320, null, 0, null, null, null, null, null, "h264_qsv",          HardwareAccelerationType.qsv,           true, 4096, 2304)]
    [InlineData(7680, 4320, null, 0, null, null, null, null, null, "h264_nvenc",        HardwareAccelerationType.nvenc,         true, 4096, 2304)]
    [InlineData(7680, 4320, null, 0, null, null, null, null, null, "h264_videotoolbox", HardwareAccelerationType.videotoolbox, true, 4096, 2304)]
    [InlineData(7680, 4320, null, 0, null, null, null, null, null, "h264_amf",          HardwareAccelerationType.amf,           true, 4096, 2304)]
    [InlineData(7680, 4320, null, 0, null, null, null, null, null, "h264_rkmpp",        HardwareAccelerationType.rkmpp,         true, 4096, 2304)]
    // HW accel configured but no HW decoder — chain falls back to SW scale internally
    [InlineData(1920, 1080, null, 0, null, null, 720, null, null, VaapiEnc,     HardwareAccelerationType.vaapi, false, 718, 404)]
    [InlineData(1920, 1080, null, 0, null, null, 720, null, null, "h264_qsv",   HardwareAccelerationType.qsv,   false, 718, 404)]
    [InlineData(1920, 1080, null, 0, null, null, 720, null, null, "h264_nvenc", HardwareAccelerationType.nvenc, false, 718, 404)]
    [InlineData(1920, 1080, null, 0, null, null, 720, null, null, "h264_amf",   HardwareAccelerationType.amf,   false, 718, 404)]
    [InlineData(1920, 1080, null, 0, null, null, 720, null, null, "h264_rkmpp", HardwareAccelerationType.rkmpp, false, 718, 404)]
    // VideoToolbox is HW-scale only (no SW fallback inside its chain)
    [InlineData(1920, 1080, null, 0, null, null, 720, null, null, "h264_videotoolbox", HardwareAccelerationType.videotoolbox, false, 720, 404)]
    // stream copy
    [InlineData(1920, 1080, null,    0, null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 1920, 1080)]
    [InlineData(1920, 1080, "1:1",   0, null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 1920, 1080)]
    [InlineData(720,  576,  "16:11", 0, null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 1047, 576)]
    [InlineData(720,  480,  "8:9",   0, null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 640,  480)]
    [InlineData(720,  480,  "32:27", 0, null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 853,  480)]
    [InlineData(720,  576,  "10:11", 0, null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 655,  576)]
    // stream copy + rotation (storage stays as source; rotation is metadata for the player)
    [InlineData(1920, 1080, null, 90,  null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 1920, 1080)]
    [InlineData(1920, 1080, null, -90, null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 1920, 1080)]
    // stream copy + rotation + anamorphic (RESOLUTION = storage × source SAR, ignores rotation)
    [InlineData(720, 576, "16:11", 90,  null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 1047, 576)]
    [InlineData(720, 480, "32:27", -90, null, null, null, null, null, Copy, HardwareAccelerationType.none, false, 853, 480)]
    // stream copy + 3D
    [InlineData(1920, 1080, null, 0, null, null, null, null, Video3DFormat.HalfSideBySide, Copy, HardwareAccelerationType.none, false, 1920, 1080)]
    // Width=0 / Height=0 treated as unset
    [InlineData(1920, 1080, null, 0, 0,    0,    null, null, null, X264, HardwareAccelerationType.none, false, 1920, 1080)]
    // HW with explicit scale (videotoolbox drops source SAR)
    [InlineData(1920, 1080, null,    0, null, null, 720,  null, null, "h264_videotoolbox", HardwareAccelerationType.videotoolbox, true, 720, 404)]
    [InlineData(1920, 1080, null,    0, null, null, 1280, 720,  null, "h264_videotoolbox", HardwareAccelerationType.videotoolbox, true, 1280, 720)]
    [InlineData(720,  576,  "16:11", 0, null, null, 640,  null, null, "h264_videotoolbox", HardwareAccelerationType.videotoolbox, true, 640, 512)]
    public void Resolve_ResolvesDims(
        int srcW,
        int srcH,
        string? sar,
        int rotation,
        int? w,
        int? h,
        int? maxW,
        int? maxH,
        Video3DFormat? threeD,
        string? encoder,
        HardwareAccelerationType hwAccel,
        bool hasHardwareDecoder,
        int expectedW,
        int expectedH)
    {
        var state = BuildState(srcW, srcH, maxW, maxH, w, h, sar, rotation, threeD);
        var options = new EncodingOptions { HardwareAccelerationType = hwAccel };

        OutputDimensionResolver.Resolve(state, options, encoder, hasHardwareDecoder);

        Assert.Equal(expectedW, state.ResolvedOutputWidth);
        Assert.Equal(expectedH, state.ResolvedOutputHeight);
    }

    [Theory]
    [InlineData(null, 1, 1)]
    [InlineData("", 1, 1)]
    [InlineData("1:1", 1, 1)]
    [InlineData("16:11", 16, 11)]
    [InlineData("bad", 1, 1)]
    [InlineData("0:1", 1, 1)] // zero-num — defensive fallback.
    [InlineData("1:0", 1, 1)] // zero-den — defensive fallback.
    [InlineData("-1:1", 1, 1)] // negatives — defensive fallback.
    [InlineData("1:-1", 1, 1)]
    [InlineData("abc:def", 1, 1)]
    [InlineData("1:2:3", 1, 1)] // too many parts.
    public void ParseSampleAspectRatio_ValidOrMalformed_ReturnsExpected(string? input, int expectedNum, int expectedDen)
    {
        var (num, den) = OutputDimensionResolver.ParseSampleAspectRatio(input);
        Assert.Equal(expectedNum, num);
        Assert.Equal(expectedDen, den);
    }

    [Fact]
    public void Resolve_NoVideoStream_LeavesResolvedDimsUnset()
    {
        var state = new EncodingJobInfo(TranscodingJobType.Hls)
        {
            IsVideoRequest = true,
            BaseRequest = new BaseEncodingJobOptions { MaxWidth = 720 },
        };

        OutputDimensionResolver.Resolve(state, new EncodingOptions(), X264, hasHardwareDecoder: false);

        Assert.Null(state.ResolvedOutputWidth);
        Assert.Null(state.ResolvedOutputHeight);
    }

    [Fact]
    public void Resolve_AlreadyResolved_LeavesExistingValuesUntouched()
    {
        var state = BuildState(srcW: 1920, srcH: 1080, maxW: 720, maxH: null);
        state.ResolvedOutputWidth = 1;
        state.ResolvedOutputHeight = 2;

        OutputDimensionResolver.Resolve(state, new EncodingOptions(), X264, hasHardwareDecoder: false);

        Assert.Equal(1, state.ResolvedOutputWidth);
        Assert.Equal(2, state.ResolvedOutputHeight);
    }

    private static EncodingJobInfo BuildState(
        int srcW,
        int srcH,
        int? maxW,
        int? maxH,
        int? w = null,
        int? h = null,
        string? sar = null,
        int rotation = 0,
        Video3DFormat? threeD = null)
    {
        return new EncodingJobInfo(TranscodingJobType.Hls)
        {
            IsVideoRequest = true,
            BaseRequest = new BaseEncodingJobOptions
            {
                Width = w,
                Height = h,
                MaxWidth = maxW,
                MaxHeight = maxH,
            },
            VideoStream = new MediaStream
            {
                Type = MediaStreamType.Video,
                Width = srcW,
                Height = srcH,
                SampleAspectRatio = sar,
                Rotation = rotation,
            },
            MediaSource = new MediaSourceInfo { Video3DFormat = threeD },
        };
    }
}
