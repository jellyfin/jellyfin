using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.Ssdp
{
    public class SsdpHandler : IDisposable
    {
        private Socket _socket;

        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        const string SSDPAddr = "239.255.255.250";
        const int SSDPPort = 1900;
        private readonly string _serverSignature;

        private readonly IPAddress _ssdpIp = IPAddress.Parse(SSDPAddr);
        private readonly IPEndPoint _ssdpEndp = new IPEndPoint(IPAddress.Parse(SSDPAddr), SSDPPort);

        private Timer _queueTimer;
        private Timer _notificationTimer;

        private readonly AutoResetEvent _datagramPosted = new AutoResetEvent(false);
        private readonly ConcurrentQueue<Datagram> _messageQueue = new ConcurrentQueue<Datagram>();

        private bool _isDisposed;
        private readonly ConcurrentDictionary<Guid, List<UpnpDevice>> _devices = new ConcurrentDictionary<Guid, List<UpnpDevice>>();

        public SsdpHandler(ILogger logger, IServerConfigurationManager config, string serverSignature)
        {
            _logger = logger;
            _config = config;
            _serverSignature = serverSignature;

            _config.NamedConfigurationUpdated += _config_ConfigurationUpdated;
        }

        void _config_ConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                ReloadAliveNotifier();
            }
        }

        public event EventHandler<SsdpMessageEventArgs> MessageReceived;

        private async void OnMessageReceived(SsdpMessageEventArgs args)
        {
            if (string.Equals(args.Method, "M-SEARCH", StringComparison.OrdinalIgnoreCase))
            {
                string mx = null;
                args.Headers.TryGetValue("mx", out mx);
                int delaySeconds;
                if (!string.IsNullOrWhiteSpace(mx) &&
                    int.TryParse(mx, NumberStyles.Any, CultureInfo.InvariantCulture, out delaySeconds)
                    && delaySeconds > 0)
                {
                    if (_config.GetDlnaConfiguration().EnableDebugLogging)
                    {
                        _logger.Debug("Delaying search response by {0} seconds", delaySeconds);
                    }

                    await Task.Delay(delaySeconds * 1000).ConfigureAwait(false);
                }

                RespondToSearch(args.EndPoint, args.Headers["st"]);
            }

            EventHelper.FireEventIfNotNull(MessageReceived, this, args, _logger);
        }

        public IEnumerable<UpnpDevice> RegisteredDevices
        {
            get
            {
                return _devices.Values.SelectMany(i => i).ToList();
            }
        }

        public void Start()
        {
            _socket = CreateMulticastSocket();

            _logger.Info("SSDP service started");
            Receive();

            ReloadAliveNotifier();
        }

        public void SendSearchMessage(EndPoint localIp)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            values["HOST"] = "239.255.255.250:1900";
            values["USER-AGENT"] = "UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2";

            values["MAN"] = "\"ssdp:discover\"";

            // Search target
            values["ST"] = "ssdp:all";

            // Seconds to delay response
            values["MX"] = "3";

            SendDatagram("M-SEARCH * HTTP/1.1", values, localIp);
        }

        public void SendDatagram(string header,
            Dictionary<string, string> values,
            EndPoint localAddress,
            int sendCount = 1)
        {
            SendDatagram(header, values, _ssdpEndp, localAddress, false, sendCount);
        }

        public void SendDatagram(string header,
            Dictionary<string, string> values,
            EndPoint endpoint,
            EndPoint localAddress,
            bool ignoreBindFailure,
            int sendCount = 1)
        {
            var msg = new SsdpMessageBuilder().BuildMessage(header, values);

            var dgram = new Datagram(endpoint, localAddress, _logger, msg, sendCount, ignoreBindFailure);

            if (_messageQueue.Count == 0)
            {
                dgram.Send();
                return;
            }

            _messageQueue.Enqueue(dgram);
            StartQueueTimer();
        }

        private void RespondToSearch(EndPoint endpoint, string deviceType)
        {
            if (_config.GetDlnaConfiguration().EnableDebugLogging)
            {
                _logger.Debug("RespondToSearch");
            }

            const string header = "HTTP/1.1 200 OK";

            foreach (var d in RegisteredDevices)
            {
                if (string.Equals(deviceType, "ssdp:all", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(deviceType, d.Type, StringComparison.OrdinalIgnoreCase))
                {
                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    values["CACHE-CONTROL"] = "max-age = 600";
                    values["DATE"] = DateTime.Now.ToString("R");
                    values["EXT"] = "";
                    values["LOCATION"] = d.Descriptor.ToString();
                    values["SERVER"] = _serverSignature;
                    values["ST"] = d.Type;
                    values["USN"] = d.USN;

                    SendDatagram(header, values, endpoint, null, true);
                    SendDatagram(header, values, endpoint, new IPEndPoint(d.Address, 0), true);
                    //SendDatagram(header, values, endpoint, null, true);

                    if (_config.GetDlnaConfiguration().EnableDebugLogging)
                    {
                        _logger.Debug("{1} - Responded to a {0} request to {2}", d.Type, endpoint, d.Address.ToString());
                    }
                }
            }
        }

        private readonly object _queueTimerSyncLock = new object();
        private void StartQueueTimer()
        {
            lock (_queueTimerSyncLock)
            {
                if (_queueTimer == null)
                {
                    _queueTimer = new Timer(QueueTimerCallback, null, 1000, Timeout.Infinite);
                }
                else
                {
                    _queueTimer.Change(1000, Timeout.Infinite);
                }
            }
        }

        private void QueueTimerCallback(object state)
        {
            while (_messageQueue.Count != 0)
            {
                Datagram msg;
                if (!_messageQueue.TryPeek(out msg))
                {
                    continue;
                }

                if (msg != null && (!_isDisposed || msg.TotalSendCount > 1))
                {
                    msg.Send();
                    if (msg.SendCount > msg.TotalSendCount)
                    {
                        _messageQueue.TryDequeue(out msg);
                    }
                    break;
                }

                _messageQueue.TryDequeue(out msg);
            }

            _datagramPosted.Set();

            if (_messageQueue.Count > 0)
            {
                StartQueueTimer();
            }
            else
            {
                DisposeQueueTimer();
            }
        }

        private void Receive()
        {
            try
            {
                var buffer = new byte[1024];

                EndPoint endpoint = new IPEndPoint(IPAddress.Any, SSDPPort);

                _socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint, ReceiveCallback, buffer);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                _logger.Debug("Error in BeginReceiveFrom", ex);
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                EndPoint endpoint = new IPEndPoint(IPAddress.Any, SSDPPort);

                var length = _socket.EndReceiveFrom(result, ref endpoint);

                var received = (byte[])result.AsyncState;

                if (_config.GetDlnaConfiguration().EnableDebugLogging)
                {
                    _logger.Debug(Encoding.ASCII.GetString(received));
                }

                var args = SsdpHelper.ParseSsdpResponse(received);
                args.EndPoint = endpoint;

                if (_config.GetDlnaConfiguration().EnableDebugLogging)
                {
                    var headerTexts = args.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value));
                    var headerText = string.Join(",", headerTexts.ToArray());

                    _logger.Debug("{0} message received from {1} on {3}. Headers: {2}", args.Method, args.EndPoint, headerText, _socket.LocalEndPoint);
                }

                OnMessageReceived(args);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed to read SSDP message", ex);
            }

            if (_socket != null)
            {
                Receive();
            }
        }

        public void Dispose()
        {
            _config.NamedConfigurationUpdated -= _config_ConfigurationUpdated;

            _isDisposed = true;
            while (_messageQueue.Count != 0)
            {
                _datagramPosted.WaitOne();
            }

            DisposeSocket();
            DisposeQueueTimer();
            DisposeNotificationTimer();

            _datagramPosted.Dispose();
        }

        private void DisposeSocket()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }
        }

        private void DisposeQueueTimer()
        {
            lock (_queueTimerSyncLock)
            {
                if (_queueTimer != null)
                {
                    _queueTimer.Dispose();
                    _queueTimer = null;
                }
            }
        }

        private Socket CreateMulticastSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(_ssdpIp, 0));

            socket.Bind(new IPEndPoint(IPAddress.Any, SSDPPort));

            return socket;
        }

        private void NotifyAll()
        {
            if (_config.GetDlnaConfiguration().EnableDebugLogging)
            {
                _logger.Debug("Sending alive notifications");
            }
            foreach (var d in RegisteredDevices)
            {
                NotifyDevice(d, "alive");
            }
        }

        private void NotifyDevice(UpnpDevice dev, string type, int sendCount = 1)
        {
            const string header = "NOTIFY * HTTP/1.1";

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // If needed later for non-server devices, these headers will need to be dynamic 
            values["HOST"] = "239.255.255.250:1900";
            values["CACHE-CONTROL"] = "max-age = 600";
            values["LOCATION"] = dev.Descriptor.ToString();
            values["SERVER"] = _serverSignature;
            values["NTS"] = "ssdp:" + type;
            values["NT"] = dev.Type;
            values["USN"] = dev.USN;

            if (_config.GetDlnaConfiguration().EnableDebugLogging)
            {
                _logger.Debug("{0} said {1}", dev.USN, type);
            }

            SendDatagram(header, values, new IPEndPoint(dev.Address, 0), sendCount);
        }

        public void RegisterNotification(Guid uuid, Uri descriptionUri, IPAddress address, IEnumerable<string> services)
        {
            List<UpnpDevice> list;
            lock (_devices)
            {
                if (!_devices.TryGetValue(uuid, out list))
                {
                    _devices.TryAdd(uuid, list = new List<UpnpDevice>());
                }
            }

            list.AddRange(services.Select(i => new UpnpDevice(uuid, i, descriptionUri, address)));

            NotifyAll();
            _logger.Debug("Registered mount {0} at {1}", uuid, descriptionUri);
        }

        public void UnregisterNotification(Guid uuid)
        {
            List<UpnpDevice> dl;
            if (_devices.TryRemove(uuid, out dl))
            {

                foreach (var d in dl.ToList())
                {
                    NotifyDevice(d, "byebye", 2);
                }

                _logger.Debug("Unregistered mount {0}", uuid);
            }
        }

        private readonly object _notificationTimerSyncLock = new object();
        private int _aliveNotifierIntervalMs;
        private void ReloadAliveNotifier()
        {
            if (!_config.GetDlnaConfiguration().BlastAliveMessages)
            {
                DisposeNotificationTimer();
                return;
            }

            var intervalMs = _config.GetDlnaConfiguration().BlastAliveMessageIntervalSeconds * 1000;

            if (_notificationTimer == null || _aliveNotifierIntervalMs != intervalMs)
            {
                lock (_notificationTimerSyncLock)
                {
                    if (_notificationTimer == null)
                    {
                        _logger.Debug("Starting alive notifier");
                        const int initialDelayMs = 3000;
                        _notificationTimer = new Timer(state => NotifyAll(), null, initialDelayMs, intervalMs);
                    }
                    else
                    {
                        _logger.Debug("Updating alive notifier");
                        _notificationTimer.Change(intervalMs, intervalMs);
                    }

                    _aliveNotifierIntervalMs = intervalMs;
                }
            }
        }

        private void DisposeNotificationTimer()
        {
            lock (_notificationTimerSyncLock)
            {
                if (_notificationTimer != null)
                {
                    _logger.Debug("Stopping alive notifier");
                    _notificationTimer.Dispose();
                    _notificationTimer = null;
                }
            }
        }
    }
}
