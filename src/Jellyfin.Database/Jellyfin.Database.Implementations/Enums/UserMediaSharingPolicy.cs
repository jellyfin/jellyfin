namespace Jellyfin.Database.Implementations.Enums;

/// <summary>
/// A user's own preference for creating scoped media share links.
/// </summary>
/// <remarks>
/// Stored on the user row as an integer. The value is only consulted when the
/// server-wide media sharing policy is one of the per-user policies; a hard
/// server-wide yes or no ignores it.
/// </remarks>
public enum UserMediaSharingPolicy
{
    /// <summary>
    /// Inherit the server-wide default. This is the value for every existing and new user,
    /// and it means "not explicitly chosen".
    /// </summary>
    Default = 0,

    /// <summary>
    /// The user may create share links, when the server-wide policy permits per-user choice.
    /// </summary>
    Enabled = 1,

    /// <summary>
    /// The user may not create share links.
    /// </summary>
    Disabled = 2
}
