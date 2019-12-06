using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SimpleEpisodeTests
    {
        [Fact]
        public void TestSimpleEpisodePath1()
        {
            Test(@"/server/anything_s01e02.mp4", "anything", 1, 2);
        }

        [Fact]
        public void TestSimpleEpisodePath2()
        {
            Test(@"/server/anything_s1e2.mp4", "anything", 1, 2);
        }

        [Fact]
        public void TestSimpleEpisodePath3()
        {
            Test(@"/server/anything_s01.e02.mp4", "anything", 1, 2);
        }

        [Fact]
        public void TestSimpleEpisodePath4()
        {
            Test(@"/server/anything_s01_e02.mp4", "anything", 1, 2);
        }

        [Fact]
        public void TestSimpleEpisodePath5()
        {
            Test(@"/server/anything_102.mp4", "anything", 1, 2);
        }

        [Fact]
        public void TestSimpleEpisodePath6()
        {
            Test(@"/server/anything_1x02.mp4", "anything", 1, 2);
        }

        // FIXME
        // [Fact]
        public void TestSimpleEpisodePath7()
        {
            Test(@"/server/The Walking Dead 4x01.mp4", "The Walking Dead", 4, 1);
        }

        [Fact]
        public void TestSimpleEpisodePath8()
        {
            Test(@"/server/the_simpsons-s02e01_18536.mp4", "the_simpsons", 2, 1);
        }


        [Fact]
        public void TestSimpleEpisodePath9()
        {
            Test(@"/server/Temp/S01E02 foo.mp4", string.Empty, 1, 2);
        }

        [Fact]
        public void TestSimpleEpisodePath10()
        {
            Test(@"Series/4-12 - The Woman.mp4", string.Empty, 4, 12);
        }

        [Fact]
        public void TestSimpleEpisodePath11()
        {
            Test(@"Series/4x12 - The Woman.mp4", string.Empty, 4, 12);
        }

        [Fact]
        public void TestSimpleEpisodePath12()
        {
            Test(@"Series/LA X, Pt. 1_s06e32.mp4", "LA X, Pt. 1", 6, 32);
        }

        private void Test(string path, string seriesName, int? seasonNumber, int? episodeNumber)
        {
            var options = new NamingOptions();

            var result = new EpisodeResolver(options)
                .Resolve(path, false);

            Assert.Equal(seasonNumber, result.SeasonNumber);
            Assert.Equal(episodeNumber, result.EpisodeNumber);
            Assert.Equal(seriesName, result.SeriesName, true);
        }
    }
}
