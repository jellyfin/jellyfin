using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Server.Implementations.ShareLinks;

/// <summary>
/// Pure evaluation of the effective media sharing policy.
/// </summary>
/// <remarks>
/// Kept free of services and I/O so every global/per-user combination can be unit tested
/// directly. The precedence is:
/// <list type="bullet">
/// <item><see cref="MediaSharingPolicy.Disabled"/> and <see cref="MediaSharingPolicy.Enabled"/>
/// are hard server-wide answers and ignore the user preference.</item>
/// <item><see cref="MediaSharingPolicy.PerUserDefaultNo"/> allows only users who explicitly
/// enabled sharing.</item>
/// <item><see cref="MediaSharingPolicy.PerUserDefaultYes"/> allows everyone except users who
/// explicitly disabled sharing.</item>
/// </list>
/// </remarks>
public static class ShareLinkPolicyEvaluator
{
    /// <summary>
    /// Evaluates the effective media sharing policy for one user.
    /// </summary>
    /// <param name="serverPolicy">The server-wide policy.</param>
    /// <param name="userPolicy">The user's own preference.</param>
    /// <returns><c>true</c> when sharing is allowed for the user.</returns>
    public static bool IsSharingAllowed(MediaSharingPolicy serverPolicy, UserMediaSharingPolicy userPolicy)
    {
        return serverPolicy switch
        {
            MediaSharingPolicy.Enabled => true,
            MediaSharingPolicy.Disabled => false,
            MediaSharingPolicy.PerUserDefaultYes => !userPolicy.Equals(UserMediaSharingPolicy.Disabled),
            MediaSharingPolicy.PerUserDefaultNo => userPolicy.Equals(UserMediaSharingPolicy.Enabled),
            _ => false
        };
    }

    /// <summary>
    /// Gets a value indicating whether the server-wide policy is a hard answer that the
    /// per-user preference cannot override.
    /// </summary>
    /// <param name="serverPolicy">The server-wide policy.</param>
    /// <returns><c>true</c> for <see cref="MediaSharingPolicy.Enabled"/> and <see cref="MediaSharingPolicy.Disabled"/>.</returns>
    public static bool IsServerPolicyHard(MediaSharingPolicy serverPolicy)
    {
        return serverPolicy.Equals(MediaSharingPolicy.Enabled)
            || serverPolicy.Equals(MediaSharingPolicy.Disabled);
    }
}
