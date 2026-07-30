#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixRankingService : ICustomNetflixRankingService
{
    private readonly ICustomNetflixRepository _repository;
    private readonly ICustomNetflixCacheService _cache;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly CustomNetflixCardDtoCache _cardDtoCache;

    public CustomNetflixRankingService(
        ICustomNetflixRepository repository,
        ICustomNetflixCacheService cache,
        IUserManager userManager,
        ILibraryManager libraryManager,
        CustomNetflixCardDtoCache cardDtoCache)
    {
        _repository = repository;
        _cache = cache;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _cardDtoCache = cardDtoCache;
    }

    public async Task<CustomNetflixRankedItemsResponseDto> GetTrendingAsync(Guid jellyfinUserId, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await GetSnapshotAsync(
                CustomNetflixRankingSnapshots.TrendingId,
                limit,
                _repository.GetTrendingItemsAsync,
                cancellationToken).ConfigureAwait(false);
            var response = await MapAsync(
                jellyfinUserId,
                CustomNetflixRankingSnapshots.TrendingId,
                "Trending",
                "customnetflix.ranking.trending",
                snapshot.Items,
                snapshot.GeneratedAt,
                cancellationToken).ConfigureAwait(false);
            CustomNetflixMetrics.ObserveRankingRequest("trending", "success");
            return response;
        }
        catch
        {
            CustomNetflixMetrics.ObserveRankingRequest("trending", "error");
            throw;
        }
    }

    public async Task<CustomNetflixRankedItemsResponseDto> GetTopTenAsync(Guid jellyfinUserId, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await GetSnapshotAsync(
                CustomNetflixRankingSnapshots.TopTenId,
                limit,
                _repository.GetTopTenItemsAsync,
                cancellationToken).ConfigureAwait(false);
            var response = await MapAsync(
                jellyfinUserId,
                CustomNetflixRankingSnapshots.TopTenId,
                "Top 10",
                "customnetflix.ranking.top_10",
                snapshot.Items,
                snapshot.GeneratedAt,
                cancellationToken).ConfigureAwait(false);
            CustomNetflixMetrics.ObserveRankingRequest("top10", "success");
            return response;
        }
        catch
        {
            CustomNetflixMetrics.ObserveRankingRequest("top10", "error");
            throw;
        }
    }

    private async Task<RankingSnapshotRow> GetSnapshotAsync(
        string rankingId,
        int limit,
        Func<int, CancellationToken, Task<IReadOnlyList<RankedItemRow>>> loadLiveItems,
        CancellationToken cancellationToken)
    {
        var normalizedLimit = CustomNetflixRankingSnapshots.NormalizeLimit(rankingId, limit);
        var utcNow = DateTime.UtcNow;
        var cached = await _cache.GetStringAsync(RedisKeyBuilder.RankingSnapshot(rankingId), cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedSnapshot = CustomNetflixRankingSnapshotSerializer.Deserialize(cached, normalizedLimit, utcNow);
            if (cachedSnapshot is not null)
            {
                return cachedSnapshot;
            }
        }

        var snapshot = await _repository.GetRankingSnapshotAsync(rankingId, normalizedLimit, utcNow, cancellationToken).ConfigureAwait(false);
        if (snapshot is not null)
        {
            await CacheSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
            return snapshot;
        }

        var generatedAt = DateTime.UtcNow;
        var liveItems = await loadLiveItems(normalizedLimit, cancellationToken).ConfigureAwait(false);
        snapshot = new RankingSnapshotRow(
            rankingId,
            liveItems,
            generatedAt,
            generatedAt.Add(CustomNetflixRankingSnapshots.SnapshotTtl));
        await _repository.SaveRankingSnapshotAsync(
            rankingId,
            snapshot.Items,
            snapshot.GeneratedAt,
            snapshot.ExpiresAt,
            cancellationToken).ConfigureAwait(false);
        await CacheSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return snapshot;
    }

    private async Task CacheSnapshotAsync(RankingSnapshotRow snapshot, CancellationToken cancellationToken)
    {
        var ttl = snapshot.ExpiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        await _cache.SetStringAsync(
            RedisKeyBuilder.RankingSnapshot(snapshot.RankingId),
            CustomNetflixRankingSnapshotSerializer.Serialize(snapshot),
            ttl,
            cancellationToken).ConfigureAwait(false);
    }

    private Task<CustomNetflixRankedItemsResponseDto> MapAsync(
        Guid jellyfinUserId,
        string id,
        string title,
        string titleKey,
        IReadOnlyList<RankedItemRow> rows,
        DateTime generatedAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var items = new List<CustomNetflixRankedItemDto>(rows.Count);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (user is not null)
        {
            var visibleItems = new List<(RankedItemRow Row, BaseItem Item)>(rows.Count);
            foreach (var row in rows)
            {
                var item = _libraryManager.GetItemById<BaseItem>(row.ItemId, user);
                if (item is null)
                {
                    continue;
                }

                visibleItems.Add((row, item));
            }

            var itemDtos = _cardDtoCache.GetBaseItemDtos(
                visibleItems.Select(entry => entry.Item).ToArray(),
                user);
            for (var index = 0; index < visibleItems.Count; index++)
            {
                var row = visibleItems[index].Row;
                items.Add(new CustomNetflixRankedItemDto
                {
                    Rank = row.Rank,
                    Score = Math.Round(row.Score, 3),
                    Item = itemDtos[index]
                });
            }
        }

        return Task.FromResult(new CustomNetflixRankedItemsResponseDto
        {
            Id = id,
            Title = title,
            TitleKey = titleKey,
            GeneratedAt = generatedAt,
            Items = items
        });
    }
}
