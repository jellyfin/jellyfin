using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class AbsoluteEpisodeNumberTests
    {
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
            var options = new NamingOptions();

            var result = new EpisodeResolver(options)
                .Resolve(path, false, null, null, true);

            Assert.Equal(episodeNumber, result?.EpisodeNumber);
        }
    }
}
