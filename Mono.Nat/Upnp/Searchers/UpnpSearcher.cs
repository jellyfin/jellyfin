//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//   Nicholas Terry <nick.i.terry@gmail.com>
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
// Copyright (C) 2014 Nicholas Terry
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
using System.Collections.Generic;
using System.Text;
using System.Net;
using Mono.Nat.Upnp;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using MediaBrowser.Controller.Dlna;

namespace Mono.Nat
{
    internal class UpnpSearcher : ISearcher
    {
        private const int SearchPeriod = 5 * 60; // The time in seconds between each search
		static UpnpSearcher instance = new UpnpSearcher();
		public static List<UdpClient> sockets = CreateSockets();

		public static UpnpSearcher Instance
		{
			get { return instance; }
		}

        public event EventHandler<DeviceEventArgs> DeviceFound;
        public event EventHandler<DeviceEventArgs> DeviceLost;

        private List<INatDevice> devices;
		private Dictionary<IPAddress, DateTime> lastFetched;
        private DateTime nextSearch;
        private IPEndPoint searchEndpoint;

        UpnpSearcher()
        {
            devices = new List<INatDevice>();
			lastFetched = new Dictionary<IPAddress, DateTime>();
            //searchEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            searchEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
        }

		static List<UdpClient> CreateSockets()
		{
			List<UdpClient> clients = new List<UdpClient>();
			try
			{
				foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
				{
					foreach (UnicastIPAddressInformation address in n.GetIPProperties().UnicastAddresses)
					{
						if (address.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							try
							{
								clients.Add(new UdpClient(new IPEndPoint(address.Address, 0)));
							}
							catch
							{
								continue; // Move on to the next address.
							}
						}
					}
				}
			}
			catch (Exception)
			{
				clients.Add(new UdpClient(0));
			}
			return clients;
		}

        public void Search()
		{
			foreach (UdpClient s in sockets)
			{
				try
				{
					Search(s);
				}
				catch
				{
					// Ignore any search errors
				}
			}
		}

        void Search(UdpClient client)
        {
            nextSearch = DateTime.Now.AddSeconds(SearchPeriod);
            byte[] data = DiscoverDeviceMessage.EncodeSSDP();

            // UDP is unreliable, so send 3 requests at a time (per Upnp spec, sec 1.1.2)
            for (int i = 0; i < 3; i++)
                client.Send(data, data.Length, searchEndpoint);
        }

        public IPEndPoint SearchEndpoint
        {
            get { return searchEndpoint; }
        }

        public void Handle(IPAddress localAddress, UpnpDeviceInfo deviceInfo, IPEndPoint endpoint)
        {
            // No matter what, this method should never throw an exception. If something goes wrong
            // we should still be in a position to handle the next reply correctly.
            try
            {
                /* For UPnP Port Mapping we need ot find either WANPPPConnection or WANIPConnection. 
				 Any other device type is no good to us for this purpose. See the IGP overview paper 
				 page 5 for an overview of device types and their hierarchy.
				 http://upnp.org/specs/gw/UPnP-gw-InternetGatewayDevice-v1-Device.pdf */

                /* TODO: Currently we are assuming version 1 of the protocol. We should figure out which
				 version it is and apply the correct URN. */

                /* Some routers don't correctly implement the version ID on the URN, so we only search for the type
				 prefix. */

                // We have an internet gateway device now
                UpnpNatDevice d = new UpnpNatDevice(localAddress, deviceInfo, endpoint, string.Empty);

                if (devices.Contains(d))
                {
                    // We already have found this device, so we just refresh it to let people know it's
                    // Still alive. If a device doesn't respond to a search, we dump it.
                    devices[devices.IndexOf(d)].LastSeen = DateTime.Now;
                }
                else
                {

                    // If we send 3 requests at a time, ensure we only fetch the services list once
                    // even if three responses are received
                    if (lastFetched.ContainsKey(endpoint.Address))
                    {
                        DateTime last = lastFetched[endpoint.Address];
                        if ((DateTime.Now - last) < TimeSpan.FromSeconds(20))
                            return;
                    }
                    lastFetched[endpoint.Address] = DateTime.Now;

                    // Once we've parsed the information we need, we tell the device to retrieve it's service list
                    // Once we successfully receive the service list, the callback provided will be invoked.
                    NatUtility.Log("Fetching service list: {0}", d.HostEndPoint);
                    d.GetServicesList(DeviceSetupComplete);
                }
            }
            catch (Exception ex)
            {
                NatUtility.Log("Unhandled exception when trying to decode a device's response Send me the following data: ");
                NatUtility.Log("ErrorMessage:");
                NatUtility.Log(ex.Message);
            }
        }

