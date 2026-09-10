using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.Tmdb;
using MediaBrowser.Providers.Plugins.Tmdb.TV;
using TMDbLib.Objects.TvShows;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb;

public static class TmdbSeasonProviderTests
{
    // Modelled on the "TVDB Order" group of Naruto, whose third group holds episodes of TMDb season 2.
    private static TvGroup CreateGroup() => new()
    {
        Id = "65356cb791f0ea00c422eb8e",
        Name = "Season 3",
        Order = 3,
        Episodes =
        [
            new() { Order = 1, SeasonNumber = 2, EpisodeNumber = 85, AirDate = new DateTime(2004, 11, 3, 0, 0, 0, DateTimeKind.Utc) },
            new() { Order = 0, SeasonNumber = 2, EpisodeNumber = 84, AirDate = new DateTime(2004, 10, 27, 0, 0, 0, DateTimeKind.Utc) },
            new() { Order = 2, SeasonNumber = 2, EpisodeNumber = 86, AirDate = null }
        ]
    };

    [Fact]
    public static void MapGroupToSeason_UsesGroupIdAndEarliestAirDate()
    {
        var result = TmdbSeasonProvider.MapGroupToSeason(CreateGroup(), 3, false);

        Assert.True(result.HasMetadata);
        Assert.Equal(3, result.Item.IndexNumber);
        Assert.Equal(new DateTime(2004, 10, 27, 0, 0, 0, DateTimeKind.Utc), result.Item.PremiereDate);
        Assert.Equal(2004, result.Item.ProductionYear);
        Assert.Equal("65356cb791f0ea00c422eb8e", result.Item.GetProviderId(TmdbUtils.EpisodeGroupProviderKey));
    }

    [Fact]
    public static void MapGroupToSeason_DoesNotSetSeasonTmdbId()
    {
        // A group spans whatever TMDb seasons its episodes come from, so no single season describes it.
        var result = TmdbSeasonProvider.MapGroupToSeason(CreateGroup(), 3, true);

        Assert.False(result.Item.HasProviderId(MetadataProvider.Tmdb));
        Assert.Null(result.Item.Overview);
    }

    [Theory]
    [InlineData(true, "Season 3")]
    [InlineData(false, null)]
    public static void MapGroupToSeason_ImportsNameOnlyWhenConfigured(bool importSeasonName, string? expected)
    {
        var result = TmdbSeasonProvider.MapGroupToSeason(CreateGroup(), 3, importSeasonName);

        Assert.Equal(expected, result.Item.Name);
    }

    [Fact]
    public static void MapGroupToSeason_WithoutAirDates_LeavesPremiereDateUnset()
    {
        var group = new TvGroup
        {
            Id = "65356f2bc14fee00ad9e753c",
            Name = "Specials",
            Order = 0,
            Episodes = [new() { Order = 0, AirDate = null }]
        };

        var result = TmdbSeasonProvider.MapGroupToSeason(group, 0, true);

        Assert.Null(result.Item.PremiereDate);
        Assert.Null(result.Item.ProductionYear);
    }
}
