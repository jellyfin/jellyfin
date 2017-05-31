using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api
{
    [Route("/Startup/Complete", "POST", Summary = "Reports that the startup wizard has been completed")]
    public class ReportStartupWizardComplete : IReturnVoid
    {
    }

    [Route("/Startup/Info", "GET", Summary = "Gets initial server info")]
    public class GetStartupInfo : IReturn<StartupInfo>
    {
    }

    [Route("/Startup/Configuration", "GET", Summary = "Gets initial server configuration")]
    public class GetStartupConfiguration : IReturn<StartupConfiguration>
    {
    }

    [Route("/Startup/Configuration", "POST", Summary = "Updates initial server configuration")]
    public class UpdateStartupConfiguration : StartupConfiguration, IReturnVoid
    {
    }

    [Route("/Startup/User", "GET", Summary = "Gets initial user info")]
    public class GetStartupUser : IReturn<StartupUser>
    {
    }

    [Route("/Startup/User", "POST", Summary = "Updates initial user info")]
    public class UpdateStartupUser : StartupUser, IReturn<UpdateStartupUserResult>
    {
    }

    [Authenticated(AllowBeforeStartupWizard = true, Roles = "Admin")]
    public class StartupWizardService : BaseApiService
    {
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly IUserManager _userManager;
        private readonly IConnectManager _connectManager;
        private readonly IMediaEncoder _mediaEncoder;

        public StartupWizardService(IServerConfigurationManager config, IServerApplicationHost appHost, IUserManager userManager, IConnectManager connectManager, IMediaEncoder mediaEncoder)
        {
            _config = config;
            _appHost = appHost;
            _userManager = userManager;
            _connectManager = connectManager;
            _mediaEncoder = mediaEncoder;
        }

        public void Post(ReportStartupWizardComplete request)
        {
            _config.Configuration.IsStartupWizardCompleted = true;
            SetWizardFinishValues(_config.Configuration);
            _config.SaveConfiguration();
        }

        public async Task<object> Get(GetStartupInfo request)
        {
            var info = await _appHost.GetSystemInfo().ConfigureAwait(false);

            return new StartupInfo
            {
                SupportsRunningAsService = info.SupportsRunningAsService,
                HasMediaEncoder = !string.IsNullOrWhiteSpace(_mediaEncoder.EncoderPath)
            };
        }

        public object Get(GetStartupConfiguration request)
        {
            var result = new StartupConfiguration
            {
                UICulture = _config.Configuration.UICulture,
                MetadataCountryCode = _config.Configuration.MetadataCountryCode,
                PreferredMetadataLanguage = _config.Configuration.PreferredMetadataLanguage
            };

            return result;
        }

        private void SetWizardFinishValues(ServerConfiguration config)
        {
            config.EnableStandaloneMusicKeys = true;
            config.EnableCaseSensitiveItemIds = true;
            config.SkipDeserializationForBasicTypes = true;
            config.SkipDeserializationForAudio = true;
            config.EnableLocalizedGuids = true;
            config.EnableSimpleArtistDetection = true;
            config.EnableNormalizedItemByNameIds = true;
            config.DisableLiveTvChannelUserDataName = true;
            config.EnableSimpleSortNameHandling = true;
        }

        public void Post(UpdateStartupConfiguration request)
        {
            _config.Configuration.UICulture = request.UICulture;
            _config.Configuration.MetadataCountryCode = request.MetadataCountryCode;
            _config.Configuration.PreferredMetadataLanguage = request.PreferredMetadataLanguage;
            _config.SaveConfiguration();
        }

        public object Get(GetStartupUser request)
        {
            var user = _userManager.Users.First();

            return new StartupUser
            {
                Name = user.Name,
                ConnectUserName = user.ConnectUserName
            };
        }

        public async Task<object> Post(UpdateStartupUser request)
        {
            var user = _userManager.Users.First();

            user.Name = request.Name;
            await _userManager.UpdateUser(user).ConfigureAwait(false);

            var result = new UpdateStartupUserResult();

            if (!string.IsNullOrWhiteSpace(user.ConnectUserName) &&
                string.IsNullOrWhiteSpace(request.ConnectUserName))
            {
                await _connectManager.RemoveConnect(user.Id.ToString("N")).ConfigureAwait(false);
            }
            else if (!string.Equals(user.ConnectUserName, request.ConnectUserName, StringComparison.OrdinalIgnoreCase))
            {
                result.UserLinkResult = await _connectManager.LinkUser(user.Id.ToString("N"), request.ConnectUserName).ConfigureAwait(false);
            }

            return result;
        }
    }

    public class StartupConfiguration
    {
        public string UICulture { get; set; }
        public string MetadataCountryCode { get; set; }
        public string PreferredMetadataLanguage { get; set; }
    }

    public class StartupInfo
    {
        public bool SupportsRunningAsService { get; set; }
        public bool HasMediaEncoder { get; set; }
    }

    public class StartupUser
    {
        public string Name { get; set; }
        public string ConnectUserName { get; set; }
    }

    public class UpdateStartupUserResult
    {
        public UserLinkResult UserLinkResult { get; set; }
    }
}
