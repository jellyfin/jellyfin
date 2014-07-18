using MediaBrowser.Controller;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Events;
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

        private readonly SsdpHandler _ssdpHandler;
        private readonly DeviceDiscovery _deviceDiscovery;
        
        public PlayToManager(ILogger logger, ISessionManager sessionManager, IItemRepository itemRepository, ILibraryManager libraryManager, IUserManager userManager, IDlnaManager dlnaManager, IServerApplicationHost appHost, IImageProcessor imageProcessor, SsdpHandler ssdpHandler, DeviceDiscovery deviceDiscovery)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _itemRepository = itemRepository;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _appHost = appHost;
            _imageProcessor = imageProcessor;
            _ssdpHandler = ssdpHandler;
            _deviceDiscovery = deviceDiscovery;
        }

        public void Start()
        {
            _deviceDiscovery.DeviceDiscovered += _deviceDiscovery_DeviceDiscovered;
        }

        async void _deviceDiscovery_DeviceDiscovered(object sender, GenericEventArgs<DeviceDiscoveryInfo> e)
        {
            var device = e.Argument.Device;
            var localIp = e.Argument.LocalIpAddress;

            var usn = e.Argument.Usn;
            var nt = e.Argument.Nt;

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
            
            if (device.RendererCommands != null)
            {
                var uri = e.Argument.Uri;

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
                        _ssdpHandler,
                        serverAddress);

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
