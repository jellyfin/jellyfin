using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.SmartCollections;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SmartCollectionFilters = Jellyfin.Database.Implementations.Entities.SmartCollectionFilters;
using User = Jellyfin.Database.Implementations.Entities.User;

namespace Jellyfin.Server.Implementations.Tests.SmartCollections;

public sealed class SmartCollectionsEvaluationIntegrationTests : IAsyncLifetime
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Dictionary<string, Guid> _itemIds = new(StringComparer.Ordinal);
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"jellyfin-smartcollection-eval-{Guid.NewGuid()}.db");
    private Mock<Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior> _lockingBehavior = null!;
    private JellyfinDbContext _context = null!;
    private IMemoryCache _cache = null!;
    private SmartCollectionsManager _manager = null!;

    public async ValueTask InitializeAsync()
    {
        _lockingBehavior = new Mock<Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior>();
        _lockingBehavior
            .Setup(x => x.OnSaveChangesAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<Func<Task>>()))
            .Returns<JellyfinDbContext, Func<Task>>(async (_, saveChanges) => await saveChanges());
        _lockingBehavior
            .Setup(x => x.OnSaveChanges(It.IsAny<JellyfinDbContext>(), It.IsAny<Action>()))
            .Callback<JellyfinDbContext, Action>((_, saveChanges) => saveChanges());

        var dbFactory = new TestDbContextFactory(_dbPath, _lockingBehavior.Object);
        _context = dbFactory.CreateDbContext();
        await _context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await SeedItems();

        var user = new User("smartcollectionuser", "hash", "salt") { Id = _userId };
        var userManager = new Mock<IUserManager>();
        userManager.Setup(x => x.GetUserById(_userId)).Returns(user);

        var itemRepository = new BaseItemRepository(
            dbFactory,
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IItemTypeLookup>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ILogger<BaseItemRepository>>());

        _cache = new MemoryCache(new MemoryCacheOptions());
        _manager = new SmartCollectionsManager(
            Mock.Of<ISmartCollectionsRepository>(),
            userManager.Object,
            Mock.Of<ILogger<SmartCollectionsManager>>(),
            itemRepository,
            _cache);
    }

    public ValueTask DisposeAsync()
    {
        _context.Dispose();
        _cache.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task EvaluateAsync_WithRatingFilter_ReturnsMatchingItems()
    {
        // Arrange
        var filters = new SmartCollectionFilters { MinCommunityRating = 8 };

        // Act
        var result = await Evaluate(filters);

        // Assert
        AssertIdsEqual(
            [_itemIds["Romance 2018"], _itemIds["Action 2020"], _itemIds["Romance Drama 2021"]],
            result);
    }

    [Fact]
    public async Task EvaluateAsync_WithGenreFilter_ReturnsMatchingItems()
    {
        // Arrange
        var filters = new SmartCollectionFilters();
        filters.Genres.Add("Romance");

        // Act
        var result = await Evaluate(filters);

        // Assert
        AssertIdsEqual(
            [_itemIds["Romance 2018"], _itemIds["Romance 2019"], _itemIds["Romance Drama 2021"]],
            result);
    }

    [Fact]
    public async Task EvaluateAsync_WithYearFilter_ReturnsMatchingItems()
    {
        // Arrange
        var filters = new SmartCollectionFilters
        {
            YearFrom = 2018,
            YearTo = 2020
        };

        // Act
        var result = await Evaluate(filters);

        // Assert
        AssertIdsEqual(
            [_itemIds["Romance 2018"], _itemIds["Romance 2019"], _itemIds["Action 2020"]],
            result);
    }

    [Fact]
    public async Task EvaluateAsync_WithCombinedFilters_ReturnsIntersection()
    {
        // Arrange
        var filters = new SmartCollectionFilters
        {
            MinCommunityRating = 8,
            YearFrom = 2020,
            YearTo = 2021
        };
        filters.Genres.Add("Romance");

        // Act
        var result = await Evaluate(filters);

        // Assert
        AssertIdsEqual([_itemIds["Romance Drama 2021"]], result);
    }

    [Fact]
    public async Task EvaluateAsync_WithRealRepository_AppliesLimit()
    {
        // Act
        var result = await Evaluate(new SmartCollectionFilters(), 2);

        // Assert
        Assert.Equal(2, result.Count);
    }

    private async Task<IReadOnlyList<Guid>> Evaluate(SmartCollectionFilters filters, int limit = 50)
    {
        var result = await _manager.EvaluateAsync(filters, _userId.ToString(), limit);
        return result.ToArray();
    }

    private async Task SeedItems()
    {
        var genreValues = new Dictionary<string, ItemValue>(StringComparer.OrdinalIgnoreCase);
        var itemValueMaps = new List<ItemValueMap>();
        var items = new[]
        {
            CreateMovie("Romance 2018", 2018, 8.1f, ["Romance"]),
            CreateMovie("Romance 2019", 2019, 5.0f, ["Romance"]),
            CreateMovie("Action 2020", 2020, 9.0f, ["Action"]),
            CreateMovie("Drama 2017", 2017, 7.2f, ["Drama"]),
            CreateMovie("Romance Drama 2021", 2021, 8.5f, ["Romance", "Drama"])
        };

        foreach (var item in items)
        {
            _itemIds[item.Name!] = item.Id;

            foreach (var genre in item.Genres!.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!genreValues.TryGetValue(genre, out var itemValue))
                {
                    itemValue = new ItemValue
                    {
                        ItemValueId = Guid.NewGuid(),
                        Type = ItemValueType.Genre,
                        Value = genre,
                        CleanValue = genre.ToLowerInvariant()
                    };
                    genreValues[genre] = itemValue;
                }

                itemValueMaps.Add(new ItemValueMap
                {
                    ItemId = item.Id,
                    Item = item,
                    ItemValueId = itemValue.ItemValueId,
                    ItemValue = itemValue
                });
            }
        }

        _context.ItemValues.AddRange(genreValues.Values);
        _context.BaseItems.AddRange(items);
        _context.ItemValuesMap.AddRange(itemValueMaps);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static BaseItemEntity CreateMovie(string name, int productionYear, float communityRating, string[] genres)
    {
        return new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = typeof(Movie).FullName!,
            SortName = name,
            CleanName = name.ToLowerInvariant(),
            PresentationUniqueKey = Guid.NewGuid().ToString("N"),
            IsMovie = true,
            IsVirtualItem = false,
            IsFolder = false,
            ProductionYear = productionYear,
            CommunityRating = communityRating,
            Genres = string.Join('|', genres),
            DateCreated = DateTime.UtcNow
        };
    }

    private static void AssertIdsEqual(IReadOnlyCollection<Guid> expected, IReadOnlyCollection<Guid> actual)
    {
        Assert.Equal(expected.Order().ToArray(), actual.Order().ToArray());
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
