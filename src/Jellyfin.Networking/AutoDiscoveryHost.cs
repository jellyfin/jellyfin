using System;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoDiscoveryHost" /> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{UdpServerEntryPoint}"/> interface.</param>
    /// <param name="appHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
    /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
    public AutoDiscoveryHost(
        ILogger<AutoDiscoveryHost> logger,
        IServerApplicationHost appHost,
        IConfigurationManager configurationManager)
    {
        _logger = logger;
        _appHost = appHost;
        _configurationManager = configurationManager;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configurationManager.GetNetworkConfiguration().AutoDiscovery)
        {
            return;
        }

        using var udpClient = new UdpClient(PortNumber);
        udpClient.MulticastLoopback = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                var text = Encoding.UTF8.GetString(result.Buffer);
                if (text.Contains("who is JellyfinServer?", StringComparison.OrdinalIgnoreCase))
                {
                    await RespondToV2Message(udpClient, result.RemoteEndPoint, stoppingToken).ConfigureAwait(false);
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
