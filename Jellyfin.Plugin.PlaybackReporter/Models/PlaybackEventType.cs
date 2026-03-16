namespace Jellyfin.Plugin.PlaybackReporter.Models;

/// <summary>
/// The type of playback problem event that was recorded.
/// </summary>
public enum PlaybackEventType
{
    /// <summary>
    /// A file failed to play, or a session stopped abnormally soon after starting.
    /// </summary>
    PlaybackFailure,

    /// <summary>
    /// The client requested a format that couldn't be direct-played and was transcoded.
    /// Captures what was requested versus what format was ultimately served.
    /// </summary>
    FormatMismatch,

    /// <summary>
    /// The client requested a specific media source (by ID) but a different source was played.
    /// </summary>
    ItemMismatch,

    /// <summary>
    /// High dynamic range content (HDR10, HDR10+, Dolby Vision, HLG, etc.) could not be
    /// direct-played on the client and required transcoding or tone-mapping.
    /// </summary>
    HdrPlaybackIssue,
}
