using System;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class OrderMapperTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public OrderMapperTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

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

    /// <summary>
    /// Runs against SQLite rather than a compiled expression, so a sort that
    /// cannot be translated to SQL fails here rather than at runtime.
    /// </summary>
    [Fact]
    public void ShouldOrderByTheRequestingUsersOwnRating()
    {
        using var context = CreateDbContext();

        var user = new User("test-user", "auth-provider", "pwdreset-provider") { Id = Guid.NewGuid() };
        var otherUser = new User("other-user", "auth-provider", "pwdreset-provider") { Id = Guid.NewGuid() };
        context.Users.AddRange(user, otherUser);

        var ratedHigh = CreateItem("rated-high");
        var ratedLow = CreateItem("rated-low");
        var unrated = CreateItem("unrated");
        var ratedByAnotherUser = CreateItem("rated-by-another-user");
        context.BaseItems.AddRange(ratedHigh, ratedLow, unrated, ratedByAnotherUser);

        context.UserData.AddRange(
            CreateUserData(user.Id, ratedHigh.Id, 9),
            CreateUserData(user.Id, ratedLow.Id, 2),
            CreateUserData(otherUser.Id, ratedByAnotherUser.Id, 10));
        context.SaveChanges();

        var order = OrderMapper.MapOrderByField(ItemSortBy.UserRating, new InternalItemsQuery { User = user }, context);

        // The schema seeds a placeholder BaseItem, so scope to the fixtures.
        var itemIds = new[] { ratedHigh.Id, ratedLow.Id, unrated.Id, ratedByAnotherUser.Id };
        var ordered = context.BaseItems
            .Where(e => itemIds.Contains(e.Id))
            .OrderByDescending(order)
            .Select(e => e.Name)
            .ToList();

        Assert.Equal("rated-high", ordered[0]);
        Assert.Equal("rated-low", ordered[1]);

        // Another user's rating must not leak into this user's ordering, so it
        // sorts alongside the unrated item rather than ahead of everything.
        Assert.Equal(
            new[] { "rated-by-another-user", "unrated" },
            ordered.Skip(2).Order().ToArray());
    }

    /// <summary>
    /// The sort is only defined for a user, so it must fall back rather than
    /// dereference a null one.
    /// </summary>
    [Fact]
    public void ShouldFallBackToSortNameWhenThereIsNoUser()
    {
        using var context = CreateDbContext();

        var order = OrderMapper.MapOrderByField(ItemSortBy.UserRating, new InternalItemsQuery(), context).Compile();

        var entity = new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", SortName = "a-sort-name" };

        Assert.Equal("a-sort-name", order(entity));
    }

    private static BaseItemEntity CreateItem(string name)
    {
        return new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "Test",
            Name = name,
            SortName = name
        };
    }

    private static UserData CreateUserData(Guid userId, Guid itemId, double rating)
    {
        return new UserData
        {
            CustomDataKey = "key",
            ItemId = itemId,
            Item = null,
            UserId = userId,
            User = null,
            Rating = rating
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
