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
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating MultiCast socket", ex);
                //StartSocketRetryTimer();
            }
        }

        public void Dispose()
        {
            _config.NamedConfigurationUpdated -= _config_ConfigurationUpdated;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;

            _isDisposed = true;

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
