using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// User controller.
    /// </summary>
    [Route("/Users")]
    public class UserController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly ISessionManager _sessionManager;
        private readonly INetworkManager _networkManager;
        private readonly IDeviceManager _deviceManager;
        private readonly IAuthorizationContext _authContext;
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public UserController(
            IUserManager userManager,
            ISessionManager sessionManager,
            INetworkManager networkManager,
            IDeviceManager deviceManager,
            IAuthorizationContext authContext,
            IServerConfigurationManager config)
        {
            _userManager = userManager;
            _sessionManager = sessionManager;
            _networkManager = networkManager;
            _deviceManager = deviceManager;
            _authContext = authContext;
            _config = config;
        }

        /// <summary>
        /// Gets a list of users.
        /// </summary>
        /// <param name="isHidden">Optional filter by IsHidden=true or false.</param>
        /// <param name="isDisabled">Optional filter by IsDisabled=true or false.</param>
        /// <param name="isGuest">Optional filter by IsGuest=true or false.</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<UserDto>> GetUsers(
            [FromQuery] bool? isHidden,
            [FromQuery] bool? isDisabled,
            [FromQuery] bool? isGuest)
        {
            return Ok(Get(isHidden, isDisabled, isGuest, false, false));
        }

        /// <summary>
        /// Gets a list of publicly visible users for display on a login screen.
        /// </summary>
        /// <returns></returns>
        [HttpGet("Public")]
        public ActionResult<IEnumerable<UserDto>> GetPublicUsers()
        {
            // If the startup wizard hasn't been completed then just return all users
            if (!_config.Configuration.IsStartupWizardCompleted)
            {
                return GetUsers(null, false, null);
            }

            return Ok(Get(false, false, false, true, true));
        }

        /// <summary>
        /// Gets a user by Id.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        // TODO: authorize escapeParentalControl
        public ActionResult<UserDto> GetUserById([FromRoute] Guid id)
        {
            var user = _userManager.GetUserById(id);

            if (user == null)
            {
                throw new ResourceNotFoundException("User not found");
            }

            var result = _userManager.GetUserDto(user, HttpContext.Connection.RemoteIpAddress.ToString());

            return Ok(result);
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.RequiresElevation)]
        public ActionResult DeleteUser([FromRoute] Guid id)
        {
            var user = _userManager.GetUserById(id);

            if (user == null)
            {
                throw new ResourceNotFoundException("User not found");
            }

            _sessionManager.RevokeUserTokens(user.Id, null);
            _userManager.DeleteUser(user);
            return NoContent();
        }

        /// <summary>
        /// Authenticates a user.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <param name="pw"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost("{id}/Authenticate")]
        public async Task<ActionResult<AuthenticationResult>> AuthenticateUser(
            [FromRoute, Required] Guid id,
            [FromQuery, BindRequired] string pw,
            [FromQuery, BindRequired] string password)
        {
            var user = _userManager.GetUserById(id);

            if (user == null)
            {
                return NotFound("User not found");
            }

            if (!string.IsNullOrEmpty(password) && string.IsNullOrEmpty(pw))
            {
                throw new MethodNotAllowedException();
            }

            // Password should always be null
            return await AuthenticateUserByName(user.Username, null, pw).ConfigureAwait(false);
        }

        /// <summary>
        /// Authenticates a user by name.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="pw"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost("AuthenticateByName")]
        public async Task<ActionResult<AuthenticationResult>> AuthenticateUserByName(
            [FromQuery, BindRequired] string username,
            [FromQuery, BindRequired] string pw,
            [FromQuery, BindRequired] string password)
        {
            var auth = _authContext.GetAuthorizationInfo(Request);

            try
            {
                var result = await _sessionManager.AuthenticateNewSession(new AuthenticationRequest
                {
                    App = auth.Client,
                    AppVersion = auth.Version,
                    DeviceId = auth.DeviceId,
                    DeviceName = auth.Device,
                    Password = pw,
                    PasswordSha1 = password,
                    RemoteEndPoint = HttpContext.Connection.RemoteIpAddress.ToString(),
                    Username = username
                }).ConfigureAwait(false);

                return Ok(result);
            }
            catch (SecurityException e)
            {
                // rethrow adding IP address to message
                throw new SecurityException($"[{HttpContext.Connection.RemoteIpAddress}] {e.Message}", e);
            }
        }

        /// <summary>
        /// Updates a user's password.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="currentPassword"></param>
        /// <param name="currentPw"></param>
        /// <param name="newPw"></param>
        /// <param name="resetPassword">Whether to reset the password.</param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("{id}/Password")]
        [Authorize]
        public async Task<ActionResult> UpdateUserPassword(
            [FromRoute] Guid id,
            [FromQuery] string currentPassword,
            [FromQuery] string currentPw,
            [FromQuery] string newPw,
            [FromQuery] bool resetPassword)
        {
            AssertCanUpdateUser(_authContext, _userManager, id, true);

            var user = _userManager.GetUserById(id);

            if (user == null)
            {
                return NotFound("User not found");
            }

            if (resetPassword)
            {
                await _userManager.ResetPassword(user).ConfigureAwait(false);
            }
            else
            {
                var success = await _userManager.AuthenticateUser(
                    user.Username,
                    currentPw,
                    currentPassword,
                    HttpContext.Connection.RemoteIpAddress.ToString(),
                    false).ConfigureAwait(false);

                if (success == null)
                {
                    throw new ArgumentException("Invalid user or password entered.");
                }

                await _userManager.ChangePassword(user, newPw).ConfigureAwait(false);

                var currentToken = _authContext.GetAuthorizationInfo(Request).Token;

                _sessionManager.RevokeUserTokens(user.Id, currentToken);
            }

            return NoContent();
        }

        /// <summary>
        /// Updates a user's easy password.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newPassword"></param>
        /// <param name="newPw"></param>
        /// <param name="resetPassword"></param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("{id}/EasyPassword")]
        [Authorize]
        public ActionResult UpdateUserEasyPassword(
            [FromRoute] Guid id,
            [FromQuery] string newPassword,
            [FromQuery] string newPw,
            [FromQuery] bool resetPassword)
        {
            AssertCanUpdateUser(_authContext, _userManager, id, true);

            var user = _userManager.GetUserById(id);

            if (user == null)
            {
                return NotFound("User not found");
            }

            if (resetPassword)
            {
                _userManager.ResetEasyPassword(user);
            }
            else
            {
                _userManager.ChangeEasyPassword(user, newPw, newPassword);
            }

            return NoContent();
        }

        /// <summary>
        /// Updates a user.
        /// </summary>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("{id}")]
        [Authorize]
        public ActionResult UpdateUser() // TODO: missing UserDto
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates a user policy.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("{id}/Policy")]
        [Authorize]
        public ActionResult UpdateUserPolicy([FromRoute] Guid id) // TODO: missing UserPolicy
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates a user configuration.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("{id}/Configuration")]
        [Authorize]
        public ActionResult UpdateUserConfiguration([FromRoute] Guid id) // TODO: missing UserConfiguration
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a user.
        /// </summary>
        /// <param name="name">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("/Users/New")]
        [Authorize(Policy = Policies.RequiresElevation)]
        public async Task<ActionResult> CreateUserByName(
            [FromBody] string name,
            [FromBody] string password)
        {
            var newUser = _userManager.CreateUser(name);

            // no need to authenticate password for new user
            if (password != null)
            {
                await _userManager.ChangePassword(newUser, password).ConfigureAwait(false);
            }

            var result = _userManager.GetUserDto(newUser, HttpContext.Connection.RemoteIpAddress.ToString());

            return Ok(result);
        }

        /// <summary>
        /// Initiates the forgot password process for a local user.
        /// </summary>
        /// <param name="enteredUsername">The entered username.</param>
        /// <returns></returns>
        [HttpPost("ForgotPassword")]
        public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword([FromBody] string enteredUsername)
        {
            var isLocal = HttpContext.Connection.RemoteIpAddress.Equals(HttpContext.Connection.LocalIpAddress)
                          || _networkManager.IsInLocalNetwork(HttpContext.Connection.RemoteIpAddress.ToString());

            var result = await _userManager.StartForgotPasswordProcess(enteredUsername, isLocal).ConfigureAwait(false);

            return Ok(result);
        }

        /// <summary>
        /// Redeems a forgot password pin.
        /// </summary>
        /// <param name="pin">The pin.</param>
        /// <returns></returns>
        [HttpPost("ForgotPassword/Pin")]
        public async Task<ActionResult<PinRedeemResult>> ForgotPasswordPin([FromBody] string pin)
        {
            var result = await _userManager.RedeemPasswordResetPin(pin).ConfigureAwait(false);
            return Ok(result);
        }

        private IEnumerable<UserDto> Get(bool? isHidden, bool? isDisabled, bool? isGuest, bool filterByDevice, bool filterByNetwork)
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
                var deviceId = _authContext.GetAuthorizationInfo(Request).DeviceId;

                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    users = users.Where(i => _deviceManager.CanAccessDevice(i, deviceId));
                }
            }

            if (filterByNetwork)
            {
                if (!_networkManager.IsInLocalNetwork(HttpContext.Connection.RemoteIpAddress.ToString()))
                {
                    users = users.Where(i => i.HasPermission(PermissionKind.EnableRemoteAccess));
                }
            }

            var result = users
                .OrderBy(u => u.Username)
                .Select(i => _userManager.GetUserDto(i, HttpContext.Connection.RemoteIpAddress.ToString()))
                .ToArray();

            return result;
        }
    }
}
