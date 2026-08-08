using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Sorting;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Sorting;

public class DateAddedToListComparerTests
{
    [Fact]
    public void Compare_ItemsInDefaultList_OrdersByDateAdded()
    {
        var olderItem = new Movie { Id = Guid.NewGuid() };
        var newerItem = new Movie { Id = Guid.NewGuid() };
        var user = CreateUser();
        var defaultList = CreateDefaultList(user.Id);
        var itemListManager = new Mock<IItemListManager>();
        itemListManager
            .Setup(manager => manager.GetOrCreateDefaultListAsync(user.Id))
            .ReturnsAsync(defaultList);
        itemListManager
            .Setup(manager => manager.GetListItemDatesAsync(defaultList.Id))
            .ReturnsAsync(new Dictionary<Guid, DateTime>
            {
                [olderItem.Id] = new DateTime(2026, 1, 1),
                [newerItem.Id] = new DateTime(2026, 1, 2)
            });
        var comparer = CreateComparer(user, itemListManager.Object);

        Assert.True(comparer.Compare(olderItem, newerItem) < 0);
        Assert.True(comparer.Compare(newerItem, olderItem) > 0);
    }

    [Fact]
    public void Compare_ItemsAbsentFromDefaultList_SortsConsistently()
    {
        var listedItem = new Movie { Id = Guid.NewGuid() };
        var firstAbsentItem = new Movie { Id = Guid.NewGuid() };
        var secondAbsentItem = new Movie { Id = Guid.NewGuid() };
        var user = CreateUser();
        var defaultList = CreateDefaultList(user.Id);
        var itemListManager = new Mock<IItemListManager>();
        itemListManager
            .Setup(manager => manager.GetOrCreateDefaultListAsync(user.Id))
            .ReturnsAsync(defaultList);
        itemListManager
            .Setup(manager => manager.GetListItemDatesAsync(defaultList.Id))
            .ReturnsAsync(new Dictionary<Guid, DateTime>
            {
                [listedItem.Id] = new DateTime(2026, 1, 1)
            });
        var comparer = CreateComparer(user, itemListManager.Object);

        Assert.True(comparer.Compare(firstAbsentItem, listedItem) < 0);
        Assert.True(comparer.Compare(listedItem, firstAbsentItem) > 0);
        Assert.Equal(0, comparer.Compare(firstAbsentItem, secondAbsentItem));
        Assert.Equal(0, comparer.Compare(secondAbsentItem, firstAbsentItem));
    }

    [Fact]
    public void Compare_MultipleComparisons_ResolvesDefaultListDatesOnce()
    {
        var items = new[]
        {
            new Movie { Id = Guid.NewGuid() },
            new Movie { Id = Guid.NewGuid() },
            new Movie { Id = Guid.NewGuid() }
        };
        var user = CreateUser();
        var defaultList = CreateDefaultList(user.Id);
        var itemListManager = new Mock<IItemListManager>();
        itemListManager
            .Setup(manager => manager.GetOrCreateDefaultListAsync(user.Id))
            .ReturnsAsync(defaultList);
        itemListManager
            .Setup(manager => manager.GetListItemDatesAsync(defaultList.Id))
            .ReturnsAsync(new Dictionary<Guid, DateTime>
            {
                [items[0].Id] = new DateTime(2026, 1, 1),
                [items[1].Id] = new DateTime(2026, 1, 2),
                [items[2].Id] = new DateTime(2026, 1, 3)
            });
        var comparer = CreateComparer(user, itemListManager.Object);

        Assert.True(comparer.Compare(items[0], items[1]) < 0);
        Assert.True(comparer.Compare(items[1], items[2]) < 0);
        Assert.True(comparer.Compare(items[2], items[0]) > 0);
        Assert.Equal(0, comparer.Compare(items[1], items[1]));
        itemListManager.Verify(
            manager => manager.GetOrCreateDefaultListAsync(user.Id),
            Times.Once);
        itemListManager.Verify(
            manager => manager.GetListItemDatesAsync(defaultList.Id),
            Times.Once);
    }

    private static DateAddedToListComparer CreateComparer(User user, IItemListManager itemListManager)
    {
        return new DateAddedToListComparer
        {
            User = user,
            ItemListManager = itemListManager
        };
    }

    private static ItemList CreateDefaultList(Guid userId)
    {
        return new ItemList
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Watchlist",
            ListType = ItemListType.Watchlist,
            IsDefault = true
        };
    }

    private static User CreateUser()
    {
        return new User("sort-user", "authentication", "password-reset");
    }
}
