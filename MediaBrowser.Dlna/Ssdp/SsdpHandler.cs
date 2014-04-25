using MediaBrowser.Controller.Configuration;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
        }

        public event EventHandler<SsdpMessageEventArgs> MessageReceived;

        private void OnMessageReceived(SsdpMessageEventArgs args)
        {
            if (string.Equals(args.Method, "M-SEARCH", StringComparison.OrdinalIgnoreCase))
            {
                RespondToSearch(args.EndPoint, args.Headers["st"]);
            }
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

            StartNotificationTimer();
        }

        public void SendDatagram(string header,
            Dictionary<string, string> values,
            IPAddress localAddress,
            int sendCount = 1)
        {
            SendDatagram(header, values, _ssdpEndp, localAddress, sendCount);
        }

        public void SendDatagram(string header, 
            Dictionary<string, string> values, 
            IPEndPoint endpoint,
            IPAddress localAddress,
            int sendCount = 1)
        {
            var msg = new SsdpMessageBuilder().BuildMessage(header, values);

            var dgram = new Datagram(endpoint, localAddress, _logger, msg, sendCount);
            if (_messageQueue.Count == 0)
            {
                dgram.Send();
                return;
            }

            _messageQueue.Enqueue(dgram);
            StartQueueTimer();
        }

        public void SendDatagramFromDevices(string header,
            Dictionary<string, string> values,
            IPEndPoint endpoint,
            string deviceType)
        {
            foreach (var d in RegisteredDevices)
            {
                if (string.Equals(deviceType, "ssdp:all", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(deviceType, d.Type, StringComparison.OrdinalIgnoreCase))
                {
                    SendDatagram(header, values, endpoint, d.Address);
                }
            }
        }

        private void RespondToSearch(IPEndPoint endpoint, string deviceType)
        {
            if (_config.Configuration.DlnaOptions.EnableDebugLogging)
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

                    SendDatagram(header, values, endpoint, d.Address);

                    _logger.Info("{1} - Responded to a {0} request to {2}", d.Type, endpoint, d.Address.ToString());
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
                var receivedCount = _socket.EndReceiveFrom(result, ref endpoint);
                var received = (byte[])result.AsyncState;

                if (_config.Configuration.DlnaOptions.EnableDebugLogging)
                {
                    _logger.Debug("{0} - SSDP Received a datagram", endpoint);
                }

                using (var reader = new StreamReader(new MemoryStream(received), Encoding.ASCII))
                {
                    var proto = (reader.ReadLine() ?? string.Empty).Trim();
                    var method = proto.Split(new[] { ' ' }, 2)[0];
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }
                        var parts = line.Split(new[] { ':' }, 2);

                        if (parts.Length >= 2)
                        {
                            headers[parts[0]] = parts[1].Trim();
                        }
                    }

                    if (_config.Configuration.DlnaOptions.EnableDebugLogging)
                    {
                        _logger.Debug("{0} - Datagram method: {1}", endpoint, method);
                    }

                    OnMessageReceived(new SsdpMessageEventArgs
                    {
                        Method = method,
                        Headers = headers,
                        EndPoint = (IPEndPoint)endpoint
                    });
                }
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
            if (_config.Configuration.DlnaOptions.EnableDebugLogging)
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

            if (_config.Configuration.DlnaOptions.EnableDebugLogging)
            {
                _logger.Debug("{0} said {1}", dev.USN, type);
            }

            SendDatagram(header, values, dev.Address, sendCount);
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
        private void StartNotificationTimer()
        {
            if (!_config.Configuration.DlnaOptions.BlastAliveMessages)
            {
                return;
            }

            var intervalMs = _config.Configuration.DlnaOptions.BlastAliveMessageIntervalSeconds * 1000;

            lock (_notificationTimerSyncLock)
            {
                if (_notificationTimer == null)
                {
                    _notificationTimer = new Timer(state => NotifyAll(), null, intervalMs, intervalMs);
                }
                else
                {
                    _notificationTimer.Change(intervalMs, intervalMs);
                }
            }
        }

        private void DisposeNotificationTimer()
        {
            lock (_notificationTimerSyncLock)
            {
                if (_notificationTimer != null)
                {
                    _notificationTimer.Dispose();
                    _notificationTimer = null;
                }
            }
        }
    }
}
