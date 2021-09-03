using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Model.Tests.Entities
{
    public class MediaStreamTests
    {
        [Theory]
        [InlineData(null, null, false, null)]
        [InlineData(null, 0, false, null)]
        [InlineData(0, null, false, null)]
        [InlineData(640, 480, false, "480p")]
        [InlineData(640, 480, true, "480i")]
        [InlineData(720, 576, false, "576p")]
        [InlineData(720, 576, true, "576i")]
        [InlineData(960, 540, false, "540p")]
        [InlineData(960, 540, true, "540i")]
        [InlineData(1280, 720, false, "720p")]
        [InlineData(1280, 720, true, "720i")]
        [InlineData(1920, 1080, false, "1080p")]
        [InlineData(1920, 1080, true, "1080i")]
        [InlineData(4096, 3072, false, "4K")]
        [InlineData(8192, 6144, false, "8K")]
        [InlineData(512, 384, false, "480p")]
        [InlineData(576, 336, false, "480p")]
        [InlineData(624, 352, false, "480p")]
        [InlineData(640, 352, false, "480p")]
        [InlineData(704, 396, false, "480p")]
        [InlineData(720, 404, false, "480p")]
        [InlineData(720, 480, false, "480p")]
        [InlineData(768, 576, false, "576p")]
        [InlineData(960, 720, false, "720p")]
        [InlineData(1280, 528, false, "720p")]
        [InlineData(1280, 532, false, "720p")]
        [InlineData(1280, 534, false, "720p")]
        [InlineData(1280, 536, false, "720p")]
        [InlineData(1280, 544, false, "720p")]
        [InlineData(1280, 690, false, "720p")]
        [InlineData(1280, 694, false, "720p")]
        [InlineData(1280, 696, false, "720p")]
        [InlineData(1280, 716, false, "720p")]
        [InlineData(1280, 718, false, "720p")]
        [InlineData(1912, 792, false, "1080p")]
        [InlineData(1916, 1076, false, "1080p")]
        [InlineData(1918, 1080, false, "1080p")]
        [InlineData(1920, 796, false, "1080p")]
        [InlineData(1920, 800, false, "1080p")]
        [InlineData(1920, 802, false, "1080p")]
        [InlineData(1920, 804, false, "1080p")]
        [InlineData(1920, 808, false, "1080p")]
        [InlineData(1920, 816, false, "1080p")]
        [InlineData(1920, 856, false, "1080p")]
        [InlineData(1920, 960, false, "1080p")]
        [InlineData(1920, 1024, false, "1080p")]
        [InlineData(1920, 1040, false, "1080p")]
        [InlineData(1920, 1072, false, "1080p")]
        [InlineData(1440, 1072, false, "1080p")]
        [InlineData(1440, 1080, false, "1080p")]
        [InlineData(3840, 1600, false, "4K")]
        [InlineData(3840, 1606, false, "4K")]
        [InlineData(3840, 1608, false, "4K")]
        [InlineData(3840, 2160, false, "4K")]
        [InlineData(7680, 4320, false, "8K")]
        public void GetResolutionText_Valid(int? width, int? height, bool interlaced, string expected)
        {
            var mediaStream = new MediaStream()
            {
                Width = width,
                Height = height,
                IsInterlaced = interlaced
            };

            Assert.Equal(expected, mediaStream.GetResolutionText());
        }
    }
}
