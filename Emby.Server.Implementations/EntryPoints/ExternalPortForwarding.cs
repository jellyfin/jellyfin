#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using Microsoft.Extensions.Logging;
using Mono.Nat;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Server entrypoint handling external port forwarding.
    /// </summary>
    public class ExternalPortForwarding : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger<ExternalPortForwarding> _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IDeviceDiscovery _deviceDiscovery;
        private readonly INetworkManager _networkManager;
        //// private readonly ConcurrentDictionary<IPEndPoint, byte> _createdRules = new ConcurrentDictionary<IPEndPoint, byte>();

        private Timer _timer;
        private string _configIdentifier;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalPortForwarding"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The application host.</param>
        /// <param name="config">The configuration manager.</param>
        /// <param name="deviceDiscovery">The device discovery.</param>
        /// <param name="networkManager">The network manager.</param>
        public ExternalPortForwarding(
            ILogger<ExternalPortForwarding> logger,
            IServerApplicationHost appHost,
            IServerConfigurationManager config,
            IDeviceDiscovery deviceDiscovery,
            INetworkManager networkManager)
        {
            _logger = logger;
            _appHost = appHost;
            _config = config;
            _networkManager = networkManager;
            _deviceDiscovery = deviceDiscovery;
        }

        private string GetConfigIdentifier()
        {
            const char Separator = '|';
            var config = _config.Configuration;

            return new StringBuilder(32)
                .Append(config.EnableUPnP).Append(Separator)
                .Append(config.PublicPort).Append(Separator)
                .Append(config.PublicHttpsPort).Append(Separator)
                .Append(_appHost.HttpPort).Append(Separator)
                .Append(_appHost.HttpsPort).Append(Separator)
                .Append(_appHost.ListenWithHttps).Append(Separator)
                .Append(config.EnableRemoteAccess).Append(Separator)
                .ToString();
        }

        private void OnConfigurationUpdated(object sender, EventArgs e)
        {
            var oldConfigIdentifier = _configIdentifier;
            _configIdentifier = GetConfigIdentifier();

            if (!string.Equals(_configIdentifier, oldConfigIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                Stop();
                Start();
            }
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            Start();

            _config.ConfigurationUpdated += OnConfigurationUpdated;

            return Task.CompletedTask;
        }

        private void Start()
        {
            if (!_config.Configuration.EnableUPnP || !_config.Configuration.EnableRemoteAccess)
            {
                return;
            }

            _logger.LogInformation("Starting NAT discovery");
            NatUtility.Logger = _logger;
            NatUtility.DeviceFound += OnNatUtilityDeviceFound;
            NatUtility.DeviceLost += OnNatUtilityDeviceLost;
            NatUtility.BeginDiscovery();

            //// _timer = new Timer((_) => _createdRules.Clear(), null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            _timer = new Timer(CheckInboundAccessibility, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            _deviceDiscovery.DeviceDiscovered += OnDeviceDiscoveryDeviceDiscovered;
            _networkManager.NetworkChanged += OnNetworkChanged;
        }

        private void CheckInboundAccessibility(object state)
        {
            if (!_disposed)
            {
                _ = CheckAccess();
            }
        }

        private async Task<bool> CheckInternetAccess()
        {
            // This needs to be a call to an external site specifying our own ip address to callback on.

            return false;
        }

        private async Task CheckAccess(int count = 0)
        {
            if (_disposed)
            {
                return;
            }

            bool alive = await CheckInternetAccess().ConfigureAwait(false);

            if (!alive && count < 3)
            {
                _logger.LogWarning("Lost inbound internet access.");

                NatUtility.FinaliseDiscovery();
                NatUtility.BeginDiscovery();

                await Task.Delay(1000).ConfigureAwait(false);
                _ = CheckAccess(count + 1);
            }

            _logger.LogError("Inbound internet access is DOWN.");
        }

        private void Stop()
        {
            _logger.LogInformation("Stopping NAT discovery");

            NatUtility.FinaliseDiscovery();
            NatUtility.DeviceFound -= OnNatUtilityDeviceFound;
            NatUtility.DeviceLost -= OnNatUtilityDeviceLost;
            _timer?.Dispose();

            _deviceDiscovery.DeviceDiscovered -= OnDeviceDiscoveryDeviceDiscovered;
            _networkManager.NetworkChanged -= OnNetworkChanged;
        }

        private void OnNetworkChanged(object sender, EventArgs e)
        {
            if (!_disposed)
            {
                // Something changed on the network.
                NatUtility.FinaliseDiscovery();
                NatUtility.BeginDiscovery();
            }
        }

        private void OnDeviceDiscoveryDeviceDiscovered(object sender, GenericEventArgs<UpnpDeviceInfo> e)
        {
            NatUtility.Search(e.Argument.LocalIpAddress, NatProtocol.Upnp);
        }

        private async void OnNatUtilityDeviceFound(object sender, DeviceEventArgs e)
        {
            try
            {
                await CreateRules(e.Device).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating port forwarding rules");
            }
        }

        private async void OnNatUtilityDeviceLost(object sender, DeviceEventArgs e)
        {
            try
            {
                await RemoveRules(e.Device).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing port forwarding rules");
            }
        }

        private Task CreateRules(INatDevice device)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            //// On some systems the device discovered event seems to fire repeatedly
            //// This check will help ensure we're not trying to port map the same device over and over
            //// if (!_createdRules.TryAdd(device.DeviceEndpoint, 0))
            //// {
            ////    return Task.CompletedTask;
            //// }

            return Task.WhenAll(CreatePortMaps(device));
        }

        private Task RemoveRules(INatDevice device)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return Task.WhenAll(RemovePortMaps(device));
        }

        private IEnumerable<Task> CreatePortMaps(INatDevice device)
        {
            yield return CreatePortMap(device, _appHost.HttpPort, _config.Configuration.PublicPort);

            if (_appHost.ListenWithHttps)
            {
                yield return CreatePortMap(device, _appHost.HttpsPort, _config.Configuration.PublicHttpsPort);
            }
        }

        private IEnumerable<Task> RemovePortMaps(INatDevice device)
        {
            yield return RemovePortMap(device, _appHost.HttpPort, _config.Configuration.PublicPort);

            if (_appHost.ListenWithHttps)
            {
                yield return RemovePortMap(device, _appHost.HttpsPort, _config.Configuration.PublicHttpsPort);
            }
        }

        private async Task CreatePortMap(INatDevice device, int privatePort, int publicPort)
        {
            _logger.LogDebug(
                "Creating port map on local port {LocalPort} to public port {PublicPort} with device {DeviceEndpoint}",
                privatePort,
                publicPort,
                device.DeviceEndpoint);

            try
            {
                var mapping = new Mapping(Protocol.Tcp, privatePort, publicPort, 0, _appHost.Name);
                await device.CreatePortMapAsync(mapping).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating port map on local port {LocalPort} to public port {PublicPort} with device {DeviceEndpoint}.",
                    privatePort,
                    publicPort,
                    device.DeviceEndpoint);
            }
        }

        private async Task RemovePortMap(INatDevice device, int privatePort, int publicPort)
        {
            _logger.LogDebug(
                "Removing port map on local port {LocalPort} to public port {PublicPort} with device {DeviceEndpoint}",
                privatePort,
                publicPort,
                device.DeviceEndpoint);

            try
            {
                var mapping = new Mapping(Protocol.Tcp, privatePort, publicPort, 0, _appHost.Name);
                await device.DeletePortMapAsync(mapping).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing port map on local port {LocalPort} to public port {PublicPort} with device {DeviceEndpoint}.",
                    privatePort,
                    publicPort,
                    device.DeviceEndpoint);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (_disposed)
            {
                return;
            }

            _config.ConfigurationUpdated -= OnConfigurationUpdated;

            Stop();

            _timer = null;

            _disposed = true;
        }
    }
}
