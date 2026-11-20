using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Counts reported for a by-name item (studio, genre) by the list endpoint and by the single-item
/// endpoint. Both have to answer with the same numbers, and both count the episodes of a linked
/// series: the studio is normally only linked to the series itself.
/// </summary>
public sealed class ItemByNameCountsTests : SqliteDbTestFixture
{
    private static readonly BaseItemKind[] _studioRelatedKinds =
    [
        BaseItemKind.Audio,
        BaseItemKind.Episode,
        BaseItemKind.Movie,
        BaseItemKind.LiveTvProgram,
        BaseItemKind.MusicAlbum,
        BaseItemKind.MusicArtist,
        BaseItemKind.MusicVideo,
        BaseItemKind.Series,
        BaseItemKind.Trailer
    ];

    private static readonly Guid _studioId = Guid.Parse("11111111-0000-0000-0000-000000000001");

    private readonly BaseItemRepository _repository;
    private readonly ItemCountService _countService;
    private readonly ItemTypeLookup _itemTypeLookup;

    public ItemByNameCountsTests()
    {
        _itemTypeLookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(_itemTypeLookup);

        var queryHelpers = new Mock<IItemQueryHelpers>();
        queryHelpers
            .Setup(h => h.ApplyAccessFiltering(
                It.IsAny<JellyfinDbContext>(),
                It.IsAny<IQueryable<BaseItemEntity>>(),
                It.IsAny<InternalItemsQuery>()))
            .Returns((JellyfinDbContext _, IQueryable<BaseItemEntity> query, InternalItemsQuery _) => query);

        _countService = new ItemCountService(CreateDbContextFactory(), _itemTypeLookup, queryHelpers.Object);

        Seed();
    }

    [Fact]
    public void GetStudios_ReportsEveryCountAndATotal()
    {
        var result = _repository.GetStudios(CreateQuery([BaseItemKind.Series]));

        var counts = Assert.Single(result.Items).ItemCounts;
        Assert.NotNull(counts);
        Assert.Equal(1, counts.SeriesCount);
        Assert.Equal(1, counts.MovieCount);
        Assert.Equal(1, counts.MusicVideoCount);
        Assert.Equal(1, counts.ProgramCount);

        // Three episodes below the linked series plus the one linked under a series that is not.
        Assert.Equal(4, counts.EpisodeCount);

        // ChildCount is read off this, and used to be left at zero.
        Assert.Equal(8, counts.ItemCount);
    }

    [Fact]
    public void GetStudios_CountsAreNotRestrictedToTheRequestedItemTypes()
    {
        var seriesOnly = _repository.GetStudios(CreateQuery([BaseItemKind.Series])).Items[0].ItemCounts;
        var unfiltered = _repository.GetStudios(CreateQuery([])).Items[0].ItemCounts;

        Assert.NotNull(seriesOnly);
        Assert.NotNull(unfiltered);
        Assert.Equal(unfiltered.ItemCount, seriesOnly.ItemCount);
        Assert.Equal(unfiltered.EpisodeCount, seriesOnly.EpisodeCount);
    }

    [Fact]
    public void GetStudios_WithoutTheItemCountsField_SkipsTheCounts()
    {
        var query = CreateQuery([BaseItemKind.Series]);
        query.DtoOptions = new DtoOptions(false);

        Assert.Null(_repository.GetStudios(query).Items[0].ItemCounts);
    }

    [Fact]
    public void GetItemCountsForNameItem_MatchesTheListEndpoint()
    {
        var single = _countService.GetItemCountsForNameItem(
            BaseItemKind.Studio,
            _studioId,
            _studioRelatedKinds,
            new InternalItemsQuery());

        var fromList = _repository.GetStudios(CreateQuery([BaseItemKind.Series])).Items[0].ItemCounts;

        Assert.NotNull(fromList);
        Assert.Equal(fromList.SeriesCount, single.SeriesCount);
        Assert.Equal(fromList.EpisodeCount, single.EpisodeCount);
        Assert.Equal(fromList.MovieCount, single.MovieCount);
        Assert.Equal(fromList.MusicVideoCount, single.MusicVideoCount);
        Assert.Equal(fromList.ProgramCount, single.ProgramCount);
        Assert.Equal(fromList.ItemCount, single.ItemCount);
    }

    private static InternalItemsQuery CreateQuery(BaseItemKind[] includeItemTypes)
        => new(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = includeItemTypes,
            DtoOptions = new DtoOptions(false) { Fields = [ItemFields.ItemCounts] }
        };

    private void Seed()
    {
        using var ctx = CreateDbContext();

        ctx.BaseItems.Add(NewItem(_studioId, BaseItemKind.Studio, "Netflix"));

        var linkedSeries = Add(ctx, BaseItemKind.Series, "Linked Series", linked: true);
        var unlinkedSeries = Add(ctx, BaseItemKind.Series, "Unlinked Series", linked: false);

        // One of the linked series' own episodes carries the studio too: it must not count twice.
        AddEpisode(ctx, linkedSeries, "S1E1", linked: true);
        AddEpisode(ctx, linkedSeries, "S1E2", linked: false);
        AddEpisode(ctx, linkedSeries, "S1E3", linked: false);

        // Below a series that is not linked, so it only counts through its own link.
        AddEpisode(ctx, unlinkedSeries, "S2E1", linked: true);

        Add(ctx, BaseItemKind.Movie, "Linked Movie", linked: true);
        Add(ctx, BaseItemKind.MusicVideo, "Linked Music Video", linked: true);
        Add(ctx, BaseItemKind.LiveTvProgram, "Linked Program", linked: true);

        ctx.SaveChanges();
    }

    private Guid Add(JellyfinDbContext ctx, BaseItemKind kind, string name, bool linked)
    {
        var item = NewItem(Guid.NewGuid(), kind, name);
        ctx.BaseItems.Add(item);
        if (linked)
        {
            ctx.BaseItemStudios.Add(new BaseItemStudio { Item = null!, ItemId = item.Id, StudioItemId = _studioId });
        }

        return item.Id;
    }

    private void AddEpisode(JellyfinDbContext ctx, Guid seriesId, string name, bool linked)
    {
        var item = NewItem(Guid.NewGuid(), BaseItemKind.Episode, name);
        item.SeriesId = seriesId;
        ctx.BaseItems.Add(item);
        if (linked)
        {
            ctx.BaseItemStudios.Add(new BaseItemStudio { Item = null!, ItemId = item.Id, StudioItemId = _studioId });
        }
    }

    private BaseItemEntity NewItem(Guid id, BaseItemKind kind, string name)
        => new()
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[kind],
            Name = name,
            CleanName = name.ToLowerInvariant(),
            PresentationUniqueKey = id.ToString("N"),
            IsFolder = kind is BaseItemKind.Series or BaseItemKind.Studio,
            IsVirtualItem = false
        };
}
