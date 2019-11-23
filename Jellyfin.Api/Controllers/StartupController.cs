using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Models.Startup;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    [Authorize(Policy = "FirstTimeSetupOrElevated")]
    public class StartupController : BaseJellyfinApiController
    {
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;

        public StartupController(IServerConfigurationManager config, IUserManager userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        [HttpPost("Complete")]
        public void CompleteWizard()
        {
            _config.Configuration.IsStartupWizardCompleted = true;
            _config.SetOptimalValues();
            _config.SaveConfiguration();
        }

        [HttpGet("Configuration")]
        public StartupConfigurationDto GetStartupConfiguration()
        {
            var result = new StartupConfigurationDto
            {
                UICulture = _config.Configuration.UICulture,
                MetadataCountryCode = _config.Configuration.MetadataCountryCode,
                PreferredMetadataLanguage = _config.Configuration.PreferredMetadataLanguage
            };

            return result;
        }

        [HttpPost("Configuration")]
        public void UpdateInitialConfiguration([FromForm] string uiCulture, [FromForm] string metadataCountryCode, [FromForm] string preferredMetadataLanguage)
        {
            _config.Configuration.UICulture = uiCulture;
            _config.Configuration.MetadataCountryCode = metadataCountryCode;
            _config.Configuration.PreferredMetadataLanguage = preferredMetadataLanguage;
            _config.SaveConfiguration();
        }

        [HttpPost("RemoteAccess")]
        public void SetRemoteAccess([FromForm] bool enableRemoteAccess, [FromForm] bool enableAutomaticPortMapping)
        {
            _config.Configuration.EnableRemoteAccess = enableRemoteAccess;
            _config.Configuration.EnableUPnP = enableAutomaticPortMapping;
            _config.SaveConfiguration();
        }

        [HttpGet("User")]
        public StartupUserDto GetUser()
        {
            var user = _userManager.Users.First();

            return new StartupUserDto
            {
                Name = user.Name,
                Password = user.Password
            };
        }

        [HttpPost("User")]
        public async Task UpdateUser([FromForm] StartupUserDto startupUserDto)
        {
            var user = _userManager.Users.First();

            user.Name = startupUserDto.Name;

            _userManager.UpdateUser(user);

            if (!string.IsNullOrEmpty(startupUserDto.Password))
            {
                await _userManager.ChangePassword(user, startupUserDto.Password).ConfigureAwait(false);
            }
        }
    }
}
