using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Models.MediaSegmentsDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Api;
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
    private readonly IMediaSegmentsManager _mediaSegmentManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentsController"/> class.
    /// </summary>
    /// <param name="mediaSegmentManager">Instance of the <see cref="IMediaSegmentsManager"/> interface.</param>
    public MediaSegmentsController(
        IMediaSegmentsManager mediaSegmentManager)
    {
        _mediaSegmentManager = mediaSegmentManager;
    }

    /// <summary>
    /// Get all media segments.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="streamIndex">Just segments with MediaStreamIndex.</param>
    /// <param name="type">All segments of type.</param>
    /// <param name="typeIndex">All segments with typeIndex.</param>
    /// <response code="200">Segments returned.</response>
    /// <returns>An <see cref="OkResult"/>containing the found segments.</returns>
    [HttpGet("{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MediaSegmentDto>>> GetSegments(
        [FromRoute, Required] Guid itemId,
        [FromQuery] int? streamIndex,
        [FromQuery] MediaSegmentType? type,
        [FromQuery] int? typeIndex)
    {
        var list = await _mediaSegmentManager.GetAllMediaSegments(itemId, streamIndex, typeIndex, type).ConfigureAwait(false);
        return Ok(list.ConvertAll(MediaSegmentDto.FromMediaSegment));
    }

    /// <summary>
    /// Create or update multiple media segments.
    /// </summary>
    /// <param name="itemId">The item the segments belong to.</param>
    /// <param name="segments">All segments that should be added.</param>
    /// <response code="200">Segments returned.</response>
    /// <response code="400">Invalid segments.</response>
    /// <returns>An <see cref="OkResult"/>containing the created/updated segments.</returns>
    [HttpPost("{itemId}")]
    [Authorize(Policy = Policies.MediaSegmentsManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MediaSegmentDto>>> PostSegments(
        [FromRoute, Required] Guid itemId,
        [FromBody, Required] IReadOnlyList<MediaSegmentDto> segments)
    {
        var segmentsToAdd = segments.ConvertAll(s => s.ToMediaSegment());
        var addedSegments = await _mediaSegmentManager.CreateMediaSegments(itemId, segmentsToAdd).ConfigureAwait(false);

        return Ok(addedSegments.ConvertAll(MediaSegmentDto.FromMediaSegment));
    }

    /// <summary>
    /// Delete media segments. All query parameters can be freely defined.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="streamIndex">Segment is associated with MediaStreamIndex.</param>
    /// <param name="type">All segments of type.</param>
    /// <param name="typeIndex">All segments with typeIndex.</param>
    /// <response code="200">Segments returned.</response>
    /// <response code="404">Segments not found.</response>
    /// <returns>An <see cref="OkResult"/>containing the deleted segments.</returns>
    [HttpDelete("{itemId}")]
    [Authorize(Policy = Policies.MediaSegmentsManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MediaSegmentDto>>> DeleteSegments(
        [FromRoute, Required] Guid itemId,
        [FromQuery] int? streamIndex,
        [FromQuery] MediaSegmentType? type,
        [FromQuery] int? typeIndex)
    {
        var list = await _mediaSegmentManager.DeleteSegments(itemId, streamIndex, typeIndex, type).ConfigureAwait(false);
        return Ok(list.ConvertAll(MediaSegmentDto.FromMediaSegment));
    }
}
