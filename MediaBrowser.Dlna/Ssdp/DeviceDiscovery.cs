using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.Ssdp
{
    public class DeviceDiscovery : IDisposable
    {
        private bool _disposed;

        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly SsdpHandler _ssdpHandler;
        private readonly CancellationTokenSource _tokenSource;
        private readonly IServerApplicationHost _appHost;

        public event EventHandler<SsdpMessageEventArgs> DeviceDiscovered;
        public event EventHandler<SsdpMessageEventArgs> DeviceLeft;

        public DeviceDiscovery(ILogger logger, IServerConfigurationManager config, SsdpHandler ssdpHandler, IServerApplicationHost appHost)
        {
            _tokenSource = new CancellationTokenSource();

            _logger = logger;
            _config = config;
            _ssdpHandler = ssdpHandler;
            _appHost = appHost;
        }

        public void Start()
        {
            _ssdpHandler.MessageReceived += _ssdpHandler_MessageReceived;

            foreach (var network in GetNetworkInterfaces())
            {
                _logger.Debug("Found interface: {0}. Type: {1}. Status: {2}", network.Name, network.NetworkInterfaceType, network.OperationalStatus);

                if (!network.SupportsMulticast || OperationalStatus.Up != network.OperationalStatus || !network.GetIPProperties().MulticastAddresses.Any())
                    continue;
                
                var ipV4 = network.GetIPProperties().GetIPv4Properties();
                if (null == ipV4)
                    continue;

                var localIps = network.GetIPProperties().UnicastAddresses
                    .Where(i => i.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(i => i.Address)
                    .ToList();

                foreach (var localIp in localIps)
                {
                    try
                    {
                        CreateListener(localIp);
                    }
                    catch (Exception e)
                    {
                        _logger.ErrorException("Failed to Initilize Socket", e);
                    }
                }
            }
        }
        
        void _ssdpHandler_MessageReceived(object sender, SsdpMessageEventArgs e)
        {
            string nts;
            e.Headers.TryGetValue("NTS", out nts);

            if (String.Equals(e.Method, "NOTIFY", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(nts, "ssdp:byebye", StringComparison.OrdinalIgnoreCase) &&
                !_disposed)
            {
                EventHelper.FireEventIfNotNull(DeviceLeft, this, e, _logger);
                return;
            }

            try
            {
                var ip = _appHost.HttpServerIpAddresses.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(ip))
                {
                    e.LocalIp = IPAddress.Parse(ip);
                    TryCreateDevice(e);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating play to controller", ex);
            }
        }

        private IEnumerable<NetworkInterface> GetNetworkInterfaces()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in GetAllNetworkInterfaces", ex);
                return new List<NetworkInterface>();
            }
        }
        private void CreateListener(IPAddress localIp)
        {
            Task.Factory.StartNew(async (o) =>
            {
                try
                {
                    var endPoint = new IPEndPoint(localIp, 1900);

                    var socket = GetMulticastSocket(localIp, endPoint);

                    _logger.Info("Creating SSDP listener on {0}", localIp);

                    var receiveBuffer = new byte[64000];

                    CreateNotifier(localIp);

                    while (!_tokenSource.IsCancellationRequested)
                    {
                        var receivedBytes = await socket.ReceiveAsync(receiveBuffer, 0, 64000);

                        if (receivedBytes > 0)
                        {
                            var args = SsdpHelper.ParseSsdpResponse(receiveBuffer);
                            args.EndPoint = endPoint;
                            args.LocalIp = localIp;

                            TryCreateDevice(args);
                        }
                    }

                    _logger.Info("SSDP listener - Task completed");
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error in listener", e);
                }

            }, _tokenSource.Token, TaskCreationOptions.LongRunning);
        }

        private void CreateNotifier(IPAddress localIp)
        {
            Task.Factory.StartNew(async (o) =>
            {
                try
                {
                    while (true)
                    {
                        _ssdpHandler.SendSearchMessage(new IPEndPoint(localIp, 1900));

                        var delay = _config.GetDlnaConfiguration().ClientDiscoveryIntervalSeconds * 1000;

                        await Task.Delay(delay, _tokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in notifier", ex);
                }

            }, _tokenSource.Token, TaskCreationOptions.LongRunning);

        }

        private Socket GetMulticastSocket(IPAddress localIpAddress, EndPoint localEndpoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), localIpAddress));
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);

            socket.Bind(localEndpoint);
            
            return socket;
        }

        private void TryCreateDevice(SsdpMessageEventArgs args)
        {
            string nts;
            args.Headers.TryGetValue("NTS", out nts);

            string usn;
            if (!args.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

            string nt;
            if (!args.Headers.TryGetValue("NT", out nt)) nt = string.Empty;

            // Ignore when a device is indicating it's shutting down
            if (string.Equals(nts, "ssdp:byebye", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Need to be able to download device description
            string location;
            if (!args.Headers.TryGetValue("Location", out location) ||
                string.IsNullOrEmpty(location))
            {
                return;
            }

            if (_config.GetDlnaConfiguration().EnableDebugLogging)
            {
                var headerTexts = args.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value));
                var headerText = string.Join(",", headerTexts.ToArray());

                _logger.Debug("{0} Device message received from {1}. Headers: {2}", args.Method, args.EndPoint, headerText);
            }

            EventHelper.FireEventIfNotNull(DeviceDiscovered, this, args, _logger);
        }

        public void Dispose()
        {
            _ssdpHandler.MessageReceived -= _ssdpHandler_MessageReceived;

            if (!_disposed)
            {
                _disposed = true;
                _tokenSource.Cancel();
            }
        }
    }
}
