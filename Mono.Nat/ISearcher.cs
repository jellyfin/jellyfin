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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat
{
	interface ISearcher : IDisposable
	{
        /// <summary>
        /// This event is raised whenever a device which supports port mapping is discovered
        /// </summary>
        event EventHandler<DeviceEventArgs> DeviceFound;

        /// <summary>
        /// This event is raised whenever a device which doesn't supports port mapping is discovered.
        /// </summary>
        event EventHandler<DeviceEventUnknownArgs> DeviceUnknown;

        /// <summary>
        /// The port mapping protocol supported by the device
        /// </summary>
        NatProtocol Protocol { get; }

		/// <summary>
		/// While running the searcher constantly listens for UDP broadcasts when new devices come online.
		/// </summary>
		bool Listening { get; }

		/// <summary>
		/// Periodically send a multicast UDP message to scan for new devices.
		/// If the searcher is not listening, it will begin listening until 'Stop' is invoked.
		/// </summary>
		Task SearchAsync ();

		/// <summary>
		/// Immediately sends a unicast UDP message to this IP address to check for a compatible device.
		/// If the searcher is not listening, it will begin listening until 'Stop' is invoked.
		/// </summary>
		/// <param name="gatewayAddress">The IP address which should</param>
		Task SearchAsync (IPAddress gatewayAddress);

		/// <summary>
		/// The searcher will no longer listen for new devices.
		/// </summary>
		void Stop ();
	}
}
