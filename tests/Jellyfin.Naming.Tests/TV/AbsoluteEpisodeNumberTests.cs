using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class AbsoluteEpisodeNumberTests
    {
        [Fact]
        public void TestAbsoluteEpisodeNumber1()
        {
            Assert.Equal(12, GetEpisodeNumberFromFile(@"The Simpsons/12.avi"));
        }

        [Fact]
        public void TestAbsoluteEpisodeNumber2()
        {
            Assert.Equal(12, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons 12.avi"));
        }

        [Fact]
        public void TestAbsoluteEpisodeNumber3()
        {
            Assert.Equal(82, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons 82.avi"));
        }

        [Fact]
        public void TestAbsoluteEpisodeNumber4()
        {
            Assert.Equal(112, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons 112.avi"));
        }

        [Fact]
        public void TestAbsoluteEpisodeNumber5()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/Foo_ep_02.avi"));
        }

        [Fact]
        public void TestAbsoluteEpisodeNumber6()
        {
            Assert.Equal(889, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons 889.avi"));
        }

        [Fact]
        public void TestAbsoluteEpisodeNumber7()
        {
            Assert.Equal(101, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons 101.avi"));
        }

        private int? GetEpisodeNumberFromFile(string path)
        {
            var options = new NamingOptions();

            var result = new EpisodeResolver(options)
                .Resolve(path, false, null, null, true);

            return result.EpisodeNumber;
        }
    }
}
