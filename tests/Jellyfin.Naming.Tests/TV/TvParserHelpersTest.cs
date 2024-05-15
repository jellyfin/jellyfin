using Emby.Naming.TV;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class TvParserHelpersTest
    {
        [Theory]
        [InlineData("Ended", SeriesStatus.Ended, true)]
        [InlineData("Cancelled", SeriesStatus.Ended, true)]
        [InlineData("Continuing", SeriesStatus.Continuing, true)]
        [InlineData("Returning", SeriesStatus.Continuing, true)]
        [InlineData("Returning Series", SeriesStatus.Continuing, true)]
        [InlineData("Unreleased", SeriesStatus.Unreleased, true)]
        [InlineData("XXX", null, false)]
        public void SeriesStatusParserTest(string statusString, SeriesStatus? status, bool success)
        {
            var successful = TvParserHelpers.TryParseSeriesStatus(statusString, out var parsered);
            Assert.Equal(success, successful);

            if (success)
            {
                Assert.Equal(status, parsered);
            }
        }
    }
}
