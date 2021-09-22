#pragma warning disable CS1591

using System;
using Jellyfin.Data.Events;

namespace MediaBrowser.Model.Dlna
{
    public interface IDeviceDiscovery
    {
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered;

        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;
    }
}
