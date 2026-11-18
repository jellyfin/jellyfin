using System;

namespace MediaBrowser.Controller.Session;

/// <summary>
/// Identifies a logical video playback and its authenticated owner.
/// </summary>
/// <param name="UserId">The authenticated user identifier.</param>
/// <param name="SessionId">The server session identifier.</param>
/// <param name="ItemId">The canonical library item identifier.</param>
/// <param name="PlaybackInstanceId">The stable identifier within the user across encoding changes.</param>
public readonly record struct VideoPlaybackContext(
    Guid UserId,
    string SessionId,
    Guid ItemId,
    string PlaybackInstanceId);
