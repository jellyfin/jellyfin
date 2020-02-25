using System;
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
        [InlineData("Super movie 480p 2001.mp4", "Super movie")]
        [InlineData("Super movie [480p].mp4", "Super movie")]
        [InlineData("480 Super movie [tmdbid=12345].mp4", "480 Super movie")]
        [InlineData("Super movie(2009).mp4", "Super movie(2009).mp4")]
        [InlineData("Run lola run (lola rennt) (2009).mp4", "Run lola run (lola rennt) (2009).mp4")]
        [InlineData(@"American.Psycho.mkv", "American.Psycho.mkv")]
        [InlineData(@"American Psycho.mkv", "American Psycho.mkv")]
        [InlineData(@"[rec].mkv", "[rec].mkv")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.4k.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.UltraHD.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.UHD.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.HDR.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.HDC.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon-HDC.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.BDrip.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.BDrip-HDC.mkv", "Crouching.Tiger.Hidden.Dragon")]
        [InlineData("Crouching.Tiger.Hidden.Dragon.4K.UltraHD.HDR.BDrip-HDC.mkv", "Crouching.Tiger.Hidden.Dragon")]
        // FIXME: [InlineData("After The Sunset - [0004].mkv", "After The Sunset")]
        public void CleanStringTest(string input, string expectedName)
        {
            if (new VideoResolver(_namingOptions).TryCleanString(input, out ReadOnlySpan<char> newName))
            {
                // TODO: compare spans when XUnit supports it
                Assert.Equal(expectedName, newName.ToString());
            }
            else
            {
                Assert.Equal(expectedName, input);
            }
        }
    }
}
