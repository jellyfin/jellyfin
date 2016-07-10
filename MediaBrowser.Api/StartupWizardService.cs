using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using ServiceStack;
using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;

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
        private readonly ILiveTvManager _liveTvManager;
        private readonly IMediaEncoder _mediaEncoder;

        public StartupWizardService(IServerConfigurationManager config, IServerApplicationHost appHost, IUserManager userManager, IConnectManager connectManager, ILiveTvManager liveTvManager, IMediaEncoder mediaEncoder)
        {
            _config = config;
            _appHost = appHost;
            _userManager = userManager;
            _connectManager = connectManager;
            _liveTvManager = liveTvManager;
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
                EnableInternetProviders = _config.Configuration.EnableInternetProviders,
                SaveLocalMeta = _config.Configuration.SaveLocalMeta,
                MetadataCountryCode = _config.Configuration.MetadataCountryCode,
                PreferredMetadataLanguage = _config.Configuration.PreferredMetadataLanguage
            };

            var tvConfig = GetLiveTVConfiguration();

            if (tvConfig.TunerHosts.Count > 0)
            {
                result.LiveTvTunerPath = tvConfig.TunerHosts[0].Url;
                result.LiveTvTunerType = tvConfig.TunerHosts[0].Type;
            }

            if (tvConfig.ListingProviders.Count > 0)
            {
                result.LiveTvGuideProviderId = tvConfig.ListingProviders[0].Id;
                result.LiveTvGuideProviderType = tvConfig.ListingProviders[0].Type;
            }

            return result;
        }

        private void SetWizardFinishValues(ServerConfiguration config)
        {
            config.EnableLocalizedGuids = true;
            config.EnableStandaloneMusicKeys = true;
            config.EnableCaseSensitiveItemIds = true;
            //config.EnableFolderView = true;
            config.SchemaVersion = 108;
        }

        public void Post(UpdateStartupConfiguration request)
        {
            _config.Configuration.UICulture = request.UICulture;
            _config.Configuration.EnableInternetProviders = request.EnableInternetProviders;
            _config.Configuration.SaveLocalMeta = request.SaveLocalMeta;
            _config.Configuration.MetadataCountryCode = request.MetadataCountryCode;
            _config.Configuration.PreferredMetadataLanguage = request.PreferredMetadataLanguage;
            _config.SaveConfiguration();

            var task = UpdateTuners(request);
            Task.WaitAll(task);
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

            // TODO: This should be handled internally by xbmc metadata
            const string metadataKey = "xbmcmetadata";
            var metadata = _config.GetConfiguration<XbmcMetadataOptions>(metadataKey);
            metadata.UserId = user.Id.ToString("N");
            _config.SaveConfiguration(metadataKey, metadata);

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

        private async Task UpdateTuners(UpdateStartupConfiguration request)
        {
            var config = GetLiveTVConfiguration();
            var save = false;

            if (string.IsNullOrWhiteSpace(request.LiveTvTunerPath) ||
                string.IsNullOrWhiteSpace(request.LiveTvTunerType))
            {
                if (config.TunerHosts.Count > 0)
                {
                    config.TunerHosts.Clear();
                    save = true;
                }
            }
            else
            {
                if (!config.TunerHosts.Any(i => string.Equals(i.Type, request.LiveTvTunerType, StringComparison.OrdinalIgnoreCase) && string.Equals(i.Url, request.LiveTvTunerPath, StringComparison.OrdinalIgnoreCase)))
                {
                    // Add tuner
                    await _liveTvManager.SaveTunerHost(new TunerHostInfo
                    {
                        IsEnabled = true,
                        Type = request.LiveTvTunerType,
                        Url = request.LiveTvTunerPath

                    }).ConfigureAwait(false);
                }
            }

            if (save)
            {
                SaveLiveTVConfiguration(config);
            }
        }

        private void SaveLiveTVConfiguration(LiveTvOptions config)
        {
            _config.SaveConfiguration("livetv", config);
        }

        private LiveTvOptions GetLiveTVConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }
    }

    public class StartupConfiguration
    {
        public string UICulture { get; set; }
        public bool EnableInternetProviders { get; set; }
        public bool SaveLocalMeta { get; set; }
        public string MetadataCountryCode { get; set; }
        public string PreferredMetadataLanguage { get; set; }
        public string LiveTvTunerType { get; set; }
        public string LiveTvTunerPath { get; set; }
        public string LiveTvGuideProviderId { get; set; }
        public string LiveTvGuideProviderType { get; set; }
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
