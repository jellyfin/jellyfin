using System;
using Jellyfin.Model.Events;

namespace Jellyfin.Model.Dlna
{
    public interface IDeviceDiscovery
    {
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered;
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;
    }
}
