using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Telemetry;

namespace MediaBrowser.Controller.Telemetry;

/// <summary>
/// Metadata provider instruments published on <see cref="JellyfinTelemetry.Meter"/>.
/// </summary>
public static class ProviderMetrics
{
    /// <summary>
    /// The name of the histogram recording how long refreshing the metadata of an item takes.
    /// </summary>
    public const string RefreshDurationName = "jellyfin.metadata.refresh.duration";

    private const string ProviderTag = "jellyfin.provider";
    private const string OutcomeTag = "jellyfin.download.outcome";

    private const string OutcomeSucceeded = "succeeded";
    private const string OutcomeFailed = "failed";

    private static readonly ConcurrentDictionary<Guid, long> _refreshes = new();
    private static readonly ConcurrentDictionary<BaseItemKind, string> _kindNames = new();

    private static readonly Histogram<double> _refreshDuration = JellyfinTelemetry.Meter.CreateHistogram<double>(
        RefreshDurationName,
        "s",
        "Wall clock time a metadata refresh of a single item took.");

    private static readonly Counter<long> _subtitleDownloads = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.subtitle.downloads",
        "{download}",
        "Subtitle downloads, by provider and outcome.");

    private static readonly Counter<long> _lyricDownloads = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.lyric.downloads",
        "{download}",
        "Lyric downloads, by provider and outcome.");

#pragma warning disable IDE0052 // Held so the gauge is not collected; its callback is the useful part.
    private static readonly ObservableGauge<int> _activeRefreshes = JellyfinTelemetry.Meter.CreateObservableGauge(
        "jellyfin.metadata.refresh.active",
        () => _refreshes.Count,
        "{refresh}",
        "Metadata refreshes currently running.");
#pragma warning restore IDE0052

    /// <summary>
    /// Records that a metadata refresh started.
    /// </summary>
    /// <param name="itemId">The id of the item being refreshed.</param>
    public static void OnRefreshStarted(Guid itemId) => _refreshes[itemId] = Stopwatch.GetTimestamp();

    /// <summary>
    /// Records that a metadata refresh completed.
    /// </summary>
    /// <param name="itemId">The id of the item that was refreshed.</param>
    /// <param name="kind">The kind of the item that was refreshed.</param>
    public static void OnRefreshCompleted(Guid itemId, BaseItemKind kind)
    {
        if (!_refreshes.TryRemove(itemId, out var startedTimestamp))
        {
            return;
        }

        _refreshDuration.Record(
            Stopwatch.GetElapsedTime(startedTimestamp).TotalSeconds,
            new KeyValuePair<string, object?>(TelemetryTags.ItemKindTag, _kindNames.GetOrAdd(kind, static k => k.ToString())));
    }

    /// <summary>
    /// Records the outcome of a subtitle download.
    /// </summary>
    /// <param name="provider">The name of the provider the subtitle was downloaded from.</param>
    /// <param name="succeeded">Whether the download succeeded.</param>
    public static void OnSubtitleDownload(string? provider, bool succeeded)
        => _subtitleDownloads.Add(1, DownloadTags(provider, succeeded));

    /// <summary>
    /// Records the outcome of a lyric download.
    /// </summary>
    /// <param name="provider">The name of the provider the lyric was downloaded from.</param>
    /// <param name="succeeded">Whether the download succeeded.</param>
    public static void OnLyricDownload(string? provider, bool succeeded)
        => _lyricDownloads.Add(1, DownloadTags(provider, succeeded));

    private static TagList DownloadTags(string? provider, bool succeeded)
        => new()
        {
            { ProviderTag, TelemetryTags.Normalize(provider) },
            { OutcomeTag, succeeded ? OutcomeSucceeded : OutcomeFailed }
        };
}
