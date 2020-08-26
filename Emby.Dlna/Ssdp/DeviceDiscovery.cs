#pragma warning disable CS1591
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Dlna.Net;
using Emby.Dlna.PlayTo;
using Emby.Dlna.PlayTo.EventArgs;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Ssdp
{
    public sealed class DeviceDiscovery : IDeviceDiscovery, IDisposable
    {
        private readonly object _syncLock = new object();

        private readonly IServerConfigurationManager _config;

        private readonly INetworkManager _networkManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly SocketServer _socketServer;
        private SsdpPlayToLocator? _deviceLocator;
        private bool _disposed;

        public DeviceDiscovery(IServerConfigurationManager config, ILoggerFactory loggerFactory, INetworkManager networkManager, SocketServer socketServer)
        {
            _config = config;
            _loggerFactory = loggerFactory;
            _networkManager = networkManager;
            _socketServer = socketServer;
        }

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>>? DeviceDiscovered;

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>>? DeviceLeft;

        /// <summary>
        /// Starts a device scan.
        /// </summary>
        public void Start()
        {
            lock (_syncLock)
            {
                if (_deviceLocator == null)
                {
                    var interval = TimeSpan.FromSeconds(_config.GetDlnaConfiguration().ClientDiscoveryIntervalSeconds);

                    _deviceLocator = new SsdpPlayToLocator(_socketServer, _loggerFactory.CreateLogger<SsdpPlayToLocator>(), _networkManager);
                    _deviceLocator.DeviceAvailable += OnDeviceLocatorDeviceAvailable;
                    _deviceLocator.DeviceUnavailable += OnDeviceLocatorDeviceUnavailable;
                    _deviceLocator.Start(TimeSpan.FromSeconds(5), interval);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_deviceLocator != null)
            {
                _deviceLocator.DeviceAvailable -= OnDeviceLocatorDeviceAvailable;
                _deviceLocator.DeviceUnavailable -= OnDeviceLocatorDeviceUnavailable;
            }

            // If we still have delegates, then don't dispose as we're still in use.
            if (DeviceDiscovered != null && DeviceDiscovered.GetInvocationList().Length != 0)
            {
                return;
            }

            if (!_disposed)
            {
                _disposed = true;
                _deviceLocator?.Dispose();
                _deviceLocator = null;
            }
        }

        // Process each found device in the event handler
        private void OnDeviceLocatorDeviceAvailable(object sender, DeviceAvailableEventArgs e)
        {
            var originalHeaders = e.DiscoveredDevice.Headers;

            var headerDict = originalHeaders == null ? new Dictionary<string, KeyValuePair<string, IEnumerable<string>>>() : originalHeaders.ToDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);

            var headers = headerDict.ToDictionary(i => i.Key, i => i.Value.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

            var args = new GenericEventArgs<UpnpDeviceInfo>(
                new UpnpDeviceInfo
                {
                    Location = e.DiscoveredDevice.DescriptionLocation,
                    Headers = headers,
                    LocalIpAddress = e.LocalIpAddress
                });

            DeviceDiscovered?.Invoke(this, args);
        }

        private void OnDeviceLocatorDeviceUnavailable(object sender, DeviceUnavailableEventArgs e)
        {
            var originalHeaders = e.DiscoveredDevice.Headers;

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
    }
}
