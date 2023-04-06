using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Model.QuickConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Quick connect controller.
/// </summary>
public class QuickConnectController : BaseJellyfinApiController
{
    private readonly IQuickConnect _quickConnect;
    private readonly IAuthorizationContext _authContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickConnectController"/> class.
    /// </summary>
    /// <param name="quickConnect">Instance of the <see cref="IQuickConnect"/> interface.</param>
    /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
    public QuickConnectController(IQuickConnect quickConnect, IAuthorizationContext authContext)
    {
        _quickConnect = quickConnect;
        _authContext = authContext;
    }

    /// <summary>
    /// Gets the current quick connect state.
    /// </summary>
    /// <response code="200">Quick connect state returned.</response>
    /// <returns>Whether Quick Connect is enabled on the server or not.</returns>
    [HttpGet("Enabled")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<bool> GetQuickConnectEnabled()
    {
        return _quickConnect.IsEnabled;
    }

    /// <summary>
    /// Initiate a new quick connect request.
    /// </summary>
    /// <response code="200">Quick connect request successfully created.</response>
    /// <response code="401">Quick connect is not active on this server.</response>
    /// <returns>A <see cref="QuickConnectResult"/> with a secret and code for future use or an error message.</returns>
    [HttpPost("Initiate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuickConnectResult>> InitiateQuickConnect()
    {
        try
        {
            var auth = await _authContext.GetAuthorizationInfo(Request).ConfigureAwait(false);
            return _quickConnect.TryConnect(auth);
        }
        catch (AuthenticationException)
        {
            return Unauthorized("Quick connect is disabled");
        }
    }

    /// <summary>
    /// Old version of <see cref="InitiateQuickConnect" /> using a GET method.
    /// Still available to avoid breaking compatibility.
    /// </summary>
    /// <returns>The result of <see cref="InitiateQuickConnect" />.</returns>
    [Obsolete("Use POST request instead")]
    [HttpGet("Initiate")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<QuickConnectResult>> InitiateQuickConnectLegacy() => InitiateQuickConnect();

    /// <summary>
    /// Attempts to retrieve authentication information.
    /// </summary>
    /// <param name="secret">Secret previously returned from the Initiate endpoint.</param>
    /// <response code="200">Quick connect result returned.</response>
    /// <response code="404">Unknown quick connect secret.</response>
    /// <returns>An updated <see cref="QuickConnectResult"/>.</returns>
    [HttpGet("Connect")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QuickConnectResult> GetQuickConnectState([FromQuery, Required] string secret)
    {
        try
        {
            return _quickConnect.CheckRequestStatus(secret);
        }
        catch (ResourceNotFoundException)
        {
            return NotFound("Unknown secret");
        }
        catch (AuthenticationException)
        {
            return Unauthorized("Quick connect is disabled");
        }
    }

    /// <summary>
    /// Authorizes a pending quick connect request.
    /// </summary>
    /// <param name="code">Quick connect code to authorize.</param>
    /// <param name="userId">The user the authorize. Access to the requested user is required.</param>
    /// <response code="200">Quick connect result authorized successfully.</response>
    /// <response code="403">Unknown user id.</response>
    /// <returns>Boolean indicating if the authorization was successful.</returns>
    [HttpPost("Authorize")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> AuthorizeQuickConnect([FromQuery, Required] string code, [FromQuery] Guid? userId = null)
    {
        userId = RequestHelpers.GetUserId(User, userId);

        try
        {
            return await _quickConnect.AuthorizeRequest(userId.Value, code).ConfigureAwait(false);
        }
        catch (AuthenticationException)
        {
            return Unauthorized("Quick connect is disabled");
        }
    }
}
