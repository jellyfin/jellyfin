using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class EpisodeWithoutSeasonTests
    {
        // TODO: [Theory]
        // TODO: [InlineData(@"/server/anything_ep02.mp4", "anything", null, 2)]
        // TODO: [InlineData(@"/server/anything_ep_02.mp4", "anything", null, 2)]
        // TODO: [InlineData(@"/server/anything_part.II.mp4", "anything", null, null)]
        // TODO: [InlineData(@"/server/anything_pt.II.mp4", "anything", null, null)]
        // TODO: [InlineData(@"/server/anything_pt_II.mp4", "anything", null, null)]
        public void Test(string path, string seriesName, int? seasonNumber, int? episodeNumber)
        {
            var options = new NamingOptions();

            var result = new EpisodeResolver(options)
                .Resolve(path, false);

            Assert.Equal(seasonNumber, result?.SeasonNumber);
            Assert.Equal(episodeNumber, result?.EpisodeNumber);
            Assert.Equal(seriesName, result?.SeriesName, ignoreCase: true);
        }
    }
}
