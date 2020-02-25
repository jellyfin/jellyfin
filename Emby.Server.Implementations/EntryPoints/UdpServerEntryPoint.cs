using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Udp;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
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
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// The UDP server.
        /// </summary>
        private UdpServer _udpServer;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServerEntryPoint" /> class.
        /// </summary>
        public UdpServerEntryPoint(
            ILogger<UdpServerEntryPoint> logger,
            IServerApplicationHost appHost)
        {
            _logger = logger;
            _appHost = appHost;


        }

        /// <inheritdoc />
        public async Task RunAsync()
        {
            _udpServer = new UdpServer(_logger, _appHost);
            _udpServer.Start(PortNumber, _cancellationTokenSource.Token);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _udpServer.Dispose();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _udpServer = null;

            _disposed = true;
        }
    }
}
