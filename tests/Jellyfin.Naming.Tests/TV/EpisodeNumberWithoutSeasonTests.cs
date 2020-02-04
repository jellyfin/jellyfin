using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class EpisodeNumberWithoutSeasonTests
    {
        [Fact]
        public void TestEpisodeNumberWithoutSeason1()
        {
            Assert.Equal(8, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons.S25E08.Steal this episode.mp4"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason2()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons - 02 - Ep Name.avi"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason3()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/02.avi"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason4()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/02 - Ep Name.avi"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason5()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/02-Ep Name.avi"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason6()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/02.EpName.avi"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason7()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons - 02.avi"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason8()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons - 02 Ep Name.avi"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumberWithoutSeason9()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons 5 - 02 - Ep Name.avi"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumberWithoutSeason10()
        {
            Assert.Equal(2, GetEpisodeNumberFromFile(@"The Simpsons/The Simpsons 5 - 02 Ep Name.avi"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumberWithoutSeason11()
        {
            Assert.Equal(7, GetEpisodeNumberFromFile(@"Seinfeld/Seinfeld 0807 The Checks.avi"));
            Assert.Equal(8, GetSeasonNumberFromFile(@"Seinfeld/Seinfeld 0807 The Checks.avi"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason12()
        {
            Assert.Equal(7, GetEpisodeNumberFromFile(@"GJ Club (2013)/GJ Club - 07.mkv"));
        }

        // FIXME
        // [Fact]
        public void TestEpisodeNumberWithoutSeason13()
        {
            // This is not supported anymore after removing the episode number 365+ hack from EpisodePathParser
            Assert.Equal(13, GetEpisodeNumberFromFile(@"Case Closed (1996-2007)/Case Closed - 13.mkv"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason14()
        {
            Assert.Equal(3, GetSeasonNumberFromFile(@"Case Closed (1996-2007)/Case Closed - 317.mkv"));
            Assert.Equal(17, GetEpisodeNumberFromFile(@"Case Closed (1996-2007)/Case Closed - 317.mkv"));
        }

        [Fact]
        public void TestEpisodeNumberWithoutSeason15()
        {
            Assert.Equal(2017, GetSeasonNumberFromFile(@"Running Man/Running Man S2017E368.mkv"));
        }

        private int? GetEpisodeNumberFromFile(string path)
        {
            var options = new NamingOptions();

            var result = new EpisodeResolver(options)
                .Resolve(path, false);

            return result.EpisodeNumber;
        }

        private int? GetSeasonNumberFromFile(string path)
        {
            var options = new NamingOptions();

            var result = new EpisodeResolver(options)
                .Resolve(path, false);

            return result.SeasonNumber;
        }

    }
}
