using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StartupDtos;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
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
    // only exists for backwards compatibility on the user endpoints
    private const string DefaultUsername = "jellyfin";

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
    [Obsolete("Use RegisterUser")]
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
    [Obsolete("Use GetConfiguration")]
    public ActionResult<StartupConfigurationDto> GetStartupConfiguration()
    {
        return new StartupConfigurationDto
        {
            ServerName = _config.Configuration.ServerName,
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
    [Obsolete("Use UpdateConfiguration")]
    public ActionResult UpdateInitialConfiguration([FromBody, Required] StartupConfigurationDto startupConfiguration)
    {
        _config.Configuration.ServerName = startupConfiguration.ServerName ?? string.Empty;
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
    [Obsolete("Use UpdateNamedConfiguration")]
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
    [Obsolete("Use RegisterUser")]
    public async Task<StartupUserDto> GetFirstUser()
    {
        return new StartupUserDto
        {
            Name = _userManager.GetFirstUser()?.Username ?? DefaultUsername
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
    [Obsolete("Use RegisterUser")]
    public async Task<ActionResult> UpdateStartupUser([FromBody] StartupUserDto startupUserDto)
    {
        var user = _userManager.GetUserByName(startupUserDto.Name ?? DefaultUsername);
        if (user is not null)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(startupUserDto.Password) || string.IsNullOrEmpty(startupUserDto.Password))
        {
            return BadRequest("Password must not be empty");
        }

        user = await _userManager.CreateUserAsync(startupUserDto.Name ?? DefaultUsername).ConfigureAwait(false);
        user.SetPermission(PermissionKind.EnableContentDeletion, true);
        user.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, true);
        user.SetPermission(PermissionKind.IsAdministrator, true);
        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
        await _userManager.ChangePassword(user.Id, startupUserDto.Password).ConfigureAwait(false);

        return NoContent();
    }
}
