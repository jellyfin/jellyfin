namespace MediaBrowser.Controller.ShareLinks;

/// <summary>
/// The reason a share link token could not be created.
/// </summary>
public enum ShareLinkCreateErrorCode
{
    /// <summary>
    /// The creating user does not exist or is disabled.
    /// </summary>
    UserDisabled,

    /// <summary>
    /// Neither an absolute expiration nor a duration was supplied.
    /// </summary>
    ExpirationOrDurationRequired,

    /// <summary>
    /// The absolute expiration is in the past or further than one year in the future.
    /// </summary>
    InvalidExpiration,

    /// <summary>
    /// The duration is outside the allowed range.
    /// </summary>
    InvalidDuration,

    /// <summary>
    /// The creating user already has the maximum number of active share links.
    /// </summary>
    ActiveLinkLimitReached
}
