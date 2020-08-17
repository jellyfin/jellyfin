#nullable enable
using System;
using Emby.Dlna.Rssdp;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;

namespace Emby.Dlna.Ssdp
{
    public interface IDeviceDiscovery
    {
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered;

        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;

        void Dispose();

        void Start();
    }
}
