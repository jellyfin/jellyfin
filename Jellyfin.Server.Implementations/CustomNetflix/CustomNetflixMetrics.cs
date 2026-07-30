#pragma warning disable CS1591

using System;
using Prometheus;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixMetrics
{
    private static readonly Counter ProgressReportsTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_progress_reports_total",
        "Total CustomNetflix watch progress reports.",
        new CounterConfiguration
        {
            LabelNames = new[] { "result", "event_type" }
        });

    private static readonly Counter RankingRequestsTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_ranking_requests_total",
        "Total CustomNetflix ranking requests.",
        new CounterConfiguration
        {
            LabelNames = new[] { "ranking", "result" }
        });

    private static readonly Counter RedisOperationsTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_redis_operations_total",
        "Total CustomNetflix Redis cache operations.",
        new CounterConfiguration
        {
            LabelNames = new[] { "operation", "result" }
        });

    private static readonly Counter HomeRequestsTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_home_requests_total",
        "Total CustomNetflix home requests.",
        new CounterConfiguration
        {
            LabelNames = new[] { "result" }
        });

    private static readonly Histogram HomeRequestDurationSeconds = Metrics.CreateHistogram(
        "jellyfin_customnetflix_home_request_duration_seconds",
        "CustomNetflix home request duration.",
        new HistogramConfiguration
        {
            LabelNames = new[] { "result" },
            Buckets = Histogram.ExponentialBuckets(0.005, 2, 12)
        });

    private static readonly Counter HomeCacheLookupsTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_home_cache_lookups_total",
        "CustomNetflix home cache lookups.",
        new CounterConfiguration
        {
            LabelNames = new[] { "layer", "result" }
        });

    private static readonly Histogram PostgreSqlOperationDurationSeconds = Metrics.CreateHistogram(
        "jellyfin_customnetflix_postgresql_operation_duration_seconds",
        "CustomNetflix PostgreSQL operation duration.",
        new HistogramConfiguration
        {
            LabelNames = new[] { "operation" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 14)
        });

    private static readonly Gauge BufferDepth = Metrics.CreateGauge(
        "jellyfin_customnetflix_buffer_items",
        "Current CustomNetflix buffered item count.",
        new GaugeConfiguration
        {
            LabelNames = new[] { "buffer", "stage" }
        });

    private static readonly Counter BufferFlushesTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_buffer_flushes_total",
        "Total CustomNetflix buffer flushes.",
        new CounterConfiguration
        {
            LabelNames = new[] { "buffer", "result" }
        });

    private static readonly Counter BufferRowsTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_buffer_rows_total",
        "Total CustomNetflix buffer rows processed.",
        new CounterConfiguration
        {
            LabelNames = new[] { "buffer", "result" }
        });

    private static readonly Counter WatchEventSamplesTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_watch_event_samples_total",
        "Total CustomNetflix watch events accepted or skipped by ranking sampling.",
        new CounterConfiguration
        {
            LabelNames = new[] { "result" }
        });

    private static readonly Gauge RankingLastSuccessUnixSeconds = Metrics.CreateGauge(
        "jellyfin_customnetflix_ranking_last_success_unixtime",
        "Unix timestamp of the last successful CustomNetflix ranking refresh.",
        new GaugeConfiguration
        {
            LabelNames = new[] { "ranking" }
        });

    private static readonly Counter PurgedWatchEventsTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_purged_watch_events_total",
        "Total expired CustomNetflix watch events purged.");

    private static readonly Counter PurgeBatchesTotal = Metrics.CreateCounter(
        "jellyfin_customnetflix_purge_batches_total",
        "Total CustomNetflix watch-event purge batches.",
        new CounterConfiguration
        {
            LabelNames = new[] { "result" }
        });

    public static void ObserveProgressReport(string result, string eventType)
        => ProgressReportsTotal.WithLabels(result, eventType).Inc();

    public static void ObserveRankingRequest(string ranking, string result)
        => RankingRequestsTotal.WithLabels(ranking, result).Inc();

    public static void ObserveRedisOperation(string operation, string result)
        => RedisOperationsTotal.WithLabels(operation, result).Inc();

    public static void ObserveHomeRequest(string result, TimeSpan elapsed)
    {
        HomeRequestsTotal.WithLabels(result).Inc();
        HomeRequestDurationSeconds.WithLabels(result).Observe(elapsed.TotalSeconds);
    }

    public static void ObserveHomeCacheLookup(string layer, string result)
        => HomeCacheLookupsTotal.WithLabels(layer, result).Inc();

    public static IDisposable MeasurePostgreSqlOperation(string operation)
        => PostgreSqlOperationDurationSeconds.WithLabels(operation).NewTimer();

    public static void SetBufferDepth(string buffer, int queued, int pending)
    {
        BufferDepth.WithLabels(buffer, "queued").Set(queued);
        BufferDepth.WithLabels(buffer, "pending").Set(pending);
    }

    public static void ObserveBufferFlush(string buffer, string result, int rows)
    {
        BufferFlushesTotal.WithLabels(buffer, result).Inc();
        BufferRowsTotal.WithLabels(buffer, result).Inc(rows);
    }

    public static void ObserveWatchEventSample(string result)
        => WatchEventSamplesTotal.WithLabels(result).Inc();

    public static void SetRankingLastSuccess(string ranking, DateTime generatedAt)
        => RankingLastSuccessUnixSeconds.WithLabels(ranking).Set(new DateTimeOffset(generatedAt).ToUnixTimeSeconds());

    public static void ObservePurgeBatch(int rows, bool budgetExhausted)
    {
        PurgedWatchEventsTotal.Inc(rows);
        PurgeBatchesTotal.WithLabels(budgetExhausted ? "budget_exhausted" : "completed").Inc();
    }
}
