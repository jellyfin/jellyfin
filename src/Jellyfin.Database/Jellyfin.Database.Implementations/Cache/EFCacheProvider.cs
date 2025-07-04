using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BitFaster.Caching;
using BitFaster.Caching.Lru;
using EFCoreSecondLevelCacheInterceptor;

namespace Jellyfin.Database.Implementations.Cache;

/// <summary>
/// Custom cache provider.
/// </summary>
public sealed class EFCacheProvider : IEFCacheServiceProvider, IDisposable
{
    private readonly IEFDebugLogger _efDebugLogger;
    private readonly ICache<string, EFCachedData> _cache;
    private readonly ConcurrentDictionary<string, ISet<string>> _dependantCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFCacheProvider"/> class.
    /// </summary>
    /// <param name="efDebugLogger">The EF debug logger.</param>
    /// <param name="cacheSize">The cache size.</param>
    public EFCacheProvider(
        IEFDebugLogger efDebugLogger,
        int cacheSize)
    {
        _efDebugLogger = efDebugLogger;
        _cache = new ConcurrentLruBuilder<string, EFCachedData>()
            .WithCapacity(cacheSize)
            .WithExpireAfterAccess(TimeSpan.FromMinutes(5))
            .WithMetrics()
            .Build();

        _cache.Events.Value!.ItemRemoved += CleanupDependencies;

        _dependantCache = new ConcurrentDictionary<string, ISet<string>>(StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public void ClearAllCachedEntries()
    {
        _cache.Clear();
        _efDebugLogger.NotifyCacheInvalidation(clearAllCachedEntries: true, new HashSet<string>(StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public EFCachedData? GetValue(EFCacheKey cacheKey, EFCachePolicy cachePolicy)
    {
        _cache.TryGet(cacheKey.KeyHash, out var value);
        return value;
    }

    /// <inheritdoc />
    public void InsertValue(EFCacheKey cacheKey, EFCachedData? value, EFCachePolicy cachePolicy)
    {
        ArgumentNullException.ThrowIfNull(cacheKey);
        value ??= new EFCachedData { IsNull = true };

        _dependantCache[cacheKey.KeyHash] = cacheKey.CacheDependencies;
        _cache.AddOrUpdate(cacheKey.KeyHash, value);
    }

    /// <inheritdoc />
    public void InvalidateCacheDependencies(EFCacheKey cacheKey)
    {
        ArgumentNullException.ThrowIfNull(cacheKey);

        _dependantCache.TryRemove(cacheKey.KeyHash, out _);
        foreach (var rootCacheKey in cacheKey.CacheDependencies)
        {
            _cache.TryRemove(rootCacheKey, out _);
        }
    }

    private void CleanupDependencies(object? sender, ItemRemovedEventArgs<string, EFCachedData> e)
    {
        if (_dependantCache.TryGetValue(e.Key, out var dependantKeys))
        {
            foreach (var key in dependantKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cache.Events.Value!.ItemRemoved -= CleanupDependencies;
    }
}
