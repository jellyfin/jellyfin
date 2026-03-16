using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PlaybackReporter.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaybackReporter.Services;

/// <summary>
/// Background service that subscribes to Jellyfin session events and records playback problems.
/// </summary>
public sealed class PlaybackMonitorService : IHostedService, IDisposable
{
    // Tracks sessions that are actively playing: PlaySessionId → start info snapshot.
    private readonly ConcurrentDictionary<string, ActiveSession> _activeSessions = new(StringComparer.Ordinal);

    private readonly ISessionManager _sessionManager;
    private readonly GitHubReporter _gitHubReporter;
    private readonly ILogger<PlaybackMonitorService> _logger;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackMonitorService"/> class.
    /// </summary>
    public PlaybackMonitorService(
        ISessionManager sessionManager,
        GitHubReporter gitHubReporter,
        ILogger<PlaybackMonitorService> logger)
    {
        _sessionManager = sessionManager;
        _gitHubReporter = gitHubReporter;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;

        _logger.LogInformation("Playback Reporter: monitoring started.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;

        _logger.LogInformation("Playback Reporter: monitoring stopped.");
        return Task.CompletedTask;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        var key = e.PlaySessionId ?? e.Session?.Id ?? e.DeviceId ?? Guid.NewGuid().ToString();

        var session = new ActiveSession
        {
            StartedAt = DateTimeOffset.UtcNow,
            PlaySessionId = e.PlaySessionId,
            SessionId = e.Session?.Id,
            ClientName = e.ClientName,
            DeviceId = e.DeviceId,
            DeviceName = e.DeviceName,
            RequestedItem = e.Item,
            RequestedItemName = e.Item?.Name,
            RequestedItemId = e.Item?.Id ?? Guid.Empty,
            RequestedMediaSourceId = e.MediaSourceId,
        };

        // Capture the source format from the session's NowPlayingItem
        if (e.Session?.NowPlayingItem is { } nowPlaying)
        {
            session.ActualMediaSourceId = e.MediaSourceId;
            session.ActualItemName = nowPlaying.Name;
        }

        // If a transcode is already active on start, capture it
        if (e.Session?.TranscodingInfo is { } txInfo)
        {
            session.LastTranscodingInfo = txInfo;
        }

        _activeSessions[key] = session;

        // Check for item mismatch on start (session played different source than requested)
        CheckItemMismatch(session, e);
    }

    private void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        var key = e.PlaySessionId ?? e.Session?.Id ?? e.DeviceId;
        if (key is null || !_activeSessions.TryGetValue(key, out var session))
        {
            return;
        }

        // Update the latest transcoding info
        if (e.Session?.TranscodingInfo is { } txInfo)
        {
            session.LastTranscodingInfo = txInfo;

            // Check for HDR issue on first transcoded progress report
            if (!session.HdrIssueRecorded)
            {
                CheckHdrIssue(session, e, txInfo);
            }

            // Check for format mismatch on first transcoded progress report
            if (!session.FormatMismatchRecorded)
            {
                CheckFormatMismatch(session, e, txInfo);
            }
        }

        // Update play position for duration tracking
        if (e.PlaybackPositionTicks.HasValue)
        {
            session.LastPositionTicks = e.PlaybackPositionTicks.Value;
        }
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        var key = e.PlaySessionId ?? e.Session?.Id ?? e.DeviceId;
        if (key is null)
        {
            return;
        }

        _activeSessions.TryRemove(key, out var session);

        if (session is null)
        {
            return;
        }

        var config = Plugin.Instance?.Configuration;
        if (config is null || !config.TrackPlaybackFailures)
        {
            return;
        }

        // Calculate how long the session actually played
        var elapsed = (DateTimeOffset.UtcNow - session.StartedAt).TotalSeconds;
        var positionSeconds = session.LastPositionTicks > 0
            ? TimeSpan.FromTicks(session.LastPositionTicks).TotalSeconds
            : elapsed;

        // DirectPlayError in transcode reasons is an explicit failure signal
        bool hasDirectPlayError = session.LastTranscodingInfo?.TranscodeReasons.HasFlag(TranscodeReason.DirectPlayError) == true;

        // Very-short playback without completion is likely a failure
        bool stoppedEarly = !e.PlayedToCompletion && positionSeconds < config.MinPlaybackSecondsForFailure;

        if (hasDirectPlayError || stoppedEarly)
        {
            var errorEvent = BuildBaseEvent(session, e);
            errorEvent.EventType = PlaybackEventType.PlaybackFailure;
            errorEvent.PlayedDurationSeconds = positionSeconds;

            if (hasDirectPlayError)
            {
                errorEvent.Notes.Add("TranscodeReason.DirectPlayError was set - the player reported a direct-play failure.");
            }

            if (stoppedEarly)
            {
                errorEvent.Notes.Add(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Session stopped after {positionSeconds:F1}s without completing, below the {config.MinPlaybackSecondsForFailure}s threshold."));
            }

            RecordAndReport(errorEvent);
        }
    }

    // ── Detection Helpers ─────────────────────────────────────────────────────

    private void CheckItemMismatch(ActiveSession session, PlaybackProgressEventArgs e)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !config.TrackItemMismatches)
        {
            return;
        }

