using MediaBrowser.Controller.Configuration;
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

namespace MediaBrowser.Dlna.Server
{
    public class SsdpHandler : IDisposable
    {
        private readonly AutoResetEvent _datagramPosted = new AutoResetEvent(false);
        private readonly ConcurrentQueue<Datagram> _messageQueue = new ConcurrentQueue<Datagram>();

        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly string _serverSignature;
        private bool _isDisposed;

        const string SSDPAddr = "239.255.255.250";
        const int SSDPPort = 1900;

        private readonly IPEndPoint _ssdpEndp = new IPEndPoint(IPAddress.Parse(SSDPAddr), SSDPPort);
        private readonly IPAddress _ssdpIp = IPAddress.Parse(SSDPAddr);

        private UdpClient _udpClient;

        private readonly Dictionary<Guid, List<UpnpDevice>> _devices = new Dictionary<Guid, List<UpnpDevice>>();

        private Timer _queueTimer;
        private Timer _notificationTimer;

        public SsdpHandler(ILogger logger, IServerConfigurationManager config, string serverSignature)
        {
            _logger = logger;
            _config = config;
            _serverSignature = serverSignature;

            Start();
        }

        private IEnumerable<UpnpDevice> Devices
        {
            get
            {
                UpnpDevice[] devs;
                lock (_devices)
                {
                    devs = _devices.Values.SelectMany(i => i).ToArray();
                }
                return devs;
            }
        }

        private void Start()
        {
            _udpClient = new UdpClient();
            _udpClient.Client.UseOnlyOverlappedIO = true;
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.ExclusiveAddressUse = false;
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, SSDPPort));
            _udpClient.JoinMulticastGroup(_ssdpIp, 2);
            _logger.Info("SSDP service started");
            Receive();

            StartNotificationTimer();
        }

        private void Receive()
        {
            try
            {
                _udpClient.BeginReceive(ReceiveCallback, null);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                var endpoint = new IPEndPoint(IPAddress.None, SSDPPort);
                var received = _udpClient.EndReceive(result, ref endpoint);

                if (_config.Configuration.DlnaOptions.EnableDebugLogging)
                {
                    _logger.Debug("{0} - SSDP Received a datagram", endpoint);
                }

                using (var reader = new StreamReader(new MemoryStream(received), Encoding.ASCII))
                {
                    var proto = (reader.ReadLine() ?? string.Empty).Trim();
                    var method = proto.Split(new[] { ' ' }, 2)[0];
                    var headers = new Headers();
                    for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }
                        var parts = line.Split(new[] { ':' }, 2);
                        headers[parts[0]] = parts[1].Trim();
                    }

                    if (_config.Configuration.DlnaOptions.EnableDebugLogging)
                    {
                        _logger.Debug("{0} - Datagram method: {1}", endpoint, method);
                        //_logger.Debug(headers);
                    }

                    if (string.Equals(method, "M-SEARCH", StringComparison.OrdinalIgnoreCase))
                    {
                        RespondToSearch(endpoint, headers["st"]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Failed to read SSDP message", ex);
            }

            if (!_isDisposed)
            {
                Receive();
            }
        }

        private void RespondToSearch(IPEndPoint endpoint, string req)
        {
            if (req == "ssdp:all")
            {
                req = null;
            }

            if (_config.Configuration.DlnaOptions.EnableDebugLogging)
            {
                _logger.Debug("RespondToSearch");
            }

            foreach (var d in Devices)
            {
                if (!string.IsNullOrEmpty(req) && !string.Equals(req, d.Type, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SendSearchResponse(endpoint, d);
            }
        }

        private void SendSearchResponse(IPEndPoint endpoint, UpnpDevice dev)
        {
            var headers = new Headers(true);
            headers.Add("CACHE-CONTROL", "max-age = 600");
            headers.Add("DATE", DateTime.Now.ToString("R"));
            headers.Add("EXT", "");
            headers.Add("LOCATION", dev.Descriptor.ToString());
            headers.Add("SERVER", _serverSignature);
            headers.Add("ST", dev.Type);
            headers.Add("USN", dev.USN);

            var msg = String.Format("HTTP/1.1 200 OK\r\n{0}\r\n", headers.HeaderBlock);

            SendDatagram(endpoint, dev.Address, msg, false);

            _logger.Info("{1} - Responded to a {0} request", dev.Type, endpoint);
        }

        private void SendDatagram(IPEndPoint endpoint, IPAddress localAddress, string msg, bool sticky)
        {
            if (_isDisposed)
            {
                return;
            }

            var dgram = new Datagram(endpoint, localAddress, _logger, msg, sticky);
            if (_messageQueue.Count == 0)
            {
                dgram.Send();
            }
            _messageQueue.Enqueue(dgram);
            StartQueueTimer();
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

                if (msg != null && (!_isDisposed || msg.Sticky))
                {
                    msg.Send();
                    if (msg.SendCount > 2)
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

        private void NotifyAll()
        {
            _logger.Debug("Sending alive notifications");
            foreach (var d in Devices)
            {
                NotifyDevice(d, "alive", false);
            }
        }

        private void NotifyDevice(UpnpDevice dev, string type, bool sticky)
        {
            _logger.Debug("NotifyDevice");
            var headers = new Headers(true);
            headers.Add("HOST", "239.255.255.250:1900");
            headers.Add("CACHE-CONTROL", "max-age = 600");
            headers.Add("LOCATION", dev.Descriptor.ToString());
            headers.Add("SERVER", _serverSignature);
            headers.Add("NTS", "ssdp:" + type);
            headers.Add("NT", dev.Type);
            headers.Add("USN", dev.USN);

            var msg = String.Format("NOTIFY * HTTP/1.1\r\n{0}\r\n", headers.HeaderBlock);

            _logger.Debug("{0} said {1}", dev.USN, type);
            SendDatagram(_ssdpEndp, dev.Address, msg, sticky);
        }

        public void RegisterNotification(Guid uuid, Uri descriptor, IPAddress address)
        {
            List<UpnpDevice> list;
            lock (_devices)
            {
                if (!_devices.TryGetValue(uuid, out list))
                {
                    _devices.Add(uuid, list = new List<UpnpDevice>());
                }
            }

            foreach (var t in new[]
            {
                "upnp:rootdevice", 
                "urn:schemas-upnp-org:device:MediaServer:1", 
                "urn:schemas-upnp-org:service:ContentDirectory:1", 
                "uuid:" + uuid
            })
            {
                list.Add(new UpnpDevice(uuid, t, descriptor, address));
            }

            NotifyAll();
            _logger.Debug("Registered mount {0} at {1}", uuid, descriptor);
        }

        private void UnregisterNotification(Guid uuid)
        {
            List<UpnpDevice> dl;
            lock (_devices)
            {
                if (!_devices.TryGetValue(uuid, out dl))
                {
                    return;
                }
                _devices.Remove(uuid);
            }
            foreach (var d in dl)
            {
                NotifyDevice(d, "byebye", true);
            }
            _logger.Debug("Unregistered mount {0}", uuid);
        }

        public void Dispose()
        {
            _isDisposed = true;
            while (_messageQueue.Count != 0)
            {
                _datagramPosted.WaitOne();
            }

            _udpClient.DropMulticastGroup(_ssdpIp);
            _udpClient.Close();

            DisposeNotificationTimer();
            DisposeQueueTimer();
            _datagramPosted.Dispose();
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

        private readonly object _notificationTimerSyncLock = new object();
        private void StartNotificationTimer()
        {
            const int intervalMs = 60000;

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
