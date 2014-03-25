using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MediaBrowser.Dlna.Server
{
    public class SsdpHandler : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly string _serverSignature;
        private bool _isDisposed = false;

        const string SSDPAddr = "239.255.255.250";
        const int SSDPPort = 1900;

        private readonly IPEndPoint _ssdpEndp = new IPEndPoint(IPAddress.Parse(SSDPAddr), SSDPPort);
        private readonly IPAddress _ssdpIp = IPAddress.Parse(SSDPAddr);

        private UdpClient _udpClient;

        private readonly Dictionary<Guid, List<UpnpDevice>> _devices = new Dictionary<Guid, List<UpnpDevice>>();

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
                        var parts = line.Split(new char[] { ':' }, 2);
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
                if (!string.IsNullOrEmpty(req) && req != d.Type)
                {
                    continue;
                }

                SendSearchResponse(endpoint, d);
            }
        }

        private void SendSearchResponse(IPEndPoint endpoint, UpnpDevice dev)
        {
            var headers = new RawHeaders();
            headers.Add("CACHE-CONTROL", "max-age = 600");
            headers.Add("DATE", DateTime.Now.ToString("R"));
            headers.Add("EXT", "");
            headers.Add("LOCATION", dev.Descriptor.ToString());
            headers.Add("SERVER", _serverSignature);
            headers.Add("ST", dev.Type);
            headers.Add("USN", dev.USN);

            SendDatagram(endpoint, String.Format("HTTP/1.1 200 OK\r\n{0}\r\n", headers.HeaderBlock), false);
            _logger.Info("{1} - Responded to a {0} request", dev.Type, endpoint);
        }

        private void SendDatagram(IPEndPoint endpoint, string msg, bool sticky)
        {
            if (_isDisposed)
            {
                return;
            }
            //var dgram = new Datagram(endpoint, msg, sticky);
            //if (messageQueue.Count == 0)
            //{
            //    dgram.Send();
            //}
            //messageQueue.Enqueue(dgram);
            //queueTimer.Enabled = true;
        }

        private void NotifyAll()
        {
            _logger.Debug("NotifyAll");
            foreach (var d in Devices)
            {
                NotifyDevice(d, "alive", false);
            }
        }

        private void NotifyDevice(UpnpDevice dev, string type, bool sticky)
        {
            _logger.Debug("NotifyDevice");
            var headers = new RawHeaders();
            headers.Add("HOST", "239.255.255.250:1900");
            headers.Add("CACHE-CONTROL", "max-age = 600");
            headers.Add("LOCATION", dev.Descriptor.ToString());
            headers.Add("SERVER", _serverSignature);
            headers.Add("NTS", "ssdp:" + type);
            headers.Add("NT", dev.Type);
            headers.Add("USN", dev.USN);

            SendDatagram(_ssdpEndp, String.Format("NOTIFY * HTTP/1.1\r\n{0}\r\n", headers.HeaderBlock), sticky);
            _logger.Debug("{0} said {1}", dev.USN, type);
        }

        private void RegisterNotification(Guid UUID, Uri Descriptor)
        {
            List<UpnpDevice> list;
            lock (_devices)
            {
                if (!_devices.TryGetValue(UUID, out list))
                {
                    _devices.Add(UUID, list = new List<UpnpDevice>());
                }
            }

            foreach (var t in new[] { "upnp:rootdevice", "urn:schemas-upnp-org:device:MediaServer:1", "urn:schemas-upnp-org:service:ContentDirectory:1", "uuid:" + UUID })
            {
                list.Add(new UpnpDevice(UUID, t, Descriptor));
            }

            NotifyAll();
            _logger.Debug("Registered mount {0}", UUID);
        }

        internal void UnregisterNotification(Guid UUID)
        {
            List<UpnpDevice> dl;
            lock (_devices)
            {
                if (!_devices.TryGetValue(UUID, out dl))
                {
                    return;
                }
                _devices.Remove(UUID);
            }
            foreach (var d in dl)
            {
                NotifyDevice(d, "byebye", true);
            }
            _logger.Debug("Unregistered mount {0}", UUID);
        }

        public void Dispose()
        {
            _isDisposed = true;
            //while (messageQueue.Count != 0)
            //{
            //    datagramPosted.WaitOne();
            //}

            _udpClient.DropMulticastGroup(_ssdpIp);
            _udpClient.Close();

            //notificationTimer.Enabled = false;
            //queueTimer.Enabled = false;
            //notificationTimer.Dispose();
            //queueTimer.Dispose();
            //datagramPosted.Dispose();
        }
    }
}
