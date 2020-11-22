using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Udp;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class UdpServerEntryPoint.
    /// </summary>
    public sealed class UdpServerEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// The port of the UDP server.
        /// </summary>
        public const int PortNumber = 7359;

        private readonly ILogger<UdpServerEntryPoint> _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfiguration _config;
        private readonly IServerConfigurationManager _serverConfiguration;
        private UdpServer _udpServer;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServerEntryPoint"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{UdpServerEntryPoint}"/>.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/>.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        /// <param name="serverConfiguration">The <see cref="IServerConfigurationManager"/>.</param>
        public UdpServerEntryPoint(
            ILogger<UdpServerEntryPoint> logger,
            IServerApplicationHost appHost,
            IConfiguration configuration,
            IServerConfigurationManager serverConfiguration)
        {
            _logger = logger;
            _appHost = appHost;
            _config = configuration;
            _serverConfiguration = serverConfiguration;

            _serverConfiguration.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
        }

        /// <summary>
        /// The OnNamedConfigurationUpdated.
        /// </summary>
        /// <param name="sender">The <see cref="object"/>.</param>
        /// <param name="e">The e<see cref="ConfigurationUpdateEventArgs"/>.</param>
        private void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                ReloadComponent();
            }
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            ReloadComponent();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _serverConfiguration.NamedConfigurationUpdated -= OnNamedConfigurationUpdated;
            _cancellationTokenSource.Cancel();
            if (_udpServer != null)
            {
                _udpServer.Dispose();
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _udpServer = null;

            _disposed = true;
        }

        private void ReloadComponent()
        {
            if (_serverConfiguration.GetNetworkConfiguration().AutoDiscovery)
            {
                try
                {
                    _udpServer = new UdpServer(_logger, _appHost, _config);
                    _udpServer.Start(PortNumber, _cancellationTokenSource.Token);
                }
                catch (SocketException ex)
                {
                    _logger.LogWarning(ex, "Unable to start AutoDiscovery listener on UDP port {PortNumber}", PortNumber);
                }
            }
            else if (_udpServer != null)
            {
                _udpServer.Dispose();
                _udpServer = null;
            }
        }
    }
}
