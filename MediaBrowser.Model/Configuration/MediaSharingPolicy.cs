namespace MediaBrowser.Model.Configuration;

/// <summary>
/// The server-wide policy that controls who may create scoped media share links.
/// </summary>
/// <remarks>
/// A share link is a bearer credential: whoever holds the URL can stream the scoped
/// item until the link expires or is revoked. Enabling media sharing therefore also
/// enables the exposure risk described on <see cref="ServerConfiguration.MediaSharingPolicy"/>.
/// </remarks>
public enum MediaSharingPolicy
{
    /// <summary>
    /// Media sharing is disabled for every user. This is the default.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Media sharing is decided per user. Users that have not made an explicit choice
    /// are treated as disabled.
    /// </summary>
    PerUserDefaultNo = 1,

    /// <summary>
    /// Media sharing is decided per user. Users that have not made an explicit choice
    /// are treated as enabled.
    /// </summary>
    PerUserDefaultYes = 2,

    /// <summary>
    /// Media sharing is enabled for every user. The per-user preference is ignored and
    /// cannot be edited.
    /// </summary>
    Enabled = 3
}
