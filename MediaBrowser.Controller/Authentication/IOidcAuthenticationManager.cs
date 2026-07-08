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
    /// <param name="providerId">The provider id.</param>
    /// <param name="code">The one-time code.</param>
    /// <param name="remoteEndPoint">The remote endpoint of the exchange request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Jellyfin authentication result.</returns>
    Task<AuthenticationResult> ExchangeCodeAsync(string providerId, string code, string remoteEndPoint, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a one-time account-linking start code for an authenticated Jellyfin user.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="userId">The Jellyfin user id.</param>
    /// <param name="returnUrl">The optional relative return URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A one-time account-linking start code.</returns>
    Task<string> CreateLinkCodeAsync(string providerId, Guid userId, string? returnUrl, CancellationToken cancellationToken);

    /// <summary>
    /// Consumes a one-time account-linking start code.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <param name="code">The one-time account-linking start code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account-linking request.</returns>
    Task<OidcLinkRequest> ConsumeLinkCodeAsync(string providerId, string code, CancellationToken cancellationToken);

    /// <summary>
    /// Gets linked external identities for a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The linked identities.</returns>
    Task<IReadOnlyList<ExternalIdentityDto>> GetExternalIdentitiesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an explicit external identity link for a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The external identity link request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created identity.</returns>
    Task<ExternalIdentityDto> CreateExternalIdentityAsync(Guid userId, OidcExternalIdentityCreateRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Links a validated external identity to a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The validated external identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created identity.</returns>
    Task<ExternalIdentityDto> LinkExternalIdentityAsync(Guid userId, OidcExternalIdentityRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an external identity link.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="identityId">The external identity id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the delete.</returns>
    Task DeleteExternalIdentityAsync(Guid userId, Guid identityId, CancellationToken cancellationToken);
}
