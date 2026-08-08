using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Verifies user-list cleanup performed by item deletion against the SQLite provider.
/// </summary>
public sealed class ItemListBaseItemMapDeletionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly ItemPersistenceService _itemPersistenceService;

    public ItemListBaseItemMapDeletionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        _itemPersistenceService = new ItemPersistenceService(
            factory.Object,
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    [Fact]
    public void DeleteItem_DetachesListEntriesWithoutPlaceholderingThem()
    {
        Guid userId;
        Guid firstListId;
        Guid secondListId;
        Guid deletedItemId;
        Guid retainedItemId;
        const string UserDataKey = "deleted-item-key";

        using (var context = CreateDbContext())
        {
            var user = new User("deletion-user", "authentication", "password-reset");
            var firstList = CreateList(user.Id, "Watchlist", isDefault: true);
            var secondList = CreateList(user.Id, "Custom", isDefault: false);
            var deletedItem = CreateItem("Deleted Item");
            var retainedItem = CreateItem("Retained Item");

            context.Users.Add(user);
            context.ItemLists.AddRange(firstList, secondList);
            context.BaseItems.AddRange(deletedItem, retainedItem);
            context.ItemListBaseItemMap.AddRange(
                CreateListItem(firstList, deletedItem),
                CreateListItem(secondList, deletedItem),
                CreateListItem(firstList, retainedItem));
            context.UserData.Add(new UserData
            {
                ItemId = deletedItem.Id,
                Item = deletedItem,
                UserId = user.Id,
                User = user,
                CustomDataKey = UserDataKey
            });
            context.SaveChanges();

            userId = user.Id;
            firstListId = firstList.Id;
            secondListId = secondList.Id;
            deletedItemId = deletedItem.Id;
            retainedItemId = retainedItem.Id;
        }

        var beforeDelete = DateTime.UtcNow;
        _itemPersistenceService.DeleteItem(deletedItemId);
        var afterDelete = DateTime.UtcNow;

        using (var context = CreateDbContext())
        {
            var seededListIds = new[] { firstListId, secondListId };

            var detachedEntries = context.ItemListBaseItemMap
                .Where(entry => seededListIds.Contains(entry.ItemListId)
                    && entry.CustomDataKey == deletedItemId.ToString())
                .ToList();
            Assert.Equal(2, detachedEntries.Count);
            Assert.All(
                detachedEntries,
                entry =>
                {
                    Assert.False(entry.ItemId.HasValue);
                    Assert.NotNull(entry.RetentionDate);
                    Assert.InRange(entry.RetentionDate!.Value, beforeDelete, afterDelete);
                });
            Assert.DoesNotContain(
                detachedEntries,
                entry => entry.ItemId.HasValue
                    && entry.ItemId!.Value.Equals(BaseItemRepository.PlaceholderId));

            var retainedEntry = Assert.Single(context.ItemListBaseItemMap.Where(
                entry => seededListIds.Contains(entry.ItemListId)
                    && entry.CustomDataKey == retainedItemId.ToString()));
            Assert.Equal(firstListId, retainedEntry.ItemListId);
            Assert.Equal(retainedItemId, retainedEntry.ItemId);
            Assert.Null(retainedEntry.RetentionDate);

            var retainedUserData = Assert.Single(context.UserData.Where(
                data => data.UserId.Equals(userId) && data.CustomDataKey == UserDataKey));
            Assert.Equal(BaseItemRepository.PlaceholderId, retainedUserData.ItemId);
            Assert.NotNull(retainedUserData.RetentionDate);

            Assert.False(context.BaseItems.Any(item => item.Id.Equals(deletedItemId)));
            Assert.True(context.BaseItems.Any(item => item.Id.Equals(retainedItemId)));
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private static ItemList CreateList(Guid userId, string name, bool isDefault)
    {
        return new ItemList
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            ListType = ItemListType.Watchlist,
            IsDefault = isDefault,
            AutoRemoveWatched = isDefault,
            DateCreated = new DateTime(2026, 1, 1),
            DateModified = new DateTime(2026, 1, 1)
        };
    }

    private static BaseItemEntity CreateItem(string name)
    {
        return new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "MediaBrowser.Controller.Entities.Movies.Movie",
            Name = name,
            SortName = name,
            MediaType = "Video",
            IsMovie = true
        };
    }

    private static ItemListBaseItemMap CreateListItem(ItemList list, BaseItemEntity item)
    {
        return new ItemListBaseItemMap
        {
            ItemListId = list.Id,
            ItemList = list,
            CustomDataKey = item.Id.ToString(),
            ItemId = item.Id,
            Item = item,
            DateAdded = new DateTime(2026, 1, 1)
        };
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
