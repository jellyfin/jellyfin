using System;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// The event arguments for the <see cref="IBandwidthLimiterProviderService.BandwidthLimitUpdated"/> event.
/// </summary>
public class BandwidthLimitOptionEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BandwidthLimitOptionEventArgs"/> class.
    /// </summary>
    /// <param name="bandwidthLimitOption">The <see cref="BandwidthLimitOption"/>.</param>
    public BandwidthLimitOptionEventArgs(BandwidthLimitOption bandwidthLimitOption)
    {
        BandwidthLimitOption = bandwidthLimitOption;
    }

    /// <summary>
    /// Gets the updated <see cref="BandwidthLimitOption"/>.
    /// </summary>
    public BandwidthLimitOption BandwidthLimitOption { get; private set; }
}
