using System;
using NetworkCollection.Ssdp;

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Defines the <see cref="ISsdpPlayToLocator" />.
    /// </summary>
    public interface ISsdpPlayToLocator
    {
        /// <summary>
        /// Raised when a new device is discovered.
        /// </summary>
        event EventHandler<SsdpDeviceInfo> DeviceDiscovered;

        /// <summary>
        /// Raised when a notification is received that indicates a device has shutdown or otherwise become unavailable.
        /// </summary>
        event EventHandler<SsdpDeviceInfo> DeviceLeft;

        /// <summary>
        /// Disposes the listener.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Starts the listener.
        /// </summary>
        void Start();
    }
}
