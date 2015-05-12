using System;
using System.Collections.Generic;
using System.Net;

namespace MediaBrowser.Controller.Dlna
{
    public class SsdpMessageEventArgs
    {
        public string Method { get; set; }

        public EndPoint EndPoint { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public IPEndPoint LocalEndPoint { get; set; }
        public byte[] Message { get; set; }

        public SsdpMessageEventArgs()
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
