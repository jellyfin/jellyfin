using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Mono.Nat;
using NATLogger = Mono.Nat.Logging.ILogger;

namespace Jellyfin.Networking.UPnP
{
    /// <summary>
    /// Server entrypoint handling external port forwarding.
    /// </summary>
    public class ExternalPortForwarding : IDisposable
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger<ExternalPortForwarding> _logger;
        private readonly IServerConfigurationManager _config;
        private readonly INetworkManager _networkManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IGatewayMonitor _gatewayMonitor;
        private readonly object _lock;
        private readonly List<INatDevice> _devices;
        private NetworkConfiguration _networkConfig;
        private bool _disposed = false;
        private bool _stopped = true;
        private string _configIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalPortForwarding"/> class.
        /// </summary>
        /// <param name="appHost">The application host.</param>
        /// <param name="config">The configuration manager.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="loggerFactory">Logger Factory.</param>
        /// <param name="gwMonitor">Gateway Monitor.</param>
        public ExternalPortForwarding(
            IServerApplicationHost appHost,
            IServerConfigurationManager config,
            INetworkManager networkManager,
            ILoggerFactory loggerFactory,
            IGatewayMonitor gwMonitor)
        {
            _lock = new object();
            _loggerFactory = loggerFactory ?? throw new NullReferenceException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ExternalPortForwarding>();
            _appHost = appHost ?? throw new NullReferenceException(nameof(appHost));
            _config = config ?? throw new NullReferenceException(nameof(config));
            _networkManager = networkManager ?? throw new NullReferenceException(nameof(networkManager));
            _gatewayMonitor = gwMonitor ?? throw new NullReferenceException(nameof(gwMonitor));
            _devices = new List<INatDevice>();
            _configIdentifier = GetConfigIdentifier();
            Mono.Nat.Logging.Logger.Factory = GetLogger;
            _config.NamedConfigurationUpdated += OnConfigurationUpdated;
            _networkConfig = _config.GetNetworkConfiguration();
            Start();
        }

        /// <summary>
        /// Gets a value indicating whether uPnP is active.
        /// </summary>
        public bool IsUPnPActive => _networkConfig.EnableUPnP &&
            _networkConfig.EnableRemoteAccess &&
            (_appHost.ListenWithHttps || (!_appHost.ListenWithHttps && _networkConfig.UPnPCreateHttpPortMap));

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

            _config.NamedConfigurationUpdated -= OnConfigurationUpdated;

            Stop();

