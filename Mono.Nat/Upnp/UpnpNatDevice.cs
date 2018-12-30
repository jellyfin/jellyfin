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
using Microsoft.Extensions.Logging;
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
            if (localAddress == null)
            {
                throw new ArgumentNullException("localAddress");
            }

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
                _logger.LogDebug("Found device at: {0}", locationDetails);
                // This bit strings out the "http://" from the string
                locationDetails = locationDetails.Substring(7);

                this.hostEndPoint = hostEndPoint;

                // The service description URL is the remainder of the "locationDetails" string. The bit that was originally after the ip
                // and port information
                this.serviceDescriptionUrl = locationDetails.Substring(locationDetails.IndexOf('/'));
            }
            else
            {
                _logger.LogDebug("Couldn't decode address. Please send following string to the developer: ");
            }
        }

        public async Task GetServicesList()
        {
            // Create a HTTPWebRequest to download the list of services the device offers
            var message = new GetServicesMessage(this.serviceDescriptionUrl, this.hostEndPoint, _logger);

            using (var response = await _httpClient.SendAsync(message.Encode(), message.Method).ConfigureAwait(false))
            {
                OnServicesReceived(response);
            }
        }

        private void OnServicesReceived(HttpResponseInfo response)
        {
            int abortCount = 0;
            int bytesRead = 0;
            byte[] buffer = new byte[10240];
            StringBuilder servicesXml = new StringBuilder();
            XmlDocument xmldoc = new XmlDocument();

            using (var s = response.Content)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogDebug("{0}: Couldn't get services list: {1}", HostEndPoint, response.StatusCode);
                    return; // FIXME: This the best thing to do??
                }

                while (true)
                {
                    bytesRead = s.Read(buffer, 0, buffer.Length);
                    servicesXml.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    try
                    {
                        xmldoc.LoadXml(servicesXml.ToString());
                        break;
                    }
                    catch (XmlException)
                    {
                        // If we can't receive the entire XML within 500ms, then drop the connection
                        // Unfortunately not all routers supply a valid ContentLength (mine doesn't)
                        // so this hack is needed to keep testing our recieved data until it gets successfully
                        // parsed by the xmldoc. Without this, the code will never pick up my router.
                        if (abortCount++ > 50)
                        {
                            return;
                        }
                        _logger.LogDebug("{0}: Couldn't parse services list", HostEndPoint);
                        System.Threading.Thread.Sleep(10);
                    }
                }

                XmlNamespaceManager ns = new XmlNamespaceManager(xmldoc.NameTable);
                ns.AddNamespace("ns", "urn:schemas-upnp-org:device-1-0");
                XmlNodeList nodes = xmldoc.SelectNodes("//*/ns:serviceList", ns);

                foreach (XmlNode node in nodes)
                {
                    //Go through each service there
                    foreach (XmlNode service in node.ChildNodes)
                    {
                        //If the service is a WANIPConnection, then we have what we want
                        string type = service["serviceType"].InnerText;
                        _logger.LogDebug("{0}: Found service: {1}", HostEndPoint, type);

                        // TODO: Add support for version 2 of UPnP.
                        if (string.Equals(type, "urn:schemas-upnp-org:service:WANPPPConnection:1", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(type, "urn:schemas-upnp-org:service:WANIPConnection:1", StringComparison.OrdinalIgnoreCase))
                        {
                            this.controlUrl = service["controlURL"].InnerText;
                            _logger.LogDebug("{0}: Found upnp service at: {1}", HostEndPoint, controlUrl);

                            Uri u;
                            if (Uri.TryCreate(controlUrl, UriKind.RelativeOrAbsolute, out u))
                            {
                                if (u.IsAbsoluteUri)
                                {
                                    EndPoint old = hostEndPoint;
                                    IPAddress parsedHostIpAddress;
                                    if (IPAddress.TryParse(u.Host, out parsedHostIpAddress))
                                    {
                                        this.hostEndPoint = new IPEndPoint(parsedHostIpAddress, u.Port);
                                        //_logger.LogDebug("{0}: Absolute URI detected. Host address is now: {1}", old, HostEndPoint);
                                        this.controlUrl = controlUrl.Substring(u.GetLeftPart(UriPartial.Authority).Length);
                                        //_logger.LogDebug("{0}: New control url: {1}", HostEndPoint, controlUrl);
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogDebug("{0}: Assuming control Uri is relative: {1}", HostEndPoint, controlUrl);
                            }
                            return;
                        }
                    }
                }

                //If we get here, it means that we didn't get WANIPConnection service, which means no uPnP forwarding
                //So we don't invoke the callback, so this device is never added to our lists
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

        public override async Task CreatePortMap(Mapping mapping)
        {
            CreatePortMappingMessage message = new CreatePortMappingMessage(mapping, localAddress, this);
            using (await _httpClient.SendAsync(message.Encode(), message.Method).ConfigureAwait(false))
            {

            }
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
