using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Telemetry;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Telemetry;

/// <summary>
/// Playback and transcoding instruments published on <see cref="JellyfinTelemetry.Meter"/>.
/// </summary>
public static class PlaybackMetrics
{
    /// <summary>
    /// The name of the histogram recording how long playback sessions last.
    /// </summary>
    public const string PlaybackDurationName = "jellyfin.playback.duration";

    private const string PlayMethodTag = "jellyfin.play_method";
    private const string MediaTypeTag = "jellyfin.media_type";
    private const string OutcomeTag = "jellyfin.playback.outcome";
    private const string TranscodeTypeTag = "jellyfin.transcode.type";
    private const string HardwareAccelerationTag = "jellyfin.transcode.hardware_acceleration";
    private const string VideoCodecTag = "jellyfin.transcode.video_codec";
    private const string AudioCodecTag = "jellyfin.transcode.audio_codec";
    private const string ReasonTag = "jellyfin.transcode.reason";

    private const string OutcomeCompleted = "completed";
    private const string OutcomeAbandoned = "abandoned";
    private const string OutcomeFailed = "failed";
    private const string Unknown = "unknown";

    private static readonly ConcurrentDictionary<string, TrackedPlayback> _playback = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, TrackedTranscode> _transcodes = new(StringComparer.Ordinal);

    // Both gauges report a zero for every combination seen since startup, so that a series drops to 0 when
    // playback ends instead of going stale and leaving a gap the dashboard cannot distinguish from downtime.
    // Bounded by the enums: at most 3 x 5 playback and 2 x 8 transcode combinations.
    private static readonly ConcurrentDictionary<(PlayMethod PlayMethod, MediaType MediaType), byte> _seenPlaybackTags = new();
    private static readonly ConcurrentDictionary<(TranscodingJobType Type, HardwareAccelerationType Acceleration), byte> _seenTranscodeTags = new();

    private static readonly Counter<long> _playbackStarted = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.playback.started",
        "{session}",
        "Playback sessions started.");

    private static readonly Counter<long> _playbackStopped = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.playback.stopped",
        "{session}",
        "Playback sessions ended, by outcome.");

    private static readonly Histogram<double> _playbackDuration = JellyfinTelemetry.Meter.CreateHistogram<double>(
        PlaybackDurationName,
        "s",
        "Wall clock time between playback starting and stopping.");

    private static readonly Counter<long> _transcodeStarted = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.transcode.started",
        "{job}",
        "Transcoding jobs started.");

    private static readonly Counter<long> _transcodeStopped = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.transcode.stopped",
        "{job}",
        "Transcoding jobs that ended.");

    private static readonly Counter<long> _transcodeReasons = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.transcode.reason",
        "{reason}",
        "Reasons transcoding was required, incremented once per reason on each job.");

#pragma warning disable IDE0052 // Held so the gauges are not collected; their callbacks are the useful part.
    private static readonly ObservableGauge<int> _activeSessions = JellyfinTelemetry.Meter.CreateObservableGauge(
        "jellyfin.playback.sessions.active",
        ObserveActiveSessions,
        "{session}",
        "Playback sessions currently in progress.");

    private static readonly ObservableGauge<int> _activeTranscodes = JellyfinTelemetry.Meter.CreateObservableGauge(
        "jellyfin.transcode.active",
        ObserveActiveTranscodes,
        "{job}",
        "Transcoding jobs currently running.");
