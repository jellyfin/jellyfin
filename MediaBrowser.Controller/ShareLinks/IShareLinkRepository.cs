using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.ShareLinks;

/// <summary>
/// Repository for scoped media share links.
/// </summary>
public interface IShareLinkRepository
{
    /// <summary>
    /// Gets a share link by its Paseto token id (jti).
    /// </summary>
    /// <param name="jti">The Paseto token id (jti) of the share link.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The share link, or <c>null</c> if it does not exist.</returns>
    Task<ShareLink?> GetByIdAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new share link.
    /// </summary>
    /// <param name="shareLink">The share link to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateAsync(ShareLink shareLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically sets the first access timestamp of a share link.
    /// </summary>
    /// <param name="jti">The Paseto token id (jti) of the share link.</param>
    /// <param name="unixSecondsFirstAccessed">The first access timestamp in unix seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if the timestamp was set by this call, <c>false</c> if the link is already accessed or does not exist.</returns>
    Task<bool> TrySetFirstAccessAsync(string jti, long unixSecondsFirstAccessed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a share link.
    /// </summary>
    /// <param name="jti">The Paseto token id (jti) of the share link.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if a share link was revoked, <c>false</c> if it does not exist.</returns>
    Task<bool> RevokeAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the share links created by a user.
    /// </summary>
    /// <param name="userId">The id of the creating user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The share links, ordered by creation time descending.</returns>
    Task<IReadOnlyList<ShareLink>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the share links created by a user for a specific media item.
    /// </summary>
    /// <param name="userId">The id of the creating user.</param>
    /// <param name="itemId">The id of the media item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The share links, ordered by creation time descending.</returns>
    Task<IReadOnlyList<ShareLink>> ListByItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all share links created by a user.
    /// </summary>
    /// <param name="userId">The id of the creating user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteUserLinksAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes share links that are revoked, absolutely expired or whose duration has elapsed.
    /// </summary>
    /// <param name="unixNow">The current timestamp in unix seconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted share links.</returns>
    Task<int> PurgeExpiredAsync(long unixNow, CancellationToken cancellationToken = default);
}
