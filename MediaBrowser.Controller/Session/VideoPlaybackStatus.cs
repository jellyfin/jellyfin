namespace MediaBrowser.Controller.Session;

/// <summary>
/// Describes the result of a video playback registry operation.
/// </summary>
public enum VideoPlaybackStatus
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// A new playback would exceed the user's limit.
    /// </summary>
    LimitReached,

    /// <summary>
    /// The play session or active admission was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The identifier is bound to a different user, session, item or playback.
    /// </summary>
    OwnershipMismatch,

    /// <summary>
    /// The playback expired without an active operation.
    /// </summary>
    Expired,

    /// <summary>
    /// The playback was stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// A newer encoding generation superseded the stop request.
    /// </summary>
    Superseded
}
