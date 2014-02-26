using System;
using System.Linq;
using System.Text;

namespace MediaBrowser.Dlna.PlayTo
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
        public static Uri ParseSsdpResponse(string data)
        {
            var res = (from line in data.Split(new[] { '\r', '\n' })
                       where line.ToLowerInvariant().StartsWith("location:")
                       select line).FirstOrDefault();

            return !string.IsNullOrEmpty(res) ? new Uri(res.Substring(9).Trim()) : null;
        }

        /// <summary>
        /// Parses data into SSDP event.        
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        [Obsolete("Not yet used", true)]
        public static string ParseSsdpEvent(string data)
        {
            var sid = (from line in data.Split(new[] { '\r', '\n' })
                       where line.ToLowerInvariant().StartsWith("sid:")
                       select line).FirstOrDefault();

            return data;
        }
    }
}
