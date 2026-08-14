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
    public void ShouldReturnMappedOrderForSortingByUserRating()
    {
        var user = new User("test-user", "auth-provider", "pwdreset-provider");
        var orderFunc = OrderMapper.MapOrderByField(ItemSortBy.UserRating, new InternalItemsQuery { User = user }, null!).Compile();

        var ratedEntity = CreateEntityWithUserData(user, 7.5);
        var unratedEntity = CreateEntityWithUserData(user, null);
        var entityRatedByAnotherUser = CreateEntityWithUserData(new User("other-user", "auth-provider", "pwdreset-provider"), 9);
        var entityWithoutUserData = new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", UserData = [] };

        Assert.Equal(7.5, orderFunc(ratedEntity));
        Assert.Null(orderFunc(unratedEntity));

        // Another user's rating must not leak into this user's ordering.
        Assert.Null(orderFunc(entityRatedByAnotherUser));
        Assert.Null(orderFunc(entityWithoutUserData));
    }

    private static BaseItemEntity CreateEntityWithUserData(User user, double? rating)
    {
        var itemId = Guid.NewGuid();
        return new BaseItemEntity
        {
            Id = itemId,
            Type = "Test",
            UserData =
            [
                new UserData
                {
                    CustomDataKey = "key",
                    ItemId = itemId,
                    Item = null,
                    UserId = user.Id,
                    User = null,
                    Rating = rating
                }
            ]
        };
    }
}
