using System;

namespace MediaBrowser.Controller.Dlna
{
    public interface IDeviceDiscovery
    {
        event EventHandler<SsdpMessageEventArgs> DeviceDiscovered;
        event EventHandler<SsdpMessageEventArgs> DeviceLeft;
    }
}
