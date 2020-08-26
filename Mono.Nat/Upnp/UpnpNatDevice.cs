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
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mono.Nat.Logging;

namespace Mono.Nat.Upnp
{
    sealed class UpnpNatDevice : NatDevice, IEquatable<UpnpNatDevice>
    {
        static Logger Log { get; } = Logger.Create ();

        /// <summary>
        /// The url we can use to control the port forwarding
        /// </summary>
        internal Uri DeviceControlUri { get; private set; }

        /// <summary>
        /// The IP address of the LAN device (the current machine)
        /// </summary>
        IPAddress LocalAddress { get; }

        /// <summary>
        /// The service type we're using on the device
        /// </summary>
        public string ServiceType { get; private set; }

        internal UpnpNatDevice (IPAddress localAddress, IPEndPoint deviceEndpoint, Uri deviceControlUri, string serviceType)
            : base (deviceEndpoint, NatProtocol.Upnp)
        {
            LocalAddress = localAddress;
            DeviceControlUri = deviceControlUri;
            ServiceType = serviceType;
        }

        public override async Task<Mapping> CreatePortMapAsync (Mapping mapping)
        {
            var message = new CreatePortMappingMessage (mapping, LocalAddress, this);
            var response = await SendMessageAsync (message).ConfigureAwait (false);
            if (!(response is CreatePortMappingResponseMessage))
                throw new MappingException (ErrorCode.Unknown, "Invalid response received when creating the port map");
            return mapping;
        }

        public override async Task<Mapping> DeletePortMapAsync (Mapping mapping)
        {
            var message = new DeletePortMappingMessage (mapping, this);
            var response = await SendMessageAsync (message).ConfigureAwait (false);
            if (!(response is DeletePortMapResponseMessage))
                throw new MappingException (ErrorCode.Unknown, "Invalid response received when deleting the port map");
            return mapping;
        }

        public override async Task<Mapping[]> GetAllMappingsAsync ()
        {
            var mappings = new List<Mapping> ();

            // Is it OK to hardcode 1000 mappings as the maximum? Probably better than an infinite loop
            // which would rely on routers correctly reporting all the mappings have been retrieved...
            try {
                for (int i = 0; i < 1000; i++) {
                    var message = new GetGenericPortMappingEntry (i, this);
                    // If we get a null response, or it's the wrong type, bail out.
                    // It means we've iterated over the entire array.
                    var resp = await SendMessageAsync (message).ConfigureAwait (false);
                    if (!(resp is GetGenericPortMappingEntryResponseMessage response))
                        break;

                    mappings.Add (new Mapping (response.Protocol, response.InternalPort, response.ExternalPort, response.LeaseDuration, response.PortMappingDescription));
                }
            } catch (MappingException ex) {
                // Error code 713 means we successfully iterated to the end of the array and have all the mappings.
                // Exception driven code flow ftw!
                if (ex.ErrorCode != ErrorCode.SpecifiedArrayIndexInvalid)
                    throw;
            }

            return mappings.ToArray ();
        }

        public override async Task<IPAddress> GetExternalIPAsync ()
        {
            var message = new GetExternalIPAddressMessage (this);
            var response = await SendMessageAsync (message).ConfigureAwait (false);
            if (!(response is GetExternalIPAddressResponseMessage msg))
                throw new MappingException (ErrorCode.Unknown, "Invalid response received when getting the external IP address");
            return msg.ExternalIPAddress;
        }

        public override async Task<Mapping> GetSpecificMappingAsync (Protocol protocol, int publicPort)
        {
            var message = new GetSpecificPortMappingEntryMessage (protocol, publicPort, this);
            var response = await SendMessageAsync (message).ConfigureAwait (false);
            if (!(response is GetSpecificPortMappingEntryResponseMessage msg))
                throw new MappingException (ErrorCode.Unknown, "Invalid response received when getting the specific mapping");
            return new Mapping (protocol, msg.InternalPort, publicPort, msg.LeaseDuration, msg.PortMappingDescription);
        }

        async Task<ResponseMessage> SendMessageAsync (RequestMessage message)
        {
            HttpWebRequest request = message.Encode (out byte[] body);
            // If this device has multiple active network devices, ensure the web request is sent from the network device which
            // received the response from the router. That way when we attempt to map a port, the IPAddress we are mapping to
            // is the same as the IPAddress which issues the WebRequest. Most uPnP implementations don't allow a device to
            // forward a port to a *different* IP address.
            request.ServicePoint.BindIPEndPointDelegate = delegate (ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount) {
                Log.InfoFormatted ("The WebRequest being sent to {0} has been bound to local IP address {1}", remoteEndPoint, LocalAddress);
                return new IPEndPoint (LocalAddress, 0);
            };

            Log.InfoFormatted ("uPnP Request: {0}{1}", Environment.NewLine, Encoding.UTF8.GetString (body));

            if (body.Length > 0) {
                request.ContentLength = body.Length;
                using (var stream = await request.GetRequestStreamAsync ().ConfigureAwait (false))
                    await stream.WriteAsync (body, 0, body.Length).ConfigureAwait (false);
            }

            try {
                using (var response = await request.GetResponseAsync ().ConfigureAwait (false))
                    return await DecodeMessageFromResponse (response.GetResponseStream (), (int) response.ContentLength);
            } catch (WebException ex) {
                // Even if the request "failed" i want to continue on to read out the response from the router
                using (var response = ex.Response as HttpWebResponse) {
                    if (response == null)
                        throw new MappingException ("Unexpected error sending a message to the device", ex);
                    else
                        return await DecodeMessageFromResponse (response.GetResponseStream (), (int) response.ContentLength);
                }
            }
        }

        public override bool Equals (object obj)
            => Equals (obj as UpnpNatDevice);

        public bool Equals (UpnpNatDevice other)
            => other != null && DeviceControlUri == other.DeviceControlUri;

        public override int GetHashCode ()
            => DeviceControlUri.GetHashCode ();

        async Task<ResponseMessage> DecodeMessageFromResponse (Stream s, int length)
        {
            StringBuilder data = new StringBuilder ();
            int bytesRead;
            byte[] buffer = BufferHelpers.Rent ();
            try {
                // Read out the content of the message, hopefully picking everything up in the case where we have no contentlength
                if (length != -1) {
                    while (length > 0) {
                        bytesRead = await s.ReadAsync (buffer, 0, Math.Min (buffer.Length, length)).ConfigureAwait (false);
                        data.Append (Encoding.UTF8.GetString (buffer, 0, bytesRead));
                        length -= bytesRead;
                    }
                } else {
                    while ((bytesRead = await s.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false)) != 0)
                        data.Append (Encoding.UTF8.GetString (buffer, 0, bytesRead));
                }
            } finally {
                BufferHelpers.Release (buffer);
            }

            // Once we have our content, we need to see what kind of message it is. If we received
            // an error message we will immediately throw a MappingException.
            var dataString = data.ToString ();
            Log.InfoFormatted ("uPnP Response: {0}, {1}", Environment.NewLine, dataString);
            return ResponseMessage.Decode (this, dataString);
        }

        public override string ToString ()
        {
            return $"UpnpNatDevice - EndPoint: {DeviceEndpoint}, External IP: Manually Check, Control Url: {DeviceControlUri}, Service Type: {ServiceType}, Last Seen: {LastSeen}";
        }
    }
}
