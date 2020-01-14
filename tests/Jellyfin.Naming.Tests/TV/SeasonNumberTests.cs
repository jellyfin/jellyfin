using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SeasonNumberTests
    {
        private int? GetSeasonNumberFromEpisodeFile(string path)
        {
            var options = new NamingOptions();

            var result = new EpisodeResolver(options)
                .Resolve(path, false);

            return result.SeasonNumber;
        }

        [Fact]
        public void TestSeasonNumber1()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"/Show/Season 02/S02E03 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber2()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/seriesname S01x02 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber3()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/S01x02 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber4()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/seriesname S01xE02 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber5()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/01x02 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber6()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/S01E02 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber7()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/S01xE02 blah.avi"));
        }

        // FIXME
        // [Fact]
        public void TestSeasonNumber8()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/seriesname 01x02 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber9()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/seriesname S01x02 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber10()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/seriesname S01E02 blah.avi"));
        }

        [Fact]
        public void TestSeasonNumber11()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 2/Elementary - 02x03 - 02x04 - 02x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber12()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 2/02x03 - 02x04 - 02x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber13()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 2/02x03-04-15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber14()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 2/Elementary - 02x03-04-15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber15()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 02/02x03-E15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber16()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 02/Elementary - 02x03-E15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber17()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 02/02x03 - x04 - x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber18()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 02/Elementary - 02x03 - x04 - x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber19()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 02/02x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber20()
        {
            Assert.Equal(2, GetSeasonNumberFromEpisodeFile(@"Season 02/Elementary - 02x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestSeasonNumber21()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/Elementary - S01E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestSeasonNumber22()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Season 1/S01E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestSeasonNumber23()
        {
            Assert.Equal(25, GetSeasonNumberFromEpisodeFile(@"Season 25/The Simpsons.S25E09.Steal this episode.mp4"));
        }

        [Fact]
        public void TestSeasonNumber24()
        {
            Assert.Equal(25, GetSeasonNumberFromEpisodeFile(@"The Simpsons/The Simpsons.S25E09.Steal this episode.mp4"));
        }

        [Fact]
        public void TestSeasonNumber25()
        {
            Assert.Equal(2016, GetSeasonNumberFromEpisodeFile(@"2016/Season s2016e1.mp4"));
        }

        // FIXME
        // [Fact]
        public void TestSeasonNumber26()
        {
            // This convention is not currently supported, just adding in case we want to look at it in the future
            Assert.Equal(2016, GetSeasonNumberFromEpisodeFile(@"2016/Season 2016x1.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber1()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/2009x02 blah.avi"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber2()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/S2009x02 blah.avi"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber3()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/S2009E02 blah.avi"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber4()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/S2009xE02 blah.avi"));
        }

        // FIXME
        // [Fact]
        public void TestFourDigitSeasonNumber5()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/seriesname 2009x02 blah.avi"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber6()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/seriesname S2009x02 blah.avi"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber7()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/seriesname S2009E02 blah.avi"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber8()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/Elementary - 2009x03 - 2009x04 - 2009x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber9()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/2009x03 - 2009x04 - 2009x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber10()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/2009x03-04-15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber11()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/Elementary - 2009x03 - x04 - x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber12()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/2009x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber13()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/Elementary - 2009x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber14()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/Elementary - S2009E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber15()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/S2009E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber16()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/Elementary - 2009x03 - x04 - x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber17()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/2009x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber18()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/Elementary - 2009x03x04x15 - Ep Name.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber19()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/Elementary - S2009E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestFourDigitSeasonNumber20()
        {
            Assert.Equal(2009, GetSeasonNumberFromEpisodeFile(@"Season 2009/S2009E23-E24-E26 - The Woman.mp4"));
        }

        [Fact]
        public void TestNoSeriesFolder()
        {
            Assert.Equal(1, GetSeasonNumberFromEpisodeFile(@"Series/1-12 - The Woman.mp4"));
        }
    }
}
