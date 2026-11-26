using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Selectable offline download audio and subtitle tracks for a video item.
/// </summary>
public sealed class OfflineDownloadOptionsResult
{
    /// <summary>
    /// Gets or sets the selectable audio tracks.
    /// </summary>
    public IReadOnlyList<OfflineDownloadAudioTrack> AudioTracks { get; set; } = Array.Empty<OfflineDownloadAudioTrack>();

    /// <summary>
    /// Gets or sets the selectable subtitle tracks.
    /// </summary>
    public IReadOnlyList<OfflineDownloadSubtitleTrack> SubtitleTracks { get; set; } = Array.Empty<OfflineDownloadSubtitleTrack>();
}
