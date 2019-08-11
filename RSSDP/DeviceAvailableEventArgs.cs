using System;
using System.Net;

namespace Rssdp
{
    /// <summary>
    /// Event arguments for the <see cref="Infrastructure.SsdpDeviceLocatorBase.DeviceAvailable"/> event.
    /// </summary>
    public sealed class DeviceAvailableEventArgs : EventArgs
    {
        public IPAddress LocalIpAddress { get; set; }

        #region Fields

        private readonly DiscoveredSsdpDevice _DiscoveredDevice;
        private readonly bool _IsNewlyDiscovered;

        #endregion

        #region Constructors

        /// <summary>
        /// Full constructor.
        /// </summary>
        /// <param name="discoveredDevice">A <see cref="DiscoveredSsdpDevice"/> instance representing the available device.</param>
        /// <param name="isNewlyDiscovered">A boolean value indicating whether or not this device came from the cache. See <see cref="IsNewlyDiscovered"/> for more detail.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="discoveredDevice"/> parameter is null.</exception>
        public DeviceAvailableEventArgs(DiscoveredSsdpDevice discoveredDevice, bool isNewlyDiscovered)
        {
            if (discoveredDevice == null) throw new ArgumentNullException(nameof(discoveredDevice));

            _DiscoveredDevice = discoveredDevice;
            _IsNewlyDiscovered = isNewlyDiscovered;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns true if the device was discovered due to an alive notification, or a search and was not already in the cache. Returns false if the item came from the cache but matched the current search request.
        /// </summary>
        public bool IsNewlyDiscovered
        {
            get { return _IsNewlyDiscovered; }
        }

        /// <summary>
        /// A reference to a <see cref="DiscoveredSsdpDevice"/> instance containing the discovered details and allowing access to the full device description.
        /// </summary>
        public DiscoveredSsdpDevice DiscoveredDevice
        {
            get { return _DiscoveredDevice; }
        }

        #endregion

    }
}
