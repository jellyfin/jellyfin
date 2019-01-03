using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Services;
using MediaBrowser.Common.Net;
using System.Threading;

namespace MediaBrowser.Api
{
    [Route("/Startup/Complete", "POST", Summary = "Reports that the startup wizard has been completed", IsHidden = true)]
    public class ReportStartupWizardComplete : IReturnVoid
    {
    }

    [Route("/Startup/Configuration", "GET", Summary = "Gets initial server configuration", IsHidden = true)]
    public class GetStartupConfiguration : IReturn<StartupConfiguration>
    {
    }

    [Route("/Startup/Configuration", "POST", Summary = "Updates initial server configuration", IsHidden = true)]
    public class UpdateStartupConfiguration : StartupConfiguration, IReturnVoid
    {
    }

    [Route("/Startup/RemoteAccess", "POST", Summary = "Updates initial server configuration", IsHidden = true)]
    public class UpdateRemoteAccessConfiguration : IReturnVoid
    {
        public bool EnableRemoteAccess { get; set; }
        public bool EnableAutomaticPortMapping { get; set; }
    }

    [Route("/Startup/User", "GET", Summary = "Gets initial user info", IsHidden = true)]
    public class GetStartupUser : IReturn<StartupUser>
    {
    }

    [Route("/Startup/User", "POST", Summary = "Updates initial user info", IsHidden = true)]
    public class UpdateStartupUser : StartupUser, IReturn<UpdateStartupUserResult>
    {
    }

    [Authenticated(AllowBeforeStartupWizard = true, Roles = "Admin")]
    public class StartupWizardService : BaseApiService
    {
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly IUserManager _userManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IHttpClient _httpClient;

        public StartupWizardService(IServerConfigurationManager config, IHttpClient httpClient, IServerApplicationHost appHost, IUserManager userManager, IMediaEncoder mediaEncoder)
        {
            _config = config;
            _appHost = appHost;
            _userManager = userManager;
            _mediaEncoder = mediaEncoder;
            _httpClient = httpClient;
        }

        public void Post(ReportStartupWizardComplete request)
        {
            _config.Configuration.IsStartupWizardCompleted = true;
            _config.SetOptimalValues();
            _config.SaveConfiguration();
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

        public void Post(UpdateStartupConfiguration request)
        {
            _config.Configuration.UICulture = request.UICulture;
            _config.Configuration.MetadataCountryCode = request.MetadataCountryCode;
            _config.Configuration.PreferredMetadataLanguage = request.PreferredMetadataLanguage;
            _config.SaveConfiguration();
        }

        public void Post(UpdateRemoteAccessConfiguration request)
        {
            _config.Configuration.EnableRemoteAccess = request.EnableRemoteAccess;
            _config.Configuration.EnableUPnP = request.EnableAutomaticPortMapping;
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
            _userManager.UpdateUser(user);

            var result = new UpdateStartupUserResult();

            return result;
        }
    }

    public class StartupConfiguration
    {
        public string UICulture { get; set; }
        public string MetadataCountryCode { get; set; }
        public string PreferredMetadataLanguage { get; set; }
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
