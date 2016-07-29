using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MediaBrowser.Dlna.Ssdp
{
    public class SsdpHandler : IDisposable, ISsdpHandler
    {
        private Socket _multicastSocket;

        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        const string SSDPAddr = "239.255.255.250";
        const int SSDPPort = 1900;
        private readonly string _serverSignature;

        private readonly IPAddress _ssdpIp = IPAddress.Parse(SSDPAddr);
        private readonly IPEndPoint _ssdpEndp = new IPEndPoint(IPAddress.Parse(SSDPAddr), SSDPPort);

        private Timer _notificationTimer;

        private bool _isDisposed;
        private readonly Dictionary<string, List<UpnpDevice>> _devices = new Dictionary<string, List<UpnpDevice>>();

        private readonly IApplicationHost _appHost;

        private readonly int _unicastPort = 1901;
        private UdpClient _unicastClient;

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

        private async void OnMessageReceived(SsdpMessageEventArgs args, bool isMulticast)
        {
            if (IgnoreMessage(args, isMulticast))
            {
                return;
            }

            LogMessageReceived(args, isMulticast);

            var headers = args.Headers;
            string st;

            if (string.Equals(args.Method, "M-SEARCH", StringComparison.OrdinalIgnoreCase) && headers.TryGetValue("st", out st))
            {
                TimeSpan delay = GetSearchDelay(headers);

                if (_config.GetDlnaConfiguration().EnableDebugLog)
                {
                    _logger.Debug("Delaying search response by {0} seconds", delay.TotalSeconds);
                }

                await Task.Delay(delay).ConfigureAwait(false);

                RespondToSearch(args.EndPoint, st);
            }

            EventHelper.FireEventIfNotNull(MessageReceived, this, args, _logger);
        }

        internal void LogMessageReceived(SsdpMessageEventArgs args, bool isMulticast)
        {
            var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLog;

            if (enableDebugLogging)
            {
                var headerTexts = args.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value));
                var headerText = string.Join(",", headerTexts.ToArray());

                var protocol = isMulticast ? "Multicast" : "Unicast";
                var localEndPointString = args.LocalEndPoint == null ? "null" : args.LocalEndPoint.ToString();
                _logger.Debug("{0} message received from {1} on {3}. Protocol: {4} Headers: {2}", args.Method, args.EndPoint, headerText, localEndPointString, protocol);
            }
        }

        internal bool IgnoreMessage(SsdpMessageEventArgs args, bool isMulticast)
        {
            string usn;
            if (args.Headers.TryGetValue("USN", out usn))
            {
                // USN=uuid:b67df29b5c379445fde78c3774ab518d::urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1
                if (RegisteredDevices.Any(i => string.Equals(i.USN, usn, StringComparison.OrdinalIgnoreCase)))
                {
                    //var headerTexts = args.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value));
                    //var headerText = string.Join(",", headerTexts.ToArray());

                    //var protocol = isMulticast ? "Multicast" : "Unicast";
                    //var localEndPointString = args.LocalEndPoint == null ? "null" : args.LocalEndPoint.ToString();
                    //_logger.Debug("IGNORING {0} message received from {1} on {3}. Protocol: {4} Headers: {2}", args.Method, args.EndPoint, headerText, localEndPointString, protocol);

                    return true;
                }
            }

            string serverId;
            if (args.Headers.TryGetValue("X-EMBY-SERVERID", out serverId))
            {
                if (string.Equals(serverId, _appHost.SystemId, StringComparison.OrdinalIgnoreCase))
                {
                    //var headerTexts = args.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value));
                    //var headerText = string.Join(",", headerTexts.ToArray());

                    //var protocol = isMulticast ? "Multicast" : "Unicast";
                    //var localEndPointString = args.LocalEndPoint == null ? "null" : args.LocalEndPoint.ToString();
                    //_logger.Debug("IGNORING {0} message received from {1} on {3}. Protocol: {4} Headers: {2}", args.Method, args.EndPoint, headerText, localEndPointString, protocol);

                    return true;
                }
            }

            return false;
        }

        public IEnumerable<UpnpDevice> RegisteredDevices
        {
            get
            {
                lock (_devices)
                {
                    var devices = _devices.ToList();

                    return devices.SelectMany(i => i.Value).ToList();
                }
            }
        }

        public void Start()
        {
            DisposeSocket();
            StopAliveNotifier();

            RestartSocketListener();
            ReloadAliveNotifier();

            CreateUnicastClient();

            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                Start();
            }
        }

        public void SendSearchMessage(EndPoint localIp)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            values["HOST"] = "239.255.255.250:1900";
            values["USER-AGENT"] = "UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2";
            values["X-EMBY-SERVERID"] = _appHost.SystemId;

            values["MAN"] = "\"ssdp:discover\"";

            // Search target
            values["ST"] = "ssdp:all";

            // Seconds to delay response
            values["MX"] = "3";

            var header = "M-SEARCH * HTTP/1.1";

            var msg = new SsdpMessageBuilder().BuildMessage(header, values);

            // UDP is unreliable, so send 3 requests at a time (per Upnp spec, sec 1.1.2)
            SendDatagram(msg, _ssdpEndp, localIp, true);

            SendUnicastRequest(msg);
        }

        public async void SendDatagram(string msg,
            EndPoint endpoint,
            EndPoint localAddress,
            bool isBroadcast,
            int sendCount = 3)
        {
            var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLog;

            for (var i = 0; i < sendCount; i++)
            {
                if (i > 0)
                {
                    await Task.Delay(500).ConfigureAwait(false);
                }

                var dgram = new Datagram(endpoint, localAddress, _logger, msg, isBroadcast, enableDebugLogging);
                dgram.Send();
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
            var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLog;

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

                    var msg = new SsdpMessageBuilder().BuildMessage(header, values);

                    SendDatagram(msg, endpoint, null, false, 2);
                    SendDatagram(msg, endpoint, new IPEndPoint(d.Address, 0), false, 2);
                    //SendDatagram(header, values, endpoint, null, true);

                    if (enableDebugLogging)
                    {
                        _logger.Debug("{1} - Responded to a {0} request to {2}", d.Type, endpoint, d.Address.ToString());
                    }
                }
            }
        }

        private void RestartSocketListener()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                _multicastSocket = CreateMulticastSocket();

                _logger.Info("MultiCast socket created");

                Receive();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating MultiCast socket", ex);
                //StartSocketRetryTimer();
            }
        }

        private void Receive()
        {
            try
            {
                var buffer = new byte[1024];

                EndPoint endpoint = new IPEndPoint(IPAddress.Any, SSDPPort);

                _multicastSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint, ReceiveCallback, buffer);
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

                var length = _multicastSocket.EndReceiveFrom(result, ref endpoint);

                var received = (byte[])result.AsyncState;

                var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLog;

                if (enableDebugLogging)
                {
                    _logger.Debug(Encoding.ASCII.GetString(received));
                }

                var args = SsdpHelper.ParseSsdpResponse(received);
                args.EndPoint = endpoint;

                OnMessageReceived(args, true);
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

            if (_multicastSocket != null)
            {
                Receive();
            }
        }

        public void Dispose()
        {
            _config.NamedConfigurationUpdated -= _config_ConfigurationUpdated;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

            _isDisposed = true;

            DisposeUnicastClient();
            DisposeSocket();
            StopAliveNotifier();
        }

        private void DisposeSocket()
        {
            if (_multicastSocket != null)
            {
                _multicastSocket.Close();
                _multicastSocket.Dispose();
                _multicastSocket = null;
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
            var enableDebugLogging = _config.GetDlnaConfiguration().EnableDebugLog;

            if (enableDebugLogging)
            {
                _logger.Debug("Sending alive notifications");
            }
            foreach (var d in RegisteredDevices)
            {
                NotifyDevice(d, "alive", enableDebugLogging);
            }
        }

        private void NotifyDevice(UpnpDevice dev, string type, bool logMessage)
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

            var msg = new SsdpMessageBuilder().BuildMessage(header, values);

            SendDatagram(msg, _ssdpEndp, new IPEndPoint(dev.Address, 0), true, 2);
            //SendUnicastRequest(msg, 1);
        }

        public void RegisterNotification(string uuid, Uri descriptionUri, IPAddress address, IEnumerable<string> services)
        {
            lock (_devices)
            {
                List<UpnpDevice> list;
                List<UpnpDevice> dl;
                if (_devices.TryGetValue(uuid, out dl))
                {
                    list = dl;
                }
                else
                {
                    list = new List<UpnpDevice>();
                    _devices[uuid] = list;
                }

                list.AddRange(services.Select(i => new UpnpDevice(uuid, i, descriptionUri, address)));

                NotifyAll();
                _logger.Debug("Registered mount {0} at {1}", uuid, descriptionUri);
            }
        }

        public void UnregisterNotification(string uuid)
        {
            lock (_devices)
            {
                List<UpnpDevice> dl;
                if (_devices.TryGetValue(uuid, out dl))
                {
                    _devices.Remove(uuid);
                    foreach (var d in dl.ToList())
                    {
                        NotifyDevice(d, "byebye", true);
                    }

                    _logger.Debug("Unregistered mount {0}", uuid);
                }
            }
        }

        private void CreateUnicastClient()
        {
            if (_unicastClient == null)
            {
                try
                {
                    _unicastClient = new UdpClient(_unicastPort);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error creating unicast client", ex);
                }

                UnicastSetBeginReceive();
            }
        }

        private void DisposeUnicastClient()
        {
            if (_unicastClient != null)
            {
                try
                {
                    _unicastClient.Close();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing unicast client", ex);
                }

                _unicastClient = null;
            }
        }

        /// <summary>
        /// Listen for Unicast SSDP Responses
        /// </summary>
        private void UnicastSetBeginReceive()
        {
            try
            {
                var ipRxEnd = new IPEndPoint(IPAddress.Any, _unicastPort);
                var udpListener = new UdpState { EndPoint = ipRxEnd };

                udpListener.UdpClient = _unicastClient;
                _unicastClient.BeginReceive(UnicastReceiveCallback, udpListener);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in UnicastSetBeginReceive", ex);
            }
        }

        /// <summary>
        /// The UnicastReceiveCallback receives Http Responses 
        /// and Fired the SatIpDeviceFound Event for adding the SatIpDevice  
        /// </summary>
        /// <param name="ar"></param>
        private void UnicastReceiveCallback(IAsyncResult ar)
        {
            var udpClient = ((UdpState)(ar.AsyncState)).UdpClient;
            var endpoint = ((UdpState)(ar.AsyncState)).EndPoint;
            if (udpClient.Client != null)
            {
                try
                {
                    var responseBytes = udpClient.EndReceive(ar, ref endpoint);
                    var args = SsdpHelper.ParseSsdpResponse(responseBytes);

                    args.EndPoint = endpoint;

                    OnMessageReceived(args, false);

                    UnicastSetBeginReceive();
                }
                catch (ObjectDisposedException)
                {

                }
                catch (SocketException)
                {

                }
                catch (Exception)
                {
                    // If called while shutting down, seeing a NullReferenceException inside EndReceive
                }
            }
        }

        private void SendUnicastRequest(string request, int sendCount = 3)
        {
            if (_unicastClient == null)
            {
                return;
            }

            var ipSsdp = IPAddress.Parse(SSDPAddr);
            var ipTxEnd = new IPEndPoint(ipSsdp, SSDPPort);

            SendUnicastRequest(request, ipTxEnd, sendCount);
        }

        private async void SendUnicastRequest(string request, IPEndPoint toEndPoint, int sendCount = 3)
        {
            if (_unicastClient == null)
            {
                return;
            }

            //_logger.Debug("Sending unicast request");

            byte[] req = Encoding.ASCII.GetBytes(request);

            try
            {
                for (var i = 0; i < sendCount; i++)
                {
                    if (i > 0)
                    {
                        await Task.Delay(50).ConfigureAwait(false);
                    }
                    _unicastClient.Send(req, req.Length, toEndPoint);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in SendUnicastRequest", ex);
            }
        }

        private readonly object _notificationTimerSyncLock = new object();
        private int _aliveNotifierIntervalMs;
        private void ReloadAliveNotifier()
        {
            var config = _config.GetDlnaConfiguration();

            if (!config.BlastAliveMessages)
            {
                StopAliveNotifier();
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

        private void StopAliveNotifier()
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

        public class UdpState
        {
            public UdpClient UdpClient;
            public IPEndPoint EndPoint;
        }
    }
}
