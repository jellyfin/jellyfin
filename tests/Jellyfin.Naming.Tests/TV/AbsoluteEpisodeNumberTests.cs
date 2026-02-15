using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class AbsoluteEpisodeNumberTests
    {
        private readonly EpisodeResolver _resolver = new EpisodeResolver(new NamingOptions());

        [Theory]
        [InlineData("The Simpsons/12.avi", 12)]
        [InlineData("The Simpsons/The Simpsons 12.avi", 12)]
        [InlineData("The Simpsons/The Simpsons 82.avi", 82)]
        [InlineData("The Simpsons/The Simpsons 112.avi", 112)]
        [InlineData("The Simpsons/Foo_ep_02.avi", 2)]
        [InlineData("The Simpsons/The Simpsons 889.avi", 889)]
        [InlineData("The Simpsons/The Simpsons 101.avi", 101)]
        public void GetEpisodeNumberFromFileTest(string path, int episodeNumber)
        {
            var result = _resolver.Resolve(path, false, null, null, true);

            Assert.Equal(episodeNumber, result?.EpisodeNumber);
        }
    }
}