            _disposed = true;
        }

        // /// <summary>
        // /// Creates a logging instance for Mono.NAT.
        // /// </summary>
        // /// <param name="name">Name of instance.</param>
        // /// <returns>ILogger implementation.</returns>
        private NATLogger GetLogger(string name)
        {
            return new LoggingInterface(_loggerFactory.CreateLogger(name));
        }

        /// <summary>
        /// Converts the uPNP settings to a string.
        /// </summary>
        /// <returns>String representation of the settings.</returns>
        private string GetConfigIdentifier()
        {
            const char Separator = '|';
            return new StringBuilder(32)
                .Append(_networkConfig.EnableUPnP).Append(Separator)
                .Append(_networkConfig.PublicPort).Append(Separator)
                .Append(_networkConfig.PublicHttpsPort).Append(Separator)
                .Append(_networkConfig.HttpServerPortNumber).Append(Separator)
                .Append(_networkConfig.HttpsPortNumber).Append(Separator)
                .Append(_appHost.ListenWithHttps).Append(Separator)
                .Append(_networkConfig.EnableRemoteAccess).Append(Separator)
                .ToString();
        }

        /// <summary>
        /// Triggered when the configuration is updated.
        /// </summary>
        /// <param name="sender">Owner of the event.</param>
        /// <param name="e">Event parameters.</param>
        private void OnConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (e.Key.Equals("Network", StringComparison.Ordinal))
            {
                _networkConfig = (NetworkConfiguration)e.NewConfiguration;
                var oldConfigIdentifier = _configIdentifier;
                _configIdentifier = GetConfigIdentifier();

                if (!string.Equals(_configIdentifier, oldConfigIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    Stop();
                    if (IsUPnPActive)
                    {
                        Start();
                    }
                }

                // TODO: check !_networkManager.UPnPActive for changes and remove port mappings if they have.
            }
        }

        /// <summary>
        /// Class to provide an interface between ILogger and NATLogger.
        /// </summary>

        /// <summary>
        /// Starts the discovery.
        /// </summary>
        private void Start()
        {
            if (!IsUPnPActive)
            {
                return;
            }

            if (_stopped)
            {
                lock (_lock)
                {
                    if (_stopped)
                    {
                        _logger.LogInformation("Starting NAT discovery.");

                        NatUtility.DeviceFound += KnownDeviceFound;
                        NatUtility.StartDiscovery();

                        _networkManager.NetworkChanged += OnNetworkChange;
                        _gatewayMonitor.OnGatewayFailure += OnNetworkChange;

                        _stopped = false;
                    }
                }
            }
        }

        /// <summary>
        /// Triggered when there is a change in the network.
        /// </summary>
        /// <param name="sender">INetworkManager object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNetworkChange(object sender, EventArgs e)
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
                        NatUtility.StopDiscovery();
                        _gatewayMonitor.ResetGateways();
                        NatUtility.StartDiscovery();
                    }
                }
            }
        }

        /// <summary>
        /// Stops discovery, and releases resources.
        /// </summary>
        private void Stop()
        {
            if (!_stopped)
            {
                lock (_lock)
                {
                    if (!_stopped)
                    {
                        _logger.LogInformation("Stopping NAT discovery.");

                        NatUtility.StopDiscovery();
                        NatUtility.DeviceFound -= KnownDeviceFound;

                        _gatewayMonitor.ResetGateways();
                        _gatewayMonitor.OnGatewayFailure -= OnNetworkChange;

                        _networkManager.NetworkChanged -= OnNetworkChange;

                        foreach (INatDevice device in _devices)
                        {
                            RemoveRules(device);
                        }

                        _devices.Clear();
                        _stopped = true;
                    }
                }
            }
        }

        /// <summary>
        /// Triggered when a device is found.
        /// </summary>
        /// <param name="sender">Objects triggering the event.</param>
        /// <param name="e">Event arguments.</param>
        private async void KnownDeviceFound(object sender, DeviceEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _logger.LogDebug("Found Internet device. Attempting to create rules.");
                await CreateRules(e.Device).ConfigureAwait(false);

                _devices.Add(e.Device);

                await _gatewayMonitor.AddGateway(e.Device.DeviceEndpoint.Address).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating port forwarding rules");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        /// <summary>
        /// Creates rules on a device.
        /// </summary>
        /// <param name="device">Destination device.</param>
        /// <returns>Task.</returns>
        private Task CreateRules(INatDevice device)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return Task.WhenAll(CreatePortMaps(device));
        }

        /// <summary>
        /// Attempts to create port mappings on a device.
        /// </summary>
        /// <param name="device">Destination device.</param>
        /// <returns>IEnumerable.</returns>
        private IEnumerable<Task> CreatePortMaps(INatDevice device)
        {
            if (!_networkConfig.UPnPCreateHttpPortMap)
            {
                yield return CreatePortMap(device, _networkConfig.HttpServerPortNumber, _networkConfig.PublicPort);
            }

            if (_appHost.ListenWithHttps)
            {
                yield return CreatePortMap(device, _networkConfig.HttpsPortNumber, _networkConfig.PublicHttpsPort);
            }
        }

        /// <summary>
        /// Attempts to add a port mapping.
        /// </summary>
        /// <param name="device">Destination device.</param>
        /// <param name="privatePort">Private port number.</param>
        /// <param name="publicPort">Public port number.</param>
        /// <returns>Task.</returns>
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
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating port map on local port {LocalPort} to public port {PublicPort} with device {DeviceEndpoint}.",
                    privatePort,
                    publicPort,
                    device.DeviceEndpoint);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        /// <summary>
        /// Attempts to removes rules from a device.
        /// </summary>
        /// <param name="device">Destination device.</param>
        /// <returns>Task.</returns>
        private Task RemoveRules(INatDevice device)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return Task.WhenAll(RemovePortMaps(device));
        }

        /// <summary>
        /// Attempts to remove port mappings from a device.
        /// </summary>
        /// <param name="device">Destination device.</param>
        /// <returns>IEnumerable.</returns>
        private IEnumerable<Task> RemovePortMaps(INatDevice device)
        {
            if (_networkConfig.UPnPCreateHttpPortMap)
            {
                yield return RemovePortMap(device, _networkConfig.HttpServerPortNumber, _networkConfig.PublicPort);
            }

            if (_appHost.ListenWithHttps)
            {
                yield return RemovePortMap(device, _networkConfig.HttpsPortNumber, _networkConfig.PublicHttpsPort);
            }
        }

        /// <summary>
        /// Attempts to remove a port mapping.
        /// </summary>
        /// <param name="device">Destination device.</param>
        /// <param name="privatePort">Private port number.</param>
        /// <param name="publicPort">Public port number.</param>
        /// <returns>Task.</returns>
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
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error removing port map on local port {LocalPort} to public port {PublicPort} with device {DeviceEndpoint}.",
                    privatePort,
                    publicPort,
                    device.DeviceEndpoint);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        /// <summary>
        /// Interface class that transpose Mono.NAT logs into our logging system.
        /// </summary>
        private class LoggingInterface : NATLogger
        {
            private readonly ILogger _logger;

            /// <summary>
            /// Initializes a new instance of the <see cref="LoggingInterface"/> class.
            /// </summary>
            /// <param name="logger">ILogger instance to use.</param>
            public LoggingInterface(ILogger logger)
            {
                _logger = logger;
            }

            public void Info(string message)
            {
                _logger.LogInformation(message.Replace("\r", " ", StringComparison.OrdinalIgnoreCase).Replace("\n", string.Empty, StringComparison.OrdinalIgnoreCase));
            }

            public void Debug(string message)
            {
                _logger.LogDebug(message.Replace("\r", " ", StringComparison.OrdinalIgnoreCase).Replace("\n", string.Empty, StringComparison.OrdinalIgnoreCase));
            }

            public void Error(string message)
            {
                _logger.LogError(message.Replace("\r", " ", StringComparison.OrdinalIgnoreCase).Replace("\n", string.Empty, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
