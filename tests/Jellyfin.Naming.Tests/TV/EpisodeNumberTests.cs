using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class EpisodeNumberTests
    {
        [Fact]
        public void TestEpisodeNumber1()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 02/S02E03 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber40()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2/02x03 - 02x04 - 02x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber41()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/01x02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber42()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/S01x02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber43()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/S01E02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber44()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2/Elementary - 02x03-04-15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber45()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/S01xE02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber46()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/seriesname S01E02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber47()
        {
            Assert.Equal(36, GetEpisodeNumberFromFile(@"Season 2/[HorribleSubs] Hunter X Hunter - 136 [720p].mkv"));
        }

        [Fact]
        public void TestEpisodeNumber50()
        {
            // This convention is not currently supported, just adding in case we want to look at it in the future
            Assert.Equal(1, GetEpisodeNumberFromFile(@"2016/Season s2016e1.mp4"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumber51()
        {
            // This convention is not currently supported, just adding in case we want to look at it in the future
            Assert.Equal(1, GetEpisodeNumberFromFile(@"2016/Season 2016x1.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber52()
        {
            Assert.Equal(16, GetEpisodeNumberFromFile(@"Season 2/Episode - 16.avi"));
        }

        [Fact]
        public void TestEpisodeNumber53()
        {
            // This is not supported. Expected to fail, although it would be a good one to add support for.
            Assert.Equal(16, GetEpisodeNumberFromFile(@"Season 2/Episode 16.avi"));
        }

        [Fact]
        public void TestEpisodeNumber54()
        {
            // This is not supported. Expected to fail, although it would be a good one to add support for.
            Assert.Equal(16, GetEpisodeNumberFromFile(@"Season 2/Episode 16 - Some Title.avi"));
        }

        // [Fact]
        public void TestEpisodeNumber55()
        {
            // This is not supported. Expected to fail, although it would be a good one to add support for.
            Assert.Equal(16, GetEpisodeNumberFromFile(@"Season 2/Season 3 Episode 16.avi"));
        }

        // [Fact]
        public void TestEpisodeNumber56()
        {
            // This is not supported. Expected to fail, although it would be a good one to add support for.
            Assert.Equal(16, GetEpisodeNumberFromFile(@"Season 2/Season 3 Episode 16 - Some Title.avi"));
        }

        [Fact]
        public void TestEpisodeNumber57()
        {
            Assert.Equal(16, GetEpisodeNumberFromFile(@"Season 2/16 Some Title.avi"));
        }

        [Fact]
        public void TestEpisodeNumber58()
        {
            Assert.Equal(16, GetEpisodeNumberFromFile(@"Season 2/16 - 12 Some Title.avi"));
        }

        [Fact]
        public void TestEpisodeNumber59()
        {
            Assert.Equal(7, GetEpisodeNumberFromFile(@"Season 2/7 - 12 Angry Men.avi"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumber60()
        {
            Assert.Equal(16, GetEpisodeNumberFromFile(@"Season 2/16 12 Some Title.avi"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumber61()
        {
            Assert.Equal(7, GetEpisodeNumberFromFile(@"Season 2/7 12 Angry Men.avi"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumber62()
        {
            // This is not supported. Expected to fail, although it would be a good one to add support for.
            Assert.Equal(3, GetEpisodeNumberFromFile(@"Season 4/Uchuu.Senkan.Yamato.2199.E03.avi"));
        }

        [Fact]
        public void TestEpisodeNumber63()
        {
            Assert.Equal(3, GetEpisodeNumberFromFile(@"Season 4/Uchuu.Senkan.Yamato.2199.S04E03.avi"));
        }

        [Fact]
        public void TestEpisodeNumber64()
        {
            Assert.Equal(368, GetEpisodeNumberFromFile(@"Running Man/Running Man S2017E368.mkv"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumber65()
        {
            // Not supported yet
            Assert.Equal(7, GetEpisodeNumberFromFile(@"/The.Legend.of.Condor.Heroes.2017.V2.web-dl.1080p.h264.aac-hdctv/The.Legend.of.Condor.Heroes.2017.E07.V2.web-dl.1080p.h264.aac-hdctv.mkv"));
        }

        [Fact]
        public void TestEpisodeNumber30()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2/02x03 - 02x04 - 02x15 - Ep Name.mp4"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumber31()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/seriesname 01x02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber32()
        {
            Assert.Equal(9, GetEpisodeNumberFromFile(@"Season 25/The Simpsons.S25E09.Steal this episode.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber33()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/seriesname S01x02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber34()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2/Elementary - 02x03 - 02x04 - 02x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber35()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/seriesname S01xE02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber36()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 02/02x03 - x04 - x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber37()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 02/Elementary - 02x03 - x04 - x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber38()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 02/02x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber39()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 02/Elementary - 02x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber20()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2/02x03-04-15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber21()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 02/02x03-E15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber22()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 02/Elementary - 02x03-E15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber23()
        {
            Assert.Equal(23, GetEpisodeNumberFromFile(@"Season 1/Elementary - S01E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber24()
        {
            Assert.Equal(23, GetEpisodeNumberFromFile(@"Season 2009/S2009E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber25()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2009/2009x02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber26()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2009/S2009x02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber27()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2009/S2009E02 blah.avi"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumber28()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2009/seriesname 2009x02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber29()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/Elementary - 2009x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber11()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/2009x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber12()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/Elementary - 2009x03-E15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber13()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2009/S2009xE02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber14()
        {
            Assert.Equal(23, GetEpisodeNumberFromFile(@"Season 2009/Elementary - S2009E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber15()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2009/seriesname S2009xE02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber16()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/2009x03-E15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber17()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2009/seriesname S2009E02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber18()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/2009x03 - 2009x04 - 2009x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber19()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/2009x03 - x04 - x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber2()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2009/seriesname S2009x02 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber3()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/Elementary - 2009x03 - 2009x04 - 2009x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber4()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/Elementary - 2009x03-04-15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber5()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/2009x03-04-15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber6()
        {
            Assert.Equal(03, GetEpisodeNumberFromFile(@"Season 2009/Elementary - 2009x03 - x04 - x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestEpisodeNumber7()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/02 - blah-02 a.avi"));
        }

        [Fact]
        public void TestEpisodeNumber8()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 1/02 - blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber9()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2/02 - blah 14 blah.avi"));
        }

        [Fact]
        public void TestEpisodeNumber10()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2/02.avi"));
        }

        [Fact]
        public void TestEpisodeNumber48()
        {
            Assert.Equal(02, GetEpisodeNumberFromFile(@"Season 2/2. Infestation.avi"));
        }

        [Fact]
        public void TestEpisodeNumber49()
        {
            Assert.Equal(7, GetEpisodeNumberFromFile(@"The Wonder Years/The.Wonder.Years.S04.PDTV.x264-JCH/The Wonder Years s04e07 Christmas Party NTSC PDTV.avi"));
        }

        private int? GetEpisodeNumberFromFile(string path)
        {
            var options = new NamingOptions();

            var result = new EpisodePathParser(options)
                .Parse(path, false);

            return result.EpisodeNumber;
        }
    }
}
