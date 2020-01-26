#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IDeviceDiscovery _deviceDiscovery;

        private readonly object _createdRulesLock = new object();
        private List<IPEndPoint> _createdRules = new List<IPEndPoint>();
        private Timer _timer;
        private string _lastConfigIdentifier;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalPortForwarding"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The application host.</param>
        /// <param name="config">The configuration manager.</param>
        /// <param name="deviceDiscovery">The device discovery.</param>
        public ExternalPortForwarding(
            ILogger<ExternalPortForwarding> logger,
            IServerApplicationHost appHost,
            IServerConfigurationManager config,
            IDeviceDiscovery deviceDiscovery)
        {
            _logger = logger;
            _appHost = appHost;
            _config = config;
            _deviceDiscovery = deviceDiscovery;
        }

        private string GetConfigIdentifier()
        {
            const char Separator = '|';
            var config = _config.Configuration;

            return new StringBuilder(32)
                .Append(config.EnableUPnP).Append(Separator)
                .Append(config.PublicPort).Append(Separator)
                .Append(_appHost.HttpPort).Append(Separator)
                .Append(_appHost.HttpsPort).Append(Separator)
                .Append(_appHost.EnableHttps).Append(Separator)
                .Append(config.EnableRemoteAccess).Append(Separator)
                .ToString();
        }

        private void OnConfigurationUpdated(object sender, EventArgs e)
        {
            if (!string.Equals(_lastConfigIdentifier, GetConfigIdentifier(), StringComparison.OrdinalIgnoreCase))
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

            _logger.LogDebug("Starting NAT discovery");

            NatUtility.DeviceFound += OnNatUtilityDeviceFound;
            NatUtility.StartDiscovery();

            _timer = new Timer(ClearCreatedRules, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _deviceDiscovery.DeviceDiscovered += OnDeviceDiscoveryDeviceDiscovered;

            _lastConfigIdentifier = GetConfigIdentifier();
        }

        private void Stop()
        {
            _logger.LogDebug("Stopping NAT discovery");

            NatUtility.StopDiscovery();
            NatUtility.DeviceFound -= OnNatUtilityDeviceFound;

            _timer?.Dispose();

            _deviceDiscovery.DeviceDiscovered -= OnDeviceDiscoveryDeviceDiscovered;
        }

        private void ClearCreatedRules(object state)
        {
            lock (_createdRulesLock)
            {
                _createdRules.Clear();
            }
        }

        private void OnDeviceDiscoveryDeviceDiscovered(object sender, GenericEventArgs<UpnpDeviceInfo> e)
        {
            NatUtility.Search(e.Argument.LocalIpAddress, NatProtocol.Upnp);
        }

        private void OnNatUtilityDeviceFound(object sender, DeviceEventArgs e)
        {
            try
            {
                var device = e.Device;

                CreateRules(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating port forwarding rules");
            }
        }

        private async void CreateRules(INatDevice device)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            // On some systems the device discovered event seems to fire repeatedly
            // This check will help ensure we're not trying to port map the same device over and over
            var address = device.DeviceEndpoint;

            lock (_createdRulesLock)
            {
                if (!_createdRules.Contains(address))
                {
                    _createdRules.Add(address);
                }
                else
                {
                    return;
                }
            }

            try
            {
                await CreatePortMap(device, _appHost.HttpPort, _config.Configuration.PublicPort).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating http port map");
                return;
            }

            try
            {
                await CreatePortMap(device, _appHost.HttpsPort, _config.Configuration.PublicHttpsPort).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating https port map");
            }
        }

        private Task<Mapping> CreatePortMap(INatDevice device, int privatePort, int publicPort)
        {
            _logger.LogDebug(
                "Creating port map on local port {0} to public port {1} with device {2}",
                privatePort,
                publicPort,
                device.DeviceEndpoint);

            return device.CreatePortMapAsync(
                new Mapping(Protocol.Tcp, privatePort, publicPort, 0, _appHost.Name));
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
