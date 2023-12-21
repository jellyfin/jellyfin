using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Model.ApiClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;

namespace Jellyfin.Networking.Udp;

/// <summary>
/// Provides a Udp Server.
/// </summary>
public sealed class UdpServer : IDisposable
{
    /// <summary>
    /// The _logger.
    /// </summary>
    private readonly ILogger _logger;
    private readonly IServerApplicationHost _appHost;
    private readonly IConfiguration _config;

    private readonly byte[] _receiveBuffer = new byte[8192];

    private readonly Socket _udpSocket;
    private readonly IPEndPoint _endpoint;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpServer" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="appHost">The application host.</param>
    /// <param name="configuration">The configuration manager.</param>
    /// <param name="bindAddress"> The bind address.</param>
    /// <param name="port">The port.</param>
    public UdpServer(
        ILogger logger,
        IServerApplicationHost appHost,
        IConfiguration configuration,
        IPAddress bindAddress,
        int port)
    {
        _logger = logger;
        _appHost = appHost;
        _config = configuration;

        _endpoint = new IPEndPoint(bindAddress, port);

        _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            MulticastLoopback = false,
        };
        _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    }

    private async Task RespondToV2Message(EndPoint endpoint, CancellationToken cancellationToken)
    {
        string? localUrl = _config[AddressOverrideKey];
        if (string.IsNullOrEmpty(localUrl))
        {
            localUrl = _appHost.GetSmartApiUrl(((IPEndPoint)endpoint).Address);
        }

        if (string.IsNullOrEmpty(localUrl))
        {
            _logger.LogWarning("Unable to respond to server discovery request because the local ip address could not be determined.");
            return;
        }

        var response = new ServerDiscoveryInfo(localUrl, _appHost.SystemId, _appHost.FriendlyName);

        try
        {
            _logger.LogDebug("Sending AutoDiscovery response");
            await _udpSocket.SendToAsync(JsonSerializer.SerializeToUtf8Bytes(response), SocketFlags.None, endpoint, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Error sending response message");
        }
    }

    /// <summary>
    /// Starts the specified port.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    public void Start(CancellationToken cancellationToken)
    {
        _udpSocket.Bind(_endpoint);

        _ = Task.Run(async () => await BeginReceiveAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }

    private async Task BeginReceiveAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var endpoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                var result = await _udpSocket.ReceiveFromAsync(_receiveBuffer, endpoint, cancellationToken).ConfigureAwait(false);
                var text = Encoding.UTF8.GetString(_receiveBuffer, 0, result.ReceivedBytes);
                if (text.Contains("who is JellyfinServer?", StringComparison.OrdinalIgnoreCase))
                {
                    await RespondToV2Message(result.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _udpSocket.Dispose();
        _disposed = true;
    }
}
