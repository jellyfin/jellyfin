using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.Recommendations;

public sealed class PeopleRepositoryItemIdsTests : IDisposable
{
    // Keep the connection alive for the lifetime of the test so that the in-memory SQLite DB
    // persists between factory calls (each CreateDbContext() reuses the same connection).
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _options;
    private readonly Mock<IJellyfinDatabaseProvider> _dbProviderMock;
    private readonly Mock<IEntityFrameworkCoreLockingBehavior> _lockingMock;
    private readonly Mock<IItemTypeLookup> _itemTypeLookupMock;

    public PeopleRepositoryItemIdsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbProviderMock = new Mock<IJellyfinDatabaseProvider>();
        _dbProviderMock.Setup(p => p.OnModelCreating(It.IsAny<ModelBuilder>())); // no-op
        _dbProviderMock.Setup(p => p.ConfigureConventions(It.IsAny<ModelConfigurationBuilder>())); // no-op

        _lockingMock = new Mock<IEntityFrameworkCoreLockingBehavior>();
        _lockingMock.Setup(l => l.OnSaveChanges(It.IsAny<JellyfinDbContext>(), It.IsAny<Action>()))
            .Callback<JellyfinDbContext, Action>((_, save) => save());

        _options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        _itemTypeLookupMock = new Mock<IItemTypeLookup>();

        // Ensure schema is created once.
        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private JellyfinDbContext CreateContext()
        => new JellyfinDbContext(
            _options,
            NullLogger<JellyfinDbContext>.Instance,
            _dbProviderMock.Object,
            _lockingMock.Object);

    private Mock<IDbContextFactory<JellyfinDbContext>> BuildFactoryMock()
    {
        var factoryMock = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factoryMock.Setup(f => f.CreateDbContext()).Returns(CreateContext);
        return factoryMock;
    }

    [Fact]
    public void GetPeople_WithItemIds_ReturnsPeopleFromAnyOfTheSpecifiedItems()
    {
        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();
        var itemC = Guid.NewGuid();

        using var setup = CreateContext();
        setup.BaseItems.AddRange(
            new BaseItemEntity { Id = itemA, Type = "Movie" },
            new BaseItemEntity { Id = itemB, Type = "Movie" },
            new BaseItemEntity { Id = itemC, Type = "Movie" });

        var personA = new People { Id = Guid.NewGuid(), Name = "Alice", PersonType = "Actor" };
        var personB = new People { Id = Guid.NewGuid(), Name = "Bob", PersonType = "Director" };
        var personC = new People { Id = Guid.NewGuid(), Name = "Carol", PersonType = "Actor" };
        setup.Peoples.AddRange(personA, personB, personC);

        setup.PeopleBaseItemMap.AddRange(
            new PeopleBaseItemMap { PeopleId = personA.Id, People = personA, ItemId = itemA, Item = setup.BaseItems.Find(itemA)!, ListOrder = 0, SortOrder = 0, Role = string.Empty },
            new PeopleBaseItemMap { PeopleId = personB.Id, People = personB, ItemId = itemB, Item = setup.BaseItems.Find(itemB)!, ListOrder = 0, SortOrder = 0, Role = string.Empty },
            new PeopleBaseItemMap { PeopleId = personC.Id, People = personC, ItemId = itemC, Item = setup.BaseItems.Find(itemC)!, ListOrder = 0, SortOrder = 0, Role = string.Empty });
        setup.SaveChanges();

        var repo = new global::Jellyfin.Server.Implementations.Item.PeopleRepository(
            BuildFactoryMock().Object, _itemTypeLookupMock.Object);

        var result = repo.GetPeople(new InternalPeopleQuery
        {
            ItemIds = new[] { itemA, itemB }
        });

        var names = result.Items.Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "Alice", "Bob" }, names);
    }

    [Fact]
    public void GetPeople_WithItemIds_AttachesSourceItemIdAndSortOrderPerMapping()
    {
        var itemA = Guid.NewGuid();
        var itemB = Guid.NewGuid();

        using var setup = CreateContext();
        setup.BaseItems.AddRange(
            new BaseItemEntity { Id = itemA, Type = "Movie" },
            new BaseItemEntity { Id = itemB, Type = "Movie" });

        var sharedPerson = new People { Id = Guid.NewGuid(), Name = "Shared", PersonType = "Actor" };
        setup.Peoples.Add(sharedPerson);

        setup.PeopleBaseItemMap.AddRange(
            new PeopleBaseItemMap { PeopleId = sharedPerson.Id, People = sharedPerson, ItemId = itemA, Item = setup.BaseItems.Find(itemA)!, ListOrder = 2, SortOrder = 2, Role = "Lead" },
            new PeopleBaseItemMap { PeopleId = sharedPerson.Id, People = sharedPerson, ItemId = itemB, Item = setup.BaseItems.Find(itemB)!, ListOrder = 4, SortOrder = 4, Role = "Cameo" });
        setup.SaveChanges();

        var repo = new global::Jellyfin.Server.Implementations.Item.PeopleRepository(
            BuildFactoryMock().Object, _itemTypeLookupMock.Object);

        var result = repo.GetPeople(new InternalPeopleQuery
        {
            ItemIds = new[] { itemA, itemB }
        });

        Assert.Equal(2, result.Items.Count);
        var byItem = result.Items.ToDictionary(p => p.ItemId, p => p.SortOrder);
        Assert.Equal(2, byItem[itemA]);
        Assert.Equal(4, byItem[itemB]);
    }
}
