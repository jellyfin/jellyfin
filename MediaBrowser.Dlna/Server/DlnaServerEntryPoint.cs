using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Net;

namespace MediaBrowser.Dlna.Server
{
    public class DlnaServerEntryPoint : IServerEntryPoint
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;

        private SsdpHandler _ssdpHandler;
        private readonly IApplicationHost _appHost;
        private readonly INetworkManager _network;

        public static DlnaServerEntryPoint Instance;

        public DlnaServerEntryPoint(IServerConfigurationManager config, ILogManager logManager, IApplicationHost appHost, INetworkManager network)
        {
            Instance = this;

            _config = config;
            _appHost = appHost;
            _network = network;
            _logger = logManager.GetLogger("DlnaServer");
        }

        public void Run()
        {
            _config.ConfigurationUpdated += ConfigurationUpdated;

            ReloadServer();
        }

        void ConfigurationUpdated(object sender, EventArgs e)
        {
            ReloadServer();
        }

        private void ReloadServer()
        {
            var isStarted = _ssdpHandler != null;

            if (_config.Configuration.DlnaOptions.EnableServer && !isStarted)
            {
                StartServer();
            }
            else if (!_config.Configuration.DlnaOptions.EnableServer && isStarted)
            {
                DisposeServer();
            }
        }

        private readonly object _syncLock = new object();
        private void StartServer()
        {
            var signature = GenerateServerSignature();

            lock (_syncLock)
            {
                try
                {
                    _ssdpHandler = new SsdpHandler(_logger, _config, signature);

                    RegisterEndpoints();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error starting Dlna server", ex);
                }
            }
        }

        private void RegisterEndpoints()
        {
            foreach (var address in _network.GetLocalIpAddresses())
            {
                var guid = address.GetMD5();

                var descriptorURI = "/mediabrowser/dlna/" + guid.ToString("N") + "/description.xml";

                var uri = new Uri(string.Format("http://{0}:{1}{2}", address, _config.Configuration.HttpServerPortNumber, descriptorURI));

                _ssdpHandler.RegisterNotification(guid, uri, IPAddress.Parse(address));
            }
        }

        public UpnpDevice GetServerUpnpDevice(string uuid)
        {
            return _ssdpHandler.Devices.FirstOrDefault(i => string.Equals(uuid, i.Uuid.ToString("N"), StringComparison.OrdinalIgnoreCase));
        }

        private void DisposeServer()
        {
            lock (_syncLock)
            {
                if (_ssdpHandler != null)
                {
                    try
                    {
                        _ssdpHandler.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error disposing Dlna server", ex);
                    }
                    _ssdpHandler = null;
                }
            }
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
              "{0}{1}/{2}.{3} UPnP/1.0 DLNADOC/1.5 MediaBrowser/{4}",
              pstring,
              IntPtr.Size * 8,
              os.Version.Major,
              os.Version.Minor,
              _appHost.ApplicationVersion
              );
        }

        public void Dispose()
        {
            DisposeServer();
        }
    }
}
