using System;
using System.Collections.Generic;
using Jellyfin.Dlna.Service;
using Jellyfin.Common.Net;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Dlna;
using Jellyfin.Controller.Drawing;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.MediaEncoding;
using Jellyfin.Controller.TV;
using Jellyfin.Model.Dlna;
using Jellyfin.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Dlna.ContentDirectory
{
    public class ContentDirectory : BaseService, IContentDirectory
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly IDlnaManager _dlna;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly ILocalizationManager _localization;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IUserViewManager _userViewManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ITVSeriesManager _tvSeriesManager;

        public ContentDirectory(IDlnaManager dlna,
            IUserDataManager userDataManager,
            IImageProcessor imageProcessor,
            ILibraryManager libraryManager,
            IServerConfigurationManager config,
            IUserManager userManager,
            ILogger logger,
            IHttpClient httpClient,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IUserViewManager userViewManager,
            IMediaEncoder mediaEncoder,
            ITVSeriesManager tvSeriesManager)
            : base(logger, httpClient)
        {
            _dlna = dlna;
            _userDataManager = userDataManager;
            _imageProcessor = imageProcessor;
            _libraryManager = libraryManager;
            _config = config;
            _userManager = userManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _userViewManager = userViewManager;
            _mediaEncoder = mediaEncoder;
            _tvSeriesManager = tvSeriesManager;
        }

        private int SystemUpdateId
        {
            get
            {
                var now = DateTime.UtcNow;

                return now.Year + now.DayOfYear + now.Hour;
            }
        }

        public string GetServiceXml()
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
                null,
                _imageProcessor,
                _userDataManager,
                user,
                SystemUpdateId,
                _config,
                _localization,
                _mediaSourceManager,
                _userViewManager,
                _mediaEncoder,
                _tvSeriesManager)
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

            foreach (var user in _userManager.Users)
            {
                if (user.Policy.IsAdministrator)
                {
                    return user;
                }
            }

            foreach (var user in _userManager.Users)
            {
                return user;
            }

            return null;
        }
    }
}
