using Emby.Naming.TV;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.TV;

public class TvParserHelpersTest
{
    [Theory]
    [InlineData("Ended", SeriesStatus.Ended)]
    [InlineData("Cancelled", SeriesStatus.Ended)]
    [InlineData("Continuing", SeriesStatus.Continuing)]
    [InlineData("Returning", SeriesStatus.Continuing)]
    [InlineData("Returning Series", SeriesStatus.Continuing)]
    [InlineData("Unreleased", SeriesStatus.Unreleased)]
    public void SeriesStatusParserTest_Valid(string statusString, SeriesStatus? status)
    {
        var successful = TvParserHelpers.TryParseSeriesStatus(statusString, out var parsed);
        Assert.True(successful);
        Assert.Equal(status, parsed);
    }

    [Theory]
    [InlineData("XXX")]
    public void SeriesStatusParserTest_InValid(string statusString)
    {
        var successful = TvParserHelpers.TryParseSeriesStatus(statusString, out var parsed);
        Assert.False(successful);
        Assert.Null(parsed);
    }
}
