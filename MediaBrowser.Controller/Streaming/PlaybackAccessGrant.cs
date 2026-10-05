using System;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// Delegates read access to the resources of one on-demand playback session.
/// </summary>
/// <param name="Token">The opaque playback credential.</param>
/// <param name="UserId">The user whose playback permissions apply.</param>
/// <param name="ItemId">The authorized item.</param>
/// <param name="MediaSourceId">The authorized media source.</param>
/// <param name="DeviceId">The renderer's device identifier.</param>
/// <param name="PlaySessionId">The authorized playback session.</param>
/// <param name="ExpiresAt">The absolute expiry, regardless of activity.</param>
public sealed record PlaybackAccessGrant(
    string Token,
    Guid UserId,
    Guid ItemId,
    string MediaSourceId,
    string DeviceId,
    string PlaySessionId,
    DateTimeOffset ExpiresAt);
