#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Networking;
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
        private readonly ILoggerFactory _loggerFactory;
        private readonly IGatewayMonitor _gatewayMonitor;
        private readonly object _lock = new object();

        private NatUtility _natUtility;
        private string _configIdentifier;

        private bool _disposed = false;
        private bool _stopped = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalPortForwarding"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The application host.</param>
        /// <param name="config">The configuration manager.</param>
        /// <param name="deviceDiscovery">The device discovery.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="loggerFactory">Logger Factory.</param>
        /// <param name="gwMonitor">Gateway Monitor.</param>
        public ExternalPortForwarding(
            ILogger<ExternalPortForwarding> logger,
            IServerApplicationHost appHost,
            IServerConfigurationManager config,
            IDeviceDiscovery deviceDiscovery,
            INetworkManager networkManager,
            ILoggerFactory loggerFactory,
            IGatewayMonitor gwMonitor)
        {
            _logger = logger;
            _appHost = appHost;
            _config = config;
            _networkManager = networkManager;
            _deviceDiscovery = deviceDiscovery;
            _loggerFactory = loggerFactory;
            _gatewayMonitor = gwMonitor;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            Start();
            _config.ConfigurationUpdated += OnConfigurationUpdated;
            return Task.CompletedTask;
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

            _disposed = true;
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

        private void Start()
        {
            if (!_config.Configuration.EnableUPnP || !_config.Configuration.EnableRemoteAccess)
            {
                return;
            }

            if (_stopped)
            {
                lock (_lock)
                {
                    if (_stopped)
                    {
                        _stopped = false;
                        _logger.LogInformation("Starting NAT discovery");
                        _natUtility = new NatUtility(_loggerFactory);
                        _natUtility.DeviceFound += OnNatUtilityDeviceFound;
                        _natUtility.DeviceLost += OnNatUtilityDeviceLost;
                        _natUtility.StartDiscovery();

                        // _deviceDiscovery.DeviceDiscovered += OnDeviceDiscoveryDeviceDiscovered;
                        _networkManager.NetworkChanged += OnChange;

                        _gatewayMonitor.OnGatewayFailure += OnChange;
                    }
                }
            }
        }

        private void Stop()
        {
            if (!_stopped)
            {
                lock (_lock)
                {
                    if (!_stopped)
                    {
                        _logger.LogInformation("Stopping NAT discovery");
                        _stopped = true;

                        _natUtility.StopDiscovery();
                        _natUtility.DeviceFound -= OnNatUtilityDeviceFound;
                        _natUtility.DeviceLost -= OnNatUtilityDeviceLost;
                        _natUtility.Dispose();

                        _gatewayMonitor.ResetGateways();
                        _gatewayMonitor.OnGatewayFailure -= OnChange;

                        _deviceDiscovery.DeviceDiscovered -= OnChange;
                        _networkManager.NetworkChanged -= OnChange;
                    }
                }
            }
        }

        private void OnChange(object sender, EventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            if (!_stopped)
            {
                lock (_lock)
                {
                    if (!_stopped)
                    {
                        _natUtility.StopDiscovery();
                        _gatewayMonitor.ResetGateways();
                        _natUtility.StartDiscovery();
                    }
                }
            }
        }

        private void OnDeviceDiscoveryDeviceDiscovered(object sender, GenericEventArgs<UpnpDeviceInfo> e)
        {
            if (_disposed)
            {
                return;
            }

            _natUtility.Search(e.Argument.LocalIpAddress, NatProtocol.Upnp);
        }

        private async void OnNatUtilityDeviceFound(object sender, DeviceEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Attempting to create rules.");
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
                _logger.LogInformation("Attempting to remove rules.");
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
            _logger.LogInformation(
                "Creating port map on local port {LocalPort} to public port {PublicPort} with device {DeviceEndpoint}",
                privatePort,
                publicPort,
                device.DeviceEndpoint);

            try
            {
                var mapping = new Mapping(Protocol.Tcp, privatePort, publicPort, 0, _appHost.Name);
                await device.CreatePortMapAsync(mapping).ConfigureAwait(false);

                // Add to the monitoring..
                _ = _gatewayMonitor.AddGateway(device.DeviceEndpoint.Address);
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
            _logger.LogInformation(
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
    }
}