#pragma warning restore IDE0052

    /// <summary>
    /// Records that a playback session started.
    /// </summary>
    /// <param name="playSessionId">The play session id, falling back to <paramref name="sessionId"/> when absent.</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="playMethod">How the item is being delivered.</param>
    /// <param name="mediaType">The type of media being played.</param>
    public static void OnPlaybackStarted(string? playSessionId, string? sessionId, PlayMethod playMethod, MediaType mediaType)
    {
        var key = ResolveKey(playSessionId, sessionId);
        if (key is null)
        {
            return;
        }

        _playback[key] = new TrackedPlayback(Stopwatch.GetTimestamp(), playMethod, mediaType);
        _seenPlaybackTags.TryAdd((playMethod, mediaType), 0);

        _playbackStarted.Add(
            1,
            new KeyValuePair<string, object?>(PlayMethodTag, Describe(playMethod)),
            new KeyValuePair<string, object?>(MediaTypeTag, Describe(mediaType)));
    }

    /// <summary>
    /// Refreshes the tracked state for an in-progress playback session.
    /// </summary>
    /// <param name="playSessionId">The play session id, falling back to <paramref name="sessionId"/> when absent.</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="playMethod">How the item is currently being delivered.</param>
    /// <param name="mediaType">The type of media being played.</param>
    public static void OnPlaybackProgress(string? playSessionId, string? sessionId, PlayMethod playMethod, MediaType mediaType)
    {
        var key = ResolveKey(playSessionId, sessionId);
        if (key is null)
        {
            return;
        }

        _seenPlaybackTags.TryAdd((playMethod, mediaType), 0);
        _playback.AddOrUpdate(
            key,
            _ => new TrackedPlayback(Stopwatch.GetTimestamp(), playMethod, mediaType),
            (_, existing) => existing.PlayMethod == playMethod && existing.MediaType == mediaType
                ? existing
                : existing with { PlayMethod = playMethod, MediaType = mediaType });
    }

    /// <summary>
    /// Records that a playback session ended, and how long it ran.
    /// </summary>
    /// <param name="playSessionId">The play session id, falling back to <paramref name="sessionId"/> when absent.</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="playedToCompletion">Whether the item was watched far enough to count as played.</param>
    /// <param name="failed">Whether playback failed.</param>
    public static void OnPlaybackStopped(string? playSessionId, string? sessionId, bool playedToCompletion, bool failed)
    {
        var key = ResolveKey(playSessionId, sessionId);
        if (key is null)
        {
            return;
        }

        var outcome = failed ? OutcomeFailed : playedToCompletion ? OutcomeCompleted : OutcomeAbandoned;

        if (!_playback.TryRemove(key, out var tracked))
        {
            _playbackStopped.Add(
                1,
                new KeyValuePair<string, object?>(PlayMethodTag, Unknown),
                new KeyValuePair<string, object?>(MediaTypeTag, Unknown),
                new KeyValuePair<string, object?>(OutcomeTag, outcome));
            return;
        }

        var playMethod = Describe(tracked.PlayMethod);
        var mediaType = Describe(tracked.MediaType);

        _playbackStopped.Add(
            1,
            new KeyValuePair<string, object?>(PlayMethodTag, playMethod),
            new KeyValuePair<string, object?>(MediaTypeTag, mediaType),
            new KeyValuePair<string, object?>(OutcomeTag, outcome));

        _playbackDuration.Record(
            Stopwatch.GetElapsedTime(tracked.StartedTimestamp).TotalSeconds,
            new KeyValuePair<string, object?>(PlayMethodTag, playMethod),
            new KeyValuePair<string, object?>(MediaTypeTag, mediaType),
            new KeyValuePair<string, object?>(OutcomeTag, outcome));
    }

    /// <summary>
    /// Records that a transcoding job started.
    /// </summary>
    /// <param name="jobId">The transcoding job id.</param>
    /// <param name="type">Whether the job produces a progressive stream or HLS segments.</param>
    /// <param name="acceleration">The configured hardware acceleration.</param>
    /// <param name="videoCodec">The output video codec.</param>
    /// <param name="audioCodec">The output audio codec.</param>
    /// <param name="reasons">Why transcoding was required.</param>
    public static void OnTranscodeStarted(
        string? jobId,
        TranscodingJobType type,
        HardwareAccelerationType acceleration,
        string? videoCodec,
        string? audioCodec,
        TranscodeReason reasons)
    {
        _seenTranscodeTags.TryAdd((type, acceleration), 0);

        _transcodeStarted.Add(
            1,
            new KeyValuePair<string, object?>(TranscodeTypeTag, Describe(type)),
            new KeyValuePair<string, object?>(HardwareAccelerationTag, Describe(acceleration)),
            new KeyValuePair<string, object?>(VideoCodecTag, Normalize(videoCodec)),
            new KeyValuePair<string, object?>(AudioCodecTag, Normalize(audioCodec)));

        // TranscodeReason is a flags enum, so the combined value is high cardinality. Splitting it into one
        // increment per set flag keeps the label bounded and makes "why is this server transcoding" a sum.
        foreach (var reason in Enum.GetValues<TranscodeReason>())
        {
            if (reason != 0 && reasons.HasFlag(reason))
            {
                _transcodeReasons.Add(1, new KeyValuePair<string, object?>(ReasonTag, reason.ToString()));
            }
        }

        if (!string.IsNullOrEmpty(jobId))
        {
            _transcodes[jobId] = new TrackedTranscode(type, acceleration);
        }
    }

    /// <summary>
    /// Records that a transcoding job ended.
    /// </summary>
    /// <param name="jobId">The transcoding job id.</param>
    public static void OnTranscodeStopped(string? jobId)
    {
        if (string.IsNullOrEmpty(jobId) || !_transcodes.TryRemove(jobId, out var tracked))
        {
            return;
        }

        _transcodeStopped.Add(
            1,
            new KeyValuePair<string, object?>(TranscodeTypeTag, Describe(tracked.Type)),
            new KeyValuePair<string, object?>(HardwareAccelerationTag, Describe(tracked.Acceleration)));
    }

    private static string? ResolveKey(string? playSessionId, string? sessionId)
        => string.IsNullOrEmpty(playSessionId) ? (string.IsNullOrEmpty(sessionId) ? null : sessionId) : playSessionId;

    private static string Normalize(string? value) => string.IsNullOrEmpty(value) ? Unknown : value;

    private static IEnumerable<Measurement<int>> ObserveActiveSessions()
    {
        var counts = new Dictionary<(PlayMethod, MediaType), int>();
        foreach (var combination in _seenPlaybackTags.Keys)
        {
            counts[combination] = 0;
        }

        foreach (var tracked in _playback.Values)
        {
            var key = (tracked.PlayMethod, tracked.MediaType);
            counts.TryGetValue(key, out var current);
            counts[key] = current + 1;
        }

        foreach (var (key, count) in counts)
        {
            yield return new Measurement<int>(
                count,
                new KeyValuePair<string, object?>(PlayMethodTag, Describe(key.Item1)),
                new KeyValuePair<string, object?>(MediaTypeTag, Describe(key.Item2)));
        }
    }

    private static IEnumerable<Measurement<int>> ObserveActiveTranscodes()
    {
        var counts = new Dictionary<(TranscodingJobType, HardwareAccelerationType), int>();
        foreach (var combination in _seenTranscodeTags.Keys)
        {
            counts[combination] = 0;
        }

        foreach (var tracked in _transcodes.Values)
        {
            var key = (tracked.Type, tracked.Acceleration);
            counts.TryGetValue(key, out var current);
            counts[key] = current + 1;
        }

        foreach (var (key, count) in counts)
        {
            yield return new Measurement<int>(
                count,
                new KeyValuePair<string, object?>(TranscodeTypeTag, Describe(key.Item1)),
                new KeyValuePair<string, object?>(HardwareAccelerationTag, Describe(key.Item2)));
        }
    }

    private static string Describe(PlayMethod playMethod) => playMethod switch
    {
        PlayMethod.Transcode => "Transcode",
        PlayMethod.DirectStream => "DirectStream",
        PlayMethod.DirectPlay => "DirectPlay",
        _ => Unknown
    };

    private static string Describe(MediaType mediaType) => mediaType switch
    {
        MediaType.Video => "Video",
        MediaType.Audio => "Audio",
        MediaType.Photo => "Photo",
        MediaType.Book => "Book",
        _ => Unknown
    };

    private static string Describe(TranscodingJobType type) => type switch
    {
        TranscodingJobType.Progressive => "Progressive",
        TranscodingJobType.Hls => "Hls",
        _ => Unknown
    };

    private static string Describe(HardwareAccelerationType acceleration) => acceleration switch
    {
        HardwareAccelerationType.none => "none",
        HardwareAccelerationType.amf => "amf",
        HardwareAccelerationType.qsv => "qsv",
        HardwareAccelerationType.nvenc => "nvenc",
        HardwareAccelerationType.v4l2m2m => "v4l2m2m",
        HardwareAccelerationType.vaapi => "vaapi",
        HardwareAccelerationType.videotoolbox => "videotoolbox",
        HardwareAccelerationType.rkmpp => "rkmpp",
        _ => Unknown
    };

    private sealed record TrackedPlayback(long StartedTimestamp, PlayMethod PlayMethod, MediaType MediaType);

    private sealed record TrackedTranscode(TranscodingJobType Type, HardwareAccelerationType Acceleration);
}
