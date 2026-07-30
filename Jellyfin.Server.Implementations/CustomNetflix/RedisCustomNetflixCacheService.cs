#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class RedisCustomNetflixCacheService : ICustomNetflixCacheService, IDisposable
{
    private static readonly TimeSpan MaximumReconnectDelay = TimeSpan.FromSeconds(30);

    private readonly ILogger<RedisCustomNetflixCacheService> _logger;
    private readonly string? _redisConnectionString;
    private readonly Func<ConfigurationOptions, Task<IConnectionMultiplexer>> _connectionFactory;
    private readonly Func<DateTime> _utcNow;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private IConnectionMultiplexer? _connection;
    private DateTime _retryAfterUtc;
    private int _connectionFailureCount;

    public RedisCustomNetflixCacheService(
        IOptions<CustomNetflixOptions> options,
        ILogger<RedisCustomNetflixCacheService> logger)
        : this(
            options,
            logger,
            static async configuration => await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false),
            static () => DateTime.UtcNow)
    {
    }

    internal RedisCustomNetflixCacheService(
        IOptions<CustomNetflixOptions> options,
        ILogger<RedisCustomNetflixCacheService> logger,
        Func<ConfigurationOptions, Task<IConnectionMultiplexer>> connectionFactory,
        Func<DateTime> utcNow)
    {
        _logger = logger;
        _redisConnectionString = options.Value.RedisConnectionString;
        _connectionFactory = connectionFactory;
        _utcNow = utcNow;
    }

    public bool IsEnabled => true;

    public async Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (connection is null)
        {
            throw new CustomNetflixUnavailableException("CustomNetflix Redis is configured but unavailable.");
        }

        await connection.GetDatabase().PingAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var database = await GetDatabaseAsync(cancellationToken).ConfigureAwait(false);
            if (database is null)
            {
                CustomNetflixMetrics.ObserveRedisOperation("get", "disabled");
                return null;
            }

            var value = await database.StringGetAsync(key).ConfigureAwait(false);
            CustomNetflixMetrics.ObserveRedisOperation("get", value.HasValue ? "hit" : "miss");
            return value;
        }
        catch (Exception ex) when (IsRedisCacheFailure(ex))
        {
            ResetDisposedConnection(ex);
            CustomNetflixMetrics.ObserveRedisOperation("get", "failure");
            _logger.LogDebug(ex, "CustomNetflix Redis get failed for key {Key}; treating it as a cache miss.", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var database = await GetDatabaseAsync(cancellationToken).ConfigureAwait(false);
            if (database is null)
            {
                CustomNetflixMetrics.ObserveRedisOperation("set", "disabled");
                return;
            }

            await database.StringSetAsync(key, value, expiry).ConfigureAwait(false);
            CustomNetflixMetrics.ObserveRedisOperation("set", "success");
        }
        catch (Exception ex) when (IsRedisCacheFailure(ex))
        {
            ResetDisposedConnection(ex);
            CustomNetflixMetrics.ObserveRedisOperation("set", "failure");
            _logger.LogDebug(ex, "CustomNetflix Redis set failed for key {Key}; continuing without cache write.", key);
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
        => RemoveAsync([key], cancellationToken);

    public async Task RemoveAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (keys.Count == 0)
        {
            return;
        }

        try
        {
            var database = await GetDatabaseAsync(cancellationToken).ConfigureAwait(false);
            if (database is null)
            {
                CustomNetflixMetrics.ObserveRedisOperation("delete", "disabled");
                return;
            }

            var redisKeys = new RedisKey[keys.Count];
            for (var index = 0; index < keys.Count; index++)
            {
                redisKeys[index] = keys[index];
            }

            await database.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
            CustomNetflixMetrics.ObserveRedisOperation("delete", "success");
        }
        catch (Exception ex) when (IsRedisCacheFailure(ex))
        {
            ResetDisposedConnection(ex);
            CustomNetflixMetrics.ObserveRedisOperation("delete", "failure");
            _logger.LogDebug(ex, "CustomNetflix Redis delete failed for {Count} keys; continuing without cache invalidation.", keys.Count);
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _connection, null)?.Dispose();
        _connectionGate.Dispose();
    }

    private async Task<IDatabase?> GetDatabaseAsync(CancellationToken cancellationToken)
        => (await GetConnectionAsync(cancellationToken).ConfigureAwait(false))?.GetDatabase();

    private async Task<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = Volatile.Read(ref _connection);
        if (connection is not null)
        {
            return connection;
        }

        if (string.IsNullOrWhiteSpace(_redisConnectionString) || _utcNow() < _retryAfterUtc)
        {
            return null;
        }

        await _connectionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            connection = Volatile.Read(ref _connection);
            if (connection is not null || _utcNow() < _retryAfterUtc)
            {
                return connection;
            }

            var configuration = ConfigurationOptions.Parse(_redisConnectionString);
            configuration.AbortOnConnectFail = false;
            connection = await _connectionFactory(configuration).ConfigureAwait(false);
            Volatile.Write(ref _connection, connection);
            _connectionFailureCount = 0;
            _retryAfterUtc = DateTime.MinValue;
            CustomNetflixMetrics.ObserveRedisOperation("connect", "success");
            return connection;
        }
        catch (Exception ex) when (IsRedisConnectionFailure(ex))
        {
            _retryAfterUtc = _utcNow().Add(
                CustomNetflixRetryPolicy.GetDelay(++_connectionFailureCount, MaximumReconnectDelay));
            CustomNetflixMetrics.ObserveRedisOperation("connect", "failure");
            _logger.LogWarning(
                ex,
                "CustomNetflix Redis is unavailable; retrying after {RetryAfterUtc}.",
                _retryAfterUtc);
            return null;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private void ResetDisposedConnection(Exception exception)
    {
        if (exception is not ObjectDisposedException)
        {
            return;
        }

        var connection = Volatile.Read(ref _connection);
        if (connection is not null
            && ReferenceEquals(Interlocked.CompareExchange(ref _connection, null, connection), connection))
        {
            connection.Dispose();
            _retryAfterUtc = DateTime.MinValue;
        }
    }

    private static bool IsRedisConnectionFailure(Exception exception)
        => exception is RedisException or ArgumentException or FormatException;

    private static bool IsRedisCacheFailure(Exception exception)
        => exception is RedisException or ObjectDisposedException;
}
