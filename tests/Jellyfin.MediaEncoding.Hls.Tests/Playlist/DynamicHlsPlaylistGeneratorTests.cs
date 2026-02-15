using System;
using Jellyfin.MediaEncoding.Hls.Playlist;
using Jellyfin.MediaEncoding.Keyframes;
using Xunit;

namespace Jellyfin.MediaEncoding.Hls.Tests.Playlist
{
    public class DynamicHlsPlaylistGeneratorTests
    {
        [Theory]
        [MemberData(nameof(ComputeSegments_Valid_Success_Data))]
        public void ComputeSegments_Valid_Success(KeyframeData keyframeData, int desiredSegmentLengthMs, double[] segments)
        {
            Assert.Equal(segments, DynamicHlsPlaylistGenerator.ComputeSegments(keyframeData, desiredSegmentLengthMs));
        }

        [Fact]
        public void ComputeSegments_InvalidDuration_ThrowsArgumentException()
        {
            var keyframeData = new KeyframeData(0, new[] { MsToTicks(10000) });
            Assert.Throws<ArgumentException>(() => DynamicHlsPlaylistGenerator.ComputeSegments(keyframeData, 6000));
        }

        [Theory]
        [MemberData(nameof(ComputeEqualLengthSegments_Valid_Success_Data))]
        public void ComputeEqualLengthSegments_Valid_Success(int desiredSegmentLengthMs, long totalRuntimeTicks, double[] segments)
        {
            Assert.Equal(segments, DynamicHlsPlaylistGenerator.ComputeEqualLengthSegments(desiredSegmentLengthMs, totalRuntimeTicks));
        }

        [Theory]
        [InlineData(0, 1000000)]
        [InlineData(1000, 0)]
        public void ComputeEqualLengthSegments_Invalid_ThrowsInvalidOperationException(int desiredSegmentLengthMs, long totalRuntimeTicks)
        {
            Assert.Throws<InvalidOperationException>(() => DynamicHlsPlaylistGenerator.ComputeEqualLengthSegments(desiredSegmentLengthMs, totalRuntimeTicks));
        }

        [Theory]
        [InlineData("testfile.mkv", new string[0], false)]
        [InlineData("testfile.flv", new[] { ".mp4", ".mkv", ".ts" }, false)]
        [InlineData("testfile.flv", new[] { ".mp4", ".mkv", ".ts", ".flv" }, true)]
        [InlineData("/some/arbitrarily/long/path/testfile.mkv", new[] { "mkv" }, true)]
        public void IsExtractionAllowedForFile_Valid_Success(string filePath, string[] allowedExtensions, bool isAllowed)
        {
            Assert.Equal(isAllowed, DynamicHlsPlaylistGenerator.IsExtractionAllowedForFile(filePath, allowedExtensions));
        }

        [Theory]
        [InlineData("testfile", new[] { ".mp4" })]
        public void IsExtractionAllowedForFile_Invalid_ReturnsFalse(string filePath, string[] allowedExtensions)
        {
            Assert.False(DynamicHlsPlaylistGenerator.IsExtractionAllowedForFile(filePath, allowedExtensions));
        }

        public static TheoryData<int, long, double[]> ComputeEqualLengthSegments_Valid_Success_Data()
        {
            var data = new TheoryData<int, long, double[]>
            {
                { 6000, MsToTicks(13000), new[] { 6.0, 6.0, 1.0 } },
                { 3000, MsToTicks(15000), new[] { 3.0, 3.0, 3.0, 3.0, 3.0 } },
                { 6000, MsToTicks(25000), new[] { 6.0, 6.0, 6.0, 6.0, 1.0 } },
                { 6000, MsToTicks(20123), new[] { 6.0, 6.0, 6.0, 2.123 } },
                { 6000, MsToTicks(1234), new[] { 1.234 } }
            };

            return data;
        }

        public static TheoryData<KeyframeData, int, double[]> ComputeSegments_Valid_Success_Data()
        {
            var data = new TheoryData<KeyframeData, int, double[]>
            {
                {
                    new KeyframeData(MsToTicks(35000), new[] { 0, MsToTicks(10427), MsToTicks(20854), MsToTicks(31240) }),
                    6000,
                    new[] { 10.427, 10.427, 10.386, 3.760 }
                },
                {
                    new KeyframeData(MsToTicks(10000), new[] { 0, MsToTicks(1000), MsToTicks(2000), MsToTicks(3000), MsToTicks(4000), MsToTicks(5000) }),
                    2000,
                    new[] { 2.0, 2.0, 6.0 }
                },
                {
                    new KeyframeData(MsToTicks(10000), new[] { 0L }),
                    6000,
                    new[] { 10.0 }
                },
                {
                    new KeyframeData(MsToTicks(10000), Array.Empty<long>()),
                    6000,
                    new[] { 10.0 }
                }
            };

            return data;
        }

        private static long MsToTicks(int value) => TimeSpan.FromMilliseconds(value).Ticks;
    }
}
