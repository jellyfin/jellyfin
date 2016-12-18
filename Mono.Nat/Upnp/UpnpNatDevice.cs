//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
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

using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Dlna;

namespace Mono.Nat.Upnp
{
    public sealed class UpnpNatDevice : AbstractNatDevice, IEquatable<UpnpNatDevice>
    {
        private EndPoint hostEndPoint;
        private IPAddress localAddress;
        private string serviceDescriptionUrl;
        private string controlUrl;
        private string serviceType;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public override IPAddress LocalAddress
        {
            get { return localAddress; }
        }

        internal UpnpNatDevice(IPAddress localAddress, UpnpDeviceInfo deviceInfo, IPEndPoint hostEndPoint, string serviceType, ILogger logger, IHttpClient httpClient)
        {
            this.LastSeen = DateTime.Now;
            this.localAddress = localAddress;

            // Split the string at the "location" section so i can extract the ipaddress and service description url
            string locationDetails = deviceInfo.Location.ToString();
            this.serviceType = serviceType;
            _logger = logger;
            _httpClient = httpClient;

            // Make sure we have no excess whitespace
            locationDetails = locationDetails.Trim();

            // FIXME: Is this reliable enough. What if we get a hostname as opposed to a proper http address
            // Are we going to get addresses with the "http://" attached?
            if (locationDetails.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                NatUtility.Log("Found device at: {0}", locationDetails);
                // This bit strings out the "http://" from the string
                locationDetails = locationDetails.Substring(7);

                this.hostEndPoint = hostEndPoint;

                NatUtility.Log("Parsed device as: {0}", this.hostEndPoint.ToString());

                // The service description URL is the remainder of the "locationDetails" string. The bit that was originally after the ip
                // and port information
                this.serviceDescriptionUrl = locationDetails.Substring(locationDetails.IndexOf('/'));
            }
            else
            {
                NatUtility.Log("Couldn't decode address. Please send following string to the developer: ");
            }
        }

        internal UpnpNatDevice(IPAddress localAddress, string deviceDetails, string serviceType, ILogger logger, IHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            this.LastSeen = DateTime.Now;
            this.localAddress = localAddress;

            // Split the string at the "location" section so i can extract the ipaddress and service description url
            string locationDetails = deviceDetails.Substring(deviceDetails.IndexOf("Location", StringComparison.OrdinalIgnoreCase) + 9).Split('\r')[0];
            this.serviceType = serviceType;

            // Make sure we have no excess whitespace
            locationDetails = locationDetails.Trim();

            // FIXME: Is this reliable enough. What if we get a hostname as opposed to a proper http address
            // Are we going to get addresses with the "http://" attached?
            if (locationDetails.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                NatUtility.Log("Found device at: {0}", locationDetails);
                // This bit strings out the "http://" from the string
                locationDetails = locationDetails.Substring(7);

                // We then split off the end of the string to get something like: 192.168.0.3:241 in our string
                string hostAddressAndPort = locationDetails.Remove(locationDetails.IndexOf('/'));

                // From this we parse out the IP address and Port
                if (hostAddressAndPort.IndexOf(':') > 0)
                {
                    this.hostEndPoint = new IPEndPoint(IPAddress.Parse(hostAddressAndPort.Remove(hostAddressAndPort.IndexOf(':'))),
                    Convert.ToUInt16(hostAddressAndPort.Substring(hostAddressAndPort.IndexOf(':') + 1), System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    // there is no port specified, use default port (80)
                    this.hostEndPoint = new IPEndPoint(IPAddress.Parse(hostAddressAndPort), 80);
                }

                NatUtility.Log("Parsed device as: {0}", this.hostEndPoint.ToString());

                // The service description URL is the remainder of the "locationDetails" string. The bit that was originally after the ip
                // and port information
                this.serviceDescriptionUrl = locationDetails.Substring(locationDetails.IndexOf('/'));
            }
            else
            {
                logger.Warn("Couldn't decode address: " + deviceDetails);
            }
        }

        /// <summary>
        /// The EndPoint that the device is at
        /// </summary>
        internal EndPoint HostEndPoint
        {
            get { return this.hostEndPoint; }
        }

        /// <summary>
        /// The relative url of the xml file that describes the list of services is at
        /// </summary>
        internal string ServiceDescriptionUrl
        {
            get { return this.serviceDescriptionUrl; }
        }

        /// <summary>
        /// The relative url that we can use to control the port forwarding
        /// </summary>
        internal string ControlUrl
        {
            get { return this.controlUrl; }
        }

        /// <summary>
        /// The service type we're using on the device
        /// </summary>
        public string ServiceType
        {
            get { return serviceType; }
        }

        public override Task CreatePortMap(Mapping mapping)
        {
            CreatePortMappingMessage message = new CreatePortMappingMessage(mapping, localAddress, this);
            return _httpClient.SendAsync(message.Encode(), message.Method);
        }

        public override bool Equals(object obj)
        {
            UpnpNatDevice device = obj as UpnpNatDevice;
            return (device == null) ? false : this.Equals((device));
        }


        public bool Equals(UpnpNatDevice other)
        {
            return (other == null) ? false : (this.hostEndPoint.Equals(other.hostEndPoint)
                //&& this.controlUrl == other.controlUrl
                && this.serviceDescriptionUrl == other.serviceDescriptionUrl);
        }

        public override int GetHashCode()
        {
            return (this.hostEndPoint.GetHashCode() ^ this.controlUrl.GetHashCode() ^ this.serviceDescriptionUrl.GetHashCode());
        }

        /// <summary>
        /// Overridden.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            //GetExternalIP is blocking and can throw exceptions, can't use it here.
            return String.Format(
                "UpnpNatDevice - EndPoint: {0}, External IP: {1}, Control Url: {2}, Service Description Url: {3}, Service Type: {4}, Last Seen: {5}",
                this.hostEndPoint, "Manually Check" /*this.GetExternalIP()*/, this.controlUrl, this.serviceDescriptionUrl, this.serviceType, this.LastSeen);
        }
    }
}