using System;
using System.Globalization;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class CleanStringTests : BaseVideoTest
    {
        // FIXME
        // [Fact]
        public void TestCleanString()
        {
            Test("Super movie 480p.mp4", "Super movie");
            Test("Super movie 480p 2001.mp4", "Super movie");
            Test("Super movie [480p].mp4", "Super movie");
            Test("480 Super movie [tmdbid=12345].mp4", "480 Super movie");
        }

        // FIXME
        // [Fact]
        public void TestCleanString1()
        {
            Test("Super movie(2009).mp4", "Super movie(2009).mp4");
        }

        // FIXME
        // [Fact]
        public void TestCleanString2()
        {
            Test("Run lola run (lola rennt) (2009).mp4", "Run lola run (lola rennt) (2009).mp4");
        }

        // FIXME
        // [Fact]
        public void TestStringWithoutDate()
        {
            Test(@"American.Psycho.mkv", "American.Psycho.mkv");
            Test(@"American Psycho.mkv", "American Psycho.mkv");
        }

        // FIXME
        // [Fact]
        public void TestNameWithBrackets()
        {
            Test(@"[rec].mkv", "[rec].mkv");
        }

        // FIXME
        // [Fact]
        public void Test4k()
        {
            Test("Crouching.Tiger.Hidden.Dragon.4k.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestUltraHd()
        {
            Test("Crouching.Tiger.Hidden.Dragon.UltraHD.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestUHd()
        {
            Test("Crouching.Tiger.Hidden.Dragon.UHD.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestHDR()
        {
            Test("Crouching.Tiger.Hidden.Dragon.HDR.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestHDC()
        {
            Test("Crouching.Tiger.Hidden.Dragon.HDC.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestHDC1()
        {
            Test("Crouching.Tiger.Hidden.Dragon-HDC.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestBDrip()
        {
            Test("Crouching.Tiger.Hidden.Dragon.BDrip.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestBDripHDC()
        {
            Test("Crouching.Tiger.Hidden.Dragon.BDrip-HDC.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestMulti()
        {
            Test("Crouching.Tiger.Hidden.Dragon.4K.UltraHD.HDR.BDrip-HDC.mkv", "Crouching.Tiger.Hidden.Dragon");
        }

        // FIXME
        // [Fact]
        public void TestLeadingBraces()
        {
            // Not actually supported, just reported by a user
            Test("[0004] - After The Sunset.el.mkv", "After The Sunset");
        }

        // FIXME
        // [Fact]
        public void TestTrailingBraces()
        {
            Test("After The Sunset - [0004].mkv", "After The Sunset");
        }

        private void Test(string input, string expectedName)
        {
            var result = GetParser().CleanString(input).ToString();

            Assert.Equal(expectedName, result, true);
        }
    }
}
