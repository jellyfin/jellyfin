using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV;

public class SeasonPathParserTests
{
    [Theory]
    [InlineData("/Drive/Season 1", "/Drive", 1, true)]
    [InlineData("/Drive/Staffel 1", "/Drive", 1, true)]
    [InlineData("/Drive/Stagione 1", "/Drive", 1, true)]
    [InlineData("/Drive/sæson 1", "/Drive", 1, true)]
    [InlineData("/Drive/Temporada 1", "/Drive", 1, true)]
    [InlineData("/Drive/series 1", "/Drive", 1, true)]
    [InlineData("/Drive/Kausi 1", "/Drive", 1, true)]
    [InlineData("/Drive/Säsong 1", "/Drive", 1, true)]
    [InlineData("/Drive/Seizoen 1", "/Drive", 1, true)]
    [InlineData("/Drive/Seasong 1", "/Drive", 1, true)]
    [InlineData("/Drive/Sezon 1", "/Drive", 1, true)]
    [InlineData("/Drive/sezona 1", "/Drive", 1, true)]
    [InlineData("/Drive/sezóna 1", "/Drive", 1, true)]
    [InlineData("/Drive/Sezonul 1", "/Drive", 1, true)]
    [InlineData("/Drive/시즌 1", "/Drive", 1, true)]
    [InlineData("/Drive/シーズン 1", "/Drive", 1, true)]
    [InlineData("/Drive/сезон 1", "/Drive", 1, true)]
    [InlineData("/Drive/Сезон 1", "/Drive", 1, true)]
    [InlineData("/Drive/Season 10", "/Drive", 10, true)]
    [InlineData("/Drive/Season 100", "/Drive", 100, true)]
    [InlineData("/Drive/s1", "/Drive", 1, true)]
    [InlineData("/Drive/S1", "/Drive", 1, true)]
    [InlineData("/Drive/Season 2", "/Drive", 2, true)]
    [InlineData("/Drive/Season 02", "/Drive", 2, true)]
    [InlineData("/Drive/Seinfeld/S02", "/Seinfeld", 2, true)]
    [InlineData("/Drive/Seinfeld/2", "/Seinfeld", 2, true)]
    [InlineData("/Drive/Seinfeld Season 2", "/Drive", null, false)]
    [InlineData("/Drive/Season 2009", "/Drive", 2009, true)]
    [InlineData("/Drive/Season1", "/Drive", 1, true)]
    [InlineData("The Wonder Years/The.Wonder.Years.S04.PDTV.x264-JCH", "/The Wonder Years", 4, true)]
    [InlineData("/Drive/Season 7 (2016)", "/Drive", 7, true)]
    [InlineData("/Drive/Staffel 7 (2016)", "/Drive", 7, true)]
    [InlineData("/Drive/Stagione 7 (2016)", "/Drive", 7, true)]
    [InlineData("/Drive/Stargate SG-1/Season 1", "/Drive/Stargate SG-1", 1, true)]
    [InlineData("/Drive/Stargate SG-1/Stargate SG-1 Season 1", "/Drive/Stargate SG-1", 1, true)]
    [InlineData("/Drive/Season (8)", "/Drive", null, false)]
    [InlineData("/Drive/3.Staffel", "/Drive", 3, true)]
    [InlineData("/Drive/s06e05", "/Drive", null, false)]
    [InlineData("/Drive/The.Legend.of.Condor.Heroes.2017.V2.web-dl.1080p.h264.aac-hdctv", "/Drive", null, false)]
    [InlineData("/Drive/extras", "/Drive", 0, true)]
    [InlineData("/Drive/specials", "/Drive", 0, true)]
    [InlineData("/Drive/Episode 1 Season 2", "/Drive", null, false)]
    public void GetSeasonNumberFromPathTest(string path, string? parentPath, int? seasonNumber, bool isSeasonDirectory)
    {
        var result = SeasonPathParser.Parse(path, parentPath, true, true);

        Assert.Equal(result.SeasonNumber is not null, result.Success);
        Assert.Equal(seasonNumber, result.SeasonNumber);
        Assert.Equal(isSeasonDirectory, result.IsSeasonFolder);
    }
}
