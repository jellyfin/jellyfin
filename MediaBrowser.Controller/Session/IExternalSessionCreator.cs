using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;

namespace MediaBrowser.Controller.Session;

/// <summary>
/// Creates Jellyfin sessions for externally authenticated users.
/// </summary>
public interface IExternalSessionCreator
{
    /// <summary>
    /// Creates a Jellyfin authentication result for an externally authenticated user.
    /// </summary>
    /// <param name="request">The external authentication request.</param>
    /// <returns>The Jellyfin authentication result.</returns>
    Task<AuthenticationResult> CreateExternalSession(ExternalAuthenticationRequest request);
}
