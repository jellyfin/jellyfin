using Jellyfin.Api.Controllers;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class SubtitleControllerTests
{
    [Theory]
    [InlineData(null, "X-TIMESTAMP-MAP=MPEGTS:900000,LOCAL:00:00:00.000")]
    [InlineData("", "X-TIMESTAMP-MAP=MPEGTS:900000,LOCAL:00:00:00.000")]
    [InlineData("ts", "X-TIMESTAMP-MAP=MPEGTS:900000,LOCAL:00:00:00.000")]
    [InlineData("mp4", "X-TIMESTAMP-MAP=MPEGTS:0,LOCAL:00:00:00.000")]
    public void GetVttTimestampMap_UsesSegmentContainerOffset(string? segmentContainer, string expected)
    {
        Assert.Equal(expected, SubtitleController.GetVttTimestampMap(segmentContainer));
    }
}
