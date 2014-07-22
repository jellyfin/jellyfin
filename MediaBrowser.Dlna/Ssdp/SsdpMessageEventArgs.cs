using System;
using System.Collections.Generic;
using System.Net;

namespace MediaBrowser.Dlna.Ssdp
{
    public class SsdpMessageEventArgs
    {
        public string Method { get; set; }

        public IPEndPoint EndPoint { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public IPAddress LocalIp { get; set; }

        public SsdpMessageEventArgs()
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
