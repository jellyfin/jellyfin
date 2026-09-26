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

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers ordering by <see cref="ItemSortBy.SeriesUnplayedRuntime"/>, which adds up the runtime of
/// the episodes a user has not played yet so a series can be sorted by how long it takes to finish.
/// </summary>
public sealed class BaseItemRepositorySeriesUnplayedRuntimeTests : SqliteDbTestFixture
{
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";

    private readonly BaseItemRepository _repository;
    private readonly User _user = new("test", "auth-provider", "reset-provider");

    // Names run A..E so name order cuts across runtime order: a dropped or inverted sort key shows up
    // as a different sequence rather than as the expected one by luck.
    private readonly Guid _twentyMinutesLeft = Guid.NewGuid();
    private readonly Guid _sixtyMinutesLeft = Guid.NewGuid();
    private readonly Guid _fullyWatched = Guid.NewGuid();
    private readonly Guid _fifteenMinutesLeft = Guid.NewGuid();
    private readonly Guid _withoutEpisodes = Guid.NewGuid();

    public BaseItemRepositorySeriesUnplayedRuntimeTests()
    {
        using (var context = CreateDbContext())
        {
            Seed(context);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void Descending_PutsTheSeriesWithTheMostTimeLeftFirst()
    {
        Assert.Equal(
            [_sixtyMinutesLeft, _twentyMinutesLeft, _fifteenMinutesLeft, _fullyWatched, _withoutEpisodes],
            SeriesIds(SortOrder.Descending));
    }

    [Fact]
    public void Ascending_PutsTheSeriesWithNothingLeftFirst()
    {
        Assert.Equal(
            [_fullyWatched, _withoutEpisodes, _fifteenMinutesLeft, _twentyMinutesLeft, _sixtyMinutesLeft],
            SeriesIds(SortOrder.Ascending));
    }

    [Fact]
    public void CountsOnlyTheEpisodesLeftToWatch()
    {
        // The partly watched series runs 30 minutes in total but only 20 are left, which puts it
        // behind the series that has 60 left rather than level with it.
        var ids = SeriesIds(SortOrder.Descending);

        Assert.True(ids.IndexOf(_sixtyMinutesLeft) < ids.IndexOf(_twentyMinutesLeft));
    }

    [Fact]
    public void IgnoresVirtualEpisodes()
    {
        // Two 60 minute virtual episodes would carry this series to the front if they counted, but
        // they are missing or unaired, so only the one real 15 minute episode is left to watch.
        var ids = SeriesIds(SortOrder.Descending);

        Assert.True(ids.IndexOf(_twentyMinutesLeft) < ids.IndexOf(_fifteenMinutesLeft));
    }

    [Fact]
    public void TreatsASeriesWithoutEpisodesAsNothingLeft()
    {
        var ids = SeriesIds(SortOrder.Descending);

        Assert.Equal(ids.Count - 1, ids.IndexOf(_withoutEpisodes));
    }

    [Fact]
    public void WithoutAUser_CountsTheEpisodesNobodyPlayed()
    {
        var ids = _repository
            .GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Series],
                OrderBy = [(ItemSortBy.SeriesUnplayedRuntime, SortOrder.Descending)]
            })
            .Select(i => i.Id)
            .ToList();

        Assert.Equal(
            [_sixtyMinutesLeft, _twentyMinutesLeft, _fifteenMinutesLeft, _fullyWatched, _withoutEpisodes],
            ids);
    }

    [Fact]
    public void CombinedWithSearch_StillOrdersByTimeLeft()
    {
        // Search routes the sort through OrderMapper's correlated subquery instead of the
        // pre-aggregated join, so it needs its own coverage that it translates and agrees.
        var ids = _repository
            .GetItemList(new InternalItemsQuery(_user)
            {
                IncludeItemTypes = [BaseItemKind.Series],
                SearchTerm = "show",
                OrderBy = [(ItemSortBy.SeriesUnplayedRuntime, SortOrder.Descending)]
            })
            .Select(i => i.Id)
            .ToList();

        // A searched query gets no SortName tiebreaker, so the two series with nothing left to
        // watch tie and only their placement at the back is defined.
        Assert.Equal([_sixtyMinutesLeft, _twentyMinutesLeft, _fifteenMinutesLeft], ids.Take(3));
        Assert.Equal([_fullyWatched, _withoutEpisodes], ids.Skip(3).ToHashSet());
    }

    private List<Guid> SeriesIds(SortOrder sortOrder)
        => _repository
            .GetItemList(new InternalItemsQuery(_user)
            {
                IncludeItemTypes = [BaseItemKind.Series],
                OrderBy = [(ItemSortBy.SeriesUnplayedRuntime, sortOrder)]
            })
            .Select(i => i.Id)
            .ToList();

    private void Seed(JellyfinDbContext context)
    {
        context.Users.Add(_user);

        var series = AddSeries(context, _twentyMinutesLeft, "A partly watched show");
        AddEpisode(context, series, minutes: 10, played: true);
        AddEpisode(context, series, minutes: 10, played: false);
        AddEpisode(context, series, minutes: 10, played: false);

        series = AddSeries(context, _sixtyMinutesLeft, "B unwatched show");
        AddEpisode(context, series, minutes: 30, played: false);
        AddEpisode(context, series, minutes: 30, played: false);

        series = AddSeries(context, _fullyWatched, "C fully watched show");
        AddEpisode(context, series, minutes: 45, played: true);
        AddEpisode(context, series, minutes: 45, played: true);

        series = AddSeries(context, _fifteenMinutesLeft, "D partly unaired show");
        AddEpisode(context, series, minutes: 15, played: false);
        AddEpisode(context, series, minutes: 60, played: false, isVirtual: true);
        AddEpisode(context, series, minutes: 60, played: false, isVirtual: true);

        AddSeries(context, _withoutEpisodes, "E empty show");

        context.SaveChanges();
    }

    private BaseItemEntity AddSeries(JellyfinDbContext context, Guid id, string name)
    {
        var series = new BaseItemEntity
        {
            Id = id,
            Type = SeriesType,
            Name = name,
            SortName = name,
            CleanName = name.ToLowerInvariant(),
            PresentationUniqueKey = id.ToString("N"),
            IsFolder = true
        };

        context.BaseItems.Add(series);
        return series;
    }

    private void AddEpisode(JellyfinDbContext context, BaseItemEntity series, int minutes, bool played, bool isVirtual = false)
    {
        var episodeId = Guid.NewGuid();

        context.BaseItems.Add(new BaseItemEntity
        {
            Id = episodeId,
            Type = EpisodeType,
            Name = $"{series.Name} episode",
            PresentationUniqueKey = episodeId.ToString("N"),
            SeriesId = series.Id,
            SeriesPresentationUniqueKey = series.PresentationUniqueKey,
            RunTimeTicks = TimeSpan.FromMinutes(minutes).Ticks,
            IsVirtualItem = isVirtual
        });

        context.AncestorIds.Add(new AncestorId { ItemId = episodeId, ParentItemId = series.Id, Item = null!, ParentItem = null! });

        if (played)
        {
            context.UserData.Add(new UserData
            {
                ItemId = episodeId,
                UserId = _user.Id,
                CustomDataKey = episodeId.ToString("N"),
                Played = true,
                Item = null!,
                User = null!
            });
        }
    }
}
