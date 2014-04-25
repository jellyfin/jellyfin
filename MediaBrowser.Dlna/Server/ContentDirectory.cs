using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Dlna.Server
{
    public class ContentDirectory : IContentDirectory, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly IDlnaManager _dlna;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;

        private readonly IEventManager _eventManager;

        private int _systemUpdateId;
        private Timer _systemUpdateTimer;

        public ContentDirectory(IDlnaManager dlna,
            IUserDataManager userDataManager,
            IImageProcessor imageProcessor,
            IDtoService dtoService,
            ILibraryManager libraryManager,
            ILogManager logManager,
            IServerConfigurationManager config,
            IUserManager userManager,
            IEventManager eventManager)
        {
            _dlna = dlna;
            _userDataManager = userDataManager;
            _imageProcessor = imageProcessor;
            _dtoService = dtoService;
            _libraryManager = libraryManager;
            _config = config;
            _userManager = userManager;
            _eventManager = eventManager;
            _logger = logManager.GetLogger("DlnaContentDirectory");

            _systemUpdateTimer = new Timer(SystemUdpateTimerCallback, null, Timeout.Infinite,
                Convert.ToInt64(TimeSpan.FromMinutes(60).TotalMilliseconds));
        }

        public string GetContentDirectoryXml(IDictionary<string, string> headers)
        {
            var profile = _dlna.GetProfile(headers) ??
                          _dlna.GetDefaultProfile();

            return new ContentDirectoryXmlBuilder(profile).GetXml();
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            var profile = _dlna.GetProfile(request.Headers) ??
                          _dlna.GetDefaultProfile();

            var serverAddress = request.RequestedUrl.Substring(0, request.RequestedUrl.IndexOf("/dlna", StringComparison.OrdinalIgnoreCase));
            
            var user = GetUser(profile);

            return new ControlHandler(
                _logger,
                _libraryManager,
                profile,
                serverAddress,
                _dtoService,
                _imageProcessor,
                _userDataManager,
                user,
                _systemUpdateId)
                .ProcessControlRequest(request);
        }

        private User GetUser(DeviceProfile profile)
        {
            if (!string.IsNullOrEmpty(profile.UserId))
            {
                var user = _userManager.GetUserById(new Guid(profile.UserId));

                if (user != null)
                {
                    return user;
                }
            }

            if (!string.IsNullOrEmpty(_config.Configuration.DlnaOptions.DefaultUserId))
            {
                var user = _userManager.GetUserById(new Guid(_config.Configuration.DlnaOptions.DefaultUserId));

                if (user != null)
                {
                    return user;
                }
            }

            // No configuration so it's going to be pretty arbitrary
            return _userManager.Users.First();
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private async void SystemUdpateTimerCallback(object state)
        {
            var values = new Dictionary<string, string>();

            _systemUpdateId++;
            values["SystemUpdateID"] = _systemUpdateId.ToString(_usCulture);

            try
            {
                await _eventManager.TriggerEvent("upnp:event", values).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending system update notification", ex);
            }
        }

        private readonly object _disposeLock = new object();
        public void Dispose()
        {
            lock (_disposeLock)
            {
                DisposeUpdateTimer();
            }
        }

        private void DisposeUpdateTimer()
        {
            if (_systemUpdateTimer != null)
            {
                _systemUpdateTimer.Dispose();
                _systemUpdateTimer = null;
            }
        }
    }
}