        // A mismatch occurs when the session is playing a different media source than what was requested,
        // or when the item being played is not the item that was originally requested.
        bool sourceIdMismatch =
            !string.IsNullOrEmpty(session.RequestedMediaSourceId)
            && !string.IsNullOrEmpty(e.MediaSourceId)
            && !string.Equals(session.RequestedMediaSourceId, e.MediaSourceId, StringComparison.Ordinal);

        bool itemIdMismatch =
            e.Session?.NowPlayingItem is { } nowPlayingDto
            && nowPlayingDto.Id != session.RequestedItemId
            && session.RequestedItemId != Guid.Empty;

        if (!sourceIdMismatch && !itemIdMismatch)
        {
            return;
        }

        session.ItemMismatchRecorded = true;

        var errorEvent = BuildBaseEvent(session, e);
        errorEvent.EventType = PlaybackEventType.ItemMismatch;
        errorEvent.ActualItemName = e.Session?.NowPlayingItem?.Name;
        errorEvent.ActualMediaSourceId = e.MediaSourceId;

        if (sourceIdMismatch)
        {
            errorEvent.Notes.Add(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Requested MediaSourceId '{session.RequestedMediaSourceId}' but the session is playing '{e.MediaSourceId}'."));
        }

        if (itemIdMismatch)
        {
            errorEvent.Notes.Add(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Requested ItemId '{session.RequestedItemId}' but session is playing '{e.Session?.NowPlayingItem?.Id}'."));
        }

        RecordAndReport(errorEvent);
    }

    private void CheckFormatMismatch(ActiveSession session, PlaybackProgressEventArgs e, TranscodingInfo txInfo)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !config.TrackFormatMismatches)
        {
            return;
        }

        // A format mismatch is any transcode triggered by a codec/container incompatibility.
        const TranscodeReason formatFlags =
            TranscodeReason.ContainerNotSupported
            | TranscodeReason.VideoCodecNotSupported
            | TranscodeReason.AudioCodecNotSupported
            | TranscodeReason.VideoProfileNotSupported
            | TranscodeReason.VideoLevelNotSupported
            | TranscodeReason.VideoCodecTagNotSupported
            | TranscodeReason.AudioProfileNotSupported;

        if (txInfo.TranscodeReasons == 0 || (txInfo.TranscodeReasons & formatFlags) == 0)
        {
            return;
        }

        session.FormatMismatchRecorded = true;

        var errorEvent = BuildBaseEvent(session, e);
        errorEvent.EventType = PlaybackEventType.FormatMismatch;
        errorEvent.DeliveredContainer = txInfo.Container;
        errorEvent.DeliveredVideoCodec = txInfo.VideoCodec;
        errorEvent.DeliveredAudioCodec = txInfo.AudioCodec;
        errorEvent.PlayMethod = PlayMethod.Transcode;
        errorEvent.TranscodeReasons = txInfo.TranscodeReasons;

