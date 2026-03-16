using System;
using System.Collections.Generic;
using MediaBrowser.Model.Session;

namespace Jellyfin.Plugin.PlaybackReporter.Models;

/// <summary>
/// Represents a single recorded playback problem event.
/// </summary>
public class PlaybackErrorEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the type of playback problem.
    /// </summary>
    public PlaybackEventType EventType { get; set; }

    // ── Session / Client ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the Jellyfin session identifier.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the play-session identifier (unique per play attempt).
    /// </summary>
    public string? PlaySessionId { get; set; }

    /// <summary>
    /// Gets or sets the name of the client application (e.g. "Jellyfin Web", "Infuse").
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the device identifier.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable device name.
    /// </summary>
    public string? DeviceName { get; set; }

    // ── Media Item ───────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the Jellyfin item identifier that was requested.
    /// </summary>
    public Guid RequestedItemId { get; set; }

    /// <summary>
    /// Gets or sets the title of the item that was requested.
    /// </summary>
    public string? RequestedItemName { get; set; }

    /// <summary>
    /// Gets or sets the media source ID that was requested by the client, if any.
    /// </summary>
    public string? RequestedMediaSourceId { get; set; }

    /// <summary>
    /// Gets or sets the media source ID that was actually played.
    /// Differs from <see cref="RequestedMediaSourceId"/> when an item mismatch occurs.
    /// </summary>
    public string? ActualMediaSourceId { get; set; }

    /// <summary>
    /// Gets or sets the actual item name that was played when it differs from the requested item.
    /// </summary>
    public string? ActualItemName { get; set; }

    // ── Playback Method ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the play method that was used (DirectPlay, DirectStream, Transcode).
    /// </summary>
    public PlayMethod? PlayMethod { get; set; }

    /// <summary>
    /// Gets or sets the transcode reasons flags, when transcoding occurred.
    /// </summary>
    public TranscodeReason? TranscodeReasons { get; set; }

    // ── Format / Codec ───────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the container format of the source file (e.g. "mkv", "mp4").
    /// </summary>
    public string? SourceContainer { get; set; }

    /// <summary>
    /// Gets or sets the video codec of the source file (e.g. "hevc", "av1").
    /// </summary>
    public string? SourceVideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the audio codec of the source file (e.g. "truehd", "eac3").
    /// </summary>
    public string? SourceAudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the container format that was actually sent to the client
    /// (may differ from source when transcoding).
    /// </summary>
    public string? DeliveredContainer { get; set; }

    /// <summary>
    /// Gets or sets the video codec that was delivered to the client.
    /// </summary>
    public string? DeliveredVideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the audio codec that was delivered to the client.
    /// </summary>
    public string? DeliveredAudioCodec { get; set; }

    // ── HDR / Dynamic Range ──────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the video range type of the source (e.g. "DOVI", "HDR10", "HDR10Plus", "HLG").
    /// </summary>
    public string? SourceVideoRangeType { get; set; }

    /// <summary>
    /// Gets or sets the video range (SDR / HDR) of the source.
    /// </summary>
    public string? SourceVideoRange { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source contains Dolby Vision metadata.
    /// </summary>
    public bool HasDolbyVision { get; set; }

    /// <summary>
    /// Gets or sets the Dolby Vision profile number (1, 4, 5, 7, 8, etc.), if applicable.
    /// </summary>
    public int? DolbyVisionProfile { get; set; }

    /// <summary>
    /// Gets or sets the Dolby Vision compatibility layer / fallback type.
    /// </summary>
    public string? DolbyVisionFallback { get; set; }

    // ── Playback Duration ────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets how long the session played before the error or stop (in seconds).
    /// Used to distinguish immediate failures from mid-stream errors.
    /// </summary>
    public double? PlayedDurationSeconds { get; set; }

    // ── Extra Context ────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets free-form additional context notes about the event.
    /// </summary>
    public List<string> Notes { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether a GitHub issue has already been filed for this event.
    /// </summary>
    public bool ReportedToGitHub { get; set; }

    /// <summary>
    /// Gets or sets the URL of the GitHub issue created for this event, if any.
    /// </summary>
    public string? GitHubIssueUrl { get; set; }
}
