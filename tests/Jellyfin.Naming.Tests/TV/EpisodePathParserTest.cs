using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class EpisodePathParserTest
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("/media/Foo/Foo-S01E01", true, "Foo", 1, 1)]
        [InlineData("/media/Foo - S04E011", true, "Foo", 4, 11)]
        [InlineData("/media/Foo/Foo s01x01", true, "Foo", 1, 1)]
        [InlineData("/media/Foo (2019)/Season 4/Foo (2019).S04E03", true, "Foo (2019)", 4, 3)]
        [InlineData(@"D:\media\Foo\Foo-S01E01", true, "Foo", 1, 1)]
        [InlineData(@"D:\media\Foo - S04E011", true, "Foo", 4, 11)]
        [InlineData(@"D:\media\Foo\Foo s01x01", true, "Foo", 1, 1)]
        [InlineData(@"D:\media\Foo (2019)\Season 4\Foo (2019).S04E03", true, "Foo (2019)", 4, 3)]
        [InlineData("/Season 2/Elementary - 02x03-04-15 - Ep Name.mp4", false, "Elementary", 2, 3)]
        [InlineData("/Season 1/seriesname S01E02 blah.avi", false, "seriesname", 1, 2)]
        [InlineData("/Running Man/Running Man S2017E368.mkv", false, "Running Man", 2017, 368)]
        [InlineData("/Season 1/seriesname 01x02 blah.avi", false, "seriesname", 1, 2)]
        [InlineData("/Season 25/The Simpsons.S25E09.Steal this episode.mp4", false, "The Simpsons", 25, 9)]
        [InlineData("/Season 1/seriesname S01x02 blah.avi", false, "seriesname", 1, 2)]
        [InlineData("/Season 2/Elementary - 02x03 - 02x04 - 02x15 - Ep Name.mp4", false, "Elementary", 2, 3)]
        [InlineData("/Season 1/seriesname S01xE02 blah.avi", false, "seriesname", 1, 2)]
        [InlineData("/Season 02/Elementary - 02x03 - x04 - x15 - Ep Name.mp4", false, "Elementary", 2, 3)]
        [InlineData("/Season 02/Elementary - 02x03x04x15 - Ep Name.mp4", false, "Elementary", 2, 3)]
        [InlineData("/Season 02/Elementary - 02x03-E15 - Ep Name.mp4", false, "Elementary", 2, 3)]
        [InlineData("/Season 1/Elementary - S01E23-E24-E26 - The Woman.mp4", false, "Elementary", 1, 23)]
        [InlineData("/The Wonder Years/The.Wonder.Years.S04.PDTV.x264-JCH/The Wonder Years s04e07 Christmas Party NTSC PDTV.avi", false, "The Wonder Years", 4, 7)]
        [InlineData("/The.Sopranos/Season 3/The Sopranos Season 3 Episode 09 - The Telltale Moozadell.avi", false, "The Sopranos", 3, 9)]
        // TODO: [InlineData("/Castle Rock 2x01 Que el rio siga su curso [WEB-DL HULU 1080p h264 Dual DD5.1 Subs].mkv", "Castle Rock", 2, 1)]
        // TODO: [InlineData("/After Life 1x06 Episodio 6 [WEB-DL NF 1080p h264 Dual DD 5.1 Sub].mkv", "After Life", 1, 6)]
        // TODO: [InlineData("/Season 4/Uchuu.Senkan.Yamato.2199.E03.avi", "Uchuu Senkan Yamoto 2199", 4, 3)]
        // TODO: [InlineData("The Daily Show/The Daily Show 25x22 - [WEBDL-720p][AAC 2.0][x264] Noah Baumbach-TBS.mkv", "The Daily Show", 25, 22)]
        // TODO: [InlineData("Watchmen (2019)/Watchmen 1x03 [WEBDL-720p][EAC3 5.1][h264][-TBS] - She Was Killed by Space Junk.mkv", "Watchmen (2019)", 1, 3)]
        // TODO: [InlineData("/The.Legend.of.Condor.Heroes.2017.V2.web-dl.1080p.h264.aac-hdctv/The.Legend.of.Condor.Heroes.2017.E07.V2.web-dl.1080p.h264.aac-hdctv.mkv", "The Legend of Condor Heroes 2017", 1, 7)]
        public void ParseEpisodesCorrectly(string path, bool isDirectory, string name, int season, int episode)
        {
            EpisodePathParser p = new EpisodePathParser(_namingOptions);
            var res = p.Parse(path, isDirectory);

            Assert.True(res.Success);
            Assert.Equal(name, res.SeriesName);
            Assert.Equal(season, res.SeasonNumber);
            Assert.Equal(episode, res.EpisodeNumber);
        }

        [Theory]
        [InlineData("/test/01-03.avi", true, true)]
        public void EpisodePathParserTest_DifferentExpressionsParameters(string path, bool? isNamed, bool? isOptimistic)
        {
            EpisodePathParser p = new EpisodePathParser(_namingOptions);
            var res = p.Parse(path, false, isNamed, isOptimistic);

            Assert.True(res.Success);
        }

        [Fact]
        public void EpisodePathParserTest_FalsePositivePixelRate()
        {
            EpisodePathParser p = new EpisodePathParser(_namingOptions);
            var res = p.Parse("Series Special (1920x1080).mkv", false);

            Assert.False(res.Success);
        }

        [Fact]
        public void EpisodeResolverTest_WrongExtension()
        {
            var res = new EpisodeResolver(_namingOptions).Resolve("test.mp3", false);
            Assert.Null(res);
        }

        [Fact]
        public void EpisodeResolverTest_WrongExtensionStub()
        {
            var res = new EpisodeResolver(_namingOptions).Resolve("dvd.disc", false);
            Assert.NotNull(res);
            Assert.True(res!.IsStub);
        }

        /*
         * EpisodePathParser.cs:130 is currently unreachable, but the piece of code is useful and could be reached with addition of new EpisodeExpressions.
         * In order to preserve it but achieve 100% code coverage the test case below with made up expressions and filename is used.
         */
        [Fact]
        public void EpisodePathParserTest_EmptyDateParsers()
        {
            NamingOptions o = new NamingOptions()
            {
                EpisodeExpressions = new[] { new EpisodeExpression("(([0-9]{4})-([0-9]{2})-([0-9]{2}) [0-9]{2}:[0-9]{2}:[0-9]{2})", true) }
            };
            o.Compile();

            EpisodePathParser p = new EpisodePathParser(o);
            var res = p.Parse("ABC_2019_10_21 11:00:00", false);

            Assert.True(res.Success);
        }
    }
}
