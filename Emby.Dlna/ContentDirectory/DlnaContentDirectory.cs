#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Emby.Dlna.Service;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.ContentDirectory
{
    public class DlnaContentDirectory : BaseService, IContentDirectory
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
        private readonly ILoggerFactory _loggerFactory;

        public DlnaContentDirectory(
            IDlnaManager dlna,
            IUserDataManager userDataManager,
            IImageProcessor imageProcessor,
            ILibraryManager libraryManager,
            IServerConfigurationManager config,
            IUserManager userManager,
            IHttpClient httpClient,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IUserViewManager userViewManager,
            IMediaEncoder mediaEncoder,
            ITVSeriesManager tvSeriesManager,
            ILoggerFactory loggerFactory)
            : base(loggerFactory?.CreateLogger<DlnaContentDirectory>(), httpClient)
        {
            _dlna = dlna ?? throw new NullReferenceException(nameof(dlna));
            _userDataManager = userDataManager ?? throw new NullReferenceException(nameof(userDataManager));
            _imageProcessor = imageProcessor ?? throw new NullReferenceException(nameof(imageProcessor));
            _libraryManager = libraryManager ?? throw new NullReferenceException(nameof(dlna));
            _config = config ?? throw new NullReferenceException(nameof(config));
            _userManager = userManager ?? throw new NullReferenceException(nameof(userManager));
            _localization = localization ?? throw new NullReferenceException(nameof(localization));
            _mediaSourceManager = mediaSourceManager ?? throw new NullReferenceException(nameof(mediaSourceManager));
            _userViewManager = userViewManager ?? throw new NullReferenceException(nameof(userViewManager));
            _mediaEncoder = mediaEncoder ?? throw new NullReferenceException(nameof(mediaEncoder));
            _tvSeriesManager = tvSeriesManager ?? throw new NullReferenceException(nameof(tvSeriesManager));
            _loggerFactory = loggerFactory ?? throw new NullReferenceException(nameof(loggerFactory));
        }

        private static int SystemUpdateId
        {
            get
            {
                var now = DateTime.UtcNow;
                return now.Year + now.DayOfYear + now.Hour;
            }
        }

        /// <inheritdoc />
        public string GetServiceXml()
        {
            return ContentDirectoryXmlBuilder.GetXml();
        }

        /// <inheritdoc />
        public Task<ControlResponse> ProcessControlRequestAsync(ControlRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var profile = _dlna.GetProfile(request.Headers) ??
                          _dlna.GetDefaultProfile();

            var serverAddress = request.RequestedUrl.Substring(0, request.RequestedUrl.IndexOf("/dlna", StringComparison.OrdinalIgnoreCase));

            var user = GetUser(profile);

            return new ControlHandler(
                _loggerFactory.CreateLogger<ControlHandler>(),
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
                .ProcessControlRequestAsync(request);
        }

        private User GetUser(DeviceProfile profile)
        {
            if (!string.IsNullOrEmpty(profile.UserId))
            {
                var user = _userManager.GetUserById(Guid.Parse(profile.UserId));

                if (user != null)
                {
                    return user;
                }
            }

            var userId = _config.GetDlnaConfiguration().DefaultUserId;

            if (!string.IsNullOrEmpty(userId))
            {
                var user = _userManager.GetUserById(Guid.Parse(userId));

                if (user != null)
                {
                    return user;
                }
            }

            foreach (var user in _userManager.Users)
            {
                if (user.HasPermission(PermissionKind.IsAdministrator))
                {
                    return user;
                }
            }

            return _userManager.Users.FirstOrDefault();
        }
    }
}
