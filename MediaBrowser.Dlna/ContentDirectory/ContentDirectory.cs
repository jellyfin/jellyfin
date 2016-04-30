using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Dlna.Service;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.MediaEncoding;

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
        private readonly ILocalizationManager _localization;
        private readonly IChannelManager _channelManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IUserViewManager _userViewManager;
        private readonly Func<IMediaEncoder> _mediaEncoder;

        public ContentDirectory(IDlnaManager dlna,
            IUserDataManager userDataManager,
            IImageProcessor imageProcessor,
            ILibraryManager libraryManager,
            IServerConfigurationManager config,
            IUserManager userManager,
            ILogger logger,
            IHttpClient httpClient, ILocalizationManager localization, IChannelManager channelManager, IMediaSourceManager mediaSourceManager, IUserViewManager userViewManager, Func<IMediaEncoder> mediaEncoder)
            : base(logger, httpClient)
        {
            _dlna = dlna;
            _userDataManager = userDataManager;
            _imageProcessor = imageProcessor;
            _libraryManager = libraryManager;
            _config = config;
            _userManager = userManager;
            _localization = localization;
            _channelManager = channelManager;
            _mediaSourceManager = mediaSourceManager;
            _userViewManager = userViewManager;
            _mediaEncoder = mediaEncoder;
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
            string accessToken = null;

            var user = GetUser(profile);

            return new ControlHandler(
                Logger,
                _libraryManager,
                profile,
                serverAddress,
                accessToken,
                _imageProcessor,
                _userDataManager,
                user,
                SystemUpdateId,
                _config,
                _localization,
                _channelManager,
                _mediaSourceManager,
                _userViewManager,
                _mediaEncoder())
                .ProcessControlRequest(request);
        }

        private User GetUser(DeviceProfile profile)
        {
            if (!string.IsNullOrEmpty(profile.UserId))
            {
                var user = _userManager.GetUserById(profile.UserId);

                if (user != null)
                {
                    return user;
                }
            }

            var userId = _config.GetDlnaConfiguration().DefaultUserId;

            if (!string.IsNullOrEmpty(userId))
            {
                var user = _userManager.GetUserById(userId);

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
