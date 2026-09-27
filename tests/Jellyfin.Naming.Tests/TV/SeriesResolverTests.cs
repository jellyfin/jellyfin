using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SeriesResolverTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("The.Show.S01", "The Show")]
        [InlineData("The.Show.S01.COMPLETE", "The Show")]
        [InlineData("S.H.O.W.S01", "S.H.O.W")]
        [InlineData("The.Show.P.I.S01", "The Show P.I")]
        [InlineData("The_Show_Season_1", "The Show")]
        [InlineData("/something/The_Show/Season 10", "The Show")]
        [InlineData("The Show", "The Show")]
        [InlineData("/some/path/The Show", "The Show")]
        [InlineData("/some/path/The Show s02e10 720p hdtv", "The Show")]
        [InlineData("/some/path/The Show s02e10 the episode 720p hdtv", "The Show")]
        [InlineData("/some/path/1923 (2022)", "1923")]
        // A dotted acronym keeps its dots when it follows words, whether they are space or dot separated
        [InlineData("/some/path/Marvel's Agents of S.H.I.E.L.D.", "Marvel's Agents of S.H.I.E.L.D.")]
        [InlineData("Marvel's.Agents.of.S.H.I.E.L.D.", "Marvel's Agents of S.H.I.E.L.D.")]
        [InlineData("The.Show.S.H.O.W", "The Show S.H.O.W")]
        [InlineData("/some/path/Dawson's Creek", "Dawson's Creek")]
        public void SeriesResolverResolveTest(string path, string name)
        {
            var res = SeriesResolver.Resolve(_namingOptions, path);

            Assert.Equal(name, res.Name);
        }
    }
}
