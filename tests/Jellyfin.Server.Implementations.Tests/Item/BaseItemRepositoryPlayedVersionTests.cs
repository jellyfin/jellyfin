using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the isPlayed filter over items with alternate versions: playback is recorded against the
/// version that was actually played, so the played state belongs to the version group rather than to
/// the row that happens to carry it.
/// </summary>
public sealed class BaseItemRepositoryPlayedVersionTests : SqliteDbTestFixture
{
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";

    private readonly BaseItemRepository _repository;
    private readonly User _user = new("test", "auth-provider", "reset-provider");

    private readonly Guid _playedViaAlternate = Guid.NewGuid();
    private readonly Guid _playedOnPrimary = Guid.NewGuid();
    private readonly Guid _unplayedWithAlternate = Guid.NewGuid();
    private readonly Guid _unplayedWithoutAlternate = Guid.NewGuid();

    private readonly Guid _seriesPlayedViaAlternate = Guid.NewGuid();
    private readonly Guid _unplayedSeries = Guid.NewGuid();
    private readonly Guid _seriesPlayedAcrossVersions = Guid.NewGuid();
    private readonly Guid _partiallyPlayedSeries = Guid.NewGuid();

    public BaseItemRepositoryPlayedVersionTests()
    {
        using (var context = CreateDbContext())
        {
            Seed(context);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void IsPlayed_CountsAMoviePlayedThroughItsAlternateVersion()
    {
        Assert.Equal(
            new HashSet<Guid> { _playedOnPrimary, _playedViaAlternate },
            Ids(BaseItemKind.Movie, isPlayed: true));
    }

    [Fact]
    public void IsUnplayed_DropsAMoviePlayedThroughItsAlternateVersion()
    {
        Assert.Equal(
            new HashSet<Guid> { _unplayedWithAlternate, _unplayedWithoutAlternate },
            Ids(BaseItemKind.Movie, isPlayed: false));
    }

    [Fact]
    public void IsPlayed_KeepsAPlayedPrimaryWhoseAlternateHasNoRowOfItsOwn()
    {
        Assert.Contains(_playedOnPrimary, Ids(BaseItemKind.Movie, isPlayed: true));
    }

    [Fact]
    public void IsPlayed_CountsASeriesWatchedThroughAnEpisodeAlternateVersion()
    {
        Assert.Equal(
            new HashSet<Guid> { _seriesPlayedViaAlternate, _seriesPlayedAcrossVersions },
            Ids(BaseItemKind.Series, isPlayed: true));
        Assert.Equal(
            new HashSet<Guid> { _unplayedSeries, _partiallyPlayedSeries },
            Ids(BaseItemKind.Series, isPlayed: false));
    }

    [Fact]
    public void GetIsPlayed_CountsASeriesWatchedThroughAnEpisodeAlternateVersion()
    {
        Assert.True(_repository.GetIsPlayed(_user, _seriesPlayedViaAlternate, true));
        Assert.False(_repository.GetIsPlayed(_user, _unplayedSeries, true));
    }

    [Fact]
    public void IsResumable_DropsASeriesWhoseLastEpisodeWasPlayedThroughAnAlternateVersion()
    {
        var resumable = _repository.GetItemIdsList(new InternalItemsQuery(_user) { IsResumable = true });

        // Nothing is left to watch, so the series is not half finished.
        Assert.DoesNotContain(_seriesPlayedAcrossVersions, resumable);
        Assert.Contains(_partiallyPlayedSeries, resumable);
    }

    private HashSet<Guid> Ids(BaseItemKind kind, bool isPlayed)
        => _repository
            .GetItemList(new InternalItemsQuery(_user)
            {
                IncludeItemTypes = [kind],
                IsPlayed = isPlayed
            })
            .Select(i => i.Id)
            .ToHashSet();

    private void Seed(JellyfinDbContext context)
    {
        context.Users.Add(_user);

        // Only the alternate carries the played row, which is what playing that version records.
        AddMovieWithAlternate(context, _playedViaAlternate, "A", playedPrimary: false, playedAlternate: true);
        AddMovieWithAlternate(context, _playedOnPrimary, "B", playedPrimary: true, playedAlternate: false);
        AddMovieWithAlternate(context, _unplayedWithAlternate, "C", playedPrimary: false, playedAlternate: false);
        AddItem(context, _unplayedWithoutAlternate, MovieType, "D");

        AddSeriesWithAlternateEpisode(context, _seriesPlayedViaAlternate, "E", playedAlternate: true);
        AddSeriesWithAlternateEpisode(context, _unplayedSeries, "F", playedAlternate: false);

        AddSeriesWithTwoEpisodes(context, _seriesPlayedAcrossVersions, "G", secondPlayedViaAlternate: true);
        AddSeriesWithTwoEpisodes(context, _partiallyPlayedSeries, "H", secondPlayedViaAlternate: false);

        context.SaveChanges();
    }

    private void AddMovieWithAlternate(JellyfinDbContext context, Guid primaryId, string name, bool playedPrimary, bool playedAlternate)
    {
        AddItem(context, primaryId, MovieType, name);
        AddAlternateVersion(context, primaryId, MovieType, $"{name} 4K", playedAlternate);

        if (playedPrimary)
        {
            AddPlayedUserData(context, primaryId);
        }
    }

    private void AddSeriesWithAlternateEpisode(JellyfinDbContext context, Guid seriesId, string name, bool playedAlternate)
    {
        var episodeId = Guid.NewGuid();

        AddSeriesFolder(context, seriesId, name);

        AddItem(context, episodeId, EpisodeType, $"{name} 1");
        context.AncestorIds.Add(new AncestorId { ItemId = episodeId, ParentItemId = seriesId, Item = null!, ParentItem = null! });

        AddAlternateVersion(context, episodeId, EpisodeType, $"{name} 1 4K", playedAlternate);
    }

    // A watched first episode plus a second one that is either watched as its alternate version or not
    // watched at all, which is what separates a finished series from a half watched one.
    private void AddSeriesWithTwoEpisodes(JellyfinDbContext context, Guid seriesId, string name, bool secondPlayedViaAlternate)
    {
        AddSeriesFolder(context, seriesId, name);

        var firstId = Guid.NewGuid();
        AddItem(context, firstId, EpisodeType, $"{name} 1");
        context.AncestorIds.Add(new AncestorId { ItemId = firstId, ParentItemId = seriesId, Item = null!, ParentItem = null! });
        AddPlayedUserData(context, firstId);

        var secondId = Guid.NewGuid();
        AddItem(context, secondId, EpisodeType, $"{name} 2");
        context.AncestorIds.Add(new AncestorId { ItemId = secondId, ParentItemId = seriesId, Item = null!, ParentItem = null! });
        AddAlternateVersion(context, secondId, EpisodeType, $"{name} 2 4K", secondPlayedViaAlternate);
    }

    private void AddSeriesFolder(JellyfinDbContext context, Guid seriesId, string name)
        => context.BaseItems.Add(new BaseItemEntity
        {
            Id = seriesId,
            Type = SeriesType,
            Name = name,
            SortName = name,
            PresentationUniqueKey = seriesId.ToString("N"),
            IsFolder = true
        });

    private void AddItem(JellyfinDbContext context, Guid id, string type, string name)
        => context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = type,
            Name = name,
            SortName = name,
            PresentationUniqueKey = id.ToString("N")
        });

    private void AddAlternateVersion(JellyfinDbContext context, Guid primaryId, string type, string name, bool played)
    {
        var alternateId = Guid.NewGuid();

        // An alternate presents under its primary's key, which is what collapses the group in listings.
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = alternateId,
            Type = type,
            Name = name,
            SortName = name,
            PresentationUniqueKey = primaryId.ToString("N"),
            PrimaryVersionId = primaryId
        });

        if (played)
        {
            AddPlayedUserData(context, alternateId);
        }
    }

    private void AddPlayedUserData(JellyfinDbContext context, Guid itemId)
        => context.UserData.Add(new UserData
        {
            ItemId = itemId,
            UserId = _user.Id,
            CustomDataKey = itemId.ToString("N"),
            Played = true,
            Item = null!,
            User = null!
        });
}
