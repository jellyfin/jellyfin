using Jellyfin.Api.Controllers;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public static class SubtitleControllerTests
    {
        [Theory]
        [MemberData(nameof(GetVttTimestampMap_Success_TestData))]
        public static void GetVttTimestampMap_Success(long mpegTimestamp, string expected)
        {
            Assert.Equal(expected, SubtitleController.GetVttTimestampMap(mpegTimestamp));
        }

        public static TheoryData<long, string> GetVttTimestampMap_Success_TestData()
        {
            var data = new TheoryData<long, string>();
            data.Add(900000, "X-TIMESTAMP-MAP=MPEGTS:900000,LOCAL:00:00:00.000");
            data.Add(0, "X-TIMESTAMP-MAP=MPEGTS:0,LOCAL:00:00:00.000");
            return data;
        }

        [Theory]
        [MemberData(nameof(GetSubtitleSegmentUrl_Success_TestData))]
        public static void GetSubtitleSegmentUrl_Success(long positionTicks, long endPositionTicks, string accessToken, long vttTimestampMapMpegts, string expected)
        {
            Assert.Equal(expected, SubtitleController.GetSubtitleSegmentUrl(positionTicks, endPositionTicks, accessToken, vttTimestampMapMpegts));
        }

        public static TheoryData<long, long, string, long, string> GetSubtitleSegmentUrl_Success_TestData()
        {
            var data = new TheoryData<long, long, string, long, string>();
            data.Add(
                0,
                300000000,
                "abc123",
                0,
                "stream.vtt?CopyTimestamps=true&AddVttTimeMap=true&StartPositionTicks=0&EndPositionTicks=300000000&ApiKey=abc123&VttTimestampMapMpegts=0");
            data.Add(
                300000000,
                600000000,
                "abc123",
                900000,
                "stream.vtt?CopyTimestamps=true&AddVttTimeMap=true&StartPositionTicks=300000000&EndPositionTicks=600000000&ApiKey=abc123&VttTimestampMapMpegts=900000");
            return data;
        }
    }
}
