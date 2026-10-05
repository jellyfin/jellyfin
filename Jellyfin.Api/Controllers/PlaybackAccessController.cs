using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.PlaybackAccessDtos;
using MediaBrowser.Controller.Streaming;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Delegates playback access without exposing account credentials to a renderer.
/// </summary>
[Authorize]
public sealed class PlaybackAccessController : BaseJellyfinApiController
{
    private readonly IPlaybackAccessManager _playbackAccessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackAccessController"/> class.
    /// </summary>
    /// <param name="playbackAccessManager">The playback access manager.</param>
    public PlaybackAccessController(IPlaybackAccessManager playbackAccessManager)
    {
        _playbackAccessManager = playbackAccessManager;
    }

    /// <summary>
    /// Creates an on-demand playback grant for the authenticated user.
    /// </summary>
    /// <param name="itemId">The item to play.</param>
    /// <param name="request">The media source and renderer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The playback credential and its resource scope.</returns>
    [HttpPost("Items/{itemId}/PlaybackAccess")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlaybackAccessGrant>> CreatePlaybackAccess(
        [FromRoute, Required] Guid itemId,
        [FromBody, Required] CreatePlaybackAccessDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        // API keys cannot select an arbitrary owner for delegated playback.
        if (userId.Equals(Guid.Empty))
        {
            return Forbid();
        }

        var grant = await _playbackAccessManager.CreateAsync(
            userId,
            itemId,
            request.MediaSourceId,
            request.DeviceId,
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            cancellationToken).ConfigureAwait(false);
        return Ok(grant);
    }

    /// <summary>
    /// Revokes the authenticated user's grants for a playback session.
    /// </summary>
    /// <param name="playSessionId">The playback session identifier.</param>
    /// <returns>No content.</returns>
    [HttpDelete("PlaybackAccess/{playSessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult RevokePlaybackAccess([FromRoute, Required] string playSessionId)
    {
        _playbackAccessManager.Revoke(playSessionId, User.GetUserId());
        return NoContent();
    }
}
