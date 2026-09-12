using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Model.Tests.Entities;

public class MediaStreamVideoRangeTests
{
    [Theory]
    [InlineData(7, 6, "smpte2084", false, VideoRangeType.DOVIWithEL)]
    [InlineData(7, 6, "smpte2084", true, VideoRangeType.DOVIWithELHDR10Plus)]
    [InlineData(8, 1, "smpte2084", false, VideoRangeType.DOVIWithHDR10)]
    [InlineData(8, 1, "smpte2084", true, VideoRangeType.DOVIWithHDR10Plus)]
    [InlineData(8, 4, "arib-std-b67", false, VideoRangeType.DOVIWithHLG)]
    [InlineData(10, 1, "smpte2084", false, VideoRangeType.DOVIWithHDR10)]
    [InlineData(10, 1, "smpte2084", true, VideoRangeType.DOVIWithHDR10Plus)]
    [InlineData(10, 4, "arib-std-b67", false, VideoRangeType.DOVIWithHLG)]
    [InlineData(8, 1, "SMPTE2084", false, VideoRangeType.DOVIWithHDR10)]
    [InlineData(8, 4, "ARIB-STD-B67", false, VideoRangeType.DOVIWithHLG)]
    public void GetVideoColorRange_ValidDovi_PreservesRangeType(
        int profile, int compatibilityId, string transfer, bool hdr10Plus, VideoRangeType expected)
    {
        var stream = CreateDovi(profile, compatibilityId, "BT2020NC", transfer, "BT2020", hdr10Plus);

        Assert.Equal((VideoRange.HDR, expected), stream.GetVideoColorRange());
    }

    [Theory]
    [InlineData("bt709", "bt709", "bt709", VideoRange.SDR)]
    [InlineData("bt2020nc", "bt709", "bt2020", VideoRange.SDR)]
    [InlineData("bt2020nc", null, "bt2020", VideoRange.SDR)]
    [InlineData("bt2020nc", "", "bt2020", VideoRange.SDR)]
    [InlineData("bt2020nc", "unknown", "bt2020", VideoRange.SDR)]
    [InlineData("bt2020nc", "bt2020-10", "bt2020", VideoRange.SDR)]
    [InlineData(null, null, null, VideoRange.SDR)]
    [InlineData("bt709", "smpte2084", "bt2020", VideoRange.HDR)]
    [InlineData("bt2020nc", "smpte2084", "bt709", VideoRange.HDR)]
    [InlineData(null, "smpte2084", "bt2020", VideoRange.HDR)]
    [InlineData("bt2020nc", "smpte2084", null, VideoRange.HDR)]
    [InlineData("bt709", "arib-std-b67", "bt2020", VideoRange.HDR)]
    [InlineData("bt2020nc", "arib-std-b67", "bt709", VideoRange.HDR)]
    [InlineData(null, "arib-std-b67", "bt2020", VideoRange.HDR)]
    [InlineData("bt2020nc", "arib-std-b67", null, VideoRange.HDR)]
    public void GetVideoColorRange_InvalidDoviColors_UsesBaseLayerRange(
        string? space, string? transfer, string? primaries, VideoRange expected)
    {
        // Cover every HDR-compatible DV profile, including the HDR10+ variants.
        foreach (var (profile, compatibilityId) in new[] { (7, 6), (8, 1), (8, 4), (10, 1), (10, 4) })
        {
            foreach (var hdr10Plus in new[] { false, true })
            {
                var stream = CreateDovi(profile, compatibilityId, space, transfer, primaries, hdr10Plus);

                Assert.Equal(expected, stream.VideoRange);
                Assert.Equal(VideoRangeType.DOVIInvalid, stream.VideoRangeType);
            }
        }
    }

    [Theory]
    [InlineData(7, 6, "arib-std-b67")]
    [InlineData(8, 1, "arib-std-b67")]
    [InlineData(8, 4, "smpte2084")]
    [InlineData(10, 1, "arib-std-b67")]
    [InlineData(10, 4, "smpte2084")]
    public void GetVideoColorRange_WrongHdrTransfer_InvalidButStillHdr(int profile, int compatibilityId, string transfer)
    {
        var stream = CreateDovi(profile, compatibilityId, "bt2020nc", transfer, "bt2020", true);

        Assert.Equal((VideoRange.HDR, VideoRangeType.DOVIInvalid), stream.GetVideoColorRange());
    }

    [Theory]
    [InlineData(5, 0, null, VideoRange.HDR, VideoRangeType.DOVI)]
    [InlineData(10, 0, null, VideoRange.HDR, VideoRangeType.DOVI)]
    [InlineData(8, 2, "bt709", VideoRange.SDR, VideoRangeType.DOVIWithSDR)]
    [InlineData(10, 2, "bt709", VideoRange.SDR, VideoRangeType.DOVIWithSDR)]
    public void GetVideoColorRange_OtherDoviProfiles_PreservesClassification(
        int profile, int compatibilityId, string? transfer, VideoRange range, VideoRangeType rangeType)
    {
        var stream = CreateDovi(profile, compatibilityId, "bt709", transfer, "bt709", false);

        Assert.Equal((range, rangeType), stream.GetVideoColorRange());
    }

    [Theory]
    [InlineData(8, null, VideoRange.SDR)]
    [InlineData(8, "bt709", VideoRange.SDR)]
    [InlineData(8, "smpte2084", VideoRange.HDR)]
    [InlineData(10, null, VideoRange.SDR)]
    [InlineData(10, "arib-std-b67", VideoRange.HDR)]
    public void GetVideoColorRange_InvalidCompatibilityId_UsesBaseLayerRange(int profile, string? transfer, VideoRange expected)
    {
        var stream = CreateDovi(profile, 6, "bt2020nc", transfer, "bt2020", false);

        Assert.Equal((expected, VideoRangeType.DOVIInvalid), stream.GetVideoColorRange());
    }

    [Theory]
    [InlineData("bt709", false, VideoRange.SDR, VideoRangeType.SDR)]
    [InlineData(null, false, VideoRange.SDR, VideoRangeType.SDR)]
    [InlineData("smpte2084", false, VideoRange.HDR, VideoRangeType.HDR10)]
    [InlineData("smpte2084", true, VideoRange.HDR, VideoRangeType.HDR10Plus)]
    [InlineData("arib-std-b67", false, VideoRange.HDR, VideoRangeType.HLG)]
    public void GetVideoColorRange_WithoutDovi_PreservesClassification(
        string? transfer, bool hdr10Plus, VideoRange range, VideoRangeType rangeType)
    {
        var stream = new MediaStream { Type = MediaStreamType.Video, ColorTransfer = transfer, Hdr10PlusPresentFlag = hdr10Plus };

        Assert.Equal((range, rangeType), stream.GetVideoColorRange());
        stream.Type = MediaStreamType.Audio;
        Assert.Equal((VideoRange.Unknown, VideoRangeType.Unknown), stream.GetVideoColorRange());
    }

    private static MediaStream CreateDovi(int profile, int compatibilityId, string? space, string? transfer, string? primaries, bool hdr10Plus)
        => new()
        {
            Type = MediaStreamType.Video,
            DvProfile = profile,
            DvBlSignalCompatibilityId = compatibilityId,
            RpuPresentFlag = 1,
            BlPresentFlag = 1,
            ElPresentFlag = profile == 7 ? 1 : 0,
            ColorSpace = space,
            ColorTransfer = transfer,
            ColorPrimaries = primaries,
            Hdr10PlusPresentFlag = hdr10Plus
        };
}
