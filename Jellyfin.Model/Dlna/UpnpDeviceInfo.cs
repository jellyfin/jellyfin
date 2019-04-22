using System;
using System.Collections.Generic;
using Jellyfin.Model.Net;

namespace Jellyfin.Model.Dlna
{
    public class UpnpDeviceInfo
    {
        public Uri Location { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public IpAddressInfo LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
    }
}
