using System;
using Jellyfin.MediaEncoding.Hls.Playlist;
using Xunit;

namespace Jellyfin.MediaEncoding.Hls.Tests.Playlist
{
    public class DynamicHlsPlaylistGeneratorTests
    {
        [Theory]
        [MemberData(nameof(ComputeEqualLengthSegments_Valid_Success_Data))]
        public void ComputeEqualLengthSegments_Valid_Success(long desiredSegmentLengthMs, long totalRuntimeTicks, double[] segments)
        {
            Assert.Equal(segments, DynamicHlsPlaylistGenerator.ComputeEqualLengthSegments(desiredSegmentLengthMs, totalRuntimeTicks));
        }

        [Theory]
        [InlineData(0, 1000000)]
        [InlineData(1000, 0)]
        public void ComputeEqualLengthSegments_Invalid_ThrowsInvalidOperationException(long desiredSegmentLengthMs, long totalRuntimeTicks)
        {
            Assert.Throws<InvalidOperationException>(() => DynamicHlsPlaylistGenerator.ComputeEqualLengthSegments(desiredSegmentLengthMs, totalRuntimeTicks));
        }

        [Theory]
        [InlineData("testfile.mkv", new string[0], false)]
        [InlineData("testfile.flv", new[] { "mp4", "mkv", "ts" }, false)]
        [InlineData("testfile.flv", new[] { "mp4", "mkv", "ts", "flv" }, true)]
        [InlineData("/some/arbitrarily/long/path/testfile.mkv", new[] { "mkv" }, true)]
        public void IsExtractionAllowedForFile_Valid_Success(string filePath, string[] allowedExtensions, bool isAllowed)
        {
            Assert.Equal(isAllowed, DynamicHlsPlaylistGenerator.IsExtractionAllowedForFile(filePath, allowedExtensions));
        }

        [Theory]
        [InlineData("testfile", new[] { "mp4" })]
        public void IsExtractionAllowedForFile_Invalid_ReturnsFalse(string filePath, string[] allowedExtensions)
        {
            Assert.False(DynamicHlsPlaylistGenerator.IsExtractionAllowedForFile(filePath, allowedExtensions));
        }

        private static TheoryData<long, long, double[]> ComputeEqualLengthSegments_Valid_Success_Data()
        {
            var data = new TheoryData<long, long, double[]>
            {
                { 6000, TimeSpan.FromMilliseconds(13000).Ticks, new[] { 6.0, 6.0, 1.0 } },
                { 3000, TimeSpan.FromMilliseconds(15000).Ticks, new[] { 3.0, 3.0, 3.0, 3.0, 3.0 } },
                { 6000, TimeSpan.FromMilliseconds(25000).Ticks, new[] { 6.0, 6.0, 6.0, 6.0, 1.0 } },
                { 6000, TimeSpan.FromMilliseconds(20123).Ticks, new[] { 6.0, 6.0, 6.0, 2.123 } },
                { 6000, TimeSpan.FromMilliseconds(1234).Ticks, new[] { 1.234 } }
            };

            return data;
        }
    }
}
