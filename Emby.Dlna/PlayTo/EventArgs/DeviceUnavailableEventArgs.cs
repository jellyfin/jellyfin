using System;
using Emby.Dlna.PlayTo.Devices;

namespace Emby.Dlna.PlayTo.EventArgs
{
    /// <summary>
    /// Event arguments for the <see cref="SsdpPlayToLocator.DeviceUnavailable"/> event.
    /// </summary>
    /// <remarks>
    /// Part of this code take from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public sealed class DeviceUnavailableEventArgs
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
