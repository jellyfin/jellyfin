#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixRankingSnapshotWorker : BackgroundService
{
    private const int WatchEventPurgeBatchSize = 1000;
    private const int WatchEventPurgeMaxBatchesPerRun = 10;
    private static readonly TimeSpan WatchEventRetention = TimeSpan.FromDays(31);

    private readonly ICustomNetflixRepository _repository;
    private readonly ICustomNetflixCacheService _cache;
    private readonly CustomNetflixSchemaState _schemaState;
    private readonly ILogger<CustomNetflixRankingSnapshotWorker> _logger;

    public CustomNetflixRankingSnapshotWorker(
        ICustomNetflixRepository repository,
        ICustomNetflixCacheService cache,
        CustomNetflixSchemaState schemaState,
        ILogger<CustomNetflixRankingSnapshotWorker> logger)
    {
        _repository = repository;
        _cache = cache;
        _schemaState = schemaState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_repository.IsEnabled)
            {
                return;
            }

            await _schemaState.WaitUntilReadyAsync(stoppingToken).ConfigureAwait(false);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PurgeExpiredWatchEventsAsync(stoppingToken).ConfigureAwait(false);
                    await RefreshAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to maintain CustomNetflix ranking snapshots and watch-event retention.");
                }

                await Task.Delay(CustomNetflixRankingSnapshots.RefreshInterval, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await RefreshRankingAsync(
            CustomNetflixRankingSnapshots.TrendingId,
            await _repository.GetTrendingItemsAsync(CustomNetflixRankingSnapshots.MaxTrendingLimit, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
        await RefreshRankingAsync(
            CustomNetflixRankingSnapshots.TopTenId,
            await _repository.GetTopTenItemsAsync(CustomNetflixRankingSnapshots.MaxTopTenLimit, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task PurgeExpiredWatchEventsAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Subtract(WatchEventRetention);
        for (var batch = 0; batch < WatchEventPurgeMaxBatchesPerRun; batch++)
        {
            var purged = await _repository.PurgeWatchEventsAsync(
                cutoff,
                WatchEventPurgeBatchSize,
                cancellationToken).ConfigureAwait(false);
            var budgetExhausted = batch == WatchEventPurgeMaxBatchesPerRun - 1
                && purged == WatchEventPurgeBatchSize;
            CustomNetflixMetrics.ObservePurgeBatch(purged, budgetExhausted);
            if (purged < WatchEventPurgeBatchSize)
            {
                return;
            }
        }
    }

    private async Task RefreshRankingAsync(string rankingId, IReadOnlyList<RankedItemRow> items, CancellationToken cancellationToken)
    {
        var generatedAt = DateTime.UtcNow;
        var snapshot = new RankingSnapshotRow(
            rankingId,
            items,
            generatedAt,
            generatedAt.Add(CustomNetflixRankingSnapshots.SnapshotTtl));
        await _repository.SaveRankingSnapshotAsync(
            rankingId,
            items,
            snapshot.GeneratedAt,
            snapshot.ExpiresAt,
            cancellationToken).ConfigureAwait(false);
        await _cache.SetStringAsync(
            RedisKeyBuilder.RankingSnapshot(rankingId),
            CustomNetflixRankingSnapshotSerializer.Serialize(snapshot),
            snapshot.ExpiresAt - DateTime.UtcNow,
            cancellationToken).ConfigureAwait(false);
        CustomNetflixMetrics.SetRankingLastSuccess(rankingId, generatedAt);
    }
}
