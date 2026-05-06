using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.StartupDtos;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The startup wizard controller.
/// </summary>
[Authorize(Policy = Policies.FirstTimeSetupOrElevated)]
public class StartupController : BaseJellyfinApiController
{
    private readonly IWritableOptions<ServerConfiguration> _serverConfig;
    private readonly IWritableOptions<NetworkConfiguration> _networkConfig;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupController" /> class.
    /// </summary>
    /// <param name="serverConfig">The writable server configuration options.</param>
    /// <param name="networkConfig">The writable network configuration options.</param>
    /// <param name="userManager">The user manager.</param>
    public StartupController(
        IWritableOptions<ServerConfiguration> serverConfig,
        IWritableOptions<NetworkConfiguration> networkConfig,
        IUserManager userManager)
    {
        _serverConfig = serverConfig;
        _networkConfig = networkConfig;
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
        _serverConfig.Update(c => c.IsStartupWizardCompleted = true);
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
            ServerName = _serverConfig.Value.ServerName,
            UICulture = _serverConfig.Value.UICulture,
            MetadataCountryCode = _serverConfig.Value.MetadataCountryCode,
            PreferredMetadataLanguage = _serverConfig.Value.PreferredMetadataLanguage
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
        _serverConfig.Update(c =>
        {
            c.ServerName = startupConfiguration.ServerName ?? string.Empty;
            c.UICulture = startupConfiguration.UICulture ?? string.Empty;
            c.MetadataCountryCode = startupConfiguration.MetadataCountryCode ?? string.Empty;
            c.PreferredMetadataLanguage = startupConfiguration.PreferredMetadataLanguage ?? string.Empty;
        });
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
        _networkConfig.Update(c => c.EnableRemoteAccess = startupRemoteAccessDto.EnableRemoteAccess);
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
        var user = _userManager.GetFirstUser() ?? throw new InvalidOperationException("No user exists after initialization.");
        return new StartupUserDto
        {
            Name = user.Username
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
        var user = _userManager.GetFirstUser();
        if (user is null)
        {
            return NotFound();
        }

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
            await _userManager.ChangePassword(user.Id, startupUserDto.Password).ConfigureAwait(false);
        }

        return NoContent();
    }
}
