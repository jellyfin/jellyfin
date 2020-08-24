using System;
using System.Net;
using Emby.Dlna.PlayTo.Devices;

namespace Emby.Dlna.PlayTo.EventArgs
{
    /// <summary>
    /// Event arguments for the <see cref="SsdpPlayToLocator.DeviceAvailable"/> event.
    /// </summary>
    /// <remarks>
    /// Part of this code take from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public sealed class DeviceAvailableEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAvailableEventArgs"/> class.
        /// </summary>
        /// <param name="discoveredDevice">A <see cref="DiscoveredSsdpDevice"/> instance representing the available device.</param>
        /// <param name="isNewlyDiscovered">A boolean value indicating whether or not this device came from the cache. See <see cref="IsNewlyDiscovered"/> for more detail.</param>
        /// <param name="ipAddress">Local IP Address.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="discoveredDevice"/> parameter is null.</exception>
        public DeviceAvailableEventArgs(DiscoveredSsdpDevice discoveredDevice, bool isNewlyDiscovered, IPAddress ipAddress)
        {
            DiscoveredDevice = discoveredDevice ?? throw new ArgumentNullException(nameof(discoveredDevice));
            IsNewlyDiscovered = isNewlyDiscovered;
            LocalIpAddress = ipAddress;
        }

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public IPAddress LocalIpAddress { get; set; }

        /// <summary>
        /// Gets a value indicating whether the device was discovered due to an alive notification, or a search and was not already in the cache.
        /// Returns false if the item came from the cache but matched the current search request.
        /// </summary>
        public bool IsNewlyDiscovered { get; }

        /// <summary>
        /// Gets a reference to a <see cref="DiscoveredSsdpDevice"/> instance containing the discovered details and allowing access to the full device description.
        /// </summary>
        public DiscoveredSsdpDevice DiscoveredDevice { get; }
    }
}
