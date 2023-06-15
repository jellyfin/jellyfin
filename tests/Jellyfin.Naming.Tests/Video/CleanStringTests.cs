using Emby.Naming.Common;
using Emby.Naming.Video;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public sealed class CleanStringTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("Super movie 480p.mp4", "Super movie")]
        [InlineData("Super movie Multi.mp4", "Super movie")]
        [InlineData("Super movie 480p 2001.mp4", "Super movie")]
        [InlineData("Super movie [480p].mp4", "Super movie")]
        [InlineData("480 Super movie [tmdbid=12345].mp4", "480 Super movie")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.4k.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.UltraHD.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.UHD.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.HDR.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.HDC.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon-HDC.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.BDrip.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.BDrip-HDC.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.4K.UltraHD.HDR.BDrip-HDC.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("[HorribleSubs] Made in Abyss - 13 [720p].mkv", "Made in Abyss")]
        [InlineData("[Tsundere] Kore wa Zombie Desu ka of the Dead [BDRip h264 1920x1080 FLAC]", "Kore wa Zombie Desu ka of the Dead")]
        [InlineData("[Erai-raws] Jujutsu Kaisen - 03 [720p][Multiple Subtitle].mkv", "Jujutsu Kaisen")]
        [InlineData("[OCN] 애타는 로맨스 720p-NEXT", "애타는 로맨스")]
        [InlineData("[tvN] 혼술남녀.E01-E16.720p-NEXT", "혼술남녀")]
        [InlineData("[tvN] 연애말고 결혼 E01~E16 END HDTV.H264.720p-WITH", "연애말고 결혼")]
        // FIXME: [InlineData("After The Sunset - [0004].mkv", "After The Sunset")]
        public void CleanStringTest_NeedsCleaning_Success(string input, string expectedName)
        {
            Assert.True(VideoResolver.TryCleanString(input, _namingOptions, out var newName));
            Assert.Equal(expectedName, newName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Super movie(2009).mp4")]
        [InlineData("[rec].mkv")]
        [InlineData("American.Psycho.mkv")]
        [InlineData("American Psycho.mkv")]
        [InlineData("Run lola run (lola rennt) (2009).mp4")]
        public void CleanStringTest_DoesntNeedCleaning_False(string? input)
        {
            Assert.False(VideoResolver.TryCleanString(input, _namingOptions, out var newName));
            Assert.True(string.IsNullOrEmpty(newName));
        }
    }
}
