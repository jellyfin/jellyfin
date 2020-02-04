using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SeasonFolderTests
    {
        [Fact]
        public void TestGetSeasonNumberFromPath1()
        {
            Assert.Equal(1, GetSeasonNumberFromPath(@"/Drive/Season 1"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath2()
        {
            Assert.Equal(2, GetSeasonNumberFromPath(@"/Drive/Season 2"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath3()
        {
            Assert.Equal(2, GetSeasonNumberFromPath(@"/Drive/Season 02"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath4()
        {
            Assert.Equal(1, GetSeasonNumberFromPath(@"/Drive/Season 1"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath5()
        {
            Assert.Equal(2, GetSeasonNumberFromPath(@"/Drive/Seinfeld/S02"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath6()
        {
            Assert.Equal(2, GetSeasonNumberFromPath(@"/Drive/Seinfeld/2"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath7()
        {
            Assert.Equal(2009, GetSeasonNumberFromPath(@"/Drive/Season 2009"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath8()
        {
            Assert.Equal(1, GetSeasonNumberFromPath(@"/Drive/Season1"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath9()
        {
            Assert.Equal(4, GetSeasonNumberFromPath(@"The Wonder Years/The.Wonder.Years.S04.PDTV.x264-JCH"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath10()
        {
            Assert.Equal(7, GetSeasonNumberFromPath(@"/Drive/Season 7 (2016)"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath11()
        {
            Assert.Equal(7, GetSeasonNumberFromPath(@"/Drive/Staffel 7 (2016)"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath12()
        {
            Assert.Equal(7, GetSeasonNumberFromPath(@"/Drive/Stagione 7 (2016)"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath14()
        {
            Assert.Null(GetSeasonNumberFromPath(@"/Drive/Season (8)"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath13()
        {
            Assert.Equal(3, GetSeasonNumberFromPath(@"/Drive/3.Staffel"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath15()
        {
            Assert.Null(GetSeasonNumberFromPath(@"/Drive/s06e05"));
        }

        [Fact]
        public void TestGetSeasonNumberFromPath16()
        {
            Assert.Null(GetSeasonNumberFromPath(@"/Drive/The.Legend.of.Condor.Heroes.2017.V2.web-dl.1080p.h264.aac-hdctv"));
        }

        private int? GetSeasonNumberFromPath(string path)
        {
            var result = new SeasonPathParser()
                .Parse(path, true, true);

            return result.SeasonNumber;
        }
    }
}
