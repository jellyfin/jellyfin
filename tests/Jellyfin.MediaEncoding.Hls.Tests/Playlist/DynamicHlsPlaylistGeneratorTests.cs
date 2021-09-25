using Jellyfin.MediaEncoding.Hls.Playlist;
using Xunit;

namespace Jellyfin.MediaEncoding.Hls.Tests.Playlist
{
    public class DynamicHlsPlaylistGeneratorTests
    {
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
    }
}
