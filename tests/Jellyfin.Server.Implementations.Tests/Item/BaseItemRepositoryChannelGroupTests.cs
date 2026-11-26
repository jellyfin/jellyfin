using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryChannelGroupTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly string _channelTypeName;
    private readonly string _movieTypeName;

    private readonly BaseItemEntity _sportsOne;
    private readonly BaseItemEntity _sportsTwo;
    private readonly BaseItemEntity _news;
    private readonly BaseItemEntity _ungrouped;

    public BaseItemRepositoryChannelGroupTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _channelTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.LiveTvChannel];
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
        _repository = CreateBaseItemRepository(itemTypeLookup);

        _sportsOne = CreateChannel("ESPN");
        _sportsTwo = CreateChannel("Fox Sports");
        _news = CreateChannel("CNN");
        _ungrouped = CreateChannel("Local Access");

        // A movie that somehow carries a group value must never show up as a channel group,
        // and a channel TAG spelled like a group must not count as one.
        var movie = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _movieTypeName,
            Name = "Movie",
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };

        var sports = CreateItemValue(ItemValueType.ChannelGroup, "Sports", "sports");
        var sportsLowercase = CreateItemValue(ItemValueType.ChannelGroup, "sports", "sports");
        var news = CreateItemValue(ItemValueType.ChannelGroup, "News", "news");
        var movieGroup = CreateItemValue(ItemValueType.ChannelGroup, "Movies", "movies");
        var sportsTag = CreateItemValue(ItemValueType.Tags, "Sports", "sports");

        using var context = CreateDbContext();
        context.BaseItems.AddRange(_sportsOne, _sportsTwo, _news, _ungrouped, movie);
        context.ItemValues.AddRange(sports, sportsLowercase, news, movieGroup, sportsTag);
        context.ItemValuesMap.AddRange(
            CreateMap(_sportsOne, sports),
            CreateMap(_sportsTwo, sportsLowercase),
            CreateMap(_news, news),
            CreateMap(movie, movieGroup),
            CreateMap(_ungrouped, sportsTag));
        context.SaveChanges();
    }

    [Fact]
    public void GetLiveTvChannelGroups_ReturnsDistinctGroupsWithChannelCountsOrderedByName()
    {
        var groups = _repository.GetLiveTvChannelGroups(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.LiveTvChannel]
        });

        Assert.Equal([("News", 1), ("Sports", 2)], groups);
    }

    [Fact]
    public void GetLiveTvChannelGroups_HonoursTheItemFilter()
    {
        var groups = _repository.GetLiveTvChannelGroups(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.LiveTvChannel],
            ItemIds = [_news.Id, _ungrouped.Id]
        });

        Assert.Equal([("News", 1)], groups);
    }

    [Fact]
    public void ChannelGroupsFilter_MatchesCaseInsensitivelyAcrossAnyOfTheGroups()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.LiveTvChannel],
            ChannelGroups = ["SPORTS"]
        });

        Assert.Equal([_sportsOne.Id, _sportsTwo.Id], ids.OrderBy(id => id.Equals(_sportsTwo.Id)));

        ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.LiveTvChannel],
            ChannelGroups = ["news", "Sports"]
        });

        Assert.Equal(3, ids.Count);
        Assert.DoesNotContain(_ungrouped.Id, ids);
    }

    [Fact]
    public void ChannelGroupsFilter_UnknownGroupMatchesNothing()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.LiveTvChannel],
            ChannelGroups = ["Movies"]
        });

        Assert.Empty(ids);
    }

    [Fact]
    public void ChannelGroupSort_OrdersByGroupThenLeavesUngroupedLast()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.LiveTvChannel],
            OrderBy = [(ItemSortBy.ChannelGroup, SortOrder.Descending), (ItemSortBy.Name, SortOrder.Ascending)]
        });

        // Descending puts the ungrouped channel (null group) last on SQLite, then "sports" before "news".
        Assert.Equal([_sportsOne.Id, _sportsTwo.Id, _news.Id, _ungrouped.Id], ids);
    }

    private BaseItemEntity CreateChannel(string name)
    {
        return new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _channelTypeName,
            Name = name,
            CleanName = name.ToLowerInvariant(),
            SortName = name.ToLowerInvariant(),
            MediaType = "Video",
            IsFolder = false,
            IsVirtualItem = false
        };
    }

    private static ItemValueMap CreateMap(BaseItemEntity item, ItemValue itemValue)
    {
        return new ItemValueMap
        {
            ItemId = item.Id,
            ItemValueId = itemValue.ItemValueId,
            Item = item,
            ItemValue = itemValue
        };
    }

    private static ItemValue CreateItemValue(ItemValueType type, string value, string cleanValue)
    {
        return new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = type,
            Value = value,
            CleanValue = cleanValue
        };
    }
}
