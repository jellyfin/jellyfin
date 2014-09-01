using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MediaBrowser.Dlna.PlayTo
{
    class PlayToManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;

        private readonly IItemRepository _itemRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IImageProcessor _imageProcessor;
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;
        private readonly IUserDataManager _userDataManager;

        private readonly DeviceDiscovery _deviceDiscovery;
        
        public PlayToManager(ILogger logger, ISessionManager sessionManager, IItemRepository itemRepository, ILibraryManager libraryManager, IUserManager userManager, IDlnaManager dlnaManager, IServerApplicationHost appHost, IImageProcessor imageProcessor, DeviceDiscovery deviceDiscovery, IHttpClient httpClient, IServerConfigurationManager config, IUserDataManager userDataManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _itemRepository = itemRepository;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _appHost = appHost;
            _imageProcessor = imageProcessor;
            _deviceDiscovery = deviceDiscovery;
            _httpClient = httpClient;
            _config = config;
            _userDataManager = userDataManager;
        }

        public void Start()
        {
            _deviceDiscovery.DeviceDiscovered += _deviceDiscovery_DeviceDiscovered;
        }

        async void _deviceDiscovery_DeviceDiscovered(object sender, SsdpMessageEventArgs e)
        {
            var localIp = e.LocalIp;

            string usn;
            if (!e.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

            string nt;
            if (!e.Headers.TryGetValue("NT", out nt)) nt = string.Empty;

            string location;
            if (!e.Headers.TryGetValue("Location", out location)) location = string.Empty;
            
            // It has to report that it's a media renderer
            if (usn.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return;
            }

            if (_sessionManager.Sessions.Any(i => usn.IndexOf(i.DeviceId, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return;
            }

            try
            {
                var uri = new Uri(location);

                var device = await Device.CreateuPnpDeviceAsync(uri, _httpClient, _config, _logger).ConfigureAwait(false);

                if (device.RendererCommands != null)
                {
                    var sessionInfo = await _sessionManager.LogSessionActivity(device.Properties.ClientType, _appHost.ApplicationVersion.ToString(), device.Properties.UUID, device.Properties.Name, uri.OriginalString, null)
                        .ConfigureAwait(false);

                    var controller = sessionInfo.SessionController as PlayToController;

                    if (controller == null)
                    {
                        var serverAddress = GetServerAddress(localIp);

                        sessionInfo.SessionController = controller = new PlayToController(sessionInfo,
                            _sessionManager,
                            _itemRepository,
                            _libraryManager,
                            _logger,
                            _dlnaManager,
                            _userManager,
                            _imageProcessor,
                            serverAddress,
                            _deviceDiscovery,
                            _userDataManager);

                        controller.Init(device);

                        var profile = _dlnaManager.GetProfile(device.Properties.ToDeviceIdentification()) ??
                                      _dlnaManager.GetDefaultProfile();

                        _sessionManager.ReportCapabilities(sessionInfo.Id, new SessionCapabilities
                        {
                            PlayableMediaTypes = profile.GetSupportedMediaTypes(),

                            SupportedCommands = new List<string>
                        {
                            GeneralCommandType.VolumeDown.ToString(),
                            GeneralCommandType.VolumeUp.ToString(),
                            GeneralCommandType.Mute.ToString(),
                            GeneralCommandType.Unmute.ToString(),
                            GeneralCommandType.ToggleMute.ToString(),
                            GeneralCommandType.SetVolume.ToString(),
                            GeneralCommandType.SetAudioStreamIndex.ToString(),
                            GeneralCommandType.SetSubtitleStreamIndex.ToString()
                        },

                            SupportsMediaControl = true
                        });

                        _logger.Info("DLNA Session created for {0} - {1}", device.Properties.Name, device.Properties.ModelName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating PlayTo device.", ex);
            }
        }

        private string GetServerAddress(IPAddress localIp)
        {
            return string.Format("{0}://{1}:{2}/mediabrowser",

                "http",
                localIp,
                _appHost.HttpServerPort
                );
        }

        public void Dispose()
        {
            _deviceDiscovery.DeviceDiscovered -= _deviceDiscovery_DeviceDiscovered;
        }
    }
}
