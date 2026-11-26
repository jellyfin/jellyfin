namespace Jellyfin.Api.Helpers;

/// <summary>
/// Metadata for a selectable offline audio track.
/// </summary>
/// <param name="Index">The media stream index.</param>
/// <param name="DisplayTitle">The user-facing track title.</param>
/// <param name="Language">The track language code, if known.</param>
/// <param name="Codec">The source audio codec.</param>
/// <param name="IsDefault">Whether this track is marked as default.</param>
public sealed record OfflineDownloadAudioTrack(
    int Index,
    string? DisplayTitle,
    string? Language,
    string? Codec,
    bool IsDefault);
