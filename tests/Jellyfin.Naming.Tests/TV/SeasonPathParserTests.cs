using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV;

public class SeasonPathParserTests
{
    [Theory]
    [InlineData("/Drive/Season 1", "/Drive", 1, true)]
    [InlineData("/Drive/SEASON 1", "/Drive", 1, true)]
    [InlineData("/Drive/Staffel 1", "/Drive", 1, true)]
    [InlineData("/Drive/STAFFEL 1", "/Drive", 1, true)]
    [InlineData("/Drive/Stagione 1", "/Drive", 1, true)]
    [InlineData("/Drive/STAGIONE 1", "/Drive", 1, true)]
    [InlineData("/Drive/sæson 1", "/Drive", 1, true)]
    [InlineData("/Drive/SÆSON 1", "/Drive", 1, true)]
    [InlineData("/Drive/Temporada 1", "/Drive", 1, true)]
    [InlineData("/Drive/TEMPORADA 1", "/Drive", 1, true)]
    [InlineData("/Drive/series 1", "/Drive", 1, true)]
    [InlineData("/Drive/SERIES 1", "/Drive", 1, true)]
    [InlineData("/Drive/Kausi 1", "/Drive", 1, true)]
    [InlineData("/Drive/KAUSI 1", "/Drive", 1, true)]
    [InlineData("/Drive/Säsong 1", "/Drive", 1, true)]
    [InlineData("/Drive/SÄSONG 1", "/Drive", 1, true)]
    [InlineData("/Drive/Seizoen 1", "/Drive", 1, true)]
    [InlineData("/Drive/SEIZOEN 1", "/Drive", 1, true)]
    [InlineData("/Drive/Seasong 1", "/Drive", 1, true)]
    [InlineData("/Drive/SEASONG 1", "/Drive", 1, true)]
    [InlineData("/Drive/Sezon 1", "/Drive", 1, true)]
    [InlineData("/Drive/SEZON 1", "/Drive", 1, true)]
    [InlineData("/Drive/sezona 1", "/Drive", 1, true)]
    [InlineData("/Drive/SEZONA 1", "/Drive", 1, true)]
    [InlineData("/Drive/sezóna 1", "/Drive", 1, true)]
    [InlineData("/Drive/SEZÓNA 1", "/Drive", 1, true)]
    [InlineData("/Drive/Sezonul 1", "/Drive", 1, true)]
    [InlineData("/Drive/SEZONUL 1", "/Drive", 1, true)]
    [InlineData("/Drive/시즌 1", "/Drive", 1, true)]
    [InlineData("/Drive/シーズン 1", "/Drive", 1, true)]
    [InlineData("/Drive/сезон 1", "/Drive", 1, true)]
    [InlineData("/Drive/Сезон 1", "/Drive", 1, true)]
    [InlineData("/Drive/СЕЗОН 1", "/Drive", 1, true)]
    [InlineData("/Drive/Season 10", "/Drive", 10, true)]
    [InlineData("/Drive/Season 100", "/Drive", 100, true)]
    [InlineData("/Drive/s1", "/Drive", 1, true)]
    [InlineData("/Drive/S1", "/Drive", 1, true)]
    [InlineData("/Drive/Season 2", "/Drive", 2, true)]
    [InlineData("/Drive/Season 02", "/Drive", 2, true)]
    [InlineData("/Drive/Seinfeld/S02", "/Seinfeld", 2, true)]
    [InlineData("/Drive/Seinfeld/2", "/Seinfeld", 2, true)]
    [InlineData("/Drive/Seinfeld Season 2", "/Drive", 2, true)]
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
    [InlineData("/Drive/EXTRAS", "/Drive", 0, true)]
    [InlineData("/Drive/specials", "/Drive", 0, true)]
    [InlineData("/Drive/SPECIALS", "/Drive", 0, true)]
    [InlineData("/Drive/Episode 1 Season 2", "/Drive", null, false)]
    [InlineData("/Drive/Episode 1 SEASON 2", "/Drive", null, false)]
    [InlineData("/media/YouTube/Devyn Johnston/2024-01-24 4070 Ti SUPER in under 7 minutes", "/media/YouTube/Devyn Johnston", null, false)]
    [InlineData("/media/YouTube/Devyn Johnston/2025-01-28 5090 vs 2 SFF Cases", "/media/YouTube/Devyn Johnston", null, false)]
    [InlineData("/Drive/202401244070", "/Drive", null, false)]
    [InlineData("/Drive/Drive.S01.2160p.WEB-DL.DDP5.1.H.265-XXXX", "/Drive", 1, true)]
    [InlineData("The Wonder Years/The.Wonder.Years.S04.1080p.PDTV.x264-JCH", "/The Wonder Years", 4, true)]
    [InlineData("The Wonder Years/[The.Wonder.Years.S04.1080p.PDTV.x264-JCH]", "/The Wonder Years", 4, true)]
    [InlineData("The Wonder Years/The.Wonder.Years [S04][1080p.PDTV.x264-JCH]", "/The Wonder Years", 4, true)]
    [InlineData("The Wonder Years/The Wonder Years Season 01 1080p", "/The Wonder Years", 1, true)]

