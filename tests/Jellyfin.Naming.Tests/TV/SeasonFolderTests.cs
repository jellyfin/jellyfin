using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SeasonFolderTests
    {
        [Theory]
        [InlineData(@"/Drive/Season 1", 1)]
        [InlineData(@"/Drive/Season 2", 2)]
        [InlineData(@"/Drive/Season 02", 2)]
        [InlineData(@"/Drive/Seinfeld/S02", 2)]
        [InlineData(@"/Drive/Seinfeld/2", 2)]
        [InlineData(@"/Drive/Season 2009", 2009)]
        [InlineData(@"/Drive/Season1", 1)]
        [InlineData(@"The Wonder Years/The.Wonder.Years.S04.PDTV.x264-JCH", 4)]
        [InlineData(@"/Drive/Season 7 (2016)", 7)]
        [InlineData(@"/Drive/Staffel 7 (2016)", 7)]
        [InlineData(@"/Drive/Stagione 7 (2016)", 7)]
        [InlineData(@"/Drive/Season (8)", null)]
        [InlineData(@"/Drive/3.Staffel", 3)]
        [InlineData(@"/Drive/s06e05", null)]
        [InlineData(@"/Drive/The.Legend.of.Condor.Heroes.2017.V2.web-dl.1080p.h264.aac-hdctv", null)]
        public void GetSeasonNumberFromPathTest(string path, int? seasonNumber)
        {
            var result = SeasonPathParser.Parse(path, true, true);

            Assert.Equal(result.SeasonNumber, seasonNumber);
        }
    }
}
