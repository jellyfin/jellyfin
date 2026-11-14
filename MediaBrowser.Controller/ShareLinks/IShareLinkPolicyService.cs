using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.ShareLinks;

/// <summary>
/// Evaluates whether a user may create and use scoped media share links.
/// </summary>
public interface IShareLinkPolicyService
{
    /// <summary>
    /// Evaluates the effective media sharing policy for a user.
    /// </summary>
    /// <param name="user">The user to evaluate.</param>
    /// <returns>
    /// <c>true</c> when the user may create share links and links created by them stay
    /// usable; <c>false</c> when media sharing is disabled for that user.
    /// </returns>
    bool IsSharingAllowed(User user);
}
