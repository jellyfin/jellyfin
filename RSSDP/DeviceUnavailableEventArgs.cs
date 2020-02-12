using System;

namespace Rssdp
{
    /// <summary>
    /// Event arguments for the <see cref="Infrastructure.SsdpDeviceLocatorBase.DeviceUnavailable"/> event.
    /// </summary>
    public sealed class DeviceUnavailableEventArgs : EventArgs
    {

        #region Fields

        private readonly DiscoveredSsdpDevice _DiscoveredDevice;
        private readonly bool _Expired;

        #endregion

        #region Constructors

        /// <summary>
        /// Full constructor.
        /// </summary>
        /// <param name="discoveredDevice">A <see cref="DiscoveredSsdpDevice"/> instance representing the device that has become unavailable.</param>
        /// <param name="expired">A boolean value indicating whether this device is unavailable because it expired, or because it explicitly sent a byebye notification.. See <see cref="Expired"/> for more detail.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="discoveredDevice"/> parameter is null.</exception>
        public DeviceUnavailableEventArgs(DiscoveredSsdpDevice discoveredDevice, bool expired)
        {
            if (discoveredDevice == null) throw new ArgumentNullException(nameof(discoveredDevice));

            _DiscoveredDevice = discoveredDevice;
            _Expired = expired;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns true if the device is considered unavailable because it's cached information expired before a new alive notification or search result was received. Returns false if the device is unavailable because it sent an explicit notification of it's unavailability.
        /// </summary>
        public bool Expired
        {
            get { return _Expired; }
        }

        /// <summary>
        /// A reference to a <see cref="DiscoveredSsdpDevice"/> instance containing the discovery details of the removed device.
        /// </summary>
        public DiscoveredSsdpDevice DiscoveredDevice
        {
            get { return _DiscoveredDevice; }
        }

        #endregion
    }
}
