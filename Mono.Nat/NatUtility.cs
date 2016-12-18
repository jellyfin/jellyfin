//
// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//   Nicholas Terry <nick.i.terry@gmail.com>
//
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;

namespace Mono.Nat
{
	public static class NatUtility
	{
        private static ManualResetEvent searching;
		public static event EventHandler<DeviceEventArgs> DeviceFound;
		public static event EventHandler<DeviceEventArgs> DeviceLost;
        
		private static List<ISearcher> controllers;
		private static bool verbose;

        public static List<NatProtocol> EnabledProtocols { get; set; }

	    public static ILogger Logger { get; set; }
        public static IHttpClient HttpClient { get; set; }

        public static bool Verbose
		{
			get { return verbose; }
			set { verbose = value; }
		}
		
        static NatUtility()
        {
            EnabledProtocols = new List<NatProtocol>
            {
                NatProtocol.Pmp
            };

            searching = new ManualResetEvent(false);

            controllers = new List<ISearcher>();
            controllers.Add(PmpSearcher.Instance);

            controllers.ForEach(searcher =>
                {
                    searcher.DeviceFound += (sender, args) =>
                    {
                        if (DeviceFound != null)
                            DeviceFound(sender, args);
                    };
                    searcher.DeviceLost += (sender, args) =>
                    {
                        if (DeviceLost != null)
                            DeviceLost(sender, args);
                    };
                });

            Task.Factory.StartNew(SearchAndListen, TaskCreationOptions.LongRunning);
        }

		internal static void Log(string format, params object[] args)
		{
			var logger = Logger;
		    if (logger != null)
		        logger.Debug(format, args);
		}

        private static async Task SearchAndListen()
        {
            while (true)
            {
                searching.WaitOne();

                try
                {
                    var enabledProtocols = EnabledProtocols.ToList();

                    if (enabledProtocols.Contains(PmpSearcher.Instance.Protocol))
                    {
                        await Receive(PmpSearcher.Instance, PmpSearcher.sockets).ConfigureAwait(false);
                    }

                    foreach (ISearcher s in controllers)
                    {
                        if (s.NextSearch < DateTime.Now && enabledProtocols.Contains(s.Protocol))
                        {
                            Log("Searching for: {0}", s.GetType().Name);
                            s.Search();
                        }
                    }
                }
                catch (Exception e)
                {
                    
                }
                await Task.Delay(100).ConfigureAwait(false);
            }
		}

		static async Task Receive (ISearcher searcher, List<UdpClient> clients)
		{
			foreach (UdpClient client in clients)
			{
				if (client.Available > 0)
				{
				    IPAddress localAddress = ((IPEndPoint)client.Client.LocalEndPoint).Address;
				    var result = await client.ReceiveAsync().ConfigureAwait(false);
				    var data = result.Buffer;
				    var received = result.RemoteEndPoint;
					searcher.Handle(localAddress, data, received);
				}
            }
        }
		
		public static void StartDiscovery ()
		{
            searching.Set();
		}

		public static void StopDiscovery ()
		{
            searching.Reset();
		}
		
		//checks if an IP address is a private address space as defined by RFC 1918
		public static bool IsPrivateAddressSpace (IPAddress address)
		{
			byte[] ba = address.GetAddressBytes ();

			switch ((int)ba[0]) {
			case 10:
				return true; //10.x.x.x
			case 172:
				return ((int)ba[1] & 16) != 0; //172.16-31.x.x
			case 192:
				return (int)ba[1] == 168; //192.168.x.x
			default:
				return false;
			}
		}

	    public static void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint, NatProtocol protocol)
	    {
	        switch (protocol)
	        {
                case NatProtocol.Upnp:
	                //UpnpSearcher.Instance.Handle(localAddress, response, endpoint);
	                break;
                case NatProtocol.Pmp:
	                PmpSearcher.Instance.Handle(localAddress, response, endpoint);
	                break;
	            default:
	                throw new ArgumentException("Unexpected protocol: " + protocol);
	        }
        }

        public static void Handle(IPAddress localAddress, UpnpDeviceInfo deviceInfo, IPEndPoint endpoint, NatProtocol protocol)
        {
            switch (protocol)
            {
                case NatProtocol.Upnp:
                    var searcher = new UpnpSearcher(Logger, HttpClient);
                    searcher.DeviceFound += Searcher_DeviceFound;
                    searcher.Handle(localAddress, deviceInfo, endpoint);
                    break;
                default:
                    throw new ArgumentException("Unexpected protocol: " + protocol);
            }
        }

        private static void Searcher_DeviceFound(object sender, DeviceEventArgs e)
        {
            if (DeviceFound != null)
            {
                DeviceFound(sender, e);
            }
        }
    }
}
