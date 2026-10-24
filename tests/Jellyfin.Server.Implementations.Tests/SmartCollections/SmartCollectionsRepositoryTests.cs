using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.SmartCollections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Entities = Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Server.Implementations.Tests.SmartCollections;

public sealed class SmartCollectionsRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"jellyfin-smartcollections-{Guid.NewGuid()}.db");
    private Mock<Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior> _lockingBehavior = null!;
    private JellyfinDbContext _context = null!;
    private SmartCollectionsRepository _repository = null!;

    public async ValueTask InitializeAsync()
    {
        _lockingBehavior = new Mock<Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior>();
        _lockingBehavior
            .Setup(x => x.OnSaveChangesAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<Func<Task>>()))
            .Returns<JellyfinDbContext, Func<Task>>(async (_, saveChanges) => await saveChanges());
        _lockingBehavior
            .Setup(x => x.OnSaveChanges(It.IsAny<JellyfinDbContext>(), It.IsAny<Action>()))
            .Callback<JellyfinDbContext, Action>((_, saveChanges) => saveChanges());

        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite($"Data Source={_dbPath};Cache=Shared")
            .Options;

        _context = new JellyfinDbContext(
            options,
            Mock.Of<ILogger<JellyfinDbContext>>(),
            Mock.Of<IJellyfinDatabaseProvider>(),
            _lockingBehavior.Object);

        await _context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        _repository = new SmartCollectionsRepository(new TestDbContextFactory(_dbPath, _lockingBehavior.Object));
    }

    public ValueTask DisposeAsync()
    {
        _context.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task CreateSmartCollectionAsync_PersistsCollectionAndCanReadById()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new Entities.SmartCollectionFilters { MinCommunityRating = 7 };
        filters.Genres.Add("Drama");
        var collection = new Entities.SmartCollections("Drama Movies", userId, filters)
        {
            Limit = 25,
            SortBy = "SortName",
            SortOrder = SortOrder.Ascending
        };

        // Act
        var created = await _repository.CreateSmartCollectionAsync(collection);
        var result = await _repository.GetSmartCollectionByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Drama Movies", result.Name);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(25, result.Limit);
        Assert.Equal("SortName", result.SortBy);
        Assert.Equal(SortOrder.Ascending, result.SortOrder);
    }

    [Fact]
    public async Task GetSmartCollectionsForUserAsync_ReturnsOnlyUserCollectionsOrderedByName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await _repository.CreateSmartCollectionAsync(new Entities.SmartCollections("Beta", userId, new Entities.SmartCollectionFilters()));
        await _repository.CreateSmartCollectionAsync(new Entities.SmartCollections("Alpha", userId, new Entities.SmartCollectionFilters()));
        await _repository.CreateSmartCollectionAsync(new Entities.SmartCollections("Other", otherUserId, new Entities.SmartCollectionFilters()));

        // Act
        var result = await _repository.GetSmartCollectionsForUserAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "Alpha", "Beta" }, result.Select(collection => collection.Name));
        Assert.All(result, collection => Assert.Equal(userId, collection.UserId));
    }

    [Fact]
    public async Task UpdateSmartCollectionAsync_PersistsUpdatedValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var created = await _repository.CreateSmartCollectionAsync(
            new Entities.SmartCollections("Old Name", userId, new Entities.SmartCollectionFilters())
            {
                Limit = 25,
                SortBy = "SortName",
                SortOrder = SortOrder.Ascending
            });

        var updatedFilters = new Entities.SmartCollectionFilters { MinCriticRating = 8 };
        updatedFilters.Tags.Add("Favorite");
        created.Name = "New Name";
        created.Limit = 100;
        created.SortBy = "ProductionYear";
        created.SortOrder = SortOrder.Descending;
        created.SetFilters(updatedFilters);

        // Act
        await _repository.UpdateSmartCollectionAsync(created);
        var result = await _repository.GetSmartCollectionByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(100, result.Limit);
        Assert.Equal("ProductionYear", result.SortBy);
        Assert.Equal(SortOrder.Descending, result.SortOrder);
        Assert.Equal(8, result.GetFilters().MinCriticRating);
        Assert.Equal(new[] { "Favorite" }, result.GetFilters().Tags);
    }

    [Fact]
    public async Task DeleteSmartCollectionAsync_RemovesCollection()
    {
        // Arrange
        var created = await _repository.CreateSmartCollectionAsync(
            new Entities.SmartCollections("To Delete", Guid.NewGuid(), new Entities.SmartCollectionFilters()));

        // Act
        await _repository.DeleteSmartCollectionAsync(created.Id);
        var result = await _repository.GetSmartCollectionByIdAsync(created.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteSmartCollectionAsync_MissingCollection_DoesNotThrow()
    {
        await _repository.DeleteSmartCollectionAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task FiltersJson_RoundTripsThroughRepository()
    {
        // Arrange
        var filters = new Entities.SmartCollectionFilters
        {
            Type = "Movie",
            YearFrom = 2015,
            YearTo = 2020,
            MinCommunityRating = 7.5f,
            MinCriticRating = 8.5f
        };
        filters.Genres.Add("Romance");
        filters.Genres.Add("Drama");
        filters.Tags.Add("Award");
        filters.OfficialRatings.Add("PG-13");

        var collection = new Entities.SmartCollections("Round Trip", Guid.NewGuid(), filters);

        // Act
        var created = await _repository.CreateSmartCollectionAsync(collection);
        var result = await _repository.GetSmartCollectionByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.FiltersJson));

        var roundTrippedFilters = result.GetFilters();
        Assert.Equal("Movie", roundTrippedFilters.Type);
        Assert.Equal(new[] { "Romance", "Drama" }, roundTrippedFilters.Genres);
        Assert.Equal(new[] { "Award" }, roundTrippedFilters.Tags);
        Assert.Equal(new[] { "PG-13" }, roundTrippedFilters.OfficialRatings);
        Assert.Equal(2015, roundTrippedFilters.YearFrom);
        Assert.Equal(2020, roundTrippedFilters.YearTo);
        Assert.Equal(7.5f, roundTrippedFilters.MinCommunityRating);
        Assert.Equal(8.5f, roundTrippedFilters.MinCriticRating);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<JellyfinDbContext>
    {
        private readonly string _dbPath;
        private readonly Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior _lockingBehavior;

        public TestDbContextFactory(
            string dbPath,
            Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior lockingBehavior)
        {
            _dbPath = dbPath;
            _lockingBehavior = lockingBehavior;
        }

        public JellyfinDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<JellyfinDbContext>()
                .UseSqlite($"Data Source={_dbPath};Cache=Shared")
                .Options;

            return new JellyfinDbContext(
                options,
                Mock.Of<ILogger<JellyfinDbContext>>(),
                Mock.Of<IJellyfinDatabaseProvider>(),
                _lockingBehavior);
        }
    }
}
