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
using System.Linq;

using Mono.Nat.Pmp;
using Mono.Nat.Upnp;
using System.Threading;
using System.Collections.Generic;

namespace Mono.Nat
{
    public static class NatUtility
    {

        public static event EventHandler<DeviceEventArgs> DeviceFound;
        public static event EventHandler<DeviceEventUnknownArgs> UnknownDeviceFound;

        static readonly NatProtocol[] AllProtocols = Enum.GetValues (typeof (NatProtocol)).Cast<NatProtocol> ().ToArray ();

        static readonly object Locker = new object ();

        static readonly Dictionary<NatProtocol, ISearcher> Searchers = new Dictionary<NatProtocol, ISearcher> ();

        static ISearcher pmp => Searchers.TryGetValue (NatProtocol.Pmp, out ISearcher value) ? value : null;
        static ISearcher upnp => Searchers.TryGetValue (NatProtocol.Pmp, out ISearcher value) ? value : null;

        static ISearcher GetOrCreate (NatProtocol protocol)
        {
            if (!Searchers.TryGetValue (protocol, out ISearcher searcher)) {
                searcher = protocol == NatProtocol.Pmp ? (ISearcher) PmpSearcher.Create () : UpnpSearcher.Create ();
                searcher.DeviceFound += HandleDeviceFound;
                searcher.UnknownDeviceFound += HandleUnknownDeviceFound;
                Searchers[protocol] = searcher;
            }

            return searcher;
        }

        public static bool IsSearching => (pmp != null && pmp.Listening) || (upnp != null && upnp.Listening);

        /// <summary>
        /// Sends a single (non-periodic) message to the specified IP address to see if it supports the
        /// specified port mapping protocol, and begin listening indefinitely for responses.
        /// </summary>
        /// <param name="gatewayAddress">The IP address</param>
        /// <param name="type"></param>
        public static void Search (IPAddress gatewayAddress, NatProtocol type)
        {
            lock (Locker)
                GetOrCreate (type).SearchAsync (gatewayAddress).FireAndForget ();
        }

        static void HandleDeviceFound (object sender, DeviceEventArgs e)
        {
            DeviceFound?.Invoke (sender, e);
        }

        static void HandleUnknownDeviceFound (object sender, DeviceEventUnknownArgs e)
        {
            UnknownDeviceFound?.Invoke (sender, e);
        }

        /// <summary>
        /// Periodically send a multicast UDP message to scan for new devices, and begin listening indefinitely
        /// for responses.
        /// </summary>
        /// <param name="devices">The protocols which should be searched for. An empty array will result in all supported protocols being used.</param>
        public static void StartDiscovery (params NatProtocol[] devices)
        {
            lock (Locker) {
                devices = devices.Length == 0 ? AllProtocols : devices;
                foreach (var protocol in devices)
                    GetOrCreate (protocol).SearchAsync ().FireAndForget ();
            }
        }

        /// <summary>
        /// Parses a message received elsewhere.
        /// </summary>
        /// <param name="type">Type of message.</param>
        /// <param name="localAddress"></param>
        /// <param name="content"></param>
        /// <param name="source"></param>
        public static void ParseMessage (NatProtocol type, IPAddress localAddress, byte[] content, IPEndPoint source)
        {
            lock (Locker)
                GetOrCreate (type).HandleMessageReceived (localAddress, content, source, CancellationToken.None);
        }

        /// <summary>
        /// Stop listening for responses to the search messages, and cancel any pending searches.
        /// </summary>
        public static void StopDiscovery ()
        {
            lock (Locker) {
                foreach (var searcher in Searchers) {
                    searcher.Value.Stop ();
                    searcher.Value.Dispose ();
                }
                Searchers.Clear ();
            }
        }
    }
}
