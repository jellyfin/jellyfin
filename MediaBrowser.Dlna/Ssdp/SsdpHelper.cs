using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaBrowser.Dlna.Ssdp
{
    public class SsdpHelper
    {
        private const string SsdpRenderer = "M-SEARCH * HTTP/1.1\r\n" +
                                             "HOST: 239.255.255.250:1900\r\n" +
                                             "User-Agent: UPnP/1.0 DLNADOC/1.50 Platinum/0.6.9.1\r\n" +
                                             "ST: urn:schemas-upnp-org:device:MediaRenderer:1\r\n" +
                                             "MAN: \"ssdp:discover\"\r\n" +
                                             "MX: {0}\r\n" +
                                             "\r\n";

        /// <summary>
        /// Creates a SSDP MSearch packet for DlnaRenderers.
        /// </summary>
        /// <param name="mx">The mx. (Delaytime for device before responding)</param>
        /// <returns></returns>
        public static byte[] CreateRendererSSDP(int mx)
        {
            return Encoding.UTF8.GetBytes(string.Format(SsdpRenderer, mx));
        }

        /// <summary>
        /// Parses the socket response into a location Uri for the DeviceDescription.xml.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static Dictionary<string,string> ParseSsdpResponse(byte[] data)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var reader = new StreamReader(new MemoryStream(data), Encoding.ASCII))
            {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    var parts = line.Split(new[] { ':' }, 2);

                    if (parts.Length == 2)
                    {
                        headers[parts[0]] = parts[1].Trim();
                    }
                }
            }
            
            return headers;
        }
    }
}
