using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Dlna.Ssdp
{
    public class SsdpMessageBuilder
    {
        public string BuildMessage(string header, Dictionary<string, string> values)
        {
            var builder = new StringBuilder();

            const string argFormat = "{0}: {1}\r\n";

            builder.AppendFormat("{0}\r\n", header);

            foreach (var pair in values)
            {
                builder.AppendFormat(argFormat, pair.Key, pair.Value);
            }

            builder.Append("\r\n");

            return builder.ToString();
        }

        public string BuildDiscoveryMessage(string deviceSearchType, string mx)
        {
            const string header = "M-SEARCH * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            values["HOST"] = "239.255.255.250:1900";
            values["USER-AGENT"] = "UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2";
            values["ST"] = "ssdp:all";
            values["MAN"] = "ssdp:discover";
            values["MX"] = "10";

            return BuildMessage(header, values);
        }

        public string BuildRendererDiscoveryMessage()
        {
            return BuildDiscoveryMessage("urn:schemas-upnp-org:device:MediaRenderer:1", "3");
        }

        public string BuildMediaServerDiscoveryMessage()
        {
            return BuildDiscoveryMessage("urn:schemas-upnp-org:device:MediaRenderer:1", "3");
        }
    }
}
