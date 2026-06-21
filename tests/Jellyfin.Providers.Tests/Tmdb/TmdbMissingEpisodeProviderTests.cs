using System;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.Tmdb.TV;
using TMDbLib.Objects.Search;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb;

public class TmdbMissingEpisodeProviderTests
{
    private static readonly DateTime _today = new(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    // No air date -> never imported, regardless of options.
    [InlineData(null, true, true, false, false, false)]
    [InlineData(null, false, false, false, false, false)]
    // Future (unaired) episodes are gated by the unaired option.
    [InlineData(5, true, false, false, false, true)]
    [InlineData(5, false, false, false, false, false)]
    [InlineData(5, false, true, false, false, false)]
    // Today counts as unaired.
    [InlineData(0, true, false, false, false, true)]
    [InlineData(0, false, false, false, false, false)]
    // Past (already aired) episodes are gated by the missing option.
    [InlineData(-5, false, true, false, false, true)]
    [InlineData(-5, false, false, false, false, false)]
    [InlineData(-5, true, false, false, false, false)]
    // Specials are never imported when the specials option is off, regardless of air date.
    [InlineData(5, true, false, true, false, false)]
    [InlineData(-5, false, true, true, false, false)]
    // Specials follow the normal air-date gating when the specials option is on.
    [InlineData(5, true, false, true, true, true)]
    [InlineData(5, false, false, true, true, false)]
    [InlineData(-5, false, true, true, true, true)]
    [InlineData(-5, false, false, true, true, false)]
    public void ShouldImportEpisode_RespectsAirDateAndOptions(int? dayOffset, bool importUnaired, bool importMissing, bool isSpecial, bool importSpecials, bool expected)
    {
        DateTime? premiere = dayOffset.HasValue ? _today.AddDays(dayOffset.Value) : null;

        Assert.Equal(expected, TmdbMissingEpisodeProvider.ShouldImportEpisode(premiere, _today, importUnaired, importMissing, isSpecial, importSpecials));
    }

    [Fact]
    public void ShouldPrune_AgedOutVirtualTmdbEpisode_ReturnsTrue()
    {
        var episode = VirtualEpisode(_today.AddDays(-1), withTmdbId: true);

        Assert.True(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: true, _today, gracePeriodDays: 0, importSpecials: true));
    }

