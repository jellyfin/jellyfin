using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.ShareLinks;

/// <summary>
/// Creates and validates scoped, encrypted share link tokens.
/// </summary>
public interface IShareLinkTokenService
{
    /// <summary>
    /// Creates a share link token for a single media item and persists its revocation state.
    /// </summary>
    /// <param name="userId">The id of the user creating the share link.</param>
    /// <param name="itemId">The id of the media item the share link grants access to.</param>
    /// <param name="expiresAtUnixSeconds">The absolute expiration timestamp in unix seconds, if any.</param>
    /// <param name="durationSeconds">The duration window in seconds, if any.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The creation result, carrying the token and the persisted share link on success.</returns>
    Task<ShareLinkCreationResult> CreateAsync(
        Guid userId,
        Guid itemId,
        long? expiresAtUnixSeconds,
        int? durationSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a share link token against the live user, the item scope and the persisted share link state.
    /// </summary>
    /// <param name="shareToken">The PASETO share token.</param>
    /// <param name="routeItemId">The media item id from the request route, if any.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result, carrying the token claims on success.</returns>
    Task<ShareLinkValidationResult> ValidateAsync(
        string shareToken,
        Guid? routeItemId,
        CancellationToken cancellationToken = default);
}
