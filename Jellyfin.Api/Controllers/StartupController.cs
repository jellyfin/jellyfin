using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.StartupDtos;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

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
    /// Completes the startup wizard.
    /// </summary>
    /// <response code="204">Startup wizard completed.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult CompleteWizard()
    {
        _config.Configuration.IsStartupWizardCompleted = true;
        _config.SaveConfiguration();
        return NoContent();
    }

    /// <summary>
    /// Gets the initial startup wizard configuration.
    /// </summary>
    /// <response code="200">Initial startup wizard configuration retrieved.</response>
    /// <returns>An <see cref="OkResult"/> containing the initial startup wizard configuration.</returns>
    [HttpGet("Configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<StartupConfigurationDto> GetStartupConfiguration()
    {
        return new StartupConfigurationDto
        {
            UICulture = _config.Configuration.UICulture,
            MetadataCountryCode = _config.Configuration.MetadataCountryCode,
            PreferredMetadataLanguage = _config.Configuration.PreferredMetadataLanguage
        };
    }

    /// <summary>
    /// Sets the initial startup wizard configuration.
    /// </summary>
    /// <param name="startupConfiguration">The updated startup configuration.</param>
    /// <response code="204">Configuration saved.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Configuration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateInitialConfiguration([FromBody, Required] StartupConfigurationDto startupConfiguration)
    {
        _config.Configuration.UICulture = startupConfiguration.UICulture ?? string.Empty;
        _config.Configuration.MetadataCountryCode = startupConfiguration.MetadataCountryCode ?? string.Empty;
        _config.Configuration.PreferredMetadataLanguage = startupConfiguration.PreferredMetadataLanguage ?? string.Empty;
        _config.SaveConfiguration();
        return NoContent();
    }

    /// <summary>
    /// Sets remote access and UPnP.
    /// </summary>
    /// <param name="startupRemoteAccessDto">The startup remote access dto.</param>
    /// <response code="204">Configuration saved.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("RemoteAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult SetRemoteAccess([FromBody, Required] StartupRemoteAccessDto startupRemoteAccessDto)
    {
        NetworkConfiguration settings = _config.GetNetworkConfiguration();
        settings.EnableRemoteAccess = startupRemoteAccessDto.EnableRemoteAccess;
        _config.SaveConfiguration(NetworkConfigurationStore.StoreKey, settings);
        return NoContent();
    }

    /// <summary>
    /// Gets the first user.
    /// </summary>
    /// <response code="200">Initial user retrieved.</response>
    /// <returns>The first user.</returns>
    [HttpGet("User")]
    [HttpGet("FirstUser", Name = "GetFirstUser_2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<StartupUserDto> GetFirstUser()
    {
        // TODO: Remove this method when startup wizard no longer requires an existing user.
        await _userManager.InitializeAsync().ConfigureAwait(false);
        var user = _userManager.Users.First();
        return new StartupUserDto
        {
            Name = user.Username,
            Password = user.Password
        };
    }

    /// <summary>
    /// Sets the user name and password.
    /// </summary>
    /// <param name="startupUserDto">The DTO containing username and password.</param>
    /// <response code="204">Updated user name and password.</response>
    /// <returns>
    /// A <see cref="Task" /> that represents the asynchronous update operation.
    /// The task result contains a <see cref="NoContentResult"/> indicating success.
    /// </returns>
    [HttpPost("User")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UpdateStartupUser([FromBody] StartupUserDto startupUserDto)
    {
        var user = _userManager.Users.First();
        if (string.IsNullOrWhiteSpace(startupUserDto.Password))
        {
            return BadRequest("Password must not be empty");
        }

        if (startupUserDto.Name is not null)
        {
            user.Username = startupUserDto.Name;
        }

        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(startupUserDto.Password))
        {
            await _userManager.ChangePassword(user, startupUserDto.Password).ConfigureAwait(false);
        }

        return NoContent();
    }
}
