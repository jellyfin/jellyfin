#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Dlna.Rssdp;
using Emby.Dlna.Rssdp.EventArgs;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Ssdp
{
    public sealed class DeviceDiscovery : IDeviceDiscovery, IDisposable
    {
        private readonly object _syncLock = new object();
        private readonly INetworkManager _networkManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly SocketServer _socketServer;

        private bool _disposed;

        private SsdpDeviceLocator? _deviceLocator;

        public DeviceDiscovery(IServerConfigurationManager config, ILoggerFactory loggerFactory, INetworkManager networkManager, IServerApplicationHost appHost, SocketServer? socketServer)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _appHost = appHost ?? throw new ArgumentNullException(nameof(appHost));
            _socketServer = socketServer ?? throw new ArgumentNullException(nameof(socketServer));
        }

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>>? DeviceDiscovered;

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<UpnpDeviceInfo>>? DeviceLeft;

        /// <summary>
        /// Stops a device scan.
        /// </summary>
        public void Stop()
        {
            if (_deviceLocator != null)
            {
                _deviceLocator.Dispose();
                _deviceLocator = null;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Stop();
            }
        }

        /// <summary>
        /// Starts a device scan.
        /// </summary>
        public void Start()
        {
            lock (_syncLock)
            {
                if (DeviceDiscovered?.GetInvocationList().Length > 0 && _deviceLocator == null)
                {
                    _deviceLocator = new SsdpDeviceLocator(_socketServer, _loggerFactory.CreateLogger<SsdpDeviceLocator>(), _networkManager, _appHost.SystemId);

                    // (Optional) Set the filter so we only see notifications for devices we care about
                    // (can be any search target value i.e device type, uuid value etc - any value that appears in the
                    // DiscoverdSsdpDevice.NotificationType property or that is used with the searchTarget parameter of the Search method).
                    // _DeviceLocator.NotificationFilter = "upnp:rootdevice";

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

            DeviceDiscovered?.Invoke(this, args);
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
    }
}
