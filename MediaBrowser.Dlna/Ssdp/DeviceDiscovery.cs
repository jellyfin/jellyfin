using MediaBrowser.Common.Events;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace MediaBrowser.Dlna.Ssdp
{
    public class DeviceDiscovery : IDeviceDiscovery, IDisposable
    {
        private bool _disposed;

        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private SsdpHandler _ssdpHandler;
        private readonly CancellationTokenSource _tokenSource;
        private readonly IServerApplicationHost _appHost;

        public event EventHandler<SsdpMessageEventArgs> DeviceDiscovered;
        public event EventHandler<SsdpMessageEventArgs> DeviceLeft;
        private readonly INetworkManager _networkManager;

        public DeviceDiscovery(ILogger logger, IServerConfigurationManager config, IServerApplicationHost appHost, INetworkManager networkManager)
        {
            _tokenSource = new CancellationTokenSource();

            _logger = logger;
            _config = config;
            _appHost = appHost;
            _networkManager = networkManager;
        }

		private List<IPAddress> GetLocalIpAddresses()
		{
		    return _networkManager.GetLocalIpAddresses().ToList();
		}

        public void Start(SsdpHandler ssdpHandler)
        {
            _ssdpHandler = ssdpHandler;
            _ssdpHandler.MessageReceived += _ssdpHandler_MessageReceived;

            foreach (var localIp in GetLocalIpAddresses())
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

        async void _ssdpHandler_MessageReceived(object sender, SsdpMessageEventArgs e)
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
                if (e.LocalEndPoint == null)
                {
                    var ip = (await _appHost.GetLocalIpAddresses().ConfigureAwait(false)).FirstOrDefault(i => !IPAddress.IsLoopback(i));
                    if (ip != null)
                    {
                        e.LocalEndPoint = new IPEndPoint(ip, 0);
                    }
                }

                if (e.LocalEndPoint != null)
                {
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

        private void CreateListener(IPAddress localIp)
        {
            Task.Factory.StartNew(async (o) =>
            {
                try
                {
					_logger.Info("Creating SSDP listener on {0}", localIp);

					var endPoint = new IPEndPoint(localIp, 1900);

                    using (var socket = GetMulticastSocket(localIp, endPoint))
                    {
                        var receiveBuffer = new byte[64000];

                        CreateNotifier(localIp);

                        while (!_tokenSource.IsCancellationRequested)
                        {
                            var receivedBytes = await socket.ReceiveAsync(receiveBuffer, 0, 64000);

                            if (receivedBytes > 0)
                            {
                                var args = SsdpHelper.ParseSsdpResponse(receiveBuffer);
                                args.EndPoint = endPoint;
                                args.LocalEndPoint = new IPEndPoint(localIp, 0);

                                _ssdpHandler.LogMessageReceived(args, true);

                                TryCreateDevice(args);
                            }
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

            if (String.Equals(nts, "ssdp:byebye", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(args.Method, "NOTIFY", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_disposed)
                    {
                        EventHelper.FireEventIfNotNull(DeviceLeft, this, args, _logger);
                    }
                }

                return;
            }

            string usn;
            if (!args.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

            string nt;
            if (!args.Headers.TryGetValue("NT", out nt)) nt = string.Empty;

            // Need to be able to download device description
            string location;
            if (!args.Headers.TryGetValue("Location", out location) ||
                string.IsNullOrEmpty(location))
            {
                return;
            }

            EventHelper.FireEventIfNotNull(DeviceDiscovered, this, args, _logger);
        }

        public void Dispose()
        {
            if (_ssdpHandler != null)
            {
                _ssdpHandler.MessageReceived -= _ssdpHandler_MessageReceived;
            }

            if (!_disposed)
            {
                _disposed = true;
                _tokenSource.Cancel();
            }
        }
    }
}
