using Jellyfin.Data.Enums;
using MediaBrowser.Controller.MediaEncoding;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding;

/// <summary>
/// The rules that decide whether an HDR source can be carried into the transcoded stream
/// without tone-mapping.
/// </summary>
public class EncodingJobInfoHdrTests
{
    [Theory]
    [InlineData(VideoRangeType.HDR10, VideoRangeType.HDR10, true)]
    [InlineData(VideoRangeType.HDR10, VideoRangeType.HDR10Plus, true)]
    [InlineData(VideoRangeType.HDR10, VideoRangeType.HLG, false)]
    [InlineData(VideoRangeType.HDR10, VideoRangeType.SDR, false)]
    [InlineData(VideoRangeType.HDR10Plus, VideoRangeType.HDR10, true)]
    [InlineData(VideoRangeType.HLG, VideoRangeType.HLG, true)]
    [InlineData(VideoRangeType.HLG, VideoRangeType.HDR10, false)]
    [InlineData(VideoRangeType.SDR, VideoRangeType.HDR10, false)]

    // Dolby Vision only when a compatible HDR10/HLG base layer exists.
    [InlineData(VideoRangeType.DOVIWithHDR10, VideoRangeType.HDR10, true)]
    [InlineData(VideoRangeType.DOVIWithEL, VideoRangeType.HDR10, true)]
    [InlineData(VideoRangeType.DOVIWithHDR10Plus, VideoRangeType.HDR10, true)]
    [InlineData(VideoRangeType.DOVIWithELHDR10Plus, VideoRangeType.HDR10Plus, true)]
    [InlineData(VideoRangeType.DOVIWithHLG, VideoRangeType.HLG, true)]
    [InlineData(VideoRangeType.DOVIWithHLG, VideoRangeType.HDR10, false)]
    [InlineData(VideoRangeType.DOVI, VideoRangeType.HDR10, false)]
    [InlineData(VideoRangeType.DOVIWithSDR, VideoRangeType.HDR10, false)]
    [InlineData(VideoRangeType.DOVIInvalid, VideoRangeType.HDR10, false)]
    public void CanCarryHdr_MatchesTheSourceProfile(VideoRangeType source, VideoRangeType requested, bool expected)
        => Assert.Equal(expected, EncodingJobInfo.CanCarryHdr(requested, source));
}
