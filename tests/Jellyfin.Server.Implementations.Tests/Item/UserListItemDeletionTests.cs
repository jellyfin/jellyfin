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
public sealed class UserListItemDeletionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly ItemPersistenceService _itemPersistenceService;

    public UserListItemDeletionTests()
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
    public void DeleteItem_RemovesListEntriesWithoutPlaceholderingThem()
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
            context.UserLists.AddRange(firstList, secondList);
            context.BaseItems.AddRange(deletedItem, retainedItem);
            context.UserListItems.AddRange(
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

        _itemPersistenceService.DeleteItem(deletedItemId);

        using (var context = CreateDbContext())
        {
            var seededListIds = new[] { firstListId, secondListId };

            Assert.False(context.UserListItems.Any(
                entry => seededListIds.Contains(entry.UserListId) && entry.ItemId.Equals(deletedItemId)));
            Assert.False(context.UserListItems.Any(
                entry => seededListIds.Contains(entry.UserListId)
                    && entry.ItemId.Equals(BaseItemRepository.PlaceholderId)));

            var retainedEntry = Assert.Single(context.UserListItems.Where(
                entry => seededListIds.Contains(entry.UserListId)));
            Assert.Equal(firstListId, retainedEntry.UserListId);
            Assert.Equal(retainedItemId, retainedEntry.ItemId);

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

    private static UserList CreateList(Guid userId, string name, bool isDefault)
    {
        return new UserList
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Kind = isDefault ? UserListKind.Watchlist : UserListKind.Custom,
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

    private static UserListItem CreateListItem(UserList list, BaseItemEntity item)
    {
        return new UserListItem
        {
            UserListId = list.Id,
            UserList = list,
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
