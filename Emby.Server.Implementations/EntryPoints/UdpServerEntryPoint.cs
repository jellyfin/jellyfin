#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.ApiClient;
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

        private readonly IServerApplicationHost _appHost;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger<UdpServerEntryPoint> _logger;
        private List<UdpProcess>? _udpProcess;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServerEntryPoint"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="appHost">Application Host instance.</param>
        /// <param name="configurationManager">IServerConfigurationManager instance.</param>
        public UdpServerEntryPoint(
            ILogger<UdpServerEntryPoint> logger,
            IServerApplicationHost appHost,
            IServerConfigurationManager configurationManager)
        {
            _logger = logger ?? throw new NullReferenceException(nameof(logger));
            _appHost = appHost ?? throw new NullReferenceException(nameof(appHost));
            _config = configurationManager;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            if (_config.Configuration.AutoDiscovery)
            {
                _udpProcess = UdpServer.CreateMulticastClients(
                    PortNumber,
                    ProcessMessage,
                    _logger,
                    restrictedToLAN: false,
                    enableTracing: _config.Configuration.AutoDiscoveryTracing);

                if (_udpProcess.Count == 0)
                {
                    _logger.LogWarning("Unable to start AutoDiscovery listener on UDP port {PortNumber}", PortNumber);
                }
                else
                {
                    _logger.LogDebug("Starting auto discovery.");
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _udpProcess?.Clear();
            _udpProcess = null;
            GC.SuppressFinalize(this);
        }

        private async Task ProcessMessage(UdpProcess client, string data, IPEndPoint receivedFrom)
        {
            if (data.Contains("who is JellyfinServer?", StringComparison.OrdinalIgnoreCase))
            {
                var response = new ServerDiscoveryInfo
                {
                    Address = _appHost.GetSmartApiUrl(receivedFrom.Address),
                    Id = _appHost.SystemId,
                    Name = _appHost.FriendlyName
                };
                string reply = JsonSerializer.Serialize(response);

                try
                {
                    await UdpServer.SendUnicast(client, reply, receivedFrom).ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Error sending response to {0}->{1}", client.LocalEndPoint.Address, receivedFrom.Address);
                }

                // TODO: this code does nothing. It calls a blank function.
                var parts = data.Split('|');
                if (parts.Length > 1)
                {
                    _appHost.EnableLoopback(parts[1]);
                }
            }
        }
    }
}
