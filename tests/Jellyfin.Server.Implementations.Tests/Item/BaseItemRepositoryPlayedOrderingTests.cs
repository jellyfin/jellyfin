using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers ordering by <see cref="ItemSortBy.IsPlayed"/> and <see cref="ItemSortBy.IsUnplayed"/>, which
/// has to read the played state the isPlayed filter reports: folders hold none of their own and count
/// as played once no descendant is left unplayed.
/// </summary>
public sealed class BaseItemRepositoryPlayedOrderingTests : SqliteDbTestFixture
{
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";
    private const string BoxSetType = "MediaBrowser.Controller.Entities.Movies.BoxSet";
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";

    private readonly BaseItemRepository _repository;
    private readonly User _user = new("test", "auth-provider", "reset-provider");

    // Names run A..F so name order interleaves the two groups: a dropped or inverted played key shows
    // up as a different sequence rather than as the expected one by luck.
    private readonly Guid _watchedSeries = Guid.NewGuid();
    private readonly Guid _unwatchedSeries = Guid.NewGuid();
    private readonly Guid _partiallyWatchedSeries = Guid.NewGuid();
    private readonly Guid _secondUnwatchedSeries = Guid.NewGuid();
    private readonly Guid _thirdUnwatchedSeries = Guid.NewGuid();
    private readonly Guid _secondWatchedSeries = Guid.NewGuid();

    // Box sets reach their children through LinkedChildren instead of the ancestor chain.
    private readonly Guid _watchedBoxSet = Guid.NewGuid();
    private readonly Guid _unwatchedBoxSet = Guid.NewGuid();

    private readonly HashSet<Guid> _unwatchedSeriesIds;

    public BaseItemRepositoryPlayedOrderingTests()
    {
        _unwatchedSeriesIds = [_unwatchedSeries, _partiallyWatchedSeries, _secondUnwatchedSeries, _thirdUnwatchedSeries];

        using (var context = CreateDbContext())
        {
            Seed(context);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void IsPlayed_OrdersUnwatchedSeriesBeforeWatchedOnes()
    {
        Assert.Equal(
            [_unwatchedSeries, _partiallyWatchedSeries, _secondUnwatchedSeries, _thirdUnwatchedSeries, _watchedSeries, _secondWatchedSeries],
            SeriesIds(ItemSortBy.IsPlayed));
    }

    [Fact]
    public void IsPlayed_CountsAPartiallyWatchedSeriesAsUnwatched()
    {
        var ids = SeriesIds(ItemSortBy.IsPlayed);

        Assert.True(ids.IndexOf(_partiallyWatchedSeries) < ids.IndexOf(_watchedSeries));
    }

    [Fact]
    public void IsUnplayed_ReversesTheGroups()
    {
        Assert.Equal(
            [_watchedSeries, _secondWatchedSeries, _unwatchedSeries, _partiallyWatchedSeries, _secondUnwatchedSeries, _thirdUnwatchedSeries],
            SeriesIds(ItemSortBy.IsUnplayed));
    }

    [Fact]
    public void IsPlayed_OrdersAnUnwatchedBoxSetBeforeAWatchedOne()
    {
        var ids = _repository
            .GetItemList(Query(BaseItemKind.BoxSet, (ItemSortBy.IsPlayed, SortOrder.Ascending)))
            .Select(i => i.Id);

        Assert.Equal([_unwatchedBoxSet, _watchedBoxSet], ids);
    }

    [Fact]
    public void IsPlayedThenRandom_StillPlacesEveryUnwatchedSeriesFirst()
    {
        var order = _repository
            .GetItemList(Query(BaseItemKind.Series, (ItemSortBy.IsPlayed, SortOrder.Ascending), (ItemSortBy.Random, SortOrder.Ascending)))
            .Select(i => i.Id);

        Assert.Equal(_unwatchedSeriesIds, order.Take(_unwatchedSeriesIds.Count).ToHashSet());
    }

    [Fact]
    public void IsPlayedThenRandom_FillsAPageWithUnwatchedSeries()
    {
        var page = _repository.GetItems(new InternalItemsQuery(_user)
        {
            IncludeItemTypes = [BaseItemKind.Series],
            OrderBy = [(ItemSortBy.IsPlayed, SortOrder.Ascending), (ItemSortBy.Random, SortOrder.Ascending)],
            Limit = 4,
            EnableTotalRecordCount = true
        });

        Assert.Equal(6, page.TotalRecordCount);
        Assert.Equal(_unwatchedSeriesIds, page.Items.Select(i => i.Id).ToHashSet());
    }

    private List<Guid> SeriesIds(ItemSortBy sortBy)
        => _repository
            .GetItemList(Query(BaseItemKind.Series, (sortBy, SortOrder.Ascending)))
            .Select(i => i.Id)
            .ToList();

    private InternalItemsQuery Query(BaseItemKind kind, params (ItemSortBy OrderBy, SortOrder SortOrder)[] orderBy)
        => new(_user)
        {
            IncludeItemTypes = [kind],
            OrderBy = orderBy
        };

    private void Seed(JellyfinDbContext context)
    {
        context.Users.Add(_user);

        AddSeries(context, _watchedSeries, "A watched", playedEpisodes: 1, unplayedEpisodes: 0);
        AddSeries(context, _unwatchedSeries, "B unwatched", playedEpisodes: 0, unplayedEpisodes: 1);
        AddSeries(context, _partiallyWatchedSeries, "C partially watched", playedEpisodes: 1, unplayedEpisodes: 1);
        AddSeries(context, _secondUnwatchedSeries, "D unwatched", playedEpisodes: 0, unplayedEpisodes: 1);
        AddSeries(context, _thirdUnwatchedSeries, "E unwatched", playedEpisodes: 0, unplayedEpisodes: 1);
        AddSeries(context, _secondWatchedSeries, "F watched", playedEpisodes: 1, unplayedEpisodes: 0);

        AddBoxSet(context, _watchedBoxSet, "A watched set", played: true);
        AddBoxSet(context, _unwatchedBoxSet, "B unwatched set", played: false);

        context.SaveChanges();
    }

    private void AddSeries(JellyfinDbContext context, Guid id, string name, int playedEpisodes, int unplayedEpisodes)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = SeriesType, Name = name, SortName = name, PresentationUniqueKey = id.ToString("N"), IsFolder = true });

        for (var i = 0; i < playedEpisodes + unplayedEpisodes; i++)
        {
            var episodeId = Guid.NewGuid();
            context.BaseItems.Add(new BaseItemEntity { Id = episodeId, Type = EpisodeType, Name = $"{name} {i}", PresentationUniqueKey = episodeId.ToString("N"), SeriesId = id });
            context.AncestorIds.Add(new AncestorId { ItemId = episodeId, ParentItemId = id, Item = null!, ParentItem = null! });

            if (i < playedEpisodes)
            {
                AddPlayedUserData(context, episodeId);
            }
        }
    }

    private void AddBoxSet(JellyfinDbContext context, Guid id, string name, bool played)
    {
        var movieId = Guid.NewGuid();

        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = BoxSetType, Name = name, SortName = name, PresentationUniqueKey = id.ToString("N"), IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = movieId, Type = MovieType, Name = $"{name} movie", PresentationUniqueKey = movieId.ToString("N") });
        context.LinkedChildren.Add(new LinkedChildEntity { ParentId = id, ChildId = movieId, ChildType = LinkedChildType.Manual, SortOrder = 0 });

        if (played)
        {
            AddPlayedUserData(context, movieId);
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
