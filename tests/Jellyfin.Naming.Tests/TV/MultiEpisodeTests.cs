using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class MultiEpisodeTests
    {
        private readonly EpisodePathParser _episodePathParser = new EpisodePathParser(new NamingOptions());

        [Theory]
        [InlineData("Season 1/4x01 – 20 Hours in America (1).mkv", null)]
        [InlineData("Season 1/01x02 blah.avi", null)]
        [InlineData("Season 1/S01x02 blah.avi", null)]
        [InlineData("Season 1/S01E02 blah.avi", null)]
        [InlineData("Season 1/S01xE02 blah.avi", null)]
        [InlineData("Season 1/seriesname 01x02 blah.avi", null)]
        [InlineData("Season 1/seriesname S01x02 blah.avi", null)]
        [InlineData("Season 1/seriesname S01E02 blah.avi", null)]
        [InlineData("Season 1/seriesname S01xE02 blah.avi", null)]
        [InlineData("Season 2/02x03 - 04 Ep Name.mp4", null)]
        [InlineData("Season 2/My show name 02x03 - 04 Ep Name.mp4", null)]
        [InlineData("Season 2/Elementary - 02x03 - 02x04 - 02x15 - Ep Name.mp4", 15)]
        [InlineData("Season 2/02x03 - 02x04 - 02x15 - Ep Name.mp4", 15)]
        [InlineData("Season 2/02x03-04-15 - Ep Name.mp4", 15)]
        [InlineData("Season 2/Elementary - 02x03-04-15 - Ep Name.mp4", 15)]
        [InlineData("Season 02/02x03-E15 - Ep Name.mp4", 15)]
        [InlineData("Season 02/Elementary - 02x03-E15 - Ep Name.mp4", 15)]
        [InlineData("Season 02/02x03 - x04 - x15 - Ep Name.mp4", 15)]
        [InlineData("Season 02/Elementary - 02x03 - x04 - x15 - Ep Name.mp4", 15)]
        [InlineData("Season 02/02x03x04x15 - Ep Name.mp4", 15)]
        [InlineData("Season 02/Elementary - 02x03x04x15 - Ep Name.mp4", 15)]
        [InlineData("Season 1/Elementary - S01E23-E24-E26 - The Woman.mp4", 26)]
        [InlineData("Season 1/S01E23-E24-E26 - The Woman.mp4", 26)]
        // Four Digits seasons
        [InlineData("Season 2009/2009x02 blah.avi", null)]
        [InlineData("Season 2009/S2009x02 blah.avi", null)]
        [InlineData("Season 2009/S2009E02 blah.avi", null)]
        [InlineData("Season 2009/S2009xE02 blah.avi", null)]
        [InlineData("Season 2009/seriesname 2009x02 blah.avi", null)]
        [InlineData("Season 2009/seriesname S2009x02 blah.avi", null)]
        [InlineData("Season 2009/seriesname S2009E02 blah.avi", null)]
        [InlineData("Season 2009/seriesname S2009xE02 blah.avi", null)]
        [InlineData("Season 2009/Elementary - 2009x03 - 2009x04 - 2009x15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/2009x03 - 2009x04 - 2009x15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/2009x03-04-15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/Elementary - 2009x03-04-15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/2009x03-E15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/Elementary - 2009x03-E15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/2009x03 - x04 - x15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/Elementary - 2009x03 - x04 - x15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/2009x03x04x15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/Elementary - 2009x03x04x15 - Ep Name.mp4", 15)]
        [InlineData("Season 2009/Elementary - S2009E23-E24-E26 - The Woman.mp4", 26)]
        [InlineData("Season 2009/S2009E23-E24-E26 - The Woman.mp4", 26)]
        // Without season number
        [InlineData("Season 1/02 - blah.avi", null)]
        [InlineData("Season 2/02 - blah 14 blah.avi", null)]
        [InlineData("Season 1/02 - blah-02 a.avi", null)]
        [InlineData("Season 2/02.avi", null)]
        [InlineData("Season 1/02-03 - blah.avi", 3)]
        [InlineData("Season 2/02-04 - blah 14 blah.avi", 4)]
        [InlineData("Season 1/02-05 - blah-02 a.avi", 5)]
        [InlineData("Season 2/02-04.avi", 4)]
        [InlineData("Season 2 /[HorribleSubs] Hunter X Hunter - 136[720p].mkv", null)]
        // With format specification that must not be detected as ending episode number
        [InlineData("Season 1/series-s09e14-1080p.mkv", null)]
        [InlineData("Season 1/series-s09e14-720p.mkv", null)]
        [InlineData("Season 1/series-s09e14-720i.mkv", null)]
        [InlineData("Season 1/MOONLIGHTING_s01e01-e04.mkv", 4)]
        [InlineData("Season 1/MOONLIGHTING_s01e01-e04", 4)]
        // Hyphenated numbers in the episode title must not be read as an episode range
        [InlineData("Season 1/S01E01 The 6-10 to Lubbock [WEBRip-1080p][AV1 Opus].mkv", null)]
        [InlineData("Season 5/S05E23 11-59 [HDTV-1080p][x265 AC3].mkv", null)]
        [InlineData("Season 5/S05E23 11-59 [HDTV-1080p][HEVC AC3].mkv", null)]
        [InlineData("Season 1/S01E01 1-23-45 [Bluray-1080p][AV1 Opus].mkv", null)]
        public void TestGetEndingEpisodeNumberFromFile(string filename, int? endingEpisodeNumber)
        {
            var result = _episodePathParser.Parse(filename, false);

            Assert.Equal(result.EndingEpisodeNumber, endingEpisodeNumber);
        }
    }
}
