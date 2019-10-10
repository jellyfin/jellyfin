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
        [InlineData("/media/Foo/Foo s01x03 - the bar of foo", "Foo", 1, 3)]
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