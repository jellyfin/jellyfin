using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.ApiClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Networking;

/// <summary>
/// <see cref="BackgroundService"/> responsible for responding to auto-discovery messages.
/// </summary>
public sealed class AutoDiscoveryHost : BackgroundService
{
    /// <summary>
    /// The port to listen on for auto-discovery messages.
    /// </summary>
    private const int PortNumber = 7359;

    private readonly ILogger<AutoDiscoveryHost> _logger;
    private readonly IServerApplicationHost _appHost;
    private readonly IConfigurationManager _configurationManager;
    private readonly INetworkManager _networkManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoDiscoveryHost" /> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{AutoDiscoveryHost}"/>.</param>
    /// <param name="appHost">The <see cref="IServerApplicationHost"/>.</param>
    /// <param name="configurationManager">The <see cref="IConfigurationManager"/>.</param>
    /// <param name="networkManager">The <see cref="INetworkManager"/>.</param>
    public AutoDiscoveryHost(
        ILogger<AutoDiscoveryHost> logger,
        IServerApplicationHost appHost,
        IConfigurationManager configurationManager,
        INetworkManager networkManager)
    {
        _logger = logger;
        _appHost = appHost;
        _configurationManager = configurationManager;
        _networkManager = networkManager;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var networkConfig = _configurationManager.GetNetworkConfiguration();
        if (!networkConfig.AutoDiscovery)
        {
            return;
        }

        var udpServers = new List<Task>();
        // Linux needs to bind to the broadcast addresses to receive broadcast traffic
        if (OperatingSystem.IsLinux() && networkConfig.EnableIPv4)
        {
            udpServers.Add(ListenForAutoDiscoveryMessage(IPAddress.Broadcast, stoppingToken));
        }

        udpServers.AddRange(_networkManager.GetInternalBindAddresses()
            .Select(intf => ListenForAutoDiscoveryMessage(
                OperatingSystem.IsLinux() && intf.AddressFamily == AddressFamily.InterNetwork
                    ? NetworkUtils.GetBroadcastAddress(intf.Subnet)
                    : intf.Address,
                stoppingToken)));

        await Task.WhenAll(udpServers).ConfigureAwait(false);
    }

    private async Task ListenForAutoDiscoveryMessage(IPAddress address, CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient(new IPEndPoint(address, PortNumber));
        udpClient.MulticastLoopback = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                var text = Encoding.UTF8.GetString(result.Buffer);
                if (text.Contains("who is JellyfinServer?", StringComparison.OrdinalIgnoreCase))
                {
                    await RespondToV2Message(udpClient, result.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Failed to receive data from socket");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Broadcast socket operation cancelled");
            }
        }
    }

    private async Task RespondToV2Message(UdpClient udpClient, IPEndPoint endpoint, CancellationToken cancellationToken)
    {
        var localUrl = _appHost.GetSmartApiUrl(endpoint.Address);
        if (string.IsNullOrEmpty(localUrl))
        {
            _logger.LogWarning("Unable to respond to server discovery request because the local ip address could not be determined.");
            return;
        }

        var response = new ServerDiscoveryInfo(localUrl, _appHost.SystemId, _appHost.FriendlyName);

        try
        {
            _logger.LogDebug("Sending AutoDiscovery response");
            await udpClient
                .SendAsync(JsonSerializer.SerializeToUtf8Bytes(response).AsMemory(), endpoint, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Error sending response message");
        }
    }
}
