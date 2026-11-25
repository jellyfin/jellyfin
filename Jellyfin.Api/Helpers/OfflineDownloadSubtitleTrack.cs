namespace Jellyfin.Api.Helpers;

/// <summary>
/// Metadata for a selectable offline subtitle track.
/// </summary>
/// <param name="Index">The media stream index.</param>
/// <param name="DisplayTitle">The user-facing track title.</param>
/// <param name="Language">The track language code, if known.</param>
/// <param name="Codec">The source subtitle codec.</param>
/// <param name="IsDefault">Whether this track is marked as default.</param>
/// <param name="CanEmbed">Whether the subtitle can be embedded as switchable timed text in MP4.</param>
/// <param name="CanBurn">Whether the subtitle can be permanently burned into the video stream.</param>
public sealed record OfflineDownloadSubtitleTrack(
    int Index,
    string? DisplayTitle,
    string? Language,
    string? Codec,
    bool IsDefault,
    bool CanEmbed,
    bool CanBurn);
