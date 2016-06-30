using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.MediaEncoding;

namespace MediaBrowser.Dlna.PlayTo
{
    class PlayToManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;

        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IImageProcessor _imageProcessor;
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;
        private readonly IUserDataManager _userDataManager;
        private readonly ILocalizationManager _localization;

        private readonly IDeviceDiscovery _deviceDiscovery;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;

        private readonly List<string> _nonRendererUrls = new List<string>();
        private DateTime _lastRendererClear;

        public PlayToManager(ILogger logger, ISessionManager sessionManager, ILibraryManager libraryManager, IUserManager userManager, IDlnaManager dlnaManager, IServerApplicationHost appHost, IImageProcessor imageProcessor, IDeviceDiscovery deviceDiscovery, IHttpClient httpClient, IServerConfigurationManager config, IUserDataManager userDataManager, ILocalizationManager localization, IMediaSourceManager mediaSourceManager, IMediaEncoder mediaEncoder)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _appHost = appHost;
            _imageProcessor = imageProcessor;
            _deviceDiscovery = deviceDiscovery;
            _httpClient = httpClient;
            _config = config;
            _userDataManager = userDataManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
        }

        public void Start()
        {
            _deviceDiscovery.DeviceDiscovered += _deviceDiscovery_DeviceDiscovered;
        }

        async void _deviceDiscovery_DeviceDiscovered(object sender, SsdpMessageEventArgs e)
        {
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
                lock (_nonRendererUrls)
                {
                    if ((DateTime.UtcNow - _lastRendererClear).TotalMinutes >= 10)
                    {
                        _nonRendererUrls.Clear();
                        _lastRendererClear = DateTime.UtcNow;
                    }

                    if (_nonRendererUrls.Contains(location, StringComparer.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                var uri = new Uri(location);
                _logger.Debug("Attempting to create PlayToController from location {0}", location);
                var device = await Device.CreateuPnpDeviceAsync(uri, _httpClient, _config, _logger).ConfigureAwait(false);

                if (device.RendererCommands == null)
                {
                    lock (_nonRendererUrls)
                    {
                        _nonRendererUrls.Add(location);
                        return;
                    }
                }

                _logger.Debug("Logging session activity from location {0}", location);
                var sessionInfo = await _sessionManager.LogSessionActivity(device.Properties.ClientType, _appHost.ApplicationVersion.ToString(), device.Properties.UUID, device.Properties.Name, uri.OriginalString, null)
                    .ConfigureAwait(false);

                var controller = sessionInfo.SessionController as PlayToController;

                if (controller == null)
                {
                    var serverAddress = GetServerAddress(e.LocalEndPoint.Address);
                    string accessToken = null;

                    sessionInfo.SessionController = controller = new PlayToController(sessionInfo,
                        _sessionManager,
                        _libraryManager,
                        _logger,
                        _dlnaManager,
                        _userManager,
                        _imageProcessor,
                        serverAddress,
                        accessToken,
                        _deviceDiscovery,
                        _userDataManager,
                        _localization,
                        _mediaSourceManager,
                        _config,
                        _mediaEncoder);

                    controller.Init(device);

                    var profile = _dlnaManager.GetProfile(device.Properties.ToDeviceIdentification()) ??
                                  _dlnaManager.GetDefaultProfile();

                    _sessionManager.ReportCapabilities(sessionInfo.Id, new ClientCapabilities
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
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating PlayTo device.", ex);
            }
        }

        private string GetServerAddress(IPAddress localIp)
        {
            return _appHost.GetLocalApiUrl(localIp);
        }

        public void Dispose()
        {
            _deviceDiscovery.DeviceDiscovered -= _deviceDiscovery_DeviceDiscovered;
        }
    }
}
