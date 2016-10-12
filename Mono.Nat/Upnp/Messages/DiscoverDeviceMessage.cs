//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Net;
using System.Text;

namespace Mono.Nat.Upnp
{
    internal static class DiscoverDeviceMessage
    {
        /// <summary>
        /// The message sent to discover all uPnP devices on the network
        /// </summary>
        /// <returns></returns>
        public static byte[] EncodeSSDP()
        {
            string s = "M-SEARCH * HTTP/1.1\r\n"
                        + "HOST: 239.255.255.250:1900\r\n"
                        + "MAN: \"ssdp:discover\"\r\n"
                        + "MX: 3\r\n"
                        + "ST: ssdp:all\r\n\r\n";
            return UTF8Encoding.ASCII.GetBytes(s);
        }

        public static byte[] EncodeUnicast(IPAddress gatewayAddress)
        {
            //Format obtained from http://upnp.org/specs/arch/UPnP-arch-DeviceArchitecture-v1.1.pdf pg 31
            //This method only works with upnp 1.1 routers... unfortunately
            string s = "M-SEARCH * HTTP/1.1\r\n"
                        + "HOST: " + gatewayAddress + ":1900\r\n"
                        + "MAN: \"ssdp:discover\"\r\n"
                        + "ST: ssdp:all\r\n\r\n";
                        //+ "USER-AGENT: unix/5.1 UPnP/1.1 MyProduct/1.0\r\n\r\n";
            return UTF8Encoding.ASCII.GetBytes(s);
        }
    }
}
