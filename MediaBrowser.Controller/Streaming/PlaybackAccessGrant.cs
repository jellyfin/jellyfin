using System;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// A scoped grant for HLS playback.
/// </summary>
/// <param name="Token">The opaque playback token.</param>
/// <param name="UserId">The user the playback belongs to.</param>
/// <param name="ItemId">The item id.</param>
/// <param name="MediaSourceId">The media source id.</param>
/// <param name="DeviceId">The device id.</param>
/// <param name="PlaySessionId">The play session id.</param>
/// <param name="ExpiresAt">The expiration time in UTC.</param>
public record PlaybackAccessGrant(
    string Token,
    Guid UserId,
    Guid ItemId,
    string MediaSourceId,
    string DeviceId,
    string PlaySessionId,
    DateTime ExpiresAt);
