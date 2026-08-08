using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Library;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ItemLists;

public sealed class ItemListManagerTests : IDisposable
{
    private const string BaseItemType = "MediaBrowser.Controller.Entities.Movies.Movie";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly ServerConfiguration _configuration;
    private readonly IDbContextFactory<JellyfinDbContext> _factory;
    private readonly ItemPersistenceService _itemPersistenceService;
    private readonly Dictionary<Guid, BaseItem> _libraryItems = new();
    private readonly Mock<ILibraryManager> _libraryManager;
    private readonly ItemListManager _manager;

    public ItemListManagerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);
        _factory = factory.Object;

        _configuration = new ServerConfiguration();
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(manager => manager.Configuration).Returns(_configuration);

        _libraryManager = new Mock<ILibraryManager>();
        _libraryManager
            .Setup(manager => manager.GetItemById(It.IsAny<Guid>()))
            .Returns((Guid itemId) => _libraryItems.GetValueOrDefault(itemId));
        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        _itemPersistenceService = new ItemPersistenceService(
            _factory,
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
        _manager = new ItemListManager(
            configurationManager.Object,
            _factory,
            new Lazy<ILibraryManager>(() => _libraryManager.Object));
    }

    public void Dispose()
    {
        _manager.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetOrCreateDefaultListAsync_CalledTwice_ReturnsSameListAndCreatesOneRow()
    {
        var userId = SeedUser();

        var first = await _manager.GetOrCreateDefaultListAsync(userId);
        var second = await _manager.GetOrCreateDefaultListAsync(userId);

        Assert.True(first.Id.Equals(second.Id));
        Assert.True(first.IsDefault);
        Assert.True(first.AutoRemoveWatched);
        using var context = CreateDbContext();
        Assert.Equal(
            1,
            await context.ItemLists.CountAsync(list => list.UserId.Equals(userId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateListAsync_DuplicateName_ThrowsDuplicateItemListNameException()
    {
        var userId = SeedUser();
        var duplicateInserted = false;
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager
            .Setup(manager => manager.Configuration)
            .Returns(() =>
            {
                if (!duplicateInserted)
                {
                    var now = DateTime.UtcNow;
                    using var context = CreateDbContext();
                    context.ItemLists.Add(new ItemList
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Name = "Animation",
                        ListType = ItemListType.Watchlist,
                        DateCreated = now,
                        DateModified = now
                    });
                    context.SaveChanges();
                    duplicateInserted = true;
                }

                return _configuration;
            });
        using var manager = new ItemListManager(
            configurationManager.Object,
            _factory,
            new Lazy<ILibraryManager>(() => _libraryManager.Object));

        var exception = await Assert.ThrowsAsync<IItemListManager.DuplicateItemListNameException>(
            () => manager.CreateListAsync(userId, "Animation", true));

        Assert.IsType<DbUpdateException>(exception.InnerException);
        using var context = CreateDbContext();
        Assert.Equal(
            1,
            await context.ItemLists.CountAsync(list => list.UserId.Equals(userId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteListAsync_DefaultList_ThrowsDefaultItemListDeletionException()
    {
        var userId = SeedUser();
        var defaultList = await _manager.GetOrCreateDefaultListAsync(userId);

        await Assert.ThrowsAsync<IItemListManager.DefaultItemListDeletionException>(
            () => _manager.DeleteListAsync(defaultList.Id));

        using var context = CreateDbContext();
        Assert.True(await context.ItemLists.AnyAsync(list => list.Id.Equals(defaultList.Id), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateListAsync_OnePastConfiguredLimit_ThrowsItemListLimitExceededException()
    {
        _configuration.MaxUserListsPerUser = 2;
        var userId = SeedUser();
        await _manager.GetOrCreateDefaultListAsync(userId);
        await _manager.CreateListAsync(userId, "First custom list", false);

        await Assert.ThrowsAsync<IItemListManager.ItemListLimitExceededException>(
            () => _manager.CreateListAsync(userId, "Past the limit", false));

        using var context = CreateDbContext();
        Assert.Equal(
            2,
            await context.ItemLists.CountAsync(list => list.UserId.Equals(userId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddItemAsync_OnePastConfiguredLimit_ThrowsItemListLimitExceededException()
    {
        _configuration.MaxItemsPerUserList = 1;
        var userId = SeedUser();
        var itemIds = SeedItems(2);
        var list = await _manager.GetOrCreateDefaultListAsync(userId);
        await _manager.AddItemAsync(list.Id, itemIds[0]);

        await Assert.ThrowsAsync<IItemListManager.ItemListLimitExceededException>(
            () => _manager.AddItemAsync(list.Id, itemIds[1]));

        using var context = CreateDbContext();
        Assert.Equal(
            1,
            await context.ItemListBaseItemMap.CountAsync(entry => entry.ItemListId.Equals(list.Id), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MoveItemAsync_MovesItemWithoutCollisionsAndPreservesOtherItemsRelativeOrder()
    {
        var userId = SeedUser();
        var itemIds = SeedItems(4);
        var list = await _manager.GetOrCreateDefaultListAsync(userId);
        foreach (var itemId in itemIds)
        {
            await _manager.AddItemAsync(list.Id, itemId);
        }

        await _manager.MoveItemAsync(list.Id, itemIds[0], 2);

        using var context = CreateDbContext();
        var entries = await context.ItemListBaseItemMap
            .AsNoTracking()
            .Where(entry => entry.ItemListId.Equals(list.Id))
            .OrderBy(entry => entry.SortIndex)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal([itemIds[1], itemIds[2], itemIds[0], itemIds[3]], entries.Select(entry => entry.ItemId));
        Assert.Equal([0, 1, 2, 3], entries.Select(entry => entry.SortIndex));
        Assert.Equal(entries.Count, entries.Select(entry => entry.SortIndex).Distinct().Count());
    }

    [Fact]
    public async Task AddItemAsync_CalledTwiceForSameItem_CreatesOneRow()
    {
        var userId = SeedUser();
        var itemId = Assert.Single(SeedItems(1));
        var list = await _manager.GetOrCreateDefaultListAsync(userId);

        await _manager.AddItemAsync(list.Id, itemId);
        await _manager.AddItemAsync(list.Id, itemId);

        using var context = CreateDbContext();
        var entry = await context.ItemListBaseItemMap
            .SingleAsync(listItem => listItem.ItemListId.Equals(list.Id) && listItem.ItemId.Equals(itemId), TestContext.Current.CancellationToken);
        Assert.Equal(0, entry.SortIndex);
    }

    [Fact]
    public async Task DetachedItem_IsExcludedFromMembershipAndListItemIds()
    {
        var userId = SeedUser();
        var itemIds = SeedItems(2);
        var list = await _manager.GetOrCreateDefaultListAsync(userId);
        await _manager.AddItemAsync(list.Id, itemIds[0]);
        await _manager.AddItemAsync(list.Id, itemIds[1]);
        _itemPersistenceService.DeleteItem(itemIds[0]);

        var membership = await _manager.GetMembershipAsync(userId, itemIds);
        var listItemIds = await _manager.GetListItemIdsAsync(list.Id);
        var listItemDates = await _manager.GetListItemDatesAsync(list.Id);

        Assert.True(membership.TryGetValue(itemIds[0], out var detachedMembership));
        Assert.Empty(detachedMembership);
        Assert.True(membership.TryGetValue(itemIds[1], out var attachedMembership));
        Assert.Contains(list.Id, attachedMembership);
        Assert.DoesNotContain(itemIds[0], listItemIds);
        Assert.Contains(itemIds[1], listItemIds);
        Assert.False(listItemDates.ContainsKey(itemIds[0]));
        Assert.True(listItemDates.ContainsKey(itemIds[1]));
        Assert.False(_libraryItems[itemIds[0]].IsWatchlisted(listItemIds));
        Assert.True(_libraryItems[itemIds[1]].IsWatchlisted(listItemIds));
    }

    [Fact]
    public async Task AddItemAsync_DetachedItemDoesNotCountTowardConfiguredLimit()
    {
        _configuration.MaxItemsPerUserList = 1;
        var userId = SeedUser();
        var itemIds = SeedItems(2);
        var list = await _manager.GetOrCreateDefaultListAsync(userId);
        await _manager.AddItemAsync(list.Id, itemIds[0]);
        _itemPersistenceService.DeleteItem(itemIds[0]);

        await _manager.AddItemAsync(list.Id, itemIds[1]);

        using var context = CreateDbContext();
        var entries = await context.ItemListBaseItemMap
            .Where(entry => entry.ItemListId.Equals(list.Id))
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, entries.Count);
        var detachedEntry = Assert.Single(entries, entry => !entry.ItemId.HasValue);
        Assert.NotNull(detachedEntry.RetentionDate);
        var attachedEntry = Assert.Single(entries, entry => entry.ItemId.HasValue);
        Assert.Equal(itemIds[1], attachedEntry.ItemId);
    }

    [Fact]
    public async Task DeleteUser_ThroughDbContext_CascadesToListsAndListItems()
    {
        var userId = SeedUser();
        var itemId = Assert.Single(SeedItems(1));
        var list = await _manager.GetOrCreateDefaultListAsync(userId);
        await _manager.AddItemAsync(list.Id, itemId);

        using (var context = CreateDbContext())
        {
            var user = await context.Users.SingleAsync(entity => entity.Id.Equals(userId), TestContext.Current.CancellationToken);
            context.Users.Remove(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using (var context = CreateDbContext())
        {
            Assert.False(await context.ItemLists.AnyAsync(listEntity => listEntity.UserId.Equals(userId), TestContext.Current.CancellationToken));
            Assert.False(await context.ItemListBaseItemMap.AnyAsync(entry => entry.ItemListId.Equals(list.Id), TestContext.Current.CancellationToken));
            Assert.True(await context.BaseItems.AnyAsync(item => item.Id.Equals(itemId), TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task DeleteListAsync_CustomList_CascadesToDetachedListItems()
    {
        var userId = SeedUser();
        var itemId = Assert.Single(SeedItems(1));
        var list = await _manager.CreateListAsync(userId, "Delete me", false);
        await _manager.AddItemAsync(list.Id, itemId);
        DetachListItem(list.Id, itemId);

        await _manager.DeleteListAsync(list.Id);

        using var context = CreateDbContext();
        Assert.False(await context.ItemLists.AnyAsync(entity => entity.Id.Equals(list.Id), TestContext.Current.CancellationToken));
        Assert.False(await context.ItemListBaseItemMap.AnyAsync(entry => entry.ItemListId.Equals(list.Id), TestContext.Current.CancellationToken));
        Assert.True(await context.BaseItems.AnyAsync(item => item.Id.Equals(itemId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMembershipAsync_IncludesEveryRequestedItemIncludingNonMembers()
    {
        var userId = SeedUser();
        var itemIds = SeedItems(3);
        var list = await _manager.GetOrCreateDefaultListAsync(userId);
        await _manager.AddItemAsync(list.Id, itemIds[0]);

        var membership = await _manager.GetMembershipAsync(userId, itemIds);

        Assert.Equal(itemIds.Length, membership.Count);
        Assert.True(membership.TryGetValue(itemIds[0], out var listedItemMembership));
        Assert.Contains(list.Id, listedItemMembership);
        Assert.True(membership.TryGetValue(itemIds[1], out var firstNonMemberMembership));
        Assert.Empty(firstNonMemberMembership);
        Assert.True(membership.TryGetValue(itemIds[2], out var secondNonMemberMembership));
        Assert.Empty(secondNonMemberMembership);
    }

    private Guid SeedUser()
    {
        var user = new User(
            "test-user-" + Guid.NewGuid().ToString("N"),
            "authentication",
            "password-reset");
        using var context = CreateDbContext();
        context.Users.Add(user);
        context.SaveChanges();
        return user.Id;
    }

    private Guid[] SeedItems(int count)
    {
        var items = Enumerable.Range(0, count)
            .Select(index =>
            {
                var movie = new Movie
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Movie " + index
                };
                movie.SetProviderId(MetadataProvider.Imdb, "tt" + movie.Id.ToString("N"));
                return movie;
            })
            .ToArray();
        foreach (var item in items)
        {
            _libraryItems.Add(item.Id, item);
        }

        using var context = CreateDbContext();
        context.BaseItems.AddRange(items.Select(item => new BaseItemEntity
        {
            Id = item.Id,
            Type = BaseItemType
        }));
        context.SaveChanges();
        return items.Select(item => item.Id).ToArray();
    }

    private void DetachListItem(Guid listId, Guid itemId)
    {
        using var context = CreateDbContext();
        var entry = context.ItemListBaseItemMap.Single(
            listItem => listItem.ItemListId.Equals(listId)
                && listItem.ItemId.HasValue
                && listItem.ItemId!.Value.Equals(itemId));
        entry.ItemId = null;
        entry.Item = null;
        entry.RetentionDate = DateTime.UtcNow;
        context.SaveChanges();
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
