using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Authentication controller.
    /// </summary>
    [Route("Auth")]
    public class ApiKeyController : BaseJellyfinApiController
    {
        private readonly ISessionManager _sessionManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IAuthenticationRepository _authRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyController"/> class.
        /// </summary>
        /// <param name="sessionManager">Instance of <see cref="ISessionManager"/> interface.</param>
        /// <param name="appHost">Instance of <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="authRepo">Instance of <see cref="IAuthenticationRepository"/> interface.</param>
        public ApiKeyController(
            ISessionManager sessionManager,
            IServerApplicationHost appHost,
            IAuthenticationRepository authRepo)
        {
            _sessionManager = sessionManager;
            _appHost = appHost;
            _authRepo = authRepo;
        }

        /// <summary>
        /// Get all keys.
        /// </summary>
        /// <response code="200">Api keys retrieved.</response>
        /// <returns>A <see cref="QueryResult{AuthenticationInfo}"/> with all keys.</returns>
        [HttpGet("Keys")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<AuthenticationInfo>> GetKeys()
        {
            var result = _authRepo.Get(new AuthenticationInfoQuery
            {
                HasUser = false
            });

            return result;
        }

        /// <summary>
        /// Create a new api key.
        /// </summary>
        /// <param name="app">Name of the app using the authentication key.</param>
        /// <response code="204">Api key created.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPost("Keys")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult CreateKey([FromQuery, Required] string app)
        {
            _authRepo.Create(new AuthenticationInfo
            {
                AppName = app,
                AccessToken = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DateCreated = DateTime.UtcNow,
                DeviceId = _appHost.SystemId,
                DeviceName = _appHost.FriendlyName,
                AppVersion = _appHost.ApplicationVersionString
            });
            return NoContent();
        }

        /// <summary>
        /// Remove an api key.
        /// </summary>
        /// <param name="key">The access token to delete.</param>
        /// <response code="204">Api key deleted.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpDelete("Keys/{key}")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult RevokeKey([FromRoute, Required] string key)
        {
            _sessionManager.RevokeToken(key);
            return NoContent();
        }
    }
}
