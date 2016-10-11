using System;
using System.Collections.Generic;
using System.Net;
using MediaBrowser.Model.Events;

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
        public IPEndPoint LocalEndPoint { get; set; }
    }
}
