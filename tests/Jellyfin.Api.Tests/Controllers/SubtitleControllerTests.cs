using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class SubtitleControllerTests
{
    [Theory]
    [InlineData(900000, "X-TIMESTAMP-MAP=MPEGTS:900000,LOCAL:00:00:00.000")]
    [InlineData(0, "X-TIMESTAMP-MAP=MPEGTS:0,LOCAL:00:00:00.000")]
    public void GetVttTimestampMap_UsesMpegtsOffset(long mpegTimestamp, string expected)
    {
        Assert.Equal(expected, SubtitleController.GetVttTimestampMap(mpegTimestamp));
    }

    [Theory]
    [InlineData(null, 900000)]
    [InlineData("", 900000)]
    [InlineData("ts", 900000)]
    [InlineData("mp4", 0)]
    public void GetVttTimestampMapMpegts_UsesSegmentContainerOffset(string? segmentContainer, long expected)
    {
        Assert.Equal(expected, DynamicHlsHelper.GetVttTimestampMapMpegts(segmentContainer));
    }
}
