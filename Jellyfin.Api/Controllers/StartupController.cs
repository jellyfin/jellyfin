using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.StartupDtos;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The startup wizard controller.
    /// </summary>
    [Authorize(Policy = Policies.FirstTimeSetupOrElevated)]
    public class StartupController : BaseJellyfinApiController
    {
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupController" /> class.
        /// </summary>
        /// <param name="config">The server configuration manager.</param>
        /// <param name="userManager">The user manager.</param>
        public StartupController(IServerConfigurationManager config, IUserManager userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        /// <summary>
        /// Api endpoint for completing the startup wizard.
        /// </summary>
        /// <response code="200">Startup wizard completed.</response>
        /// <returns>Status.</returns>
        [HttpPost("Complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult CompleteWizard()
        {
            _config.Configuration.IsStartupWizardCompleted = true;
            _config.SetOptimalValues();
            _config.SaveConfiguration();
            return Ok();
        }

        /// <summary>
        /// Endpoint for getting the initial startup wizard configuration.
        /// </summary>
        /// <response code="200">Initial startup wizard configuration retrieved.</response>
        /// <returns>The initial startup wizard configuration.</returns>
        [HttpGet("Configuration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<StartupConfigurationDto> GetStartupConfiguration()
        {
            var result = new StartupConfigurationDto
            {
                UICulture = _config.Configuration.UICulture,
                MetadataCountryCode = _config.Configuration.MetadataCountryCode,
                PreferredMetadataLanguage = _config.Configuration.PreferredMetadataLanguage
            };

            return result;
        }

        /// <summary>
        /// Endpoint for updating the initial startup wizard configuration.
        /// </summary>
        /// <param name="uiCulture">The UI language culture.</param>
        /// <param name="metadataCountryCode">The metadata country code.</param>
        /// <param name="preferredMetadataLanguage">The preferred language for metadata.</param>
        /// <response code="200">Configuration saved.</response>
        /// <returns>Status.</returns>
        [HttpPost("Configuration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult UpdateInitialConfiguration(
            [FromForm] string uiCulture,
            [FromForm] string metadataCountryCode,
            [FromForm] string preferredMetadataLanguage)
        {
            _config.Configuration.UICulture = uiCulture;
            _config.Configuration.MetadataCountryCode = metadataCountryCode;
            _config.Configuration.PreferredMetadataLanguage = preferredMetadataLanguage;
            _config.SaveConfiguration();
            return Ok();
        }

        /// <summary>
        /// Endpoint for (dis)allowing remote access and UPnP.
        /// </summary>
        /// <param name="enableRemoteAccess">Enable remote access.</param>
        /// <param name="enableAutomaticPortMapping">Enable UPnP.</param>
        /// <response code="200">Configuration saved.</response>
        /// <returns>Status.</returns>
        [HttpPost("RemoteAccess")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult SetRemoteAccess([FromForm] bool enableRemoteAccess, [FromForm] bool enableAutomaticPortMapping)
        {
            _config.Configuration.EnableRemoteAccess = enableRemoteAccess;
            _config.Configuration.EnableUPnP = enableAutomaticPortMapping;
            _config.SaveConfiguration();
            return Ok();
        }

        /// <summary>
        /// Endpoint for returning the first user.
        /// </summary>
        /// <response code="200">Initial user retrieved.</response>
        /// <returns>The first user.</returns>
        [HttpGet("User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<StartupUserDto> GetFirstUser()
        {
            var user = _userManager.Users.First();
            return new StartupUserDto { Name = user.Name, Password = user.Password };
        }

        /// <summary>
        /// Endpoint for updating the user name and password.
        /// </summary>
        /// <param name="startupUserDto">The DTO containing username and password.</param>
        /// <response code="200">Updated user name and password.</response>
        /// <returns>The async task.</returns>
        [HttpPost("User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> UpdateUser([FromForm] StartupUserDto startupUserDto)
        {
            var user = _userManager.Users.First();

            user.Name = startupUserDto.Name;

            _userManager.UpdateUser(user);

            if (!string.IsNullOrEmpty(startupUserDto.Password))
            {
                await _userManager.ChangePassword(user, startupUserDto.Password).ConfigureAwait(false);
            }

            return Ok();
        }
    }
}
