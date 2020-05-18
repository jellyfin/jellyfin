#pragma warning disable CS1591
#pragma warning disable CA2227 // Collection properties should be read only

using System;
using System.Collections.Generic;
using System.Net;

namespace MediaBrowser.Model.Dlna
{
    public class UpnpDeviceInfo
    {
        public Uri Location { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public IPAddress LocalIpAddress { get; set; }

        public int LocalPort { get; set; }
    }
}
