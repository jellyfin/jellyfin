#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using Rssdp;
using Rssdp.Infrastructure;

namespace Emby.Dlna.Ssdp
{
    public sealed class DeviceDiscovery : IDeviceDiscovery, IDisposable
    {
        private readonly object _syncLock = new object();

        private readonly IServerConfigurationManager _config;

        private int _listenerCount;
        private bool _disposed;

        private event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscoveredInternal;

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceDiscovered
        {
            add
            {
                lock (_syncLock)
                {
                    _listenerCount++;
                    DeviceDiscoveredInternal += value;
                }

                StartInternal();
            }

            remove
            {
                lock (_syncLock)
                {
                    _listenerCount--;
                    DeviceDiscoveredInternal -= value;
                }
            }
        }

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>> DeviceLeft;

        private SsdpDeviceLocator _deviceLocator;

        private ISsdpCommunicationsServer _commsServer;

        public DeviceDiscovery(IServerConfigurationManager config)
        {
            _config = config;
        }

        // Call this method from somewhere in your code to start the search.
        public void Start(ISsdpCommunicationsServer communicationsServer)
        {
            _commsServer = communicationsServer;

            StartInternal();
        }

        private void StartInternal()
        {
            lock (_syncLock)
            {
                if (_listenerCount > 0 && _deviceLocator == null)
                {
                    _deviceLocator = new SsdpDeviceLocator(_commsServer);

                    // (Optional) Set the filter so we only see notifications for devices we care about
                    // (can be any search target value i.e device type, uuid value etc - any value that appears in the
                    // DiscoverdSsdpDevice.NotificationType property or that is used with the searchTarget parameter of the Search method).
                    //_DeviceLocator.NotificationFilter = "upnp:rootdevice";

                    // Connect our event handler so we process devices as they are found
                    _deviceLocator.DeviceAvailable += OnDeviceLocatorDeviceAvailable;
                    _deviceLocator.DeviceUnavailable += OnDeviceLocatorDeviceUnavailable;

                    var dueTime = TimeSpan.FromSeconds(5);
                    var interval = TimeSpan.FromSeconds(_config.GetDlnaConfiguration().ClientDiscoveryIntervalSeconds);

                    _deviceLocator.RestartBroadcastTimer(dueTime, interval);
                }
            }
        }

        // Process each found device in the event handler
        private void OnDeviceLocatorDeviceAvailable(object sender, DeviceAvailableEventArgs e)
        {
            var originalHeaders = e.DiscoveredDevice.ResponseHeaders;

            var headerDict = originalHeaders == null ? new Dictionary<string, KeyValuePair<string, IEnumerable<string>>>() : originalHeaders.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);

            var headers = headerDict.ToDictionary(i => i.Key, i => i.Value.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

            var args = new GenericEventArgs<UpnpDeviceInfo>(
                new UpnpDeviceInfo
                {
                    Location = e.DiscoveredDevice.DescriptionLocation,
                    Headers = headers,
                    LocalIpAddress = e.LocalIpAddress
                });

            DeviceDiscoveredInternal?.Invoke(this, args);
        }

        private void OnDeviceLocatorDeviceUnavailable(object sender, DeviceUnavailableEventArgs e)
        {
            var originalHeaders = e.DiscoveredDevice.ResponseHeaders;

            var headerDict = originalHeaders == null ? new Dictionary<string, KeyValuePair<string, IEnumerable<string>>>() : originalHeaders.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);

            var headers = headerDict.ToDictionary(i => i.Key, i => i.Value.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

            var args = new GenericEventArgs<UpnpDeviceInfo>(
                new UpnpDeviceInfo
                {
                    Location = e.DiscoveredDevice.DescriptionLocation,
                    Headers = headers
                });

            DeviceLeft?.Invoke(this, args);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_deviceLocator != null)
                {
                    _deviceLocator.Dispose();
                    _deviceLocator = null;
                }
            }
        }
    }
}
