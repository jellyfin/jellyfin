using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Threading.Tasks;
using Emby.Server.Implementations.QuickConnect;
using ICU4N.Util;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.QuickConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.BluRaySup;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Quick connect controller.
/// </summary>
public class QuickConnectController : BaseJellyfinApiController
{
    private readonly IAuthorizationContext _authContext;
    private readonly IUserAuthenticationManager _userAuthenticationManager;
    private readonly ILogger<QuickConnectController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickConnectController"/> class.
    /// </summary>
    /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
    /// <param name="userAuthenticationManager">Instance of the <see cref="IUserAuthenticationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    public QuickConnectController(
        IAuthorizationContext authContext,
        IUserAuthenticationManager userAuthenticationManager,
        ILogger<QuickConnectController> logger)
    {
        _authContext = authContext;
        _userAuthenticationManager = userAuthenticationManager;
        _logger = logger;
    }

    private Task<QuickConnectManager?> GetQuickConnectProvider()
    {
        return _userAuthenticationManager.ResolveConcrete<QuickConnectManager>();
    }

    /// <summary>
    /// Gets the current quick connect state.
    /// </summary>
    /// <response code="200">Quick connect state returned.</response>
    /// <returns>Whether Quick Connect is enabled on the server or not.</returns>
    [HttpGet("Enabled")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> GetQuickConnectEnabled()
    {
        return await GetQuickConnectProvider().ConfigureAwait(false) != null;
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
        var quickConnectProvider = await GetQuickConnectProvider().ConfigureAwait(false);

        if (quickConnectProvider is null)
        {
            return Unauthorized("Quick connect is disabled");
        }

        var auth = await _authContext.GetAuthorizationInfo(Request).ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrEmpty(auth.DeviceId);
        ArgumentException.ThrowIfNullOrEmpty(auth.Device);
        ArgumentException.ThrowIfNullOrEmpty(auth.Client);
        ArgumentException.ThrowIfNullOrEmpty(auth.Version);

        var res = new QuickConnectResult(
                DateTime.UtcNow,
                auth.DeviceId,
                auth.Device,
                auth.Client,
                auth.Version);

        var monitorData = await quickConnectProvider.Initiate(res).ConfigureAwait(false);

        res.Secret = monitorData.MonitorKey;
        res.Code = monitorData.UpdateKey;

        return res;
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
    /// <param name="waitForUpdate">Flag indicating whether or not to wait for update if it is equal to "1".</param>
    /// <response code="200">Quick connect result returned.</response>
    /// <response code="404">Unknown quick connect secret.</response>
    /// <returns>An updated <see cref="QuickConnectResult"/>.</returns>
    [HttpGet("Connect")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuickConnectResult>> GetQuickConnectState([FromQuery, Required] string secret, [FromQuery] string? waitForUpdate = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        var quickConnectProvider = await GetQuickConnectProvider().ConfigureAwait(false);

        if (quickConnectProvider is null)
        {
            return Unauthorized("Quick connect is disabled");
        }

        var data = await quickConnectProvider.GetData(secret, waitForUpdate == "1").ConfigureAwait(false);

        if (data is null)
        {
            return NotFound("Unknown secret");
        }

        return data;
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

        ArgumentNullException.ThrowIfNullOrEmpty(code);
        if (!userId.HasValue)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var quickConnectProvider = await GetQuickConnectProvider().ConfigureAwait(false);

        if (quickConnectProvider is null)
        {
            return Unauthorized("Quick connect is disabled");
        }

        try
        {
            var success = await quickConnectProvider.Authorize(code, userId.Value).ConfigureAwait(false);
            if (success)
            {
                _logger.LogDebug("Authorizing device with code {Code} to login as user {UserId}", code, userId);
            }

            return success;
        }
        catch (AuthenticationException)
        {
            return Unauthorized("Quick connect is disabled");
        }
    }
}
