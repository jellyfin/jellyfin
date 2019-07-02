using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Models.Startup;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    [ApiVersion("1")]
    [Route("[controller]")]
    public class StartupController : ControllerBase
    {
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;

        public StartupController(IServerConfigurationManager config, IUserManager userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        [HttpPost("Complete")]
        public void Post()
        {
            _config.Configuration.IsStartupWizardCompleted = true;
            _config.SetOptimalValues();
            _config.SaveConfiguration();
        }

        [HttpGet("Configuration")]
        public StartupConfiguration Get()
        {
            var result = new StartupConfiguration
            {
                UICulture = _config.Configuration.UICulture,
                MetadataCountryCode = _config.Configuration.MetadataCountryCode,
                PreferredMetadataLanguage = _config.Configuration.PreferredMetadataLanguage
            };

            return result;
        }

        [HttpPost("Configuration")]
        public void UpdateInitial([FromForm] string uiCulture, [FromForm] string metadataCountryCode, [FromForm] string preferredMetadataLanguage)
        {
            _config.Configuration.UICulture = uiCulture;
            _config.Configuration.MetadataCountryCode = metadataCountryCode;
            _config.Configuration.PreferredMetadataLanguage = preferredMetadataLanguage;
            _config.SaveConfiguration();
        }

        [HttpPost("RemoteAccess")]
        public void Post([FromForm] bool enableRemoteAccess, [FromForm] bool enableAutomaticPortMapping)
        {
            _config.Configuration.EnableRemoteAccess = enableRemoteAccess;
            _config.Configuration.EnableUPnP = enableAutomaticPortMapping;
            _config.SaveConfiguration();
        }

        [HttpGet("User")]
        public StartupUser GetUser()
        {
            var user = _userManager.Users.First();

            return new StartupUser
            {
                Name = user.Name,
                Password = user.Password
            };
        }

        [HttpPost("User")]
        public async Task Post([FromForm] StartupUser startupUser)
        {
            var user = _userManager.Users.First();

            user.Name = startupUser.Name;

            _userManager.UpdateUser(user);

            if (!string.IsNullOrEmpty(startupUser.Password))
            {
                await _userManager.ChangePassword(user, startupUser.Password).ConfigureAwait(false);
            }
        }
    }
}