        public void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
        {
            // Convert it to a string for easy parsing
            string dataString = null;

            // No matter what, this method should never throw an exception. If something goes wrong
            // we should still be in a position to handle the next reply correctly.
            try {
	            string urn;
                dataString = Encoding.UTF8.GetString(response);

				if (NatUtility.Verbose)
					NatUtility.Log("UPnP Response: {0}", dataString);

				/* For UPnP Port Mapping we need ot find either WANPPPConnection or WANIPConnection. 
				 Any other device type is no good to us for this purpose. See the IGP overview paper 
				 page 5 for an overview of device types and their hierarchy.
				 http://upnp.org/specs/gw/UPnP-gw-InternetGatewayDevice-v1-Device.pdf */

				/* TODO: Currently we are assuming version 1 of the protocol. We should figure out which
				 version it is and apply the correct URN. */

				/* Some routers don't correctly implement the version ID on the URN, so we only search for the type
				 prefix. */

                string log = "UPnP Response: Router advertised a '{0}' service";
                StringComparison c = StringComparison.OrdinalIgnoreCase;
                if (dataString.IndexOf("urn:schemas-upnp-org:service:WANIPConnection:", c) != -1) {
	                urn = "urn:schemas-upnp-org:service:WANIPConnection:1";
	                NatUtility.Log(log, "urn:schemas-upnp-org:service:WANIPConnection:1");
                } else if (dataString.IndexOf("urn:schemas-upnp-org:service:WANPPPConnection:", c) != -1) {
					urn = "urn:schemas-upnp-org:service:WANPPPConnection:1";
					NatUtility.Log(log, "urn:schemas-upnp-org:service:WANPPPConnection:");
				} else
					return;

                // We have an internet gateway device now
                UpnpNatDevice d = new UpnpNatDevice(localAddress, dataString, urn);

                if (devices.Contains(d))
                {
                    // We already have found this device, so we just refresh it to let people know it's
                    // Still alive. If a device doesn't respond to a search, we dump it.
                    devices[devices.IndexOf(d)].LastSeen = DateTime.Now;
                }
                else
                {

					// If we send 3 requests at a time, ensure we only fetch the services list once
					// even if three responses are received
					if (lastFetched.ContainsKey(endpoint.Address))
					{
						DateTime last = lastFetched[endpoint.Address];
						if ((DateTime.Now - last) < TimeSpan.FromSeconds(20))
							return;
					}
					lastFetched[endpoint.Address] = DateTime.Now;
					
                    // Once we've parsed the information we need, we tell the device to retrieve it's service list
                    // Once we successfully receive the service list, the callback provided will be invoked.
					NatUtility.Log("Fetching service list: {0}", d.HostEndPoint);
                    d.GetServicesList(DeviceSetupComplete);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unhandled exception when trying to decode a device's response Send me the following data: ");
                Trace.WriteLine("ErrorMessage:");
                Trace.WriteLine(ex.Message);
                Trace.WriteLine("Data string:");
                Trace.WriteLine(dataString);
            }
        }

        public DateTime NextSearch
        {
            get { return nextSearch; }
        }

        private void DeviceSetupComplete(INatDevice device)
        {
            lock (this.devices)
            {
                // We don't want the same device in there twice
                if (devices.Contains(device))
                    return;

                devices.Add(device);
            }

            OnDeviceFound(new DeviceEventArgs(device));
        }

        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }

        public NatProtocol Protocol
        {
            get { return NatProtocol.Upnp; }
        }
    }
}
