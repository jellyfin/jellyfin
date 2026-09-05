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

    /// <summary>
    /// The name of the histogram recording encoder throughput relative to the source.
    /// </summary>
    public const string TranscodeSpeedName = "jellyfin.transcode.speed";

    private const string PlayMethodTag = "jellyfin.play_method";
    private const string MediaTypeTag = "jellyfin.media_type";
    private const string OutcomeTag = "jellyfin.playback.outcome";
    private const string TranscodeTypeTag = "jellyfin.transcode.type";
    private const string HardwareAccelerationTag = "jellyfin.transcode.hardware_acceleration";
    private const string VideoCodecTag = "jellyfin.transcode.video_codec";
    private const string AudioCodecTag = "jellyfin.transcode.audio_codec";
    private const string ReasonTag = "jellyfin.transcode.reason";
    private const string ClientTag = TelemetryTags.ClientTag;

    private const string OutcomeCompleted = "completed";
    private const string OutcomeAbandoned = "abandoned";
    private const string OutcomeFailed = "failed";
    private const string Unknown = TelemetryTags.Unknown;

    private static readonly ConcurrentDictionary<string, TrackedPlayback> _playback = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, TrackedTranscode> _transcodes = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<(PlayMethod PlayMethod, MediaType MediaType, string Client), byte> _seenPlaybackTags = new();
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

    private static readonly Histogram<double> _transcodeFramerate = JellyfinTelemetry.Meter.CreateHistogram<double>(
        "jellyfin.transcode.framerate",
        "{frame}/s",
        "Framerate the encoder is achieving, sampled on every progress report.");

    private static readonly Histogram<double> _transcodeSpeed = JellyfinTelemetry.Meter.CreateHistogram<double>(
        TranscodeSpeedName,
        "1",
        "Encoder framerate relative to the framerate of the source. Below 1 the job is slower than realtime and the client will eventually stall.");

    private static readonly Counter<long> _transcodeReasons = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.transcode.reason",
        "{reason}",
        "Reasons transcoding was required, incremented once per reason on each job.");

#pragma warning disable IDE0052, CA1823 // Held so the gauges are not collected; their callbacks are the useful part.
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

    private static readonly ObservableGauge<long> _playbackBitrate = JellyfinTelemetry.Meter.CreateObservableGauge(
        "jellyfin.playback.bitrate",
        ObservePlaybackBitrate,
        "bit/s",
        "Combined bitrate of the media sources playing in active sessions. This is what is read, not what is delivered; for a transcoding session the delivered bitrate is jellyfin.transcode.bitrate.");

    private static readonly ObservableGauge<long> _transcodeBitrate = JellyfinTelemetry.Meter.CreateObservableGauge(
        "jellyfin.transcode.bitrate",
        ObserveTranscodeBitrate,
        "bit/s",
        "Combined output bitrate of the running transcoding jobs.");
