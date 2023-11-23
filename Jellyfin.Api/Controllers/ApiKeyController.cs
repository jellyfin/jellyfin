using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Authentication controller.
/// </summary>
[Route("Auth")]
public class ApiKeyController : BaseJellyfinApiController
{
    private readonly IAuthenticationManager _authenticationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyController"/> class.
    /// </summary>
    /// <param name="authenticationManager">Instance of <see cref="IAuthenticationManager"/> interface.</param>
    public ApiKeyController(IAuthenticationManager authenticationManager)
    {
        _authenticationManager = authenticationManager;
    }

    /// <summary>
    /// Get all keys.
    /// </summary>
    /// <response code="200">Api keys retrieved.</response>
    /// <returns>A <see cref="QueryResult{AuthenticationInfo}"/> with all keys.</returns>
    [HttpGet("Keys")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryResult<AuthenticationInfo>>> GetKeys()
    {
        var keys = await _authenticationManager.GetApiKeys().ConfigureAwait(false);

        return new QueryResult<AuthenticationInfo>(keys);
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
    public async Task<ActionResult> CreateKey([FromQuery, Required] string app)
    {
        await _authenticationManager.CreateApiKey(app).ConfigureAwait(false);

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
    public async Task<ActionResult> RevokeKey([FromRoute, Required] string key)
    {
        await _authenticationManager.DeleteApiKey(key).ConfigureAwait(false);

        return NoContent();
    }
}