        errorEvent.Notes.Add(string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"Source was {session.SourceContainer}/{session.SourceVideoCodec}/{session.SourceAudioCodec}; client received {txInfo.Container}/{txInfo.VideoCodec}/{txInfo.AudioCodec}."));

        RecordAndReport(errorEvent);
    }

    private void CheckHdrIssue(ActiveSession session, PlaybackProgressEventArgs e, TranscodingInfo txInfo)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !config.TrackHdrIssues)
        {
            return;
        }

        // HDR issue: VideoRangeTypeNotSupported is explicit; also flag when DOVI/HDR10+ is transcoded.
        bool rangeNotSupported = txInfo.TranscodeReasons.HasFlag(TranscodeReason.VideoRangeTypeNotSupported);
        bool isHdrContent = session.SourceVideoRange is "HDR"
            || (!string.IsNullOrEmpty(session.SourceVideoRangeType)
                && !session.SourceVideoRangeType.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                && !session.SourceVideoRangeType.Equals("SDR", StringComparison.OrdinalIgnoreCase));

        if (!rangeNotSupported && !isHdrContent)
        {
            return;
        }

        // Only report if a transcode is actually happening (not direct play of HDR content)
        if (e.Session?.TranscodingInfo is null)
        {
            return;
        }

        session.HdrIssueRecorded = true;

        var errorEvent = BuildBaseEvent(session, e);
        errorEvent.EventType = PlaybackEventType.HdrPlaybackIssue;
        errorEvent.DeliveredContainer = txInfo.Container;
        errorEvent.DeliveredVideoCodec = txInfo.VideoCodec;
        errorEvent.DeliveredAudioCodec = txInfo.AudioCodec;
        errorEvent.PlayMethod = PlayMethod.Transcode;
        errorEvent.TranscodeReasons = txInfo.TranscodeReasons;

        if (rangeNotSupported)
        {
            errorEvent.Notes.Add(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Client does not support the source video range type ({session.SourceVideoRangeType}). Content is being transcoded/tone-mapped."));
        }
        else if (isHdrContent)
        {
            errorEvent.Notes.Add(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"HDR content ({session.SourceVideoRangeType}) is being transcoded - client may not support this HDR format natively."));
        }

        if (session.HasDolbyVision)
        {
            errorEvent.Notes.Add($"Source contains Dolby Vision (Profile {session.DolbyVisionProfile}, Fallback: {session.DolbyVisionFallback ?? "none"}).");
        }

        RecordAndReport(errorEvent);
    }

    // ── Event Construction ────────────────────────────────────────────────────

    private PlaybackErrorEvent BuildBaseEvent(ActiveSession session, PlaybackProgressEventArgs e)
    {
        // Try to get source stream info from the item
        string? sourceContainer = session.SourceContainer;
        string? sourceVideoCodec = session.SourceVideoCodec;
        string? sourceAudioCodec = session.SourceAudioCodec;

        if (e.Item is Video video)
        {
            sourceContainer ??= video.Container;
        }

        return new PlaybackErrorEvent
        {
            SessionId = session.SessionId,
            PlaySessionId = session.PlaySessionId,
            ClientName = session.ClientName,
            DeviceId = session.DeviceId,
            DeviceName = session.DeviceName,
            RequestedItemId = session.RequestedItemId,
            RequestedItemName = session.RequestedItemName,
            RequestedMediaSourceId = session.RequestedMediaSourceId,
            ActualMediaSourceId = e.MediaSourceId,
            ActualItemName = e.Session?.NowPlayingItem?.Name ?? e.Item?.Name,
            PlayMethod = e.Session?.PlayState?.PlayMethod,
            SourceContainer = sourceContainer,
            SourceVideoCodec = sourceVideoCodec,
            SourceAudioCodec = sourceAudioCodec,
            SourceVideoRange = session.SourceVideoRange,
            SourceVideoRangeType = session.SourceVideoRangeType,
            HasDolbyVision = session.HasDolbyVision,
            DolbyVisionProfile = session.DolbyVisionProfile,
            DolbyVisionFallback = session.DolbyVisionFallback,
        };
    }

    // ── Persistence & Reporting ───────────────────────────────────────────────

    private void RecordAndReport(PlaybackErrorEvent errorEvent)
    {
        _logger.LogInformation(
            "Playback Reporter: recorded {EventType} for item '{Item}' on client '{Client}'",
            errorEvent.EventType,
            errorEvent.RequestedItemName,
            errorEvent.ClientName);

        PersistEvent(errorEvent);

        var config = Plugin.Instance?.Configuration;
        if (config?.AutoReportToGitHub == true)
        {
            // Fire-and-forget; errors are logged inside GitHubReporter
            _ = ReportToGitHubAsync(errorEvent, config);
        }
    }

    private async Task ReportToGitHubAsync(PlaybackErrorEvent errorEvent, Configuration.PluginConfiguration config)
    {
        var issueUrl = await _gitHubReporter.ReportAsync(errorEvent, config, CancellationToken.None).ConfigureAwait(false);
        if (issueUrl is not null)
        {
            errorEvent.ReportedToGitHub = true;
            errorEvent.GitHubIssueUrl = issueUrl;
            PersistEvent(errorEvent);
        }
    }

    /// <summary>
    /// Writes the event to a JSON file in the plugin's data folder so it persists across restarts
    /// and can be reviewed or manually reported later.
    /// </summary>
    private static void PersistEvent(PlaybackErrorEvent errorEvent)
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return;
        }

        try
        {
            var dir = Path.Combine(plugin.DataFolderPath, "events");
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, $"{errorEvent.Id}.json");
            var json = JsonSerializer.Serialize(errorEvent, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            // Don't let persistence failures crash the monitoring pipeline
            _ = ex;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── Inner Types ───────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of a playing session's state, held in memory while the session is active.
    /// </summary>
    private sealed class ActiveSession
    {
        public DateTimeOffset StartedAt { get; init; }
        public string? PlaySessionId { get; init; }
        public string? SessionId { get; init; }
        public string? ClientName { get; init; }
        public string? DeviceId { get; init; }
        public string? DeviceName { get; init; }
        public BaseItem? RequestedItem { get; init; }
        public string? RequestedItemName { get; init; }
        public Guid RequestedItemId { get; init; }
        public string? RequestedMediaSourceId { get; init; }
        public string? ActualMediaSourceId { get; set; }
        public string? ActualItemName { get; set; }
        public long LastPositionTicks { get; set; }
        public TranscodingInfo? LastTranscodingInfo { get; set; }

        // Source format (populated from MediaStream/MediaSource info)
        public string? SourceContainer { get; set; }
        public string? SourceVideoCodec { get; set; }
        public string? SourceAudioCodec { get; set; }
        public string? SourceVideoRange { get; set; }
        public string? SourceVideoRangeType { get; set; }
        public bool HasDolbyVision { get; set; }
        public int? DolbyVisionProfile { get; set; }
        public string? DolbyVisionFallback { get; set; }

        // Deduplication flags — only record each event type once per session
        public bool ItemMismatchRecorded { get; set; }
        public bool FormatMismatchRecorded { get; set; }
        public bool HdrIssueRecorded { get; set; }
    }
}
