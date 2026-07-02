using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.ApiKeyDtos;
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
[Tags("Authentication")]
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
    /// <returns>A <see cref="QueryResult{AuthenticationInfoDto}"/> with all keys.</returns>
    [HttpGet("Keys")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryResult<AuthenticationInfoDto>>> GetKeys()
    {
        var keys = await _authenticationManager.GetApiKeys().ConfigureAwait(false);

        return new QueryResult<AuthenticationInfoDto>(keys.Select(ToDto).ToList());
    }

    /// <summary>
    /// Create a new api key.
    /// </summary>
    /// <param name="app">Name of the app using the authentication key.</param>
    /// <response code="200">Api key created.</response>
    /// <returns>The created api key.</returns>
    [HttpPost("Keys")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationInfoDto>> CreateKey([FromQuery, Required] string app)
    {
        var key = await _authenticationManager.CreateApiKey(app).ConfigureAwait(false);

        return ToDto(key);
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

    private static AuthenticationInfoDto ToDto(AuthenticationInfo info)
        => new()
        {
            AccessToken = info.AccessToken,
            AppName = info.AppName,
            DateCreated = info.DateCreated
        };
}
