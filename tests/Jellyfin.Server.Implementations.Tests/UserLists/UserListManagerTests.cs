using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Library;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.UserLists;

public sealed class UserListManagerTests : IDisposable
{
    private const string BaseItemType = "MediaBrowser.Controller.Entities.Movies.Movie";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly ServerConfiguration _configuration;
    private readonly IDbContextFactory<JellyfinDbContext> _factory;
    private readonly UserListManager _manager;

    public UserListManagerTests()
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

        _manager = new UserListManager(configurationManager.Object, _factory);
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
            await context.UserLists.CountAsync(list => list.UserId.Equals(userId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateListAsync_DuplicateName_ThrowsDuplicateUserListNameException()
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
                    context.UserLists.Add(new UserList
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Name = "Animation",
                        DateCreated = now,
                        DateModified = now
                    });
                    context.SaveChanges();
                    duplicateInserted = true;
                }

                return _configuration;
            });
        using var manager = new UserListManager(configurationManager.Object, _factory);

        var exception = await Assert.ThrowsAsync<IUserListManager.DuplicateUserListNameException>(
            () => manager.CreateListAsync(userId, "Animation", true));

        Assert.IsType<DbUpdateException>(exception.InnerException);
        using var context = CreateDbContext();
        Assert.Equal(
            1,
            await context.UserLists.CountAsync(list => list.UserId.Equals(userId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteListAsync_DefaultList_ThrowsDefaultUserListDeletionException()
    {
        var userId = SeedUser();
        var defaultList = await _manager.GetOrCreateDefaultListAsync(userId);

        await Assert.ThrowsAsync<IUserListManager.DefaultUserListDeletionException>(
            () => _manager.DeleteListAsync(defaultList.Id));

        using var context = CreateDbContext();
        Assert.True(await context.UserLists.AnyAsync(list => list.Id.Equals(defaultList.Id), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateListAsync_OnePastConfiguredLimit_ThrowsUserListLimitExceededException()
    {
        _configuration.MaxUserListsPerUser = 2;
        var userId = SeedUser();
        await _manager.GetOrCreateDefaultListAsync(userId);
        await _manager.CreateListAsync(userId, "First custom list", false);

        await Assert.ThrowsAsync<IUserListManager.UserListLimitExceededException>(
            () => _manager.CreateListAsync(userId, "Past the limit", false));

        using var context = CreateDbContext();
        Assert.Equal(
            2,
            await context.UserLists.CountAsync(list => list.UserId.Equals(userId), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddItemAsync_OnePastConfiguredLimit_ThrowsUserListLimitExceededException()
    {
        _configuration.MaxItemsPerUserList = 1;
        var userId = SeedUser();
        var itemIds = SeedItems(2);
        var list = await _manager.GetOrCreateDefaultListAsync(userId);
        await _manager.AddItemAsync(list.Id, itemIds[0]);

        await Assert.ThrowsAsync<IUserListManager.UserListLimitExceededException>(
            () => _manager.AddItemAsync(list.Id, itemIds[1]));

        using var context = CreateDbContext();
        Assert.Equal(
            1,
            await context.UserListItems.CountAsync(entry => entry.UserListId.Equals(list.Id), TestContext.Current.CancellationToken));
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
        var entries = await context.UserListItems
            .AsNoTracking()
            .Where(entry => entry.UserListId.Equals(list.Id))
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
        var entry = await context.UserListItems
            .SingleAsync(listItem => listItem.UserListId.Equals(list.Id) && listItem.ItemId.Equals(itemId), TestContext.Current.CancellationToken);
        Assert.Equal(0, entry.SortIndex);
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
            Assert.False(await context.UserLists.AnyAsync(listEntity => listEntity.UserId.Equals(userId), TestContext.Current.CancellationToken));
            Assert.False(await context.UserListItems.AnyAsync(entry => entry.UserListId.Equals(list.Id), TestContext.Current.CancellationToken));
            Assert.True(await context.BaseItems.AnyAsync(item => item.Id.Equals(itemId), TestContext.Current.CancellationToken));
        }
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
        var itemIds = Enumerable.Range(0, count)
            .Select(_ => Guid.NewGuid())
            .ToArray();
        using var context = CreateDbContext();
        context.BaseItems.AddRange(itemIds.Select(itemId => new BaseItemEntity
        {
            Id = itemId,
            Type = BaseItemType
        }));
        context.SaveChanges();
        return itemIds;
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
