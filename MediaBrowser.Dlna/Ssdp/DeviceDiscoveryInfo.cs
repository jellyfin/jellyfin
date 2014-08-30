using MediaBrowser.Dlna.PlayTo;
using System;
using System.Net;

namespace MediaBrowser.Dlna.Ssdp
{
    public class DeviceDiscoveryInfo
    {
        public Device Device { get; set; }

        /// <summary>
        /// The server's ip address that the device responded to
        /// </summary>
        public IPAddress LocalIpAddress { get; set; }

        public Uri Uri { get; set; }

        public string Usn { get; set; }
        public string Nt { get; set; }
    }
}
