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
    /// Gets an unexpired grant whose owner exists and is enabled.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The grant, or <c>null</c> if unavailable.</returns>
    PlaybackAccessGrant? Get(string token);

    /// <summary>
    /// Revokes all grants for the user's play session.
    /// </summary>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="userId">The user id.</param>
    void Revoke(string playSessionId, Guid userId);
}