#pragma warning restore IDE0052, CA1823

    /// <summary>
    /// Records that a playback session started.
    /// </summary>
    /// <param name="playSessionId">The play session id, falling back to <paramref name="sessionId"/> when absent.</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="playMethod">How the item is being delivered.</param>
    /// <param name="mediaType">The type of media being played.</param>
    /// <param name="client">The name of the client application playing the item.</param>
    /// <param name="bitrate">The bitrate of the media source, in bits per second, when it is known.</param>
    public static void OnPlaybackStarted(
        string? playSessionId,
        string? sessionId,
        PlayMethod playMethod,
        MediaType mediaType,
        string? client,
        int? bitrate)
    {
        var key = ResolveKey(playSessionId, sessionId);
        if (key is null)
        {
            return;
        }

        var clientName = TelemetryTags.Client(client);

        _playback[key] = new TrackedPlayback(Stopwatch.GetTimestamp(), playMethod, mediaType, clientName, bitrate > 0 ? bitrate.Value : 0);
        _seenPlaybackTags.TryAdd((playMethod, mediaType, clientName), 0);

        _playbackStarted.Add(
            1,
            new KeyValuePair<string, object?>(PlayMethodTag, Describe(playMethod)),
            new KeyValuePair<string, object?>(MediaTypeTag, Describe(mediaType)),
            new KeyValuePair<string, object?>(ClientTag, clientName));
    }

    /// <summary>
    /// Refreshes the tracked state for an in-progress playback session.
    /// </summary>
    /// <param name="playSessionId">The play session id, falling back to <paramref name="sessionId"/> when absent.</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="playMethod">How the item is currently being delivered.</param>
    /// <param name="mediaType">The type of media being played.</param>
    /// <param name="client">The name of the client application playing the item.</param>
    public static void OnPlaybackProgress(string? playSessionId, string? sessionId, PlayMethod playMethod, MediaType mediaType, string? client)
    {
        var key = ResolveKey(playSessionId, sessionId);
        if (key is null)
        {
            return;
        }

        var clientName = TelemetryTags.Client(client);

        _seenPlaybackTags.TryAdd((playMethod, mediaType, clientName), 0);
        _playback.AddOrUpdate(
            key,
            // The bitrate is only known when the media source was resolved, which happens on playback start.
            _ => new TrackedPlayback(Stopwatch.GetTimestamp(), playMethod, mediaType, clientName, 0),
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
                new KeyValuePair<string, object?>(ClientTag, Unknown),
                new KeyValuePair<string, object?>(OutcomeTag, outcome));
            return;
        }

        var tags = new TagList
        {
            { PlayMethodTag, Describe(tracked.PlayMethod) },
            { MediaTypeTag, Describe(tracked.MediaType) },
            { ClientTag, tracked.Client },
            { OutcomeTag, outcome }
        };

        _playbackStopped.Add(1, tags);
        _playbackDuration.Record(Stopwatch.GetElapsedTime(tracked.StartedTimestamp).TotalSeconds, tags);
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
            _transcodes[jobId] = new TrackedTranscode(type, acceleration, 0);
        }
    }

    /// <summary>
    /// Records the throughput of a running transcoding job, as reported by ffmpeg.
    /// </summary>
    /// <param name="jobId">The transcoding job id.</param>
    /// <param name="framerate">The framerate the encoder is achieving.</param>
    /// <param name="bitrate">The output bitrate, in bits per second.</param>
    /// <param name="sourceFramerate">The framerate of the source, used to put <paramref name="framerate"/> in context.</param>
    public static void OnTranscodeProgress(string? jobId, float? framerate, int? bitrate, float? sourceFramerate)
    {
        // Progress is reported once before ffmpeg has produced any numbers, and for jobs that started
        // before the metrics existed, so anything unknown is skipped rather than recorded as a zero.
        if (string.IsNullOrEmpty(jobId) || !_transcodes.TryGetValue(jobId, out var tracked))
        {
            return;
        }

        // Comparing against the value that was read keeps a job that stopped in the meantime from
        // being resurrected. A lost update is corrected by the next progress report.
        if (bitrate > 0)
        {
            _transcodes.TryUpdate(jobId, tracked with { Bitrate = bitrate.Value }, tracked);
        }

        if (framerate is not > 0)
        {
            return;
        }

        var tags = new TagList
        {
            { TranscodeTypeTag, Describe(tracked.Type) },
            { HardwareAccelerationTag, Describe(tracked.Acceleration) }
        };

        _transcodeFramerate.Record(framerate.Value, tags);

        if (sourceFramerate > 0)
        {
            _transcodeSpeed.Record(framerate.Value / sourceFramerate.Value, tags);
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

    private static string Normalize(string? value) => TelemetryTags.Normalize(value);

    private static IEnumerable<Measurement<int>> ObserveActiveSessions()
    {
        var counts = new Dictionary<(PlayMethod, MediaType, string), int>();
        foreach (var combination in _seenPlaybackTags.Keys)
        {
            counts[combination] = 0;
        }

        foreach (var tracked in _playback.Values)
        {
            var key = (tracked.PlayMethod, tracked.MediaType, tracked.Client);
            counts.TryGetValue(key, out var current);
            counts[key] = current + 1;
        }

        foreach (var (key, count) in counts)
        {
            yield return new Measurement<int>(count, PlaybackTags(key));
        }
    }

    private static IEnumerable<Measurement<long>> ObservePlaybackBitrate()
    {
        var bitrates = new Dictionary<(PlayMethod, MediaType, string), long>();
        foreach (var combination in _seenPlaybackTags.Keys)
        {
            bitrates[combination] = 0;
        }

        foreach (var tracked in _playback.Values)
        {
            var key = (tracked.PlayMethod, tracked.MediaType, tracked.Client);
            bitrates.TryGetValue(key, out var current);
            bitrates[key] = current + tracked.Bitrate;
        }

        foreach (var (key, bitrate) in bitrates)
        {
            yield return new Measurement<long>(bitrate, PlaybackTags(key));
        }
    }

    private static TagList PlaybackTags((PlayMethod PlayMethod, MediaType MediaType, string Client) key)
        => new()
        {
            { PlayMethodTag, Describe(key.PlayMethod) },
            { MediaTypeTag, Describe(key.MediaType) },
            { ClientTag, key.Client }
        };

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
            yield return new Measurement<int>(count, TranscodeTags(key));
        }
    }

    private static IEnumerable<Measurement<long>> ObserveTranscodeBitrate()
    {
        var bitrates = new Dictionary<(TranscodingJobType, HardwareAccelerationType), long>();
        foreach (var combination in _seenTranscodeTags.Keys)
        {
            bitrates[combination] = 0;
        }

        foreach (var tracked in _transcodes.Values)
        {
            var key = (tracked.Type, tracked.Acceleration);
            bitrates.TryGetValue(key, out var current);
            bitrates[key] = current + tracked.Bitrate;
        }

        foreach (var (key, bitrate) in bitrates)
        {
            yield return new Measurement<long>(bitrate, TranscodeTags(key));
        }
    }

    private static TagList TranscodeTags((TranscodingJobType Type, HardwareAccelerationType Acceleration) key)
        => new()
        {
            { TranscodeTypeTag, Describe(key.Type) },
            { HardwareAccelerationTag, Describe(key.Acceleration) }
        };

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

    private sealed record TrackedPlayback(long StartedTimestamp, PlayMethod PlayMethod, MediaType MediaType, string Client, long Bitrate);

    private sealed record TrackedTranscode(TranscodingJobType Type, HardwareAccelerationType Acceleration, long Bitrate);
}
