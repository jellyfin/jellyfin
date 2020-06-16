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

namespace Mono.Nat
{
    using System;
    using System.Linq;
    using System.Net;
    using Microsoft.Extensions.Logging;
    using Mono.Nat.Pmp;
    using Mono.Nat.Upnp;

    /// <summary>
    /// Defines the <see cref="NatUtility" />.
    /// </summary>
    public static class NatUtility
    {
        /// <summary>
        /// Defines the LoggerLocker.
        /// </summary>
        private static readonly object _loggerLocker = new object();

        /// <summary>
        /// Defines the Locker.
        /// </summary>
        private static readonly object _locker = new object();

        /// <summary>
        /// Initializes static members of the <see cref="NatUtility"/> class.
        /// </summary>
        static NatUtility()
        {
            foreach (var searcher in new ISearcher[] { UpnpSearcher.Instance, PmpSearcher.Instance })
            {
                searcher.DeviceFound += (o, e) => DeviceFound?.Invoke(null, e);
                searcher.DeviceLost += (o, e) => DeviceLost?.Invoke(null, e);
            }
        }

        /// <summary>
        /// Defines the DeviceFound.
        /// </summary>
        public static event EventHandler<DeviceEventArgs> DeviceFound;

        /// <summary>
        /// Defines the DeviceLost.
        /// </summary>
        public static event EventHandler<DeviceEventArgs> DeviceLost;

        /// <summary>
        /// Gets or sets the Logger.
        /// </summary>
#pragma warning disable CS3003 // Type is not CLS-compliant
        public static ILogger Logger { get; set; }
#pragma warning restore CS3003 // Type is not CLS-compliant

        /// <summary>
        /// Gets a value indicating whether IsSearching.
        /// </summary>
        public static bool IsSearching => PmpSearcher.Instance.Listening || UpnpSearcher.Instance.Listening;

        /// <summary>
        /// Sends a single (non-periodic) message to the specified IP address to see if it supports the
        /// specified port mapping protocol, and begin listening indefinitely for responses.
        /// </summary>
        /// <param name="gatewayAddress">The IP address.</param>
        /// <param name="type">.</param>
        public static void Search(IPAddress gatewayAddress, NatProtocol type)
        {
            lock (_locker)
            {
                if (type == NatProtocol.Pmp)
                {
                    PmpSearcher.Instance.Begin();
                    _ = PmpSearcher.Instance.SearchAsync(gatewayAddress).FireAndForget();
                }
                else if (type == NatProtocol.Upnp)
                {
                    UpnpSearcher.Instance.Begin();
                    _ = UpnpSearcher.Instance.SearchAsync(gatewayAddress).FireAndForget();
                }
                else
                {
                    throw new InvalidOperationException("Unsuported type given");
                }
            }
        }

        /// <summary>
        /// Re-initialises this object, then periodically send a multicast UDP message to scan for new devices, and begin listening indefinitely for responses.
        /// </summary>
        /// <param name="devices">The protocols which should be searched for. An empty array will result in all supported protocols being used.</param>
        public static void StartDiscovery(params NatProtocol[] devices)
        {
            lock (_locker)
            {
                if (devices.Length == 0 || devices.Contains(NatProtocol.Pmp))
                {
                    PmpSearcher.Instance.Begin();
                    _ = PmpSearcher.Instance.SearchAsync().FireAndForget();
                }

                if (devices.Length == 0 || devices.Contains(NatProtocol.Upnp))
                {
                    UpnpSearcher.Instance.Begin();
                    _ = UpnpSearcher.Instance.SearchAsync().FireAndForget();
                }
            }
        }

        /// <summary>
        /// Stop listening for responses to the search messages, cancel any pending searches, and frees up resources.
        /// </summary>
        public static void StopDiscovery()
        {
            lock (_locker)
            {
                PmpSearcher.Instance.Finish();
                UpnpSearcher.Instance.Finish();
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        internal static void LogDebug(string format, params object[] args)
        {
            ILogger logger = Logger;

            if (logger != null)
            {
                lock (_loggerLocker)
                {
                    logger.LogDebug(format, args);
                }
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        internal static void LogError(Exception ex, string format, params object[] args)
        {
            ILogger logger = Logger;

            if (logger != null)
            {
                lock (_loggerLocker)
                {
                    logger.LogError(ex, format, args);
                }
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        internal static void LogWarning(string format, params object[] args)
        {
            ILogger logger = Logger;

            if (logger != null)
            {
                lock (_loggerLocker)
                {
                    logger.LogWarning(format, args);
                }
            }
        }
    }
}
