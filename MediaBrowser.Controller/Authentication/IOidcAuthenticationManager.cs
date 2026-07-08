using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Authentication;

namespace MediaBrowser.Controller.Authentication;

/// <summary>
/// Handles native OpenID Connect authentication.
/// </summary>
public interface IOidcAuthenticationManager
{
    /// <summary>
    /// Completes a validated OpenID Connect sign-in and returns a one-time exchange code.
    /// </summary>
    /// <param name="request">The external identity request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A one-time exchange code.</returns>
    Task<string> CompleteSignInAsync(OidcExternalIdentityRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges a one-time code for a Jellyfin authentication result.
    /// </summary>
    /// <param name="code">The one-time code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Jellyfin authentication result.</returns>
    Task<AuthenticationResult> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Gets linked external identities for a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The linked identities.</returns>
    Task<IReadOnlyList<ExternalIdentityDto>> GetExternalIdentitiesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an external identity link.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="identityId">The external identity id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the delete.</returns>
    Task DeleteExternalIdentityAsync(Guid userId, Guid identityId, CancellationToken cancellationToken);

    /// <summary>
    /// Logs out a Jellyfin session and optionally returns an upstream logout URL.
    /// </summary>
    /// <param name="accessToken">The Jellyfin access token.</param>
    /// <param name="returnUrl">The relative return URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The logout result.</returns>
    Task<OidcLogoutResult> LogoutAsync(string accessToken, string? returnUrl, CancellationToken cancellationToken);
}
