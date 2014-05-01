using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Dlna.PlayTo;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Net;

namespace MediaBrowser.Dlna.Main
{
    public class DlnaEntryPoint : IServerEntryPoint
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly INetworkManager _network;

        private PlayToManager _manager;
        private readonly ISessionManager _sessionManager;
        private readonly IHttpClient _httpClient;
        private readonly IItemRepository _itemRepo;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;
        
        private SsdpHandler _ssdpHandler;

        private readonly List<Guid> _registeredServerIds = new List<Guid>();
        private bool _dlnaServerStarted;

        public DlnaEntryPoint(IServerConfigurationManager config, ILogManager logManager, IServerApplicationHost appHost, INetworkManager network, ISessionManager sessionManager, IHttpClient httpClient, IItemRepository itemRepo, ILibraryManager libraryManager, IUserManager userManager, IDlnaManager dlnaManager, IDtoService dtoService, IImageProcessor imageProcessor)
        {
            _config = config;
            _appHost = appHost;
            _network = network;
            _sessionManager = sessionManager;
            _httpClient = httpClient;
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
            _logger = logManager.GetLogger("Dlna");
        }

        public void Run()
        {
            StartSsdpHandler();
            ReloadComponents();

            _config.ConfigurationUpdated += ConfigurationUpdated;
        }

        void ConfigurationUpdated(object sender, EventArgs e)
        {
            ReloadComponents();
        }

        private void ReloadComponents()
        {
            var isServerStarted = _dlnaServerStarted;

            if (_config.Configuration.DlnaOptions.EnableServer && !isServerStarted)
            {
                StartDlnaServer();
            }
            else if (!_config.Configuration.DlnaOptions.EnableServer && isServerStarted)
            {
                DisposeDlnaServer();
            }

            var isPlayToStarted = _manager != null;

            if (_config.Configuration.DlnaOptions.EnablePlayTo && !isPlayToStarted)
            {
                StartPlayToManager();
            }
            else if (!_config.Configuration.DlnaOptions.EnablePlayTo && isPlayToStarted)
            {
                DisposePlayToManager();
            }
        }

        private void StartSsdpHandler()
        {
            try
            {
                _ssdpHandler = new SsdpHandler(_logger, _config, GenerateServerSignature());

                _ssdpHandler.Start();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error starting Dlna server", ex);
            }
        }

        private void DisposeSsdpHandler()
        {
            try
            {
                _ssdpHandler.Dispose();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error disposing ssdp handler", ex);
            }
        }

        public void StartDlnaServer()
        {
            try
            {
                RegisterServerEndpoints();

                _dlnaServerStarted = true;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error registering endpoint", ex);
            }
        }

        private void RegisterServerEndpoints()
        {
            foreach (var address in _network.GetLocalIpAddresses())
            {
                var guid = address.GetMD5();

                var descriptorURI = "/mediabrowser/dlna/" + guid.ToString("N") + "/description.xml";

                var uri = new Uri(string.Format("http://{0}:{1}{2}", address, _config.Configuration.HttpServerPortNumber, descriptorURI));

                var services = new List<string>
                {
                    "upnp:rootdevice", 
                    "urn:schemas-upnp-org:device:MediaServer:1", 
                    "urn:schemas-upnp-org:service:ContentDirectory:1", 
                    "uuid:" + guid.ToString("N")
                };
                
                _ssdpHandler.RegisterNotification(guid, uri, IPAddress.Parse(address), services);

                _registeredServerIds.Add(guid);
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

        private readonly object _syncLock = new object();
        private void StartPlayToManager()
        {
            lock (_syncLock)
            {
                try
                {
                    _manager = new PlayToManager(_logger,
                        _config,
                        _sessionManager,
                        _httpClient,
                        _itemRepo,
                        _libraryManager,
                        _userManager,
                        _dlnaManager,
                        _appHost,
                        _dtoService,
                        _imageProcessor,
                        _ssdpHandler);

                    _manager.Start();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error starting PlayTo manager", ex);
                }
            }
        }

        private void DisposePlayToManager()
        {
            lock (_syncLock)
            {
                if (_manager != null)
                {
                    try
                    {
                        _manager.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error disposing PlayTo manager", ex);
                    }
                    _manager = null;
                }
            }
        }

        public void Dispose()
        {
            DisposeDlnaServer();
            DisposePlayToManager();
            DisposeSsdpHandler();
        }

        public void DisposeDlnaServer()
        {
            foreach (var id in _registeredServerIds)
            {
                try
                {
                    _ssdpHandler.UnregisterNotification(id);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error unregistering server", ex);
                }
            }

            _registeredServerIds.Clear();

            _dlnaServerStarted = false;
        }
    }
}
