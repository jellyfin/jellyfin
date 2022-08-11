using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Udp;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
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

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<UdpServerEntryPoint> _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfiguration _config;
        private readonly IConfigurationManager _configurationManager;

        /// <summary>
        /// The UDP server.
        /// </summary>
        private UdpServer? _udpServer;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServerEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{UdpServerEntryPoint}"/> interface.</param>
        /// <param name="appHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="configuration">Instance of the <see cref="IConfiguration"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        public UdpServerEntryPoint(
            ILogger<UdpServerEntryPoint> logger,
            IServerApplicationHost appHost,
            IConfiguration configuration,
            IConfigurationManager configurationManager)
        {
            _logger = logger;
            _appHost = appHost;
            _config = configuration;
            _configurationManager = configurationManager;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            CheckDisposed();

            if (!_configurationManager.GetNetworkConfiguration().AutoDiscovery)
            {
                return Task.CompletedTask;
            }

            try
            {
                _udpServer = new UdpServer(_logger, _appHost, _config, PortNumber);
                _udpServer.Start(_cancellationTokenSource.Token);
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Unable to start AutoDiscovery listener on UDP port {PortNumber}", PortNumber);
            }

            return Task.CompletedTask;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _udpServer?.Dispose();
            _udpServer = null;

            _disposed = true;
        }
    }
}
