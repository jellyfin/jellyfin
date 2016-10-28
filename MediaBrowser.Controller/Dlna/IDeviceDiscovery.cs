using System;
using System.Collections.Generic;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Net;

namespace MediaBrowser.Controller.Dlna
{
    public interface IDeviceDiscovery
    {
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered;
        event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;
    }

    public class UpnpDeviceInfo
    {
        public Uri Location { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public IpAddressInfo LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
    }
}