    [Fact]
    public void ShouldPrune_NotInPruningMode_ReturnsFalse()
    {
        var episode = VirtualEpisode(_today.AddDays(-1), withTmdbId: true);

        Assert.False(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: false, _today, gracePeriodDays: 0, importSpecials: true));
    }

    [Fact]
    public void ShouldPrune_StillUpcoming_ReturnsFalse()
    {
        var episode = VirtualEpisode(_today.AddDays(1), withTmdbId: true);

        Assert.False(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: true, _today, gracePeriodDays: 0, importSpecials: true));
    }

    [Fact]
    public void ShouldPrune_VirtualEpisodeFromAnotherProvider_ReturnsFalse()
    {
        // No TMDb id -> not created by this provider (e.g. a TheTVDB plugin entry) -> left untouched.
        var episode = VirtualEpisode(_today.AddDays(-1), withTmdbId: false);

        Assert.False(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: true, _today, gracePeriodDays: 0, importSpecials: true));
    }

    [Fact]
    public void ShouldPrune_PhysicalEpisode_ReturnsFalse()
    {
        var episode = new Episode { Path = "/media/show/Season 01/s01e01.mkv", PremiereDate = _today.AddDays(-1) };
        episode.SetProviderId(MetadataProvider.Tmdb, "123");

        Assert.False(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: true, _today, gracePeriodDays: 0, importSpecials: true));
    }

    [Fact]
    public void ShouldPrune_AiredWithinGracePeriod_ReturnsFalse()
    {
        // Aired two days ago but the grace period keeps it around for the file to be added.
        var episode = VirtualEpisode(_today.AddDays(-2), withTmdbId: true);

        Assert.False(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: true, _today, gracePeriodDays: 7, importSpecials: true));
    }

    [Fact]
    public void ShouldPrune_AiredBeyondGracePeriod_ReturnsTrue()
    {
        var episode = VirtualEpisode(_today.AddDays(-10), withTmdbId: true);

        Assert.True(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: true, _today, gracePeriodDays: 7, importSpecials: true));
    }

    [Fact]
    public void ShouldPrune_SpecialWithSpecialsDisabled_ReturnsTrue()
    {
        // Specials are removed entirely when the specials option is off, even when not in pruning mode.
        var episode = VirtualEpisode(_today.AddDays(5), withTmdbId: true, seasonNumber: 0);

        Assert.True(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: false, _today, gracePeriodDays: 7, importSpecials: false));
    }

    [Fact]
    public void ShouldPrune_SpecialWithSpecialsEnabled_FollowsNormalRules()
    {
        // With specials enabled, an upcoming special is kept like any other upcoming episode.
        var episode = VirtualEpisode(_today.AddDays(5), withTmdbId: true, seasonNumber: 0);

        Assert.False(TmdbMissingEpisodeProvider.ShouldPrune(episode, pruneAgedOut: true, _today, gracePeriodDays: 7, importSpecials: true));
    }

    [Fact]
    public void GetPremiereDate_NullAirDate_ReturnsNull()
    {
        Assert.Null(TmdbMissingEpisodeProvider.GetPremiereDate(new TvSeasonEpisode { AirDate = null }));
    }

    [Fact]
    public void GetPremiereDate_AirDate_ReturnsUtc()
    {
        var airDate = new DateTime(2026, 7, 28);

        var result = TmdbMissingEpisodeProvider.GetPremiereDate(new TvSeasonEpisode { AirDate = airDate });

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result!.Value.Kind);
        Assert.Equal(DateTime.SpecifyKind(airDate, DateTimeKind.Local).ToUniversalTime(), result.Value);
    }

    [Fact]
    public void UpdateVirtualEpisode_PlaceholderTitleReplaced_UpdatesAndReturnsTrue()
    {
        var episode = new Episode { Name = "Episode 14" };
        var tmdbEpisode = new TvSeasonEpisode { Name = "The Real Title" };

        Assert.True(TmdbMissingEpisodeProvider.UpdateVirtualEpisode(episode, tmdbEpisode, null));
        Assert.Equal("The Real Title", episode.Name);
    }

    [Fact]
    public void UpdateVirtualEpisode_NoChanges_ReturnsFalse()
    {
        var date = _today;
        var episode = new Episode { Name = "Same", Overview = "Description", PremiereDate = date };
        var tmdbEpisode = new TvSeasonEpisode { Name = "Same", Overview = "Description" };

        Assert.False(TmdbMissingEpisodeProvider.UpdateVirtualEpisode(episode, tmdbEpisode, date));
    }

    [Fact]
    public void UpdateVirtualEpisode_EmptyTmdbValues_DoNotOverwrite()
    {
        var episode = new Episode { Name = "Existing", Overview = "Existing overview" };
        var tmdbEpisode = new TvSeasonEpisode { Name = string.Empty, Overview = null };

        Assert.False(TmdbMissingEpisodeProvider.UpdateVirtualEpisode(episode, tmdbEpisode, null));
        Assert.Equal("Existing", episode.Name);
        Assert.Equal("Existing overview", episode.Overview);
    }

    [Fact]
    public void UpdateVirtualEpisode_RescheduledAirDate_UpdatesPremiereAndYear()
    {
        var episode = new Episode { Name = "X", PremiereDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) };
        var newAirDate = new DateTime(2026, 8, 15);
        var newPremiere = DateTime.SpecifyKind(newAirDate, DateTimeKind.Local).ToUniversalTime();
        var tmdbEpisode = new TvSeasonEpisode { Name = "X", AirDate = newAirDate };

        Assert.True(TmdbMissingEpisodeProvider.UpdateVirtualEpisode(episode, tmdbEpisode, newPremiere));
        Assert.Equal(newPremiere, episode.PremiereDate);
        Assert.Equal(2026, episode.ProductionYear);
    }

    private static Episode VirtualEpisode(DateTime premiereDate, bool withTmdbId, int? seasonNumber = null)
    {
        var episode = new Episode { PremiereDate = premiereDate, IsVirtualItem = true, ParentIndexNumber = seasonNumber };
        if (withTmdbId)
        {
            episode.SetProviderId(MetadataProvider.Tmdb, "123");
        }

        return episode;
    }
}
