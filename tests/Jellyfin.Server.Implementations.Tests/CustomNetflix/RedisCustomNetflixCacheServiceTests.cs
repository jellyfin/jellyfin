using System;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.CustomNetflix;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class RedisCustomNetflixCacheServiceTests
{
    [Fact]
    public async Task GetStringAsync_RetriesConnectionAfterInitialFailure()
    {
        var utcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var attempts = 0;
        var database = new Mock<IDatabase>();
        database
            .Setup(mock => mock.StringGetAsync((RedisKey)"key", CommandFlags.None))
            .ReturnsAsync((RedisValue)"cached");
        var connection = new Mock<IConnectionMultiplexer>();
        connection
            .Setup(mock => mock.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(database.Object);
        using var cache = new RedisCustomNetflixCacheService(
            Options.Create(new CustomNetflixOptions { RedisConnectionString = "localhost:6379" }),
            NullLogger<RedisCustomNetflixCacheService>.Instance,
            _ => ++attempts == 1
                ? Task.FromException<IConnectionMultiplexer>(new ArgumentException("Redis unavailable."))
                : Task.FromResult(connection.Object),
            () => utcNow);

        var first = await cache.GetStringAsync("key", TestContext.Current.CancellationToken);
        utcNow = utcNow.AddSeconds(1);
        var recovered = await cache.GetStringAsync("key", TestContext.Current.CancellationToken);

        Assert.Null(first);
        Assert.Equal("cached", recovered);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task RedisCache_RoundTripsWhenIntegrationConnectionIsConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("JELLYFIN_TEST_REDIS_CONNECTION_STRING");
        Assert.SkipUnless(
            !string.IsNullOrWhiteSpace(connectionString),
            "Set JELLYFIN_TEST_REDIS_CONNECTION_STRING to run this integration test.");
        using var cache = new RedisCustomNetflixCacheService(
            Options.Create(new CustomNetflixOptions { RedisConnectionString = connectionString }),
            NullLogger<RedisCustomNetflixCacheService>.Instance);
        var key = $"jellyfin:test:customnetflix:{Guid.NewGuid():N}";

        await cache.CheckHealthAsync(TestContext.Current.CancellationToken);
        await cache.SetStringAsync(key, "value", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        Assert.Equal("value", await cache.GetStringAsync(key, TestContext.Current.CancellationToken));
        await cache.RemoveAsync(key, TestContext.Current.CancellationToken);
        Assert.Null(await cache.GetStringAsync(key, TestContext.Current.CancellationToken));
    }
}
