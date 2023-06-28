using System;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Options object for storing the download limit value.
/// </summary>
public class BandwidthLimitOption
{
    /// <summary>
    /// Gets or sets the Guid of the users id.
    /// </summary>
    public Guid User { get; set; }

    /// <summary>
    /// Gets or sets the bandwidth per second for a download.
    /// </summary>
    public long BandwidthPerSec { get; set; }
}
