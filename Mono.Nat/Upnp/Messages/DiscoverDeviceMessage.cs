namespace Mono.Nat.Upnp
{
    using System.Net;
    using System.Text;

    /// <summary>
    /// Defines the <see cref="DiscoverDeviceMessage" />.
    /// </summary>
    internal static class DiscoverDeviceMessage
    {
        /// <summary>
        /// The EncodeUnicast.
        /// </summary>
        /// <param name="gatewayAddress">The gatewayAddress<see cref="IPAddress"/>.</param>
        /// <returns>A byte array.</returns>
        public static byte[] EncodeUnicast(IPAddress gatewayAddress)
        {
            // Format obtained from http://upnp.org/specs/arch/UPnP-arch-DeviceArchitecture-v1.1.pdf pg 31
            // This method only works with upnp 1.1 routers... unfortunately
            string s = "M-SEARCH * HTTP/1.1\r\n"
                        + "HOST: " + gatewayAddress + ":1900\r\n"
                        + "MAN: \"ssdp:discover\"\r\n"
                        + "ST: ssdp:all\r\n\r\n";
            //// + "USER-AGENT: unix/5.1 UPnP/1.1 MyProduct/1.0\r\n\r\n";
            return Encoding.ASCII.GetBytes(s);
        }

        public static string EncodeUnicastEmpty(IPAddress gatewayAddress)
        {
            // Format obtained from http://upnp.org/specs/arch/UPnP-arch-DeviceArchitecture-v1.1.pdf pg 31
            // This method only works with upnp 1.1 routers... unfortunately
            return "M-SEARCH * HTTP/1.1\r\n"
                        + "HOST: " + gatewayAddress + ":1900\r\n"
                        + "MAN: \"ssdp:discover\"\r\n"
                        + "ST: {0}\r\n";
            //// + "USER-AGENT: unix/5.1 UPnP/1.1 MyProduct/1.0\r\n\r\n";
        }

        /// <summary>
        /// The message sent to discover all uPnP devices on the network.
        /// </summary>
        /// <returns>.</returns>
        public static byte[] EncodeSSDP()
        {
            string s = "M-SEARCH * HTTP/1.1\r\n"
                        + "HOST: 239.255.255.250:1900\r\n"
                        + "MAN: \"ssdp:discover\"\r\n"
                        + "MX: 3\r\n"
                        + "ST: ssdp:all\r\n\r\n";
            return Encoding.ASCII.GetBytes(s);
        }

        public static string EncodeSSDPEmpty()
        {
            return "M-SEARCH * HTTP/1.1\r\n"
                        + "HOST: 239.255.255.250:1900\r\n"
                        + "MAN: \"ssdp:discover\"\r\n"
                        + "MX: 3\r\n"
                        + "ST: {0}\r\n";
        }
    }
}
