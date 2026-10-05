using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// Manages playback grants, which let a device stream an item without the user's access token.
/// </summary>
public interface IPlaybackAccessManager
{
    /// <summary>
    /// Creates a grant for a media source the user is allowed to play.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="deviceId">The device id.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The grant.</returns>
    Task<PlaybackAccessGrant> CreateAsync(Guid userId, Guid itemId, string mediaSourceId, string deviceId, string playSessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a valid grant.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The grant, or <c>null</c> if it is unknown, expired or its user is disabled.</returns>
    PlaybackAccessGrant? Get(string token);

    /// <summary>
    /// Revokes the grants of a play session.
    /// </summary>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="userId">The user id.</param>
    void Revoke(string playSessionId, Guid userId);
}