    public void GetSeasonNumberFromPathTest(string path, string? parentPath, int? seasonNumber, bool isSeasonDirectory)
    {
        var result = SeasonPathParser.Parse(path, parentPath, true, true);

        Assert.Equal(result.SeasonNumber is not null, result.Success);
        Assert.Equal(seasonNumber, result.SeasonNumber);
        Assert.Equal(isSeasonDirectory, result.IsSeasonFolder);
    }

    [Theory]
    // Series-prefixed season folders: "{SeriesName} - Season {N}" (Sonarr default naming)
    [InlineData("/Drive/Series/Solar Opposites/Solar Opposites - Season 1", "/Drive/Series", 1, true)]
    [InlineData("/Drive/Series/The Office/The Office Season 2", "/Drive/Series", 2, true)]
    [InlineData("/Drive/Series/Friends/Friends - Staffel 5", "/Drive/Series", 5, true)]
    [InlineData("/Drive/Series/Friends/Friends - Säsong 3", "/Drive/Series", 3, true)]
    [InlineData("/Drive/Series/The Boys/The Boys - Temporada 4", "/Drive/Series", 4, true)]
    // Series-prefixed with year: "{SeriesName} (2020) - Season {N}"
    [InlineData("/Drive/Series/Solar Opposites (2020)/Solar Opposites (2020) - Season 1", "/Drive/Series", 1, true)]
    [InlineData("/Drive/Series/Star Trek Discovery (2017)/Star Trek Discovery (2017) - Season 3", "/Drive/Series", 3, true)]
    // Series name containing a season keyword (e.g. "The Series Finale - Season 1")
    // Should still find the actual season number
    [InlineData("/Drive/TV/The Series Finale/The Series Finale - Season 1", "/Drive/TV", 1, true)]
    // Season with year suffix: "Season 7 (2023)" — should parse as 7, not 72023
    [InlineData("/Drive/Series/Rick and Morty/Rick and Morty - Season 7 (2023)", "/Drive/Series", 7, true)]
    [InlineData("/Drive/Series/Show/Show - Season 4 (2024)", "/Drive/Series", 4, true)]
    // Standard naming still works exactly as before
    [InlineData("/Drive/Series/Season 1", "/Drive/Series", 1, true)]
    [InlineData("/Drive/Series/S01", "/Drive/Series", 1, true)]
    [InlineData("/Drive/Series/Staffel 1", "/Drive/Series", 1, true)]
    // Defensive: ensure non-season-keyword-containing words don't false-match
    [InlineData("/Drive/Series/Reasoning/Reasoning - S01", "/Drive/Series", 1, true)]
    [InlineData("/Drive/Series/The Reason/The Reason S02", "/Drive/Series", 2, true)]
    // Year edge cases: should NOT sanitize when the "year" part isn't actually a year
    [InlineData("/Drive/Series/Season 2009", "/Drive", 2009, true)]
    [InlineData("/Drive/Series/Season 100", "/Drive", 100, true)]
    public void GetSeasonNumberFromPathSeriesPrefixTest(string path, string? parentPath, int? seasonNumber, bool isSeasonDirectory)
    {
        var result = SeasonPathParser.Parse(path, parentPath, true, true);

        Assert.Equal(result.SeasonNumber is not null, result.Success);
        Assert.Equal(seasonNumber, result.SeasonNumber);
        Assert.Equal(isSeasonDirectory, result.IsSeasonFolder);
    }

    [Theory]
    [InlineData("/Drive/300 Collection/300 (2006)", "/Drive/300 Collection", null, false)]
    [InlineData("/Drive/300 Collection/300 Rise of an Empire", "/Drive/300 Collection", null, false)]
    [InlineData("/Drive/300 Collection/1", "/Drive/300 Collection", null, false)]
    [InlineData("/Drive/300 Collection/300 Disc 1", "/Drive/300 Collection", null, false)]
    [InlineData("/Drive/28 Years Later Collection/28 Days Later", "/Drive/28 Years Later Collection", null, false)]
    [InlineData("/Drive/28 Years Later Collection/28 Weeks Later (2007)", "/Drive/28 Years Later Collection", null, false)]
    [InlineData("/Drive/28 Years Later Collection/28 Years Later 2025", "/Drive/28 Years Later Collection", null, false)]
    [InlineData("/Drive/300 Collection/Season 1", "/Drive/300 Collection", 1, true)]
    [InlineData("/Drive/28 Years Later Collection/Season 01", "/Drive/28 Years Later Collection", 1, true)]
    [InlineData("/Drive/300 Collection/S01", "/Drive/300 Collection", 1, true)]
    [InlineData("/Drive/300 Collection/S1", "/Drive/300 Collection", 1, true)]

    public void GetSeasonNumberFromPathMixedLibraryTest(string path, string? parentPath, int? seasonNumber, bool isSeasonDirectory)
    {
        var result = SeasonPathParser.Parse(path, parentPath, false, false);

        Assert.Equal(result.SeasonNumber is not null, result.Success);
        Assert.Equal(seasonNumber, result.SeasonNumber);
        Assert.Equal(isSeasonDirectory, result.IsSeasonFolder);
    }
}
