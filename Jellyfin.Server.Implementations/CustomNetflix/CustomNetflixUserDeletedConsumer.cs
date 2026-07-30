#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixUserDeletedConsumer : IEventConsumer<UserDeletedEventArgs>
{
    private readonly ICustomNetflixRepository _repository;
    private readonly ICustomNetflixCacheService _cache;
    private readonly ILogger<CustomNetflixUserDeletedConsumer> _logger;

    public CustomNetflixUserDeletedConsumer(
        ICustomNetflixRepository repository,
        ICustomNetflixCacheService cache,
        ILogger<CustomNetflixUserDeletedConsumer> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task OnEvent(UserDeletedEventArgs eventArgs)
    {
        if (!_repository.IsEnabled)
        {
            return;
        }

        var userId = eventArgs.Argument.Id;
        var keys = await _repository.PurgeUserDataAsync(userId, default).ConfigureAwait(false);
        var cacheKeys = new List<string>(
            (keys.ProfileIds.Count * CustomNetflixHomeSnapshots.MaxLimit)
            + keys.ActiveProfileTokenHashes.Count
            + 2);
        cacheKeys.AddRange(keys.ProfileIds.SelectMany(CustomNetflixHomeSnapshots.CacheKeys));
        cacheKeys.AddRange(keys.ActiveProfileTokenHashes.Select(tokenHash =>
            RedisKeyBuilder.ActiveProfile(userId, tokenHash)));
        cacheKeys.Add(RedisKeyBuilder.RankingSnapshot(CustomNetflixRankingSnapshots.TrendingId));
        cacheKeys.Add(RedisKeyBuilder.RankingSnapshot(CustomNetflixRankingSnapshots.TopTenId));
        await _cache.RemoveAsync(cacheKeys.Distinct().ToArray(), default).ConfigureAwait(false);

        _logger.LogInformation(
            "Purged CustomNetflix data for deleted Jellyfin user {UserId} ({ProfileCount} profiles).",
            userId,
            keys.ProfileIds.Count);
    }
}
