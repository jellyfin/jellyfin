using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SeriesResolverTests
    {
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
        public void SeriesResolverResolveTest(string path, string name)
        {
            NamingOptions o = new NamingOptions();
            var res = SeriesResolver.Resolve(o, path);

            Assert.Equal(name, res.Name);
        }
    }
}
