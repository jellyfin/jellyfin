using System;
using Microsoft.Extensions.Options;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Provides access to the Bitrate Limit option.
/// </summary>
public interface IBandwidthLimiterProviderService
{
    /// <summary>
    /// Will be invoked when a User was updated.
    /// </summary>
    event EventHandler<BandwidthLimitOptionEventArgs> BandwidthLimitUpdated;

    /// <summary>
    /// Provides the download speed limit option.
    /// </summary>
    /// <param name="user">The user to check the limit for.</param>
    /// <returns>An observable download speed option.</returns>
    BandwidthLimitOption GetLimit(Guid user);
}
