using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
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

    private static readonly ConcurrentDictionary<BaseItemKind, string> _kindNames = new();

    private static readonly Histogram<double> _refreshDuration = JellyfinTelemetry.Meter.CreateHistogram<double>(
        RefreshDurationName,
        "s",
        "Wall clock time refreshing the metadata of a single item took.");

    private static readonly Counter<long> _subtitleDownloads = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.subtitle.downloads",
        "{download}",
        "Subtitle downloads, by provider and outcome.");

    private static readonly Counter<long> _lyricDownloads = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.lyric.downloads",
        "{download}",
        "Lyric downloads, by provider and outcome.");

#pragma warning disable IDE0052, CA1823 // Held so the gauge is not collected; its callback is the useful part.
    private static readonly ObservableGauge<int> _activeRefreshes = JellyfinTelemetry.Meter.CreateObservableGauge(
        "jellyfin.metadata.refresh.active",
        () => Volatile.Read(ref _activeRefreshCount),
        "{refresh}",
        "Metadata refreshes of single items currently running.");
#pragma warning restore IDE0052, CA1823

    private static int _activeRefreshCount;

    /// <summary>
    /// Records that the metadata refresh of a single item started. The returned timestamp has to be
    /// handed back to <see cref="OnRefreshCompleted"/>, which every caller has to reach.
    /// </summary>
    /// <returns>The timestamp the refresh started at.</returns>
    public static long OnRefreshStarted()
    {
        Interlocked.Increment(ref _activeRefreshCount);
        return Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Records that the metadata refresh of a single item completed.
    /// </summary>
    /// <param name="startedTimestamp">The timestamp returned by <see cref="OnRefreshStarted"/>.</param>
    /// <param name="kind">The kind of the item that was refreshed.</param>
    public static void OnRefreshCompleted(long startedTimestamp, BaseItemKind kind)
    {
        Interlocked.Decrement(ref _activeRefreshCount);

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
            { ProviderTag, TelemetryTags.Provider(provider) },
            { OutcomeTag, succeeded ? OutcomeSucceeded : OutcomeFailed }
        };
}
