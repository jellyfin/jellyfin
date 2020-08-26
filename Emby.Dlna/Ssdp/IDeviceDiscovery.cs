#nullable enable
using System;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;

namespace Emby.Dlna.Ssdp
{
    /// <summary>
    /// Object responsible for discovering ssdp devices on the LAN.
    /// </summary>
    public interface IDeviceDiscovery
    {
        /// <summary>
        /// Event triggered every time a ssdp device is discovered.
        /// </summary>
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered;

        /// <summary>
        /// Event triggered every time a ssdp device notifies it is leaving.
        /// </summary>
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;

        /// <summary>
        /// Dispose method.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Starts the discovery of ssdp netwwork devices.
        /// </summary>
        void Start();
    }
}
