using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Dlna.Service;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Dlna.ContentDirectory
{
    public class ContentDirectory : BaseService, IContentDirectory, IDisposable
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly IDlnaManager _dlna;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;

        public ContentDirectory(IDlnaManager dlna,
            IUserDataManager userDataManager,
            IImageProcessor imageProcessor,
            ILibraryManager libraryManager,
            IServerConfigurationManager config,
            IUserManager userManager,
            ILogger logger,
            IHttpClient httpClient)
            : base(logger, httpClient)
        {
            _dlna = dlna;
            _userDataManager = userDataManager;
            _imageProcessor = imageProcessor;
            _libraryManager = libraryManager;
            _config = config;
            _userManager = userManager;
        }

        private int SystemUpdateId
        {
            get
            {
                var now = DateTime.UtcNow;

                return now.Year + now.DayOfYear + now.Hour;
            }
        }

        public string GetServiceXml(IDictionary<string, string> headers)
        {
            return new ContentDirectoryXmlBuilder().GetXml();
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            var profile = _dlna.GetProfile(request.Headers) ??
                          _dlna.GetDefaultProfile();

            var serverAddress = request.RequestedUrl.Substring(0, request.RequestedUrl.IndexOf("/dlna", StringComparison.OrdinalIgnoreCase));
            
            var user = GetUser(profile);

            return new ControlHandler(
                Logger,
                _libraryManager,
                profile,
                serverAddress,
                _imageProcessor,
                _userDataManager,
                user,
                SystemUpdateId,
                _config)
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

        public void Dispose()
        {

        }
    }
}
