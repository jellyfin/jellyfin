#nullable disable
#pragma warning disable CS1591

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
