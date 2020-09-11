using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class EpisodePathParserTest
    {
        [Theory]
        [InlineData("/media/Foo/Foo-S01E01", "Foo", 1, 1)]
        [InlineData("/media/Foo - S04E011", "Foo", 4, 11)]
        [InlineData("/media/Foo/Foo s01x01", "Foo", 1, 1)]
        [InlineData("/media/Foo (2019)/Season 4/Foo (2019).S04E03", "Foo (2019)", 4, 3)]
        [InlineData("D:\\media\\Foo\\Foo-S01E01", "Foo", 1, 1)]
        [InlineData("D:\\media\\Foo - S04E011", "Foo", 4, 11)]
        [InlineData("D:\\media\\Foo\\Foo s01x01", "Foo", 1, 1)]
        [InlineData("D:\\media\\Foo (2019)\\Season 4\\Foo (2019).S04E03", "Foo (2019)", 4, 3)]
        [InlineData("/Season 2/Elementary - 02x03-04-15 - Ep Name.mp4", "Elementary", 2, 3)]
        [InlineData("/Season 1/seriesname S01E02 blah.avi", "seriesname", 1, 2)]
        [InlineData("/Running Man/Running Man S2017E368.mkv", "Running Man", 2017, 368)]
        [InlineData("/Season 1/seriesname 01x02 blah.avi", "seriesname", 1, 2)]
        [InlineData("/Season 25/The Simpsons.S25E09.Steal this episode.mp4", "The Simpsons", 25, 9)]
        [InlineData("/Season 1/seriesname S01x02 blah.avi", "seriesname", 1, 2)]
        [InlineData("/Season 2/Elementary - 02x03 - 02x04 - 02x15 - Ep Name.mp4", "Elementary", 2, 3)]
        [InlineData("/Season 1/seriesname S01xE02 blah.avi", "seriesname", 1, 2)]
        [InlineData("/Season 02/Elementary - 02x03 - x04 - x15 - Ep Name.mp4", "Elementary", 2, 3)]
        [InlineData("/Season 02/Elementary - 02x03x04x15 - Ep Name.mp4", "Elementary", 2, 3)]
        [InlineData("/Season 02/Elementary - 02x03-E15 - Ep Name.mp4", "Elementary", 2, 3)]
        [InlineData("/Season 1/Elementary - S01E23-E24-E26 - The Woman.mp4", "Elementary", 1, 23)]
        [InlineData("/The Wonder Years/The.Wonder.Years.S04.PDTV.x264-JCH/The Wonder Years s04e07 Christmas Party NTSC PDTV.avi", "The Wonder Years", 4, 7)]
        // TODO: [InlineData("/Castle Rock 2x01 Que el rio siga su curso [WEB-DL HULU 1080p h264 Dual DD5.1 Subs].mkv", "Castle Rock", 2, 1)]
        // TODO: [InlineData("/After Life 1x06 Episodio 6 [WEB-DL NF 1080p h264 Dual DD 5.1 Sub].mkv", "After Life", 1, 6)]
        // TODO: [InlineData("/Season 4/Uchuu.Senkan.Yamato.2199.E03.avi", "Uchuu Senkan Yamoto 2199", 4, 3)]
        // TODO: [InlineData("The Daily Show/The Daily Show 25x22 - [WEBDL-720p][AAC 2.0][x264] Noah Baumbach-TBS.mkv", "The Daily Show", 25, 22)]
        // TODO: [InlineData("Watchmen (2019)/Watchmen 1x03 [WEBDL-720p][EAC3 5.1][h264][-TBS] - She Was Killed by Space Junk.mkv", "Watchmen (2019)", 1, 3)]
        // TODO: [InlineData("/The.Legend.of.Condor.Heroes.2017.V2.web-dl.1080p.h264.aac-hdctv/The.Legend.of.Condor.Heroes.2017.E07.V2.web-dl.1080p.h264.aac-hdctv.mkv", "The Legend of Condor Heroes 2017", 1, 7)]
        public void ParseEpisodesCorrectly(string path, string name, int season, int episode)
        {
            NamingOptions o = new NamingOptions();
            EpisodePathParser p = new EpisodePathParser(o);
            var res = p.Parse(path, false);

            Assert.True(res.Success);
            Assert.Equal(name, res.SeriesName);
            Assert.Equal(season, res.SeasonNumber);
            Assert.Equal(episode, res.EpisodeNumber);
        }
    }
}
