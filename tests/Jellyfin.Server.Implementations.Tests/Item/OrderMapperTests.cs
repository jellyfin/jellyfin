using System;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public class OrderMapperTests
{
    [Fact]
    public void ShouldReturnMappedOrderForSortingByPremierDate()
    {
        var orderFunc = OrderMapper.MapOrderByField(ItemSortBy.PremiereDate, new InternalItemsQuery(), null!).Compile();

        var expectedDate = new DateTime(1, 2, 3);
        var expectedProductionYearDate = new DateTime(4, 1, 1);

        var entityWithOnlyProductionYear = new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", ProductionYear = expectedProductionYearDate.Year };
        var entityWithOnlyPremierDate = new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", PremiereDate = expectedDate };
        var entityWithBothPremierDateAndProductionYear = new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", PremiereDate = expectedDate, ProductionYear = expectedProductionYearDate.Year };
        var entityWithoutEitherPremierDateOrProductionYear = new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test" };

        var resultWithOnlyProductionYear = orderFunc(entityWithOnlyProductionYear);
        var resultWithOnlyPremierDate = orderFunc(entityWithOnlyPremierDate);
        var resultWithBothPremierDateAndProductionYear = orderFunc(entityWithBothPremierDateAndProductionYear);
        var resultWithoutEitherPremierDateOrProductionYear = orderFunc(entityWithoutEitherPremierDateOrProductionYear);

        Assert.Equal(resultWithOnlyProductionYear, expectedProductionYearDate);
        Assert.Equal(resultWithOnlyPremierDate, expectedDate);
        Assert.Equal(resultWithBothPremierDateAndProductionYear, expectedDate);
        Assert.Null(resultWithoutEitherPremierDateOrProductionYear);
    }

    [Fact]
    public void ShouldReturnMappedOrderForSortingByChannelGroup()
    {
        var orderFunc = OrderMapper.MapOrderByField(ItemSortBy.ChannelGroup, new InternalItemsQuery(), null!).Compile();

        var grouped = new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test" };
        grouped.ItemValues =
        [
            Map(grouped, ItemValueType.Tags, "a tag"),
            Map(grouped, ItemValueType.ChannelGroup, "sports")
        ];
        var ungrouped = new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test" };
        ungrouped.ItemValues = [Map(ungrouped, ItemValueType.Tags, "sports")];

        Assert.Equal("sports", orderFunc(grouped));
        Assert.Null(orderFunc(ungrouped));
    }

    private static ItemValueMap Map(BaseItemEntity item, ItemValueType type, string cleanValue)
    {
        var value = new ItemValue { ItemValueId = Guid.NewGuid(), Type = type, Value = cleanValue, CleanValue = cleanValue };
        return new ItemValueMap { ItemId = item.Id, ItemValueId = value.ItemValueId, Item = item, ItemValue = value };
    }
}
