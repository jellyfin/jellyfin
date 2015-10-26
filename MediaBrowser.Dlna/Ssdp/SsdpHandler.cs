using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
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
    public class SsdpHandler : IDisposable, ISsdpHandler
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

        private readonly IApplicationHost _appHost;

        public SsdpHandler(ILogger logger, IServerConfigurationManager config, IApplicationHost appHost)
        {
            _logger = logger;
            _config = config;
            _appHost = appHost;

            _config.NamedConfigurationUpdated += _config_ConfigurationUpdated;
            _serverSignature = GenerateServerSignature();
        }

        private string GenerateServerSignature()
        {
            var os = Environment.OSVersion;
            var pstring = os.Platform.ToString();
            switch (os.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    pstring = "WIN";
                    break;
            }

            return String.Format(
              "{0}{1}/{2}.{3} UPnP/1.0 DLNADOC/1.5 Emby/{4}",
              pstring,
              IntPtr.Size * 8,
              os.Version.Major,
              os.Version.Minor,
              _appHost.ApplicationVersion
              );
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
            var headers = args.Headers;
            string st;

            if (string.Equals(args.Method, "M-SEARCH", StringComparison.OrdinalIgnoreCase) && headers.TryGetValue("st", out st))
            {
                TimeSpan delay = GetSearchDelay(headers);

                if (_config.GetDlnaConfiguration().EnableDebugLogging)
                {
                    _logger.Debug("Delaying search response by {0} seconds", delay.TotalSeconds);
                }

                await Task.Delay(delay).ConfigureAwait(false);

                RespondToSearch(args.EndPoint, st);
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
            RestartSocketListener();

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

            // UDP is unreliable, so send 3 requests at a time (per Upnp spec, sec 1.1.2)
            SendDatagram("M-SEARCH * HTTP/1.1", values, _ssdpEndp, localIp, true, 2);
        }

        public void SendDatagram(string header,
            Dictionary<string, string> values,
            EndPoint endpoint,
            EndPoint localAddress,
            bool isBroadcast,
            int sendCount)
        {
            var msg = new SsdpMessageBuilder().BuildMessage(header, values);
            var queued = false;

            var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLogging;

            for (var i = 0; i < sendCount; i++)
            {
                var dgram = new Datagram(endpoint, localAddress, _logger, msg, isBroadcast, enableDebugLogging);

                if (_messageQueue.Count == 0)
                {
                    dgram.Send();
                }
                else
                {
                    _messageQueue.Enqueue(dgram);
                    queued = true;
                }
            }

            if (queued)
            {
                StartQueueTimer();
            }
        }

        /// <summary>
        /// According to the spec: http://www.upnp.org/specs/arch/UPnP-arch-DeviceArchitecture-v1.0-20080424.pdf
        /// Device responses should be delayed a random duration between 0 and this many seconds to balance 
        /// load for the control point when it processes responses.  In my testing kodi times out after mx      
        /// so we will generate from mx - 1
        /// </summary>
        /// <param name="headers">The mx headers</param>
        /// <returns>A timepsan for the amount to delay before returning search result.</returns>
        private TimeSpan GetSearchDelay(Dictionary<string, string> headers)
        {
            string mx;
            headers.TryGetValue("mx", out mx);
            int delaySeconds = 0;
            if (!string.IsNullOrWhiteSpace(mx)
                && int.TryParse(mx, NumberStyles.Any, CultureInfo.InvariantCulture, out delaySeconds)
                && delaySeconds > 1)
            {
                delaySeconds = new Random().Next(delaySeconds - 1);
            }

            return TimeSpan.FromSeconds(delaySeconds);
        }

        private void RespondToSearch(EndPoint endpoint, string deviceType)
        {
            var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLogging;

            var isLogged = false;

            const string header = "HTTP/1.1 200 OK";

            foreach (var d in RegisteredDevices)
            {
                if (string.Equals(deviceType, "ssdp:all", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(deviceType, d.Type, StringComparison.OrdinalIgnoreCase))
                {
                    if (!isLogged)
                    {
                        if (enableDebugLogging)
                        {
                            _logger.Debug("Responding to search from {0} for {1}", endpoint, deviceType);
                        }
                        isLogged = true;
                    }

                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    values["CACHE-CONTROL"] = "max-age = 600";
                    values["DATE"] = DateTime.Now.ToString("R");
                    values["EXT"] = "";
                    values["LOCATION"] = d.Descriptor.ToString();
                    values["SERVER"] = _serverSignature;
                    values["ST"] = d.Type;
                    values["USN"] = d.USN;

                    SendDatagram(header, values, endpoint, null, false, 1);
                    SendDatagram(header, values, endpoint, new IPEndPoint(d.Address, 0), false, 1);
                    //SendDatagram(header, values, endpoint, null, true);

                    if (enableDebugLogging)
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
                    _queueTimer = new Timer(QueueTimerCallback, null, 500, Timeout.Infinite);
                }
                else
                {
                    _queueTimer.Change(500, Timeout.Infinite);
                }
            }
        }

        private void QueueTimerCallback(object state)
        {
            Datagram msg;
            while (_messageQueue.TryDequeue(out msg))
            {
                msg.Send();
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

        private void RestartSocketListener()
        {
            if (_isDisposed)
            {
                StopSocketRetryTimer();
                return;
            }

            try
            {
                _socket = CreateMulticastSocket();

                _logger.Info("MultiCast socket created");

                StopSocketRetryTimer();

                Receive();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating MultiCast socket", ex);
                //StartSocketRetryTimer();
            }
        }

        private Timer _socketRetryTimer;
        private readonly object _socketRetryLock = new object();
        private void StartSocketRetryTimer()
        {
            lock (_socketRetryLock)
            {
                if (_socketRetryTimer == null)
                {
                    _socketRetryTimer = new Timer(s => RestartSocketListener(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
                }
            }
        }

        private void StopSocketRetryTimer()
        {
            lock (_socketRetryLock)
            {
                if (_socketRetryTimer != null)
                {
                    _socketRetryTimer.Dispose();
                    _socketRetryTimer = null;
                }
            }
        }

        private void Receive()
        {
            try
            {
                var buffer = new byte[1024];

                EndPoint endpoint = new IPEndPoint(IPAddress.Any, SSDPPort);

                _socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint, ReceiveCallback,
                    buffer);
            }
            catch (ObjectDisposedException)
            {
                if (!_isDisposed)
                {
                    //StartSocketRetryTimer();
                }
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

                var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLogging;

                if (enableDebugLogging)
                {
                    _logger.Debug(Encoding.ASCII.GetString(received));
                }

                var args = SsdpHelper.ParseSsdpResponse(received);
                args.EndPoint = endpoint;

                if (IsSelfNotification(args))
                {
                    return;
                }

                if (enableDebugLogging)
                {
                    var headerTexts = args.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value));
                    var headerText = string.Join(",", headerTexts.ToArray());

                    _logger.Debug("{0} message received from {1} on {3}. Headers: {2}", args.Method, args.EndPoint, headerText, _socket.LocalEndPoint);
                }

                OnMessageReceived(args);
            }
            catch (ObjectDisposedException)
            {
                if (!_isDisposed)
                {
                    //StartSocketRetryTimer();
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

        internal bool IsSelfNotification(SsdpMessageEventArgs args)
        {
            // Avoid responding to self search messages
            //string serverId;
            //if (args.Headers.TryGetValue("X-EMBYSERVERID", out serverId) &&
            //    string.Equals(serverId, _appHost.SystemId, StringComparison.OrdinalIgnoreCase))
            //{
            //    return true;
            //}

            string server;
            args.Headers.TryGetValue("SERVER", out server);

            if (string.Equals(server, _serverSignature, StringComparison.OrdinalIgnoreCase))
            {
                //return true;
            }
            return false;
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
            var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLogging;

            if (enableDebugLogging)
            {
                _logger.Debug("Sending alive notifications");
            }
            foreach (var d in RegisteredDevices)
            {
                NotifyDevice(d, "alive", 1, enableDebugLogging);
            }
        }

        private void NotifyDevice(UpnpDevice dev, string type, int sendCount, bool logMessage)
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

            if (logMessage)
            {
                _logger.Debug("{0} said {1}", dev.USN, type);
            }

            SendDatagram(header, values, _ssdpEndp, new IPEndPoint(dev.Address, 0), true, sendCount);
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
                    NotifyDevice(d, "byebye", 2, true);
                }

                _logger.Debug("Unregistered mount {0}", uuid);
            }
        }

        private readonly object _notificationTimerSyncLock = new object();
        private int _aliveNotifierIntervalMs;
        private void ReloadAliveNotifier()
        {
            var config = _config.GetDlnaConfiguration();

            if (!config.BlastAliveMessages)
            {
                DisposeNotificationTimer();
                return;
            }

            var intervalMs = config.BlastAliveMessageIntervalSeconds * 1000;

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
