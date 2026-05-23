using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.SmartCollections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Entities = Jellyfin.Database.Implementations.Entities;
using User = Jellyfin.Database.Implementations.Entities.User;

namespace Jellyfin.Server.Implementations.Tests.SmartCollections;

public sealed class SmartCollectionsPerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ISmartCollectionsRepository> _smartCollectionsRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IUserManager> _userManager = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly SmartCollectionsManager _manager;

    public SmartCollectionsPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _manager = new SmartCollectionsManager(
            _smartCollectionsRepo.Object,
            _userManager.Object,
            Mock.Of<ILogger<SmartCollectionsManager>>(),
            _itemRepo.Object,
            _cache);
    }

    // Helpers to generate random actual data and apply in cache and on the go.
    private static List<ItemRow> BuildRows(int count)
    {
        var rnd = new Random(42);
        var genres = new[] { "Action", "Drama", "Comedy", "SciFi", "Thriller" };
        var tags = new[] { "4k", "classic", "new", "award", "family" };
        var ratings = new[] { "G", "PG", "PG-13", "R", "NC-17" };

        var rows = new List<ItemRow>(count);
        for (var i = 0; i < count; i++)
        {
            rows.Add(new ItemRow(
                Guid.NewGuid(),
                1980 + rnd.Next(45),
                rnd.NextDouble() * 10.0,
                rnd.NextDouble() * 10.0,
                new[] { genres[rnd.Next(genres.Length)] },
                new[] { tags[rnd.Next(tags.Length)] },
                ratings[rnd.Next(ratings.Length)]));
        }

        return rows;
    }

    private static IReadOnlyList<Guid> ApplyQuery(InternalItemsQuery q, IReadOnlyList<ItemRow> rows)
    {
        IEnumerable<ItemRow> query = rows;

        if (q.Years.Length > 0)
        {
            var yearSet = q.Years.ToHashSet();
            query = query.Where(r => yearSet.Contains(r.Year));
        }

        if (q.MinCommunityRating.HasValue)
        {
            query = query.Where(r => r.CommunityRating >= q.MinCommunityRating.Value);
        }

        if (q.MinCriticRating.HasValue)
        {
            query = query.Where(r => r.CriticRating >= q.MinCriticRating.Value);
        }

        if (q.Genres.Count > 0)
        {
            var genreSet = q.Genres.Select(g => g.ToLowerInvariant()).ToHashSet();
            query = query.Where(r => r.Genres.Any(g => genreSet.Contains(g.ToLowerInvariant())));
        }

        if (q.Tags.Length > 0)
        {
            var tagSet = q.Tags.Select(t => t.ToLowerInvariant()).ToHashSet();
            query = query.Where(r => r.Tags.Any(t => tagSet.Contains(t.ToLowerInvariant())));
        }

        if (q.OfficialRatings.Length > 0)
        {
            var ratingSet = q.OfficialRatings.ToHashSet(StringComparer.OrdinalIgnoreCase);
            query = query.Where(r => ratingSet.Contains(r.OfficialRating));
        }

        if (q.Limit.HasValue)
        {
            query = query.Take(q.Limit.Value);
        }

        return query.Select(r => r.Id).ToArray();
    }

    [Fact]
    public async Task EvaluateAsync_CacheMiss_PerformanceBudget()
    {
        var userId = Guid.NewGuid();
        var user = new User("perfuser", "hash", "salt") { Id = userId };
        var filters = new Entities.SmartCollectionFilters { MinCommunityRating = 7 };
        var ids = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToArray();

        _userManager.Setup(x => x.GetUserById(userId)).Returns(user);
        _itemRepo.Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>())).Returns(ids);

        // warmup
        await _manager.EvaluateAsync(filters, userId.ToString(), 50);

        // force cache miss by changing limit each call
        var timings = new List<double>();
        for (var i = 0; i < 50; i++)
        {
            var sw = Stopwatch.StartNew();
            await _manager.EvaluateAsync(filters, userId.ToString(), 51 + i);
            sw.Stop();
            timings.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avg = timings.Average();
        var p95 = timings.OrderBy(x => x).ElementAt((int)(timings.Count * 0.95) - 1);

        _output?.WriteLine($"Cache miss avg={avg:F2}ms p95={p95:F2}ms");
        Assert.True(avg < 30, $"Average too slow: {avg:F2}ms");
        Assert.True(p95 < 60, $"P95 too slow: {p95:F2}ms");
    }

    [Fact]
    public async Task EvaluateAsync_CacheHit_PerformanceBudget()
    {
        var userId = Guid.NewGuid();
        var user = new User("perfuser2", "hash", "salt") { Id = userId };
        var filters = new Entities.SmartCollectionFilters();
        var ids = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToArray();

        _userManager.Setup(x => x.GetUserById(userId)).Returns(user);
        _itemRepo.Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>())).Returns(ids);

        await _manager.EvaluateAsync(filters, userId.ToString(), 50);

        var timings = new List<double>();
        for (var i = 0; i < 500; i++)
        {
            var sw = Stopwatch.StartNew();
            await _manager.EvaluateAsync(filters, userId.ToString(), 50);
            sw.Stop();
            timings.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avg = timings.Average();
        var p95 = timings.OrderBy(x => x).ElementAt((int)(timings.Count * 0.95) - 1);

        _output?.WriteLine($"Cache hit avg={avg:F3}ms p95={p95:F3}ms");
        Assert.True(avg < 2.0, $"Cache-hit average too slow: {avg:F3}ms");
        Assert.True(p95 < 5.0, $"Cache-hit p95 too slow: {p95:F3}ms");
    }

    [Fact]
    public async Task EvaluateAsync_Abnormaly_Large_SmartCollection()
    {
        var userId = Guid.NewGuid();
        var user = new User("perfuser3", "hash", "salt") { Id = userId };
        var filters = new Entities.SmartCollectionFilters { MinCommunityRating = 7 };
        var ids = Enumerable.Range(0, 10000).Select(_ => Guid.NewGuid()).ToArray();

        _userManager.Setup(x => x.GetUserById(userId)).Returns(user);
        _itemRepo.Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>())).Returns(ids);

        var sw = Stopwatch.StartNew();
        var result = await _manager.EvaluateAsync(filters, userId.ToString(), 10000);
        sw.Stop();

        _output?.WriteLine($"Evaluated large collection in {sw.Elapsed.TotalMilliseconds:F2}ms");
        Assert.True(sw.Elapsed.TotalMilliseconds < 200, $"Evaluation too slow: {sw.Elapsed.TotalMilliseconds:F2}ms");
        Assert.Equal(10000, result.Count());
    }

    [Fact]
    public async Task EvaluateAsync_Abnormaly_Large_Mock_SmartCollection()
    {
        var userId = Guid.NewGuid();
        var user = new User("perfuser4", "hash", "salt") { Id = userId };
        var filters = new Entities.SmartCollectionFilters();
        var rows = BuildRows(100000);
        var ids = rows.Select(r => r.Id).ToArray();

        _userManager.Setup(x => x.GetUserById(userId)).Returns(user);
        _itemRepo.Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>())).Returns((InternalItemsQuery q) => ApplyQuery(q, rows));

        var sw = Stopwatch.StartNew();
        var result = await _manager.EvaluateAsync(filters, userId.ToString(), 100000);
        sw.Stop();

        _output?.WriteLine($"Evaluated large mock collection in {sw.Elapsed.TotalMilliseconds:F2}ms");
        Assert.True(sw.Elapsed.TotalMilliseconds < 200, $"Evaluation too slow: {sw.Elapsed.TotalMilliseconds:F2}ms");
        Assert.Equal(100000, result.Count());
    }

    public void Dispose() => _cache.Dispose();

    private sealed record ItemRow(
    Guid Id,
    int Year,
    double CommunityRating,
    double CriticRating,
    string[] Genres,
    string[] Tags,
    string OfficialRating);
}
