using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SeasonNumberTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("The Daily Show/The Daily Show 25x22 - [WEBDL-720p][AAC 2.0][x264] Noah Baumbach-TBS.mkv", 25)]
        [InlineData("/Show/Season 02/S02E03 blah.avi", 2)]
        [InlineData("Season 1/seriesname S01x02 blah.avi", 1)]
        [InlineData("Season 1/S01x02 blah.avi", 1)]
        [InlineData("Season 1/seriesname S01xE02 blah.avi", 1)]
        [InlineData("Season 1/01x02 blah.avi", 1)]
        [InlineData("Season 1/S01E02 blah.avi", 1)]
        [InlineData("Season 1/S01xE02 blah.avi", 1)]
        [InlineData("Season 1/seriesname 01x02 blah.avi", 1)]
        [InlineData("Season 1/seriesname S01E02 blah.avi", 1)]
        [InlineData("Season 2/Elementary - 02x03 - 02x04 - 02x15 - Ep Name.mp4", 2)]
        [InlineData("Season 2/02x03 - 02x04 - 02x15 - Ep Name.mp4", 2)]
        [InlineData("Season 2/02x03-04-15 - Ep Name.mp4", 2)]
        [InlineData("Season 2/Elementary - 02x03-04-15 - Ep Name.mp4", 2)]
        [InlineData("Season 02/02x03-E15 - Ep Name.mp4", 2)]
        [InlineData("Season 02/Elementary - 02x03-E15 - Ep Name.mp4", 2)]
        [InlineData("Season 02/02x03 - x04 - x15 - Ep Name.mp4", 2)]
        [InlineData("Season 02/Elementary - 02x03 - x04 - x15 - Ep Name.mp4", 2)]
        [InlineData("Season 02/02x03x04x15 - Ep Name.mp4", 2)]
        [InlineData("Season 02/Elementary - 02x03x04x15 - Ep Name.mp4", 2)]
        [InlineData("Season 1/Elementary - S01E23-E24-E26 - The Woman.mp4", 1)]
        [InlineData("Season 1/S01E23-E24-E26 - The Woman.mp4", 1)]
        [InlineData("Season 25/The Simpsons.S25E09.Steal this episode.mp4", 25)]
        [InlineData("The Simpsons/The Simpsons.S25E09.Steal this episode.mp4", 25)]
        [InlineData("2016/Season s2016e1.mp4", 2016)]
        [InlineData("2016/Season 2016x1.mp4", 2016)]
        [InlineData("Season 2009/2009x02 blah.avi", 2009)]
        [InlineData("Season 2009/S2009x02 blah.avi", 2009)]
        [InlineData("Season 2009/S2009E02 blah.avi", 2009)]
        [InlineData("Season 2009/S2009xE02 blah.avi", 2009)]
        [InlineData("Season 2009/seriesname 2009x02 blah.avi", 2009)]
        [InlineData("Season 2009/seriesname S2009x02 blah.avi", 2009)]
        [InlineData("Season 2009/seriesname S2009E02 blah.avi", 2009)]
        [InlineData("Season 2009/Elementary - 2009x03 - 2009x04 - 2009x15 - Ep Name.mp4", 2009)]
        [InlineData("Season 2009/2009x03 - 2009x04 - 2009x15 - Ep Name.mp4", 2009)]
        [InlineData("Season 2009/2009x03-04-15 - Ep Name.mp4", 2009)]
        [InlineData("Season 2009/Elementary - 2009x03 - x04 - x15 - Ep Name.mp4", 2009)]
        [InlineData("Season 2009/2009x03x04x15 - Ep Name.mp4", 2009)]
        [InlineData("Season 2009/Elementary - 2009x03x04x15 - Ep Name.mp4", 2009)]
        [InlineData("Season 2009/Elementary - S2009E23-E24-E26 - The Woman.mp4", 2009)]
        [InlineData("Season 2009/S2009E23-E24-E26 - The Woman.mp4", 2009)]
        [InlineData("Series/1-12 - The Woman.mp4", 1)]
        [InlineData(@"Running Man/Running Man S2017E368.mkv", 2017)]
        [InlineData(@"Case Closed (1996-2007)/Case Closed - 317.mkv", 3)]
        // TODO: [InlineData(@"Seinfeld/Seinfeld 0807 The Checks.avi", 8)]
        public void GetSeasonNumberFromEpisodeFileTest(string path, int? expected)
        {
            var result = new EpisodeResolver(_namingOptions)
                .Resolve(path, false);

            Assert.Equal(expected, result?.SeasonNumber);
        }
    }
}
