using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.SmartCollections;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SmartCollections;

public sealed class SmartCollectionsDatabasePerformanceTests : IAsyncLifetime
{
    private const string PerfEnvVar = "RUN_SMARTCOLLECTIONS_DB_PERF_TESTS";
    private readonly ITestOutputHelper _output;
    private readonly string _dbPath;
    private JellyfinDbContext _context = null!;
    private SmartCollectionsManager _manager = null!;
    private Mock<IUserManager> _userManager = null!;
    private IMemoryCache _cache = null!;
    private Mock<ILibraryManager> _libraryManager = null!;
    private Mock<Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior> _efLockingMock = null!;
    private Guid _testUserId;

    public SmartCollectionsDatabasePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        // Use a temp SQLite database file for each test
        _dbPath = Path.Combine(Path.GetTempPath(), $"jellyfin-test-{Guid.NewGuid()}.db");
    }

    public async ValueTask InitializeAsync()
    {
        // Create DbContext pointing to temp SQLite database
        var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite($"Data Source={_dbPath};Cache=Shared");

        _efLockingMock = new Mock<Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior>();
        _efLockingMock
            .Setup(m => m.OnSaveChangesAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<Func<Task>>()))
            .Returns<JellyfinDbContext, Func<Task>>(async (ctx, inner) =>
            {
                if (inner != null)
                {
                    await inner();
                }
            });
        _efLockingMock
            .Setup(m => m.OnSaveChanges(It.IsAny<JellyfinDbContext>(), It.IsAny<Action>()))
            .Callback<JellyfinDbContext, Action>((ctx, inner) =>
            {
                inner?.Invoke();
            });

        _context = new JellyfinDbContext(
            optionsBuilder.Options,
            Mock.Of<ILogger<JellyfinDbContext>>(),
            Mock.Of<IJellyfinDatabaseProvider>(),
            _efLockingMock.Object);
        await _context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Create test user
        _testUserId = Guid.NewGuid();
        var user = new User("testuser", "hash", "salt") { Id = _testUserId };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Setup mocks
        _userManager = new Mock<IUserManager>();
        _userManager.Setup(u => u.GetUserById(_testUserId)).Returns(user);

        _cache = new MemoryCache(new MemoryCacheOptions());
        _libraryManager = new Mock<ILibraryManager>();

        // Setup real repositories
        var dbFactory = new DbContextFactory(_dbPath, _efLockingMock.Object);
        var itemRepository = new BaseItemRepository(
            dbFactory,
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IItemTypeLookup>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ILogger<BaseItemRepository>>());
        var smartCollectionsRepo = new SmartCollectionsRepository(dbFactory);

        // Create manager with real repositories
        _manager = new SmartCollectionsManager(
            smartCollectionsRepo,
            _userManager.Object,
            Mock.Of<ILogger<SmartCollectionsManager>>(),
            itemRepository,
            _cache);
    }

    public ValueTask DisposeAsync()
    {
        _context?.Dispose();
        _cache?.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Seeds the database with 100k items with realistic attributes for performance testing.
    /// </summary>
    private async Task Seed100kItems()
    {
        var genres = new[] { "Action", "Drama", "Comedy", "SciFi", "Thriller", "Horror", "Romance" };
        var tags = new[] { "4k", "classic", "new", "award", "family", "indie", "blockbuster" };
        var ratings = new[] { "G", "PG", "PG-13", "R", "NC-17" };

        // Create canonical ItemValue rows for genres and tags
        var genreValues = new Dictionary<string, ItemValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in genres)
        {
            var iv = new ItemValue
            {
                ItemValueId = Guid.NewGuid(),
                Type = ItemValueType.Genre,
                Value = g,
                CleanValue = g.ToLowerInvariant()
            };
            genreValues[g] = iv;
        }

        var tagValues = new Dictionary<string, ItemValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tags)
        {
            var iv = new ItemValue
            {
                ItemValueId = Guid.NewGuid(),
                Type = ItemValueType.Tags,
                Value = t,
                CleanValue = t.ToLowerInvariant()
            };
            tagValues[t] = iv;
        }

        // Persist ItemValue lookups
        _context.ItemValues.AddRange(genreValues.Values);
        _context.ItemValues.AddRange(tagValues.Values);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        const int batchSize = 1000;
        var itemsBatch = new List<BaseItemEntity>(batchSize);
        var mapsBatch = new List<ItemValueMap>(batchSize * 2);

        for (var i = 0; i < 100000; i++)
        {
            var chosenGenre = genres[RandomNumberGenerator.GetInt32(0, genres.Length)];
            var chosenTag = tags[RandomNumberGenerator.GetInt32(0, tags.Length)];

            // secure pseudo-random doubles in [0,1)
            var r1 = RandomNumberGenerator.GetInt32(0, 1000000) / 1000000.0;
            var r2 = RandomNumberGenerator.GetInt32(0, 1000000) / 1000000.0;
            var r3 = RandomNumberGenerator.GetInt32(0, 1000000) / 1000000.0;

            var item = new BaseItemEntity
            {
                Id = Guid.NewGuid(),
                Name = $"Item_{i}",
                Type = typeof(Movie).FullName!,
                SortName = $"Item_{i}",
                CleanName = $"item_{i}",
                PresentationUniqueKey = Guid.NewGuid().ToString("N"),
                InheritedParentalRatingValue = null,
                IsMovie = true,
                IsVirtualItem = false,
                IsFolder = false,
                ProductionYear = 1980 + RandomNumberGenerator.GetInt32(0, 45),
                // bias ~30% of items to be >= 7 to make MinCommunityRating likely to match some items
                CommunityRating = (float)(r1 < 0.3 ? (7.0 + (r2 * 3.0)) : (r3 * 7.0)),
                CriticRating = (float)(RandomNumberGenerator.GetInt32(0, 1000000) / 1000000.0 * 10.0),
                OfficialRating = ratings[RandomNumberGenerator.GetInt32(0, ratings.Length)],
                // keep string fields for informational/debugging; repository uses ItemValueMap for real filtering
                Genres = chosenGenre,
                Tags = chosenTag,
                DateCreated = DateTime.UtcNow
            };

            itemsBatch.Add(item);

            // Map genre
            mapsBatch.Add(new ItemValueMap
            {
                ItemId = item.Id,
                Item = item,
                ItemValueId = genreValues[chosenGenre].ItemValueId,
                ItemValue = genreValues[chosenGenre]
            });

            // Map tag
            mapsBatch.Add(new ItemValueMap
            {
                ItemId = item.Id,
                Item = item,
                ItemValueId = tagValues[chosenTag].ItemValueId,
                ItemValue = tagValues[chosenTag]
            });

            if (itemsBatch.Count >= batchSize)
            {
                _context.BaseItems.AddRange(itemsBatch);
                _context.ItemValuesMap.AddRange(mapsBatch);
                await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
                itemsBatch.Clear();
                mapsBatch.Clear();
            }
        }

        if (itemsBatch.Count > 0)
        {
            _context.BaseItems.AddRange(itemsBatch);
            _context.ItemValuesMap.AddRange(mapsBatch);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task EvaluateAsync_Real_100k_Items_Performance()
    {
        Assert.SkipUnless(
            string.Equals(Environment.GetEnvironmentVariable(PerfEnvVar), "1", StringComparison.Ordinal),
            $"Set {PerfEnvVar}=1 to run database performance tests.");

        await Seed100kItems();

        var totalSeeded = await _context.BaseItems.CountAsync(TestContext.Current.CancellationToken);
        Assert.True(totalSeeded >= 100000, $"Expected at least 100000 seeded items but found {totalSeeded}");

        var filters = new SmartCollectionFilters { MinCommunityRating = 7.0f };
        filters.Genres.Add("Action");

        var sw = Stopwatch.StartNew();
        var result = await _manager.EvaluateAsync(filters, _testUserId.ToString(), 1000);
        sw.Stop();

        Assert.True(
            await _context.BaseItems.AnyAsync(
                x => x.Type == typeof(Movie).FullName!,
                TestContext.Current.CancellationToken));
        _output?.WriteLine($"Evaluated real 100k collection in {sw.Elapsed.TotalMilliseconds:F2}ms with {result.Count()} results");
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task EvaluateAsync_Real_Cache_Performance()
    {
        Assert.SkipUnless(
            string.Equals(Environment.GetEnvironmentVariable(PerfEnvVar), "1", StringComparison.Ordinal),
            $"Set {PerfEnvVar}=1 to run database performance tests.");

        await Seed100kItems();

        var userId = _testUserId;
        var filters = new SmartCollectionFilters { MinCommunityRating = 7.0f };

        // Warm cache
        await _manager.EvaluateAsync(filters, userId.ToString(), 1000);

        // Measure cache hit performance
        var timings = new List<double>();
        for (var i = 0; i < 100; i++)
        {
            var sw = Stopwatch.StartNew();
            await _manager.EvaluateAsync(filters, userId.ToString(), 1000);
            sw.Stop();
            timings.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avg = timings.Average();
        Assert.True(avg < 2.0, $"Cache hit avg too slow: {avg:F3}ms");
    }

    // Helper factory for DbContext (nested to keep single top-level type)
    private sealed class DbContextFactory : IDbContextFactory<JellyfinDbContext>
    {
        private readonly string _dbPath;

        private readonly Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior _lockingBehavior;

        public DbContextFactory(string dbPath, Jellyfin.Database.Implementations.Locking.IEntityFrameworkCoreLockingBehavior lockingBehavior)
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
