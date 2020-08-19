#nullable enable
using System;
using Emby.Dlna.Rssdp.EventArgs;

namespace Emby.Dlna.Rssdp
{
    /// <summary>
    /// Interface for components that discover the existence of SSDP devices.
    /// </summary>
    /// <remarks>
    /// <para>Discovering devices includes explicit search requests as well as listening for broadcast status notifications.</para>
    /// </remarks>
    public interface ISsdpPlayToLocator
    {
        /// <summary>
        /// Event raised when a device becomes available or is found by a search request.
        /// </summary>
        event EventHandler<DeviceAvailableEventArgs>? DeviceAvailable;

        /// <summary>
        /// Event raised when a device explicitly notifies of shutdown or a device expires from the cache.
        /// </summary>
        event EventHandler<DeviceUnavailableEventArgs>? DeviceUnavailable;
    }
}
