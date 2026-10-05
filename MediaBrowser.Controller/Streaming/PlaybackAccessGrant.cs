using System;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// Access to the streams of a single playback, without the permissions of the user's account.
/// </summary>
/// <param name="Token">The token.</param>
/// <param name="UserId">The user the playback belongs to.</param>
/// <param name="ItemId">The item id.</param>
/// <param name="MediaSourceId">The media source id.</param>
/// <param name="DeviceId">The device id.</param>
/// <param name="PlaySessionId">The play session id.</param>
/// <param name="ExpiresAt">The expiry date.</param>
public record PlaybackAccessGrant(
    string Token,
    Guid UserId,
    Guid ItemId,
    string MediaSourceId,
    string DeviceId,
    string PlaySessionId,
    DateTime ExpiresAt);
