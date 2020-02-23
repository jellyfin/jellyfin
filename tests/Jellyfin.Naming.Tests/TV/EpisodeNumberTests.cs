using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class EpisodeNumberTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("Watchmen (2019)/Watchmen 1x03 [WEBDL-720p][EAC3 5.1][h264][-TBS] - She Was Killed by Space Junk.mkv", 3)]
        [InlineData("The Daily Show/The Daily Show 25x22 - [WEBDL-720p][AAC 2.0][x264] Noah Baumbach-TBS.mkv", 22)]
        [InlineData("Castle Rock 2x01 Que el rio siga su curso [WEB-DL HULU 1080p h264 Dual DD5.1 Subs].mkv", 1)]
        [InlineData("After Life 1x06 Episodio 6 [WEB-DL NF 1080p h264 Dual DD 5.1 Sub].mkv", 6)]
        [InlineData("Season 02/S02E03 blah.avi", 3)]
        [InlineData("Season 2/02x03 - 02x04 - 02x15 - Ep Name.mp4", 3)]
        [InlineData("Season 02/02x03 - x04 - x15 - Ep Name.mp4", 3)]
        [InlineData("Season 1/01x02 blah.avi", 2)]
        [InlineData("Season 1/S01x02 blah.avi", 2)]
        [InlineData("Season 1/S01E02 blah.avi", 2)]
        [InlineData("Season 2/Elementary - 02x03-04-15 - Ep Name.mp4", 3)]
        [InlineData("Season 1/S01xE02 blah.avi", 2)]
        [InlineData("Season 1/seriesname S01E02 blah.avi", 2)]
        [InlineData("Season 2/Episode - 16.avi", 16)]
        [InlineData("Season 2/Episode 16.avi", 16)]
        [InlineData("Season 2/Episode 16 - Some Title.avi", 16)]
        [InlineData("Season 2/16 Some Title.avi", 16)]
        [InlineData("Season 2/16 - 12 Some Title.avi", 16)]
        [InlineData("Season 2/7 - 12 Angry Men.avi", 7)]
        [InlineData("Season 1/seriesname 01x02 blah.avi", 2)]
        [InlineData("Season 25/The Simpsons.S25E09.Steal this episode.mp4", 9)]
        [InlineData("Season 1/seriesname S01x02 blah.avi", 2)]
        [InlineData("Season 2/Elementary - 02x03 - 02x04 - 02x15 - Ep Name.mp4", 3)]
        [InlineData("Season 1/seriesname S01xE02 blah.avi", 2)]
        [InlineData("Season 02/Elementary - 02x03 - x04 - x15 - Ep Name.mp4", 3)]
        [InlineData("Season 02/Elementary - 02x03x04x15 - Ep Name.mp4", 3)]
        [InlineData("Season 2/02x03-04-15 - Ep Name.mp4", 3)]
        [InlineData("Season 02/02x03-E15 - Ep Name.mp4", 3)]
        [InlineData("Season 02/Elementary - 02x03-E15 - Ep Name.mp4", 3)]
        [InlineData("Season 1/Elementary - S01E23-E24-E26 - The Woman.mp4", 23)]
        [InlineData("Season 2009/S2009E23-E24-E26 - The Woman.mp4", 23)]
        [InlineData("Season 2009/2009x02 blah.avi", 2)]
        [InlineData("Season 2009/S2009x02 blah.avi", 2)]
        [InlineData("Season 2009/S2009E02 blah.avi", 2)]
        [InlineData("Season 2009/seriesname 2009x02 blah.avi", 2)]
        [InlineData("Season 2009/Elementary - 2009x03x04x15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/2009x03x04x15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/Elementary - 2009x03-E15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/S2009xE02 blah.avi", 2)]
        [InlineData("Season 2009/Elementary - S2009E23-E24-E26 - The Woman.mp4", 23)]
        [InlineData("Season 2009/seriesname S2009xE02 blah.avi", 2)]
        [InlineData("Season 2009/2009x03-E15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/seriesname S2009E02 blah.avi", 2)]
        [InlineData("Season 2009/2009x03 - 2009x04 - 2009x15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/2009x03 - x04 - x15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/seriesname S2009x02 blah.avi", 2)]
        [InlineData("Season 2009/Elementary - 2009x03 - 2009x04 - 2009x15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/Elementary - 2009x03-04-15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/2009x03-04-15 - Ep Name.mp4", 3)]
        [InlineData("Season 2009/Elementary - 2009x03 - x04 - x15 - Ep Name.mp4", 3)]
        [InlineData("Season 1/02 - blah-02 a.avi", 2)]
        [InlineData("Season 1/02 - blah.avi", 2)]
        [InlineData("Season 2/02 - blah 14 blah.avi", 2)]
        [InlineData("Season 2/02.avi", 2)]
        [InlineData("Season 2/2. Infestation.avi", 2)]
        [InlineData("The Wonder Years/The.Wonder.Years.S04.PDTV.x264-JCH/The Wonder Years s04e07 Christmas Party NTSC PDTV.avi", 7)]
        [InlineData("Running Man/Running Man S2017E368.mkv", 368)]
        // TODO: [InlineData("Season 2/16 12 Some Title.avi", 16)]
        // TODO: [InlineData("/The.Legend.of.Condor.Heroes.2017.V2.web-dl.1080p.h264.aac-hdctv/The.Legend.of.Condor.Heroes.2017.E07.V2.web-dl.1080p.h264.aac-hdctv.mkv", 7)]
        // TODO: [InlineData("Season 4/Uchuu.Senkan.Yamato.2199.E03.avi", 3)]
        // TODO: [InlineData("Season 2/7 12 Angry Men.avi", 7)]
        // TODO: [InlineData("Season 02/02x03x04x15 - Ep Name.mp4", 2)]
        // TODO: [InlineData("Season 2/[HorribleSubs] Hunter X Hunter - 136 [720p].mkv", 136)]
        public void GetEpisodeNumberFromFileTest(string path, int? expected)
        {
            var result = new EpisodePathParser(_namingOptions)
                .Parse(path, false);

            Assert.Equal(expected, result.EpisodeNumber);
        }
    }
}
