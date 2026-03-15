using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class EncodingHelperTests
{
    private readonly EncodingHelper _encodingHelper;

    public EncodingHelperTests()
    {
        _encodingHelper = new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            Mock.Of<IMediaEncoder>(),
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    private static EncodingJobInfo CreateJobInfo(int? videoBitrate)
    {
        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            OutputVideoBitrate = videoBitrate,
            BaseRequest = new BaseEncodingJobOptions()
        };
    }

    private static EncodingJobInfo CreateJobInfoWithLevel(int? videoBitrate, string level)
    {
        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            OutputVideoBitrate = videoBitrate,
            BaseRequest = new BaseEncodingJobOptions { Level = level }
        };
    }

    // -- Basic parameter generation --

    [Fact]
    public void GetVideoBitrateParam_NullBitrate_ReturnsEmpty()
    {
        var state = CreateJobInfo(null);
        var result = _encodingHelper.GetVideoBitrateParam(state, "libx264");
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("libx264")]
    [InlineData("libx265")]
    public void GetVideoBitrateParam_LibX26X_ReturnsMaxrateAndBufsize(string codec)
    {
        var state = CreateJobInfo(5000);
        var result = _encodingHelper.GetVideoBitrateParam(state, codec);
        Assert.Contains("-maxrate 5000", result, StringComparison.Ordinal);
        Assert.Contains("-bufsize 10000", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("h264_qsv")]
    [InlineData("hevc_qsv")]
    public void GetVideoBitrateParam_Qsv_ContainsMbbrc(string codec)
    {
        var state = CreateJobInfo(5000);
        var result = _encodingHelper.GetVideoBitrateParam(state, codec);
        Assert.Contains("-mbbrc 1", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoBitrateParam_Av1Qsv_DoesNotContainMbbrc()
    {
        var state = CreateJobInfo(5000);
        var result = _encodingHelper.GetVideoBitrateParam(state, "av1_qsv");
        Assert.DoesNotContain("-mbbrc", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("h264_qsv")]
    [InlineData("hevc_qsv")]
    [InlineData("av1_qsv")]
    public void GetVideoBitrateParam_Qsv_ContainsExpectedParams(string codec)
    {
        var state = CreateJobInfo(5000);
        var result = _encodingHelper.GetVideoBitrateParam(state, codec);
        Assert.Contains("-b:v 5000", result, StringComparison.Ordinal);
        Assert.Contains("-maxrate 5001", result, StringComparison.Ordinal);
        Assert.Contains("-rc_init_occupancy 10000", result, StringComparison.Ordinal);
        Assert.Contains("-bufsize 20000", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVideoBitrateParam_VideoToolbox_NoBufsizeOrMaxrate()
    {
        var state = CreateJobInfo(5000);
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_videotoolbox");
        Assert.Contains("-b:v 5000", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-bufsize", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-maxrate", result, StringComparison.Ordinal);
    }

    // -- h264_qsv bitrate clamping --

    /// <summary>
    /// h264_qsv with bitrate below 1000 gets clamped to 1000 (QSV minimum).
    /// </summary>
    [Theory]
    [InlineData(500)]
    [InlineData(1)]
    [InlineData(999)]
    public void GetVideoBitrateParam_H264QsvBelowMinimum_ClampedTo1000(int lowBitrate)
    {
        var state = CreateJobInfo(lowBitrate);
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");
        Assert.Contains("-b:v 1000", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// h264_qsv bitrate is capped at 50 Mbps. Live TV streams can report absurdly
    /// high bitrates (e.g. 711 Mbps from TVHeadEnd) because the source has no
    /// bitrate metadata and Jellyfin falls back to a raw transport stream estimate.
    /// </summary>
    [Theory]
    [InlineData(711_120_679)]
    [InlineData(120_000_000)]
    [InlineData(80_000_000)]
    [InlineData(50_000_001)]
    public void GetVideoBitrateParam_H264QsvAbsurdBitrate_CappedTo50Mbps(int absurdBitrate)
    {
        var state = CreateJobInfo(absurdBitrate);
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");

        Assert.Contains("-b:v 50000000", result, StringComparison.Ordinal);
        Assert.DoesNotContain($"-b:v {absurdBitrate}", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Moderate h264_qsv bitrates that sit below the 50 Mbps cap pass through as-is.
    /// </summary>
    [Theory]
    [InlineData(8_000_000)]
    [InlineData(30_000_000)]
    [InlineData(49_999_999)]
    [InlineData(50_000_000)]
    public void GetVideoBitrateParam_H264QsvModerateBitrate_NotCapped(int bitrate)
    {
        var state = CreateJobInfo(bitrate);
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");
        Assert.Contains($"-b:v {bitrate}", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// hevc_qsv and av1_qsv should NOT be capped at 50 Mbps — the bitrate cap
    /// only applies to h264_qsv where H.264 level constraints are much tighter.
    /// </summary>
    [Theory]
    [InlineData("hevc_qsv", 120_000_000)]
    [InlineData("av1_qsv", 120_000_000)]
    public void GetVideoBitrateParam_HevcAv1Qsv_NotCappedAt50Mbps(string codec, int bitrate)
    {
        var state = CreateJobInfo(bitrate);
        var result = _encodingHelper.GetVideoBitrateParam(state, codec);
        Assert.Contains($"-b:v {bitrate}", result, StringComparison.Ordinal);
    }

    // -- Integer overflow prevention --

    /// <summary>
    /// With a very high bitrate, the QSV path computes bufsize = bitrate * 4.
    /// Without long arithmetic this would overflow int32 to a negative value
    /// (e.g. int.MaxValue/2 * 4 wraps to -4). Since h264_qsv bitrate is now
    /// capped at 50 Mbps, this test also validates the cap is applied first.
    /// </summary>
    [Fact]
    public void GetVideoBitrateParam_H264QsvMaxBitrate_NoOverflow()
    {
        int maxBitrate = int.MaxValue / 2;
        var state = CreateJobInfo(maxBitrate);
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");

        Assert.DoesNotContain("-bufsize -", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-maxrate -", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-rc_init_occupancy -", result, StringComparison.Ordinal);

        // Bitrate is capped to 50M first, so the subsequent multiplications stay in range
        Assert.Contains("-b:v 50000000", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// hevc_qsv and av1_qsv also need long arithmetic to prevent overflow in
    /// the bufsize and rc_init_occupancy computation. These codecs don't have
    /// a 50 Mbps cap, so the overflow protection via long arithmetic is critical.
    /// </summary>
    [Theory]
    [InlineData("hevc_qsv")]
    [InlineData("av1_qsv")]
    public void GetVideoBitrateParam_HevcAv1QsvMaxBitrate_NoOverflow(string codec)
    {
        int maxBitrate = int.MaxValue / 2;
        var state = CreateJobInfo(maxBitrate);
        var result = _encodingHelper.GetVideoBitrateParam(state, codec);

        Assert.DoesNotContain("-bufsize -", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-maxrate -", result, StringComparison.Ordinal);
        Assert.DoesNotContain("-rc_init_occupancy -", result, StringComparison.Ordinal);

        Assert.Contains($"-b:v {maxBitrate}", result, StringComparison.Ordinal);
        Assert.Contains($"-maxrate {Math.Min((long)maxBitrate + 1, int.MaxValue)}", result, StringComparison.Ordinal);
        Assert.Contains($"-bufsize {Math.Min((long)maxBitrate * 4, int.MaxValue)}", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// The general bufsize = bitrate * 2 (used by libx264, libx265, AMF, VAAPI etc.)
    /// must also use long arithmetic to avoid overflow with extreme values.
    /// </summary>
    [Theory]
    [InlineData("libx264")]
    [InlineData("libx265")]
    [InlineData("h264_amf")]
    [InlineData("h264_vaapi")]
    [InlineData("libsvtav1")]
    public void GetVideoBitrateParam_GeneralMaxBitrate_NoBufsizeOverflow(string codec)
    {
        int maxBitrate = int.MaxValue / 2;
        var state = CreateJobInfo(maxBitrate);
        var result = _encodingHelper.GetVideoBitrateParam(state, codec);

        Assert.DoesNotContain("-bufsize -", result, StringComparison.Ordinal);
    }

    // -- H.264 CPB level clamping --

    /// <summary>
    /// With Level 4.2 and bitrate = 50M, bufsize would be 200M, overshooting the
    /// Level 4.2 CPB limit of 62.5 Mbit. Bufsize, init_occupancy and maxrate must
    /// be clamped to the CPB limit.
    /// </summary>
    [Fact]
    public void GetVideoBitrateParam_H264QsvLevel42_BufsizeCappedToCpb()
    {
        var state = CreateJobInfoWithLevel(50_000_000, "42");
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");

        const long level42Cpb = 62_500_000;
        Assert.Contains($"-bufsize {level42Cpb}", result, StringComparison.Ordinal);
        Assert.Contains($"-rc_init_occupancy {level42Cpb}", result, StringComparison.Ordinal);
        // maxrate = min(50M+1, 62.5M CPB) = 50000001 — already below CPB, no clamp
        Assert.Contains("-maxrate 50000001", result, StringComparison.Ordinal);
        Assert.Contains("-b:v 50000000", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Level 5.1 has CPB = 168,750,000 bits. With bitrate = 50M, bufsize = 200M
    /// exceeds the limit, so it gets capped at 168.75M.
    /// </summary>
    [Fact]
    public void GetVideoBitrateParam_H264QsvLevel51_BufsizeCappedToCpb()
    {
        var state = CreateJobInfoWithLevel(50_000_000, "51");
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");

        const long level51Cpb = 168_750_000;
        Assert.Contains($"-bufsize {level51Cpb}", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// With a lower bitrate where 4 * bitrate fits inside the Level 4.2 CPB, no
    /// capping should happen — the multiplied value passes trough unchanged.
    /// </summary>
    [Fact]
    public void GetVideoBitrateParam_H264QsvLevel42LowBitrate_NoCap()
    {
        var state = CreateJobInfoWithLevel(10_000_000, "42");
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");

        // 10M * 4 = 40M < 62.5M CPB — no cap needed
        Assert.Contains("-bufsize 40000000", result, StringComparison.Ordinal);
        Assert.Contains("-rc_init_occupancy 20000000", result, StringComparison.Ordinal);
        Assert.Contains("-maxrate 10000001", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Boundary test: bitrate where 4 * bitrate == CPB exactly.
    /// Min returns the same value, so no clamping occures.
    /// </summary>
    [Fact]
    public void GetVideoBitrateParam_H264QsvLevel42ExactBoundary_NoCap()
    {
        // 62,500,000 / 4 = 15,625,000
        var state = CreateJobInfoWithLevel(15_625_000, "42");
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");

        Assert.Contains("-bufsize 62500000", result, StringComparison.Ordinal);
        Assert.Contains("-rc_init_occupancy 31250000", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Level 6.0 has a CPB of 800 Mbit. With 50 Mbps bitrate, bufsize = 200 Mbit
    /// is well below the limit, so no capping should be applied.
    /// </summary>
    [Fact]
    public void GetVideoBitrateParam_H264QsvLevel60_NoCap()
    {
        var state = CreateJobInfoWithLevel(50_000_000, "60");
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");

        // 50M * 4 = 200M < 800M CPB — no cap
        Assert.Contains("-bufsize 200000000", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// When no H.264 level is set the CPB capping logic must not fire.
    /// Bufsize should be the unconstrained 4x value.
    /// </summary>
    [Fact]
    public void GetVideoBitrateParam_H264QsvNoLevel_NoCpbCap()
    {
        var state = CreateJobInfo(50_000_000); // no level
        var result = _encodingHelper.GetVideoBitrateParam(state, "h264_qsv");

        Assert.Contains("-bufsize 200000000", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// The CPB capping logic only runs for h264_qsv. For hevc_qsv
    /// the H.264 level check is never invoked, even if a level is set.
    /// </summary>
    [Fact]
    public void GetVideoBitrateParam_HevcQsvWithLevel_NotCpbCapped()
    {
        var state = CreateJobInfoWithLevel(50_000_000, "42");
        var result = _encodingHelper.GetVideoBitrateParam(state, "hevc_qsv");

        // hevc_qsv: 50M * 4 = 200M, no h264 CPB clamping
        Assert.Contains("-bufsize 200000000", result, StringComparison.Ordinal);
    }

    // -- GetH264HighProfileMaxCpbBits lookup table --

    /// <summary>
    /// Verify every level in the H.264 Table A-1 CPB lookup returns the correct value.
    /// These are base-profile MaxCPB values in bits (kbits * 1000).
    /// </summary>
    [Theory]
    [InlineData("10", 175_000)]
    [InlineData("11", 500_000)]
    [InlineData("12", 1_000_000)]
    [InlineData("13", 2_000_000)]
    [InlineData("20", 2_000_000)]
    [InlineData("21", 4_000_000)]
    [InlineData("22", 4_000_000)]
    [InlineData("30", 10_000_000)]
    [InlineData("31", 14_000_000)]
    [InlineData("32", 20_000_000)]
    [InlineData("40", 25_000_000)]
    [InlineData("41", 62_500_000)]
    [InlineData("42", 62_500_000)]
    [InlineData("50", 168_750_000)]
    [InlineData("51", 168_750_000)]
    [InlineData("52", 168_750_000)]
    [InlineData("60", 800_000_000)]
    [InlineData("61", 800_000_000)]
    [InlineData("62", 800_000_000)]
    public void GetH264HighProfileMaxCpbBits_KnownLevel_ReturnsCorrectValue(string level, long expectedCpb)
    {
        Assert.Equal(expectedCpb, EncodingHelper.GetH264HighProfileMaxCpbBits(level));
    }

    /// <summary>
    /// Unknown or malformed levels should return long.MaxValue so the CPB clamp
    /// has no effect and the original computed value passes through.
    /// </summary>
    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("99")]
    [InlineData("4.2")]
    [InlineData("1b")]
    public void GetH264HighProfileMaxCpbBits_UnknownLevel_ReturnsMaxValue(string level)
    {
        Assert.Equal(long.MaxValue, EncodingHelper.GetH264HighProfileMaxCpbBits(level));
    }
}
