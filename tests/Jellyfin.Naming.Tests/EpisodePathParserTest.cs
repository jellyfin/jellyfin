namespace Emby.Naming.TV
{
    using Emby.Naming.Common;
    using Xunit;

    public class EpisodePathParserTest
    {
        [Theory]
        [InlineData("/media/Foo/Foo-S01E01", "Foo", 1, 1)]
        [InlineData("/media/Foo - S04E011", "Foo", 4, 11)]
        [InlineData("/media/Foo/Foo s01x01", "Foo", 1, 1)]
        [InlineData("/media/Foo (2019)/Season 4/Foo (2019).S04E03", "Foo (2019)", 4, 3)]
        public void ParseEpisodesCorrectly(string path, string name, int season, int episode)
        {
            NamingOptions o = new NamingOptions();
            EpisodePathParser p = new EpisodePathParser(o);
            var res = p.Parse(path, false);

            Assert.True(res.Success);
            Assert.Equal(name, res.SeriesName);
            Assert.Equal(season, res.SeasonNumber);
            Assert.Equal(episode, res.EpisodeNumber);

            //testing other paths delimeter
            var res2 = p.Parse(path.Replace("/", "\\"), false);
            Assert.True(res2.Success);
            Assert.Equal(name, res2.SeriesName);
            Assert.Equal(season, res2.SeasonNumber);
            Assert.Equal(episode, res2.EpisodeNumber);
        }

        [Theory]
        [InlineData("/media/Foo/Foo 889", "Foo", 889)]
        [InlineData("/media/Foo/[Bar] Foo Baz - 11 [1080p]", "Foo Baz", 11)]
        public void ParseEpisodeWithoutSeason(string path, string name, int episode)
        {
            NamingOptions o = new NamingOptions();
            EpisodePathParser p = new EpisodePathParser(o);
            var res = p.Parse(path, true, null, null, true);

            Assert.True(res.Success);
            Assert.Equal(name, res.SeriesName);
            Assert.True(res.SeasonNumber == null);
            Assert.Equal(episode, res.EpisodeNumber);

            //testing other paths delimeter
            var res2 = p.Parse(path.Replace("/", "\\"), false, null, null, true);
            Assert.True(res2.Success);
            Assert.Equal(name, res2.SeriesName);
            Assert.True(res2.SeasonNumber == null);
            Assert.Equal(episode, res2.EpisodeNumber);
        }
    }
}