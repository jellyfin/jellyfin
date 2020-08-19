using System;
using Emby.Dlna.Rssdp.Devices;

namespace Emby.Dlna.Rssdp.EventArgs
{
    /// <summary>
    /// Event arguments for the <see cref="Rsddp.ISsdpPlayToLocator.DeviceUnavailable"/> event.
    /// </summary>
    public sealed class DeviceUnavailableEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceUnavailableEventArgs"/> class.
        /// </summary>
        /// <param name="discoveredDevice">A <see cref="DiscoveredSsdpDevice"/> instance representing the device that has become unavailable.</param>
        /// <param name="expired">A boolean value indicating whether this device is unavailable because it expired, or because it explicitly
        /// sent a byebye notification.. See <see cref="Expired"/> for more detail.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="discoveredDevice"/> parameter is null.</exception>
        public DeviceUnavailableEventArgs(DiscoveredSsdpDevice discoveredDevice, bool expired)
        {
            DiscoveredDevice = discoveredDevice ?? throw new ArgumentNullException(nameof(discoveredDevice));
            Expired = expired;
        }

        /// <summary>
        /// Gets a value indicating whether the device is considered unavailable because it's cached information expired before a new alive
        /// notification or search result was received. Returns false if the device is unavailable because it sent an explicit notification of it's unavailability.
        /// </summary>
        public bool Expired { get; }

        /// <summary>
        /// Gets a reference to a <see cref="DiscoveredSsdpDevice"/> instance containing the discovery details of the removed device.
        /// </summary>
        public DiscoveredSsdpDevice DiscoveredDevice { get; }
    }
}
