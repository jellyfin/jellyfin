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
    public sealed class NatUtility : IDisposable
    {
        /// <summary>
        /// Defines the Locker.
        /// </summary>
        private readonly object _locker = new object();

        private readonly ILogger<ISearcher> _logger;

        private readonly PmpSearcher _pmpSeacher;

        private readonly UpnpSearcher _upnpSearcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="NatUtility"/> class.
        /// </summary>
        /// <param name="loggerFactory">ILoggerFactory instance.</param>
        public NatUtility(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ISearcher>();

            _upnpSearcher = new UpnpSearcher(_logger);
            _upnpSearcher.DeviceFound += (o, e) => DeviceFound?.Invoke(null, e);
            _upnpSearcher.DeviceLost += (o, e) => DeviceLost?.Invoke(null, e);

            _pmpSeacher = new PmpSearcher(_logger);
            _pmpSeacher.DeviceFound += (o, e) => DeviceFound?.Invoke(null, e);
            _pmpSeacher.DeviceLost += (o, e) => DeviceLost?.Invoke(null, e);
        }

        /// <summary>
        /// Defines the DeviceFound.
        /// </summary>
        public event EventHandler<DeviceEventArgs>? DeviceFound;

        /// <summary>
        /// Defines the DeviceLost.
        /// </summary>
        public event EventHandler<DeviceEventArgs>? DeviceLost;

        /// <summary>
        /// Gets a value indicating whether IsSearching.
        /// </summary>
        public bool IsSearching
        {
            get
            {
                if (_pmpSeacher != null)
                {
                    if (_upnpSearcher != null)
                    {
                        return _upnpSearcher.Listening || _pmpSeacher.Listening;
                    }

                    return _pmpSeacher.Listening;
                }

                return _upnpSearcher != null && _upnpSearcher.Listening;
            }
        }

        /// <summary>
        /// Sends a single (non-periodic) message to the specified IP address to see if it supports the
        /// specified port mapping protocol, and begin listening indefinitely for responses.
        /// </summary>
        /// <param name="gatewayAddress">The IP address.</param>
        /// <param name="type">.</param>
        public void Search(IPAddress gatewayAddress, NatProtocol type)
        {
            lock (_locker)
            {
                if (type == NatProtocol.Pmp)
                {
                    _ = _pmpSeacher.SearchAsync(gatewayAddress).FireAndForget(_logger);
                }
                else if (type == NatProtocol.Upnp)
                {
                    _ = _upnpSearcher.SearchAsync(gatewayAddress).FireAndForget(_logger);
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
        public void StartDiscovery(params NatProtocol[] devices)
        {
            lock (_locker)
            {
                if (devices.Length == 0 || devices.Contains(NatProtocol.Pmp))
                {
                    _ = _pmpSeacher.SearchAsync().FireAndForget(_logger);
                }

                if (devices.Length == 0 || devices.Contains(NatProtocol.Upnp))
                {
                    _ = _upnpSearcher.SearchAsync().FireAndForget(_logger);
                }
            }
        }

        /// <summary>
        /// Stop listening for responses to the search messages, cancel any pending searches, and frees up resources.
        /// </summary>
        public void StopDiscovery()
        {
            lock (_locker)
            {
                _pmpSeacher.Stop();
                _upnpSearcher.Stop();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            StopDiscovery();
            GC.SuppressFinalize(this);
        }
    }
}
