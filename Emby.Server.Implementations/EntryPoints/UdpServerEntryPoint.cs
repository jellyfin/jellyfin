using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Udp;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
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
        private readonly INetworkManager _networkManager;
        private readonly bool _enableMultiSocketBinding;

        /// <summary>
        /// The UDP server.
        /// </summary>
        private List<UdpServer> _udpServers;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServerEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{UdpServerEntryPoint}"/> interface.</param>
        /// <param name="appHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="configuration">Instance of the <see cref="IConfiguration"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        public UdpServerEntryPoint(
            ILogger<UdpServerEntryPoint> logger,
            IServerApplicationHost appHost,
            IConfiguration configuration,
            IConfigurationManager configurationManager,
            INetworkManager networkManager)
        {
            _logger = logger;
            _appHost = appHost;
            _config = configuration;
            _configurationManager = configurationManager;
            _networkManager = networkManager;
            _udpServers = new List<UdpServer>();
            _enableMultiSocketBinding = OperatingSystem.IsWindows() || OperatingSystem.IsLinux();
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
                if (_enableMultiSocketBinding)
                {
                    // Add global broadcast socket
                    _udpServers.Add(new UdpServer(_logger, _appHost, _config, System.Net.IPAddress.Broadcast, PortNumber));

                    // Add bind address specific broadcast sockets
                    foreach (var bindAddress in _networkManager.GetInternalBindAddresses())
                    {
                        if (bindAddress.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            // Not supporting IPv6 right now
                            continue;
                        }

                        var broadcastAddress = NetworkExtensions.GetBroadcastAddress(bindAddress.Subnet);
                        _logger.LogDebug("Binding UDP server to {Address} on port {PortNumber}", broadcastAddress.ToString(), PortNumber);

                        _udpServers.Add(new UdpServer(_logger, _appHost, _config, broadcastAddress, PortNumber));
                    }
                }
                else
                {
                    _udpServers.Add(new UdpServer(_logger, _appHost, _config, System.Net.IPAddress.Any, PortNumber));
                }

                foreach (var server in _udpServers)
                {
                    server.Start(_cancellationTokenSource.Token);
                }
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
            _udpServers.ForEach(s => s.Dispose());
            _udpServers.Clear();

            _disposed = true;
        }
    }
}
