#pragma warning disable CS1591
using System;
using Jellyfin.Data.Events;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.PlayTo
{
    public interface ISsdpPlayToLocator
    {
         /// <summary>
        /// Raised when a new device is discovered.
        /// </summary>
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered;

        /// <summary>
        /// Raised when a notification is received that indicates a device has shutdown or otherwise become unavailable.
        /// </summary>
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;

        void Dispose();

        void Start();
    }
}
