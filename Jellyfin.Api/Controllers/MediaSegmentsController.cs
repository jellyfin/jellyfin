using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.MediaSegmentsDtos;
using Jellyfin.Data.Enums.MediaSegmentType;
using Jellyfin.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaSegments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Media Segments controller.
/// </summary>
[Authorize]
public class MediaSegmentsController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly IMediaSegmentsManager _mediaSegmentManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentsController"/> class.
    /// </summary>
    /// <param name="mediaSegmentManager">Instance of the <see cref="IMediaSegmentsManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">The library manager.</param>
    public MediaSegmentsController(
        IMediaSegmentsManager mediaSegmentManager,
        IUserManager userManager,
        ILibraryManager libraryManager)
    {
        _userManager = userManager;
        _mediaSegmentManager = mediaSegmentManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Get all media segments.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="streamIndex">Just segments with MediaStreamIndex.</param>
    /// <param name="type">All segments of type.</param>
    /// <param name="id">Unique id of segment.</param>
    /// <response code="200">Segments returned.</response>
    /// <response code="404">itemId doesn't exist.</response>
    /// <response code="401">User is not authorized to access the requested item.</response>
    /// <returns>An <see cref="OkResult"/>containing the found segments.</returns>
    [HttpGet("{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MediaSegmentDto>>> GetSegments(
        [FromRoute, Required] Guid itemId,
        [FromQuery] int? streamIndex,
        [FromQuery] MediaSegmentType? type,
        [FromQuery] Guid? id)
    {
        var isApiKey = User.GetIsApiKey();
        var userId = User.GetUserId();
        var user = !isApiKey && !userId.IsEmpty()
            ? _userManager.GetUserById(userId) ?? throw new ResourceNotFoundException()
            : null;

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        if (!isApiKey && !item.IsVisible(user))
        {
            return Unauthorized();
        }

        var list = await _mediaSegmentManager.GetAllMediaSegments(itemId, streamIndex, type, id).ConfigureAwait(false);
        return list.Count > 0 ? Ok(list.ConvertAll(MediaSegmentDto.FromMediaSegment)) : NotFound();
    }

    /// <summary>
    /// Create or update multiple media segments. All fields are updateable except Id.
    /// </summary>
    /// <param name="itemId">The item the segments belong to.</param>
    /// <param name="segments">All segments that should be added or updated.</param>
    /// <response code="200">Segments returned.</response>
    /// <response code="400">Invalid segments.</response>
    /// <response code="401">User is not authorized to access the requested item.</response>
    /// <returns>An <see cref="OkResult"/>containing the created/updated segments.</returns>
    [HttpPost("{itemId}")]
    [Authorize(Policy = Policies.MediaSegmentsManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MediaSegmentDto>>> CreateSegments(
        [FromRoute, Required] Guid itemId,
        [FromBody, Required] IReadOnlyList<MediaSegmentDto> segments)
    {
        var isApiKey = User.GetIsApiKey();
        var userId = User.GetUserId();
        var user = !isApiKey && !userId.IsEmpty()
            ? _userManager.GetUserById(userId) ?? throw new ResourceNotFoundException()
            : null;

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        if (!isApiKey && !item.IsVisible(user))
        {
            return Unauthorized();
        }

        var segmentsToAdd = segments.ConvertAll(s => s.ToMediaSegment());
        var addedSegments = await _mediaSegmentManager.CreateMediaSegments(itemId, segmentsToAdd).ConfigureAwait(false);
        return Ok(addedSegments.ConvertAll(MediaSegmentDto.FromMediaSegment));
    }

    /// <summary>
    /// Delete media segments.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="streamIndex">Segment is associated with MediaStreamIndex.</param>
    /// <param name="type">All segments of type.</param>
    /// <param name="id">Unique id of segment.</param>
    /// <response code="200">Segments deleted.</response>
    /// <response code="404">Segments not found.</response>
    /// <response code="401">User is not authorized to access the requested item.</response>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpDelete("{itemId}")]
    [Authorize(Policy = Policies.MediaSegmentsManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteSegments(
        [FromRoute, Required] Guid itemId,
        [FromQuery] int? streamIndex,
        [FromQuery] MediaSegmentType? type,
        [FromQuery] Guid? id)
    {
        var isApiKey = User.GetIsApiKey();
        var userId = User.GetUserId();
        var user = !isApiKey && !userId.IsEmpty()
            ? _userManager.GetUserById(userId) ?? throw new ResourceNotFoundException()
            : null;

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        if (!isApiKey && !item.IsVisible(user))
        {
            return Unauthorized();
        }

        await _mediaSegmentManager.DeleteSegments(itemId, streamIndex, type, id).ConfigureAwait(false);
        return Ok();
    }
}
