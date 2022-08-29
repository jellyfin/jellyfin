using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// User controller.
    /// </summary>
    [Route("Users")]
    public class UserController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly ISessionManager _sessionManager;
        private readonly INetworkManager _networkManager;
        private readonly IDeviceManager _deviceManager;
        private readonly IAuthorizationContext _authContext;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IQuickConnect _quickConnectManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="quickConnectManager">Instance of the <see cref="IQuickConnect"/> interface.</param>
        public UserController(
            IUserManager userManager,
            ISessionManager sessionManager,
            INetworkManager networkManager,
            IDeviceManager deviceManager,
            IAuthorizationContext authContext,
            IServerConfigurationManager config,
            ILogger<UserController> logger,
            IQuickConnect quickConnectManager)
        {
            _userManager = userManager;
            _sessionManager = sessionManager;
            _networkManager = networkManager;
            _deviceManager = deviceManager;
            _authContext = authContext;
            _config = config;
            _logger = logger;
            _quickConnectManager = quickConnectManager;
        }

        /// <summary>
        /// Gets a list of users.
        /// </summary>
        /// <param name="isHidden">Optional filter by IsHidden=true or false.</param>
        /// <param name="isDisabled">Optional filter by IsDisabled=true or false.</param>
        /// <response code="200">Users returned.</response>
        /// <returns>An <see cref="IEnumerable{UserDto}"/> containing the users.</returns>
        [HttpGet]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
            [FromQuery] bool? isHidden,
            [FromQuery] bool? isDisabled)
        {
            var users = await Get(isHidden, isDisabled, false, false).ConfigureAwait(false);
            return Ok(users);
        }

        /// <summary>
        /// Gets a list of publicly visible users for display on a login screen.
        /// </summary>
        /// <response code="200">Public users returned.</response>
        /// <returns>An <see cref="IEnumerable{UserDto}"/> containing the public users.</returns>
        [HttpGet("Public")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetPublicUsers()
        {
            // If the startup wizard hasn't been completed then just return all users
            if (!_config.Configuration.IsStartupWizardCompleted)
            {
                return Ok(await Get(false, false, false, false).ConfigureAwait(false));
            }

            return Ok(await Get(false, false, true, true).ConfigureAwait(false));
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

            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = _userManager.GetUserDto(user, HttpContext.GetNormalizedRemoteIp().ToString());
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
            await _sessionManager.RevokeUserTokens(user.Id, null).ConfigureAwait(false);
            await _userManager.DeleteUserAsync(userId).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>
        /// Authenticates a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="pw">The password as plain text.</param>
        /// <param name="password">The password sha1-hash.</param>
        /// <response code="200">User authenticated.</response>
        /// <response code="403">Sha1-hashed password only is not allowed.</response>
        /// <response code="404">User not found.</response>
        /// <returns>A <see cref="Task"/> containing an <see cref="AuthenticationResult"/>.</returns>
        [HttpPost("{userId}/Authenticate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AuthenticationResult>> AuthenticateUser(
            [FromRoute, Required] Guid userId,
            [FromQuery, Required] string pw,
            [FromQuery] string? password)
        {
            var user = _userManager.GetUserById(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            if (!string.IsNullOrEmpty(password) && string.IsNullOrEmpty(pw))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Only sha1 password is not allowed.");
            }

            AuthenticateUserByName request = new AuthenticateUserByName
            {
                Username = user.Username,
                Pw = pw
            };
            return await AuthenticateUserByName(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Authenticates a user by name.
        /// </summary>
        /// <param name="request">The <see cref="AuthenticateUserByName"/> request.</param>
        /// <response code="200">User authenticated.</response>
        /// <returns>A <see cref="Task"/> containing an <see cref="AuthenticationRequest"/> with information about the new session.</returns>
        [HttpPost("AuthenticateByName")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<AuthenticationResult>> AuthenticateUserByName([FromBody, Required] AuthenticateUserByName request)
        {
            var auth = await _authContext.GetAuthorizationInfo(Request).ConfigureAwait(false);

            try
            {
                var result = await _sessionManager.AuthenticateNewSession(new AuthenticationRequest
                {
                    App = auth.Client,
                    AppVersion = auth.Version,
                    DeviceId = auth.DeviceId,
                    DeviceName = auth.Device,
                    Password = request.Pw,
                    RemoteEndPoint = HttpContext.GetNormalizedRemoteIp().ToString(),
                    Username = request.Username
                }).ConfigureAwait(false);

                return result;
            }
            catch (SecurityException e)
            {
                // rethrow adding IP address to message
                throw new SecurityException($"[{HttpContext.GetNormalizedRemoteIp()}] {e.Message}", e);
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
        public ActionResult<AuthenticationResult> AuthenticateWithQuickConnect([FromBody, Required] QuickConnectDto request)
        {
            try
            {
                return _quickConnectManager.GetAuthorizedRequest(request.Secret);
            }
            catch (SecurityException e)
            {
                // rethrow adding IP address to message
                throw new SecurityException($"[{HttpContext.GetNormalizedRemoteIp()}] {e.Message}", e);
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
        [HttpPost("{userId}/Password")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateUserPassword(
            [FromRoute, Required] Guid userId,
            [FromBody, Required] UpdateUserPassword request)
        {
            if (!await RequestHelpers.AssertCanUpdateUser(_authContext, HttpContext.Request, userId, true).ConfigureAwait(false))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to update the password.");
            }

            var user = _userManager.GetUserById(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            if (request.ResetPassword)
            {
                await _userManager.ResetPassword(user).ConfigureAwait(false);
            }
            else
            {
                if (!HttpContext.User.IsInRole(UserRoles.Administrator))
                {
                    var success = await _userManager.AuthenticateUser(
                        user.Username,
                        request.CurrentPw,
                        request.CurrentPw,
                        HttpContext.GetNormalizedRemoteIp().ToString(),
                        false).ConfigureAwait(false);

                    if (success == null)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, "Invalid user or password entered.");
                    }
                }

                await _userManager.ChangePassword(user, request.NewPw).ConfigureAwait(false);

                var currentToken = (await _authContext.GetAuthorizationInfo(Request).ConfigureAwait(false)).Token;

                await _sessionManager.RevokeUserTokens(user.Id, currentToken).ConfigureAwait(false);
            }

            return NoContent();
        }

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
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateUserEasyPassword(
            [FromRoute, Required] Guid userId,
            [FromBody, Required] UpdateUserEasyPassword request)
        {
            if (!await RequestHelpers.AssertCanUpdateUser(_authContext, HttpContext.Request, userId, true).ConfigureAwait(false))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to update the easy password.");
            }

            var user = _userManager.GetUserById(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            if (request.ResetPassword)
            {
                await _userManager.ResetEasyPassword(user).ConfigureAwait(false);
            }
            else
            {
                await _userManager.ChangeEasyPassword(user, request.NewPw, request.NewPassword).ConfigureAwait(false);
            }

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
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateUser(
            [FromRoute, Required] Guid userId,
            [FromBody, Required] UserDto updateUser)
        {
            if (!await RequestHelpers.AssertCanUpdateUser(_authContext, HttpContext.Request, userId, false).ConfigureAwait(false))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "User update not allowed.");
            }

            var user = _userManager.GetUserById(userId);

            if (!string.Equals(user.Username, updateUser.Name, StringComparison.Ordinal))
            {
                await _userManager.RenameUser(user, updateUser.Name).ConfigureAwait(false);
            }

            await _userManager.UpdateConfigurationAsync(user.Id, updateUser.Configuration).ConfigureAwait(false);

            return NoContent();
        }

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

                var currentToken = (await _authContext.GetAuthorizationInfo(Request).ConfigureAwait(false)).Token;
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
        [HttpPost("{userId}/Configuration")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpdateUserConfiguration(
            [FromRoute, Required] Guid userId,
            [FromBody, Required] UserConfiguration userConfig)
        {
            if (!await RequestHelpers.AssertCanUpdateUser(_authContext, HttpContext.Request, userId, false).ConfigureAwait(false))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "User configuration update not allowed");
            }

            await _userManager.UpdateConfigurationAsync(userId, userConfig).ConfigureAwait(false);

            return NoContent();
        }

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
            if (request.Password != null)
            {
                await _userManager.ChangePassword(newUser, request.Password).ConfigureAwait(false);
            }

            var result = _userManager.GetUserDto(newUser, HttpContext.GetNormalizedRemoteIp().ToString());

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
            var ip = HttpContext.GetNormalizedRemoteIp();
            var isLocal = HttpContext.IsLocal()
                          || _networkManager.IsInLocalNetwork(ip);

            if (isLocal)
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
        /// Gets the user based on auth token.
        /// </summary>
        /// <response code="200">User returned.</response>
        /// <response code="400">Token is not owned by a user.</response>
        /// <returns>A <see cref="UserDto"/> for the authenticated user.</returns>
        [HttpGet("Me")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<UserDto> GetCurrentUser()
        {
            var userId = ClaimHelpers.GetUserId(Request.HttpContext.User);
            if (userId is null)
            {
                return BadRequest();
            }

            var user = _userManager.GetUserById(userId.Value);
            if (user == null)
            {
                return BadRequest();
            }

            return _userManager.GetUserDto(user);
        }

        private async Task<IEnumerable<UserDto>> Get(bool? isHidden, bool? isDisabled, bool filterByDevice, bool filterByNetwork)
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
                var deviceId = (await _authContext.GetAuthorizationInfo(Request).ConfigureAwait(false)).DeviceId;

                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    users = users.Where(i => _deviceManager.CanAccessDevice(i, deviceId));
                }
            }

            if (filterByNetwork)
            {
                if (!_networkManager.IsInLocalNetwork(HttpContext.GetNormalizedRemoteIp()))
                {
                    users = users.Where(i => i.HasPermission(PermissionKind.EnableRemoteAccess));
                }
            }

            var result = users
                .OrderBy(u => u.Username)
                .Select(i => _userManager.GetUserDto(i, HttpContext.GetNormalizedRemoteIp().ToString()));

            return result;
        }
    }
}
