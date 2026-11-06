using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public static class DynamicHlsHelperTests
    {
        [Theory]
        [MemberData(nameof(GetVttTimestampMapMpegts_Success_TestData))]
        public static void GetVttTimestampMapMpegts_Success(string? segmentContainer, long expected)
        {
            Assert.Equal(expected, DynamicHlsHelper.GetVttTimestampMapMpegts(segmentContainer));
        }

        public static TheoryData<string?, long> GetVttTimestampMapMpegts_Success_TestData()
        {
            var data = new TheoryData<string?, long>();
            data.Add(null, 900000);
            data.Add(string.Empty, 900000);
            data.Add("ts", 900000);
            data.Add("mp4", 0);
            data.Add("MP4", 0);
            return data;
        }
    }
}
