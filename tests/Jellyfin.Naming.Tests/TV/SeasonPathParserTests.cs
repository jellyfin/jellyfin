using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV;

public class SeasonPathParserTests
{
    [Theory]
    [InlineData("/Drive/Season 1", 1, true)]
    [InlineData("/Drive/s1", 1, true)]
    [InlineData("/Drive/S1", 1, true)]
    [InlineData("/Drive/Season 2", 2, true)]
    [InlineData("/Drive/Season 02", 2, true)]
    [InlineData("/Drive/Seinfeld/S02", 2, true)]
    [InlineData("/Drive/Seinfeld/2", 2, true)]
    [InlineData("/Drive/Seinfeld - S02", 2, true)]
    [InlineData("/Drive/Season 2009", 2009, true)]
    [InlineData("/Drive/Season1", 1, true)]
    [InlineData("The Wonder Years/The.Wonder.Years.S04.PDTV.x264-JCH", 4, true)]
    [InlineData("/Drive/Season 7 (2016)", 7, false)]
    [InlineData("/Drive/Staffel 7 (2016)", 7, false)]
    [InlineData("/Drive/Stagione 7 (2016)", 7, false)]
    [InlineData("/Drive/Season (8)", null, false)]
    [InlineData("/Drive/3.Staffel", 3, false)]
    [InlineData("/Drive/s06e05", null, false)]
    [InlineData("/Drive/The.Legend.of.Condor.Heroes.2017.V2.web-dl.1080p.h264.aac-hdctv", null, false)]
    [InlineData("/Drive/extras", 0, true)]
    [InlineData("/Drive/specials", 0, true)]
    public void GetSeasonNumberFromPathTest(string path, int? seasonNumber, bool isSeasonDirectory)
    {
        var result = SeasonPathParser.Parse(path, true, true);

        Assert.Equal(result.SeasonNumber is not null, result.Success);
        Assert.Equal(result.SeasonNumber, seasonNumber);
        Assert.Equal(isSeasonDirectory, result.IsSeasonFolder);
    }
}
