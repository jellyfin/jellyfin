using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class DailyEpisodeTests
    {
        [Theory]
        [InlineData(@"/server/anything_1996.11.14.mp4", "anything", 1996, 11, 14)]
        [InlineData(@"/server/anything_1996-11-14.mp4", "anything", 1996, 11, 14)]
        [InlineData(@"/server/james.corden.2017.04.20.anne.hathaway.720p.hdtv.x264-crooks.mkv", "james.corden", 2017, 04, 20)]
        [InlineData(@"/server/ABC News 2018_03_24_19_00_00.mkv", "ABC News", 2018, 03, 24)]
        // TODO: [InlineData(@"/server/anything_14.11.1996.mp4", "anything", 1996, 11, 14)]
        // TODO: [InlineData(@"/server/A Daily Show - (2015-01-15) - Episode Name - [720p].mkv", "A Daily Show", 2015, 01, 15)]
        // TODO: [InlineData(@"/server/Last Man Standing_KTLADT_2018_05_25_01_28_00.wtv", "Last Man Standing", 2018, 05, 25)]
        public void Test(string path, string seriesName, int? year, int? month, int? day)
        {
            var options = new NamingOptions();

            var result = new EpisodeResolver(options)
                .Resolve(path, false);

            Assert.Null(result?.SeasonNumber);
            Assert.Null(result?.EpisodeNumber);
            Assert.Equal(year, result?.Year);
            Assert.Equal(month, result?.Month);
            Assert.Equal(day, result?.Day);
            Assert.Equal(seriesName, result?.SeriesName, true);
        }
    }
}
