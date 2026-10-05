using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// Issues playback credentials without granting account or administrative access.
/// Trusted server callers must obtain the user from the authorized playback command.
/// </summary>
public interface IPlaybackAccessManager
{
    /// <summary>
    /// Authorizes an on-demand media source and creates a playback grant.
    /// </summary>
    /// <param name="userId">The initiating user.</param>
    /// <param name="itemId">The item to play.</param>
    /// <param name="mediaSourceId">The media source to play.</param>
    /// <param name="deviceId">The renderer identifier, used for playback accounting.</param>
    /// <param name="playSessionId">A unique playback session identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The playback grant.</returns>
    Task<PlaybackAccessGrant> CreateAsync(Guid userId, Guid itemId, string mediaSourceId, string deviceId, string playSessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an unexpired grant whose owner still has playback access.
    /// Does not extend its lifetime.
    /// </summary>
    /// <param name="token">The playback credential.</param>
    /// <returns>The grant, or null when invalid.</returns>
    PlaybackAccessGrant? Get(string token);

    /// <summary>
    /// Records a successfully authorized resource request, within the absolute expiry.
    /// </summary>
    /// <param name="token">The playback credential.</param>
    void Touch(string token);

    /// <summary>
    /// Ends a user's playback grants, allowing outstanding requests a short grace period.
    /// </summary>
    /// <param name="playSessionId">The playback session identifier.</param>
    /// <param name="userId">The owner of the playback session.</param>
    void Revoke(string playSessionId, Guid userId);
}
