using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ICU4N.Util;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Authentication;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.QuickConnect;
using MediaBrowser.Model.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// User controller.
/// </summary>
[Route("Users")]
public class UserController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly IUserAuthenticationManager _userAuthenticationManager;
    private readonly ISessionManager _sessionManager;
    private readonly INetworkManager _networkManager;
    private readonly IDeviceManager _deviceManager;
    private readonly IAuthorizationContext _authContext;
    private readonly IServerConfigurationManager _config;
    private readonly ILogger _logger;
    private readonly IPlaylistManager _playlistManager;
    private readonly IEventManager _eventManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="userAuthenticationManager">Instance of the <see cref="IUserAuthenticationManager"/> interface.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
    /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
    /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
    /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="playlistManager">Instance of the <see cref="IPlaylistManager"/> interface.</param>
    /// <param name="eventManager">Instance of the <see cref="IEventManager"/> interface.</param>
    public UserController(
        IUserManager userManager,
        IUserAuthenticationManager userAuthenticationManager,
        ISessionManager sessionManager,
        INetworkManager networkManager,
        IDeviceManager deviceManager,
        IAuthorizationContext authContext,
        IServerConfigurationManager config,
        ILogger<UserController> logger,
        IPlaylistManager playlistManager,
        IEventManager eventManager)
    {
        _userManager = userManager;
        _userAuthenticationManager = userAuthenticationManager;
        _sessionManager = sessionManager;
        _networkManager = networkManager;
        _deviceManager = deviceManager;
        _authContext = authContext;
        _config = config;
        _logger = logger;
        _playlistManager = playlistManager;
        _eventManager = eventManager;
    }

    /// <summary>
    /// Gets a list of users.
    /// </summary>
    /// <param name="isHidden">Optional filter by IsHidden=true or false.</param>
    /// <param name="isDisabled">Optional filter by IsDisabled=true or false.</param>
    /// <response code="200">Users returned.</response>
    /// <returns>An <see cref="IEnumerable{UserDto}"/> containing the users.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] bool? isHidden,
        [FromQuery] bool? isDisabled)
    {
        var users = Get(isHidden, isDisabled, false, false);
        return Ok(users);
    }

    /// <summary>
    /// Gets a list of publicly visible users for display on a login screen.
    /// </summary>
    /// <response code="200">Public users returned.</response>
    /// <returns>An <see cref="IEnumerable{UserDto}"/> containing the public users.</returns>
    [HttpGet("Public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<UserDto>> GetPublicUsers()
    {
        // If the startup wizard hasn't been completed then just return all users
        if (!_config.Configuration.IsStartupWizardCompleted)
        {
            return Ok(Get(false, false, false, false));
        }

        return Ok(Get(false, false, true, true));
    }

    /// <summary>
    /// Gets a user by Id.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <response code="200">User returned.</response>
    /// <response code="404">User not found.</response>
    /// <returns>An <see cref="UserDto"/> with information about the user or a <see cref="NotFoundResult"/> if the user was not found.</returns>
    [HttpGet("{userId}")]
    [Authorize(Policy = Policies.IgnoreParentalControl)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserDto> GetUserById([FromRoute, Required] Guid userId)
    {
        var user = _userManager.GetUserById(userId);

        if (user is null)
        {
            return NotFound("User not found");
        }

        var result = _userManager.GetUserDto(user, HttpContext.GetNormalizedRemoteIP().ToString());
        return result;
    }

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <response code="204">User deleted.</response>
    /// <response code="404">User not found.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success or a <see cref="NotFoundResult"/> if the user was not found.</returns>
    [HttpDelete("{userId}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUser([FromRoute, Required] Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        await _sessionManager.RevokeUserTokens(user.Id, null).ConfigureAwait(false);
        await _playlistManager.RemovePlaylistsAsync(userId).ConfigureAwait(false);
        await _userManager.DeleteUserAsync(userId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Authenticates a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="pw">The password as plain text.</param>
    /// <response code="200">User authenticated.</response>
    /// <response code="403">Sha1-hashed password only is not allowed.</response>
    /// <response code="404">User not found.</response>
    /// <returns>A <see cref="Task"/> containing an <see cref="Session"/>.</returns>
    [HttpPost("{userId}/Authenticate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Obsolete("Authenticate with username instead")]
    public async Task<ActionResult<Session>> AuthenticateUser(
        [FromRoute, Required] Guid userId,
        [FromQuery, Required] string pw)
    {
        var user = _userManager.GetUserById(userId);

        if (user is null)
        {
            return NotFound("User not found");
        }

        AuthenticateUserByName request = new AuthenticateUserByName
        {
            Username = user.Username,
            Pw = pw
        };
        return await AuthenticateUserByName(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Authenticates a user by name and password.
    /// </summary>
    /// <param name="request">The <see cref="AuthenticateUserByName"/> request.</param>
    /// <response code="200">User authenticated.</response>
    /// <returns>A <see cref="Task"/> containing an <see cref="Session"/> with information about the new session.</returns>
    [HttpPost("AuthenticateByName")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Session>> AuthenticateUserByName([FromBody, Required] AuthenticateUserByName request)
    {
        var auth = await _authContext.GetAuthorizationInfo(Request).ConfigureAwait(false);
        var remoteEndpoint = HttpContext.GetNormalizedRemoteIP().ToString();
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            _logger.LogInformation("Authentication request without username has been denied (IP: {IP}).", remoteEndpoint);
            throw new ArgumentNullException("request.Username");
        }

        var mfaAwareClient = HttpContext.Request.Headers.ContainsKey("X-MFA-Aware");

        try
        {
            var (provider, result) = await _userAuthenticationManager.Authenticate(new UsernamePasswordAuthData(request.Username, request.Pw, request.TOTP), remoteEndpoint).ConfigureAwait(false);

            if (!result.Authenticated)
            {
                if (result.ErrorCode == 1300) // arbitrarily chosen error code used to signal that the username and password were correct, but TOTP was not
                {
                    throw new AuthenticationException("Incorrect or missing TOTP code.");
                }

                // MFA setup required. If client is MFA aware, send them the setup URI.
                // If not, simply send an error message to avoid unnecessary leaking of secret, in case of
                // clients that might display the raw error message on a screen, for example.
                else if (result.ErrorCode == 1301)
                {
                    throw new AuthenticationException((mfaAwareClient && result.ErrorData != null) ? result.ErrorData : "MFA setup required.");
                }

                await _eventManager.PublishAsync(new AuthenticationRequestEventArgs(new AuthenticationRequest
                {
                    App = auth.Client,
                    AppVersion = auth.Version,
                    DeviceId = auth.DeviceId,
                    DeviceName = auth.Device,
                    Password = request.Pw,
                    RemoteEndPoint = remoteEndpoint,
                    Username = request.Username
                })).ConfigureAwait(false);
                throw new AuthenticationException("Invalid username or password entered.");
            }

            return await _sessionManager.CreateSession(
                result.User,
                auth.DeviceId,
                auth.Client,
                auth.Version,
                auth.Device,
                provider.GetType().FullName,
                remoteEndpoint).ConfigureAwait(false);
        }
        catch (SecurityException e)
        {
            // rethrow adding IP address to message
            throw new SecurityException($"[{remoteEndpoint}] {e.Message}", e);
        }
    }

    /// <summary>
    /// Authenticates a user with quick connect.
    /// </summary>
    /// <param name="request">The <see cref="QuickConnectDto"/> request.</param>
    /// <response code="200">User authenticated.</response>
    /// <response code="400">Missing token.</response>
    /// <returns>A <see cref="Task"/> containing an <see cref="AuthenticationRequest"/> with information about the new session.</returns>
    [HttpPost("AuthenticateWithQuickConnect")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Session>> AuthenticateWithQuickConnect([FromBody, Required] QuickConnectDto request)
    {
        var remoteEndpoint = HttpContext.GetNormalizedRemoteIP().ToString();

        try
        {
            var (provider, result) = await _userAuthenticationManager.Authenticate(new ExternallyTriggeredAuthenticationData(request.Secret), remoteEndpoint, "QuickConnect").ConfigureAwait(false);

            if (provider is not IKeyedMonitorable<QuickConnectResult> monitorable)
            {
                return Unauthorized("Quick connect is disabled");
            }

            if (!result.Authenticated)
            {
                return Unauthorized("Unknown secret");
            }

            var auth = await _authContext.GetAuthorizationInfo(Request).ConfigureAwait(false);

            return await _sessionManager.CreateSession(result.User, auth.DeviceId, auth.Client, auth.Version, auth.Device, provider.GetType().FullName, remoteEndpoint).ConfigureAwait(false);
        }
        catch (SecurityException e)
        {
            // rethrow adding IP address to message
            throw new SecurityException($"[{remoteEndpoint}] {e.Message}", e);
        }
    }

    /// <summary>
    /// Updates a user's password.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The <see cref="UpdateUserPassword"/> request.</param>
    /// <response code="204">Password successfully reset.</response>
    /// <response code="403">User is not allowed to update the password.</response>
    /// <response code="404">User not found.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success or a <see cref="ForbidResult"/> or a <see cref="NotFoundResult"/> on failure.</returns>
    [HttpPost("Password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateUserPassword(
        [FromQuery] Guid? userId,
        [FromBody, Required] UpdateUserPassword request)
    {
        var requestUserId = userId ?? User.GetUserId();
        var user = _userManager.GetUserById(requestUserId);
        if (user is null)
        {
            return NotFound();
        }

        if (!RequestHelpers.AssertCanUpdateUser(User, user, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to update the password.");
        }

        var passwordProvider = await _userAuthenticationManager.ResolveProvider<UsernamePasswordAuthData>().ConfigureAwait(false);

        if (passwordProvider is not IPasswordChangeable passwordChangeable)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "You cannot change your password for this authentication provider.");
        }

        if (request.ResetPassword)
        {
            await passwordChangeable.ResetPassword(user).ConfigureAwait(false);
        }
        else
        {
            if (!User.IsInRole(UserRoles.Administrator) || (userId.HasValue && User.GetUserId().Equals(userId.Value)))
            {
                var authenticationRes = await passwordProvider.Authenticate(new UsernamePasswordAuthData(user.Username, request.CurrentPw ?? string.Empty, request.TOTP)).ConfigureAwait(false);

                if (!authenticationRes.Authenticated)
                {
                    if (authenticationRes.ErrorCode == 1300) // arbitrarily chosen error code used to signal that the username and password were correct, but TOTP was not
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, "A TOTP code is required.");
                    }

                    return StatusCode(StatusCodes.Status403Forbidden, "Invalid user or password entered.");
                }
            }

            await passwordChangeable.ChangePassword(user, request.NewPw ?? string.Empty).ConfigureAwait(false);

            var currentToken = User.GetToken();
            await _sessionManager.RevokeUserTokens(user.Id, currentToken).ConfigureAwait(false);
        }

        return NoContent();
    }

    /// <summary>
    /// Updates a user's password.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The <see cref="UpdateUserPassword"/> request.</param>
    /// <response code="204">Password successfully reset.</response>
    /// <response code="403">User is not allowed to update the password.</response>
    /// <response code="404">User not found.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success or a <see cref="ForbidResult"/> or a <see cref="NotFoundResult"/> on failure.</returns>
    [HttpPost("{userId}/Password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult> UpdateUserPasswordLegacy(
        [FromRoute, Required] Guid userId,
        [FromBody, Required] UpdateUserPassword request)
        => UpdateUserPassword(userId, request);

    /// <summary>
    /// Updates a user's easy password.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The <see cref="UpdateUserEasyPassword"/> request.</param>
    /// <response code="204">Password successfully reset.</response>
    /// <response code="403">User is not allowed to update the password.</response>
    /// <response code="404">User not found.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success or a <see cref="ForbidResult"/> or a <see cref="NotFoundResult"/> on failure.</returns>
    [HttpPost("{userId}/EasyPassword")]
    [Obsolete("Use Quick Connect instead")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult UpdateUserEasyPassword(
        [FromRoute, Required] Guid userId,
        [FromBody, Required] UpdateUserEasyPassword request)
    {
        return Forbid();
    }

    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="updateUser">The updated user model.</param>
    /// <response code="204">User updated.</response>
    /// <response code="400">User information was not supplied.</response>
    /// <response code="403">User update forbidden.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success or a <see cref="BadRequestResult"/> or a <see cref="ForbidResult"/> on failure.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateUser(
        [FromQuery] Guid? userId,
        [FromBody, Required] UserDto updateUser)
    {
        var requestUserId = userId ?? User.GetUserId();
        var user = _userManager.GetUserById(requestUserId);
        if (user is null)
        {
            return NotFound();
        }

        if (!RequestHelpers.AssertCanUpdateUser(User, user, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User update not allowed.");
        }

        if (!string.Equals(user.Username, updateUser.Name, StringComparison.Ordinal))
        {
            await _userManager.RenameUser(user, updateUser.Name).ConfigureAwait(false);
        }

        await _userManager.UpdateConfigurationAsync(requestUserId, updateUser.Configuration).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Updates a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="updateUser">The updated user model.</param>
    /// <response code="204">User updated.</response>
    /// <response code="400">User information was not supplied.</response>
    /// <response code="403">User update forbidden.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success or a <see cref="BadRequestResult"/> or a <see cref="ForbidResult"/> on failure.</returns>
    [HttpPost("{userId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult> UpdateUserLegacy(
        [FromRoute, Required] Guid userId,
        [FromBody, Required] UserDto updateUser)
        => UpdateUser(userId, updateUser);

    /// <summary>
    /// Updates a user policy.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="newPolicy">The new user policy.</param>
    /// <response code="204">User policy updated.</response>
    /// <response code="400">User policy was not supplied.</response>
    /// <response code="403">User policy update forbidden.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success or a <see cref="BadRequestResult"/> or a <see cref="ForbidResult"/> on failure..</returns>
    [HttpPost("{userId}/Policy")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateUserPolicy(
        [FromRoute, Required] Guid userId,
        [FromBody, Required] UserPolicy newPolicy)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        // If removing admin access
        if (!newPolicy.IsAdministrator && user.HasPermission(PermissionKind.IsAdministrator))
        {
            if (_userManager.Users.Count(i => i.HasPermission(PermissionKind.IsAdministrator)) == 1)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "There must be at least one user in the system with administrative access.");
            }
        }

        // If disabling
        if (newPolicy.IsDisabled && user.HasPermission(PermissionKind.IsAdministrator))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Administrators cannot be disabled.");
        }

        // If disabling
        if (newPolicy.IsDisabled && !user.HasPermission(PermissionKind.IsDisabled))
        {
            if (_userManager.Users.Count(i => !i.HasPermission(PermissionKind.IsDisabled)) == 1)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "There must be at least one enabled user in the system.");
            }

            var currentToken = User.GetToken();
            await _sessionManager.RevokeUserTokens(user.Id, currentToken).ConfigureAwait(false);
        }

        await _userManager.UpdatePolicyAsync(userId, newPolicy).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Updates a user configuration.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="userConfig">The new user configuration.</param>
    /// <response code="204">User configuration updated.</response>
    /// <response code="403">User configuration update forbidden.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Configuration")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateUserConfiguration(
        [FromQuery] Guid? userId,
        [FromBody, Required] UserConfiguration userConfig)
    {
        var requestUserId = userId ?? User.GetUserId();
        var user = _userManager.GetUserById(requestUserId);
        if (user is null)
        {
            return NotFound();
        }

        if (!RequestHelpers.AssertCanUpdateUser(User, user, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User configuration update not allowed");
        }

        await _userManager.UpdateConfigurationAsync(requestUserId, userConfig).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Updates a user configuration.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="userConfig">The new user configuration.</param>
    /// <response code="204">User configuration updated.</response>
    /// <response code="403">User configuration update forbidden.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("{userId}/Configuration")]
    [Authorize]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<ActionResult> UpdateUserConfigurationLegacy(
        [FromRoute, Required] Guid userId,
        [FromBody, Required] UserConfiguration userConfig)
        => UpdateUserConfiguration(userId, userConfig);

    /// <summary>
    /// Creates a user.
    /// </summary>
    /// <param name="request">The create user by name request body.</param>
    /// <response code="200">User created.</response>
    /// <returns>An <see cref="UserDto"/> of the new user.</returns>
    [HttpPost("New")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> CreateUserByName([FromBody, Required] CreateUserByName request)
    {
        var newUser = await _userManager.CreateUserAsync(request.Name).ConfigureAwait(false);

        // no need to authenticate password for new user
        if (request.Password is not null)
        {
            var passwordProvider = await _userAuthenticationManager.ResolveProvider<UsernamePasswordAuthData>().ConfigureAwait(false);

            if (passwordProvider is not IPasswordChangeable passwordChangeable)
            {
                await _userManager.DeleteUserAsync(newUser.Id).ConfigureAwait(false);
                throw new InvalidOperationException("You cannot create a password for this authentication provider.");
            }

            await passwordChangeable.ChangePassword(newUser, request.Password).ConfigureAwait(false);
        }

        var result = _userManager.GetUserDto(newUser, HttpContext.GetNormalizedRemoteIP().ToString());

        return result;
    }

    /// <summary>
    /// Initiates the forgot password process for a local user.
    /// </summary>
    /// <param name="forgotPasswordRequest">The forgot password request containing the entered username.</param>
    /// <response code="200">Password reset process started.</response>
    /// <returns>A <see cref="Task"/> containing a <see cref="ForgotPasswordResult"/>.</returns>
    [HttpPost("ForgotPassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword([FromBody, Required] ForgotPasswordDto forgotPasswordRequest)
    {
        var ip = HttpContext.GetNormalizedRemoteIP();
        var isLocal = HttpContext.IsLocal()
                      || _networkManager.IsInLocalNetwork(ip);

        if (!isLocal)
        {
            _logger.LogWarning("Password reset process initiated from outside the local network with IP: {IP}", ip);
        }

        var result = await _userManager.StartForgotPasswordProcess(forgotPasswordRequest.EnteredUsername, isLocal).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Redeems a forgot password pin.
    /// </summary>
    /// <param name="forgotPasswordPinRequest">The forgot password pin request containing the entered pin.</param>
    /// <response code="200">Pin reset process started.</response>
    /// <returns>A <see cref="Task"/> containing a <see cref="PinRedeemResult"/>.</returns>
    [HttpPost("ForgotPassword/Pin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PinRedeemResult>> ForgotPasswordPin([FromBody, Required] ForgotPasswordPinDto forgotPasswordPinRequest)
    {
        var result = await _userManager.RedeemPasswordResetPin(forgotPasswordPinRequest.Pin).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Enables or disables MFA for a user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="request">The <see cref="SetUserMFADto"/> containing a boolean that indicates whether you want to disable or enable MFA for this user.</param>
    /// <response code="200">Success.</response>
    /// <response code="404">User not found.</response>
    /// <returns>A <see cref="OkResult"/> indicating success, a <see cref="NotFoundResult"/> if the user was not found,
    /// or a <see cref="BadRequestResult"/> if the default username/password authentication provider is not enabled and thus MFA
    /// cannot be enabled.</returns>
    [HttpPost("MFA/{userId}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetMFA([FromRoute, Required] Guid userId, [FromBody, Required] SetUserMFADto request)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var defaultProvider = await _userAuthenticationManager.ResolveConcrete<DefaultAuthenticationProvider>().ConfigureAwait(false);

        if (defaultProvider is null)
        {
            return BadRequest("Default authentication provider is not enabled.");
        }

        await defaultProvider.SetMFA(user, request.Enable).ConfigureAwait(false);

        return Ok();
    }

    /// <summary>
    /// Gets the user based on auth token.
    /// </summary>
    /// <response code="200">User returned.</response>
    /// <response code="400">Token is not owned by a user.</response>
    /// <returns>A <see cref="UserDto"/> for the authenticated user.</returns>
    [HttpGet("Me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<UserDto> GetCurrentUser()
    {
        var userId = User.GetUserId();
        if (userId.IsEmpty())
        {
            return BadRequest();
        }

        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return BadRequest();
        }

        return _userManager.GetUserDto(user);
    }

    private IEnumerable<UserDto> Get(bool? isHidden, bool? isDisabled, bool filterByDevice, bool filterByNetwork)
    {
        var users = _userManager.Users;

        if (isDisabled.HasValue)
        {
            users = users.Where(i => i.HasPermission(PermissionKind.IsDisabled) == isDisabled.Value);
        }

        if (isHidden.HasValue)
        {
            users = users.Where(i => i.HasPermission(PermissionKind.IsHidden) == isHidden.Value);
        }

        if (filterByDevice)
        {
            var deviceId = User.GetDeviceId();

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                users = users.Where(i => _deviceManager.CanAccessDevice(i, deviceId));
            }
        }

        if (filterByNetwork)
        {
            if (!_networkManager.IsInLocalNetwork(HttpContext.GetNormalizedRemoteIP()))
            {
                users = users.Where(i => i.HasPermission(PermissionKind.EnableRemoteAccess));
            }
        }

        var result = users
            .OrderBy(u => u.Username)
            .Select(i => _userManager.GetUserDto(i, HttpContext.GetNormalizedRemoteIP().ToString()));

        return result;
    }
}
