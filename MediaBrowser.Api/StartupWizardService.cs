using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

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
    public class UpdateStartupUser : StartupUser
    {
    }

    [Authenticated(AllowBeforeStartupWizard = true, Roles = "Admin")]
    public class StartupWizardService : BaseApiService
    {
        private readonly IUserManager _userManager;

        public StartupWizardService(
            ILogger<StartupWizardService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
        }

        public void Post(ReportStartupWizardComplete request)
        {
            ServerConfigurationManager.Configuration.IsStartupWizardCompleted = true;
            ServerConfigurationManager.SetOptimalValues();
            ServerConfigurationManager.SaveConfiguration();
        }

        public object Get(GetStartupConfiguration request)
        {
            var result = new StartupConfiguration
            {
                UICulture = ServerConfigurationManager.Configuration.UICulture,
                MetadataCountryCode = ServerConfigurationManager.Configuration.MetadataCountryCode,
                PreferredMetadataLanguage = ServerConfigurationManager.Configuration.PreferredMetadataLanguage
            };

            return result;
        }

        public void Post(UpdateStartupConfiguration request)
        {
            ServerConfigurationManager.Configuration.UICulture = request.UICulture;
            ServerConfigurationManager.Configuration.MetadataCountryCode = request.MetadataCountryCode;
            ServerConfigurationManager.Configuration.PreferredMetadataLanguage = request.PreferredMetadataLanguage;
            ServerConfigurationManager.SaveConfiguration();
        }

        public void Post(UpdateRemoteAccessConfiguration request)
        {
            ServerConfigurationManager.Configuration.EnableRemoteAccess = request.EnableRemoteAccess;
            ServerConfigurationManager.Configuration.EnableUPnP = request.EnableAutomaticPortMapping;
            ServerConfigurationManager.SaveConfiguration();
        }

        public object Get(GetStartupUser request)
        {
            var user = _userManager.Users.First();

            return new StartupUser
            {
                Name = user.Name,
                Password = user.Password
            };
        }

        public async Task Post(UpdateStartupUser request)
        {
            var user = _userManager.Users.First();

            user.Name = request.Name;

            _userManager.UpdateUser(user);

            if (!string.IsNullOrEmpty(request.Password))
            {
                await _userManager.ChangePassword(user, request.Password).ConfigureAwait(false);
            }
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
        public string Password { get; set; }
    }
}
