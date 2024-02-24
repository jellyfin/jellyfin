using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
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
public class MediaSegmentController : BaseJellyfinApiController
{
    private readonly IMediaSegmentsManager _mediaSegmentManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentController"/> class.
    /// </summary>
    /// <param name="mediaSegmentManager">Instance of the <see cref="IMediaSegmentsManager"/> interface.</param>
    public MediaSegmentController(
        IMediaSegmentsManager mediaSegmentManager)
    {
        _mediaSegmentManager = mediaSegmentManager;
    }

    /// <summary>
    /// Get all media segments.
    /// </summary>
    /// <param name="itemId">Optional: Just segments with MediaSourceId.</param>
    /// <param name="streamIndex">Optional: Just segments with MediaStreamIndex.</param>
    /// <param name="type">Optional: All segments of type.</param>
    /// <param name="typeIndex">Optional: All segments with typeIndex.</param>
    /// <response code="200">Segments returned.</response>
    /// <returns>An <see cref="OkResult"/>containing the found segments.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<MediaSegment>> GetSegments(
        [FromQuery] Guid itemId,
        [FromQuery, DefaultValue(-1)] int streamIndex,
        [FromQuery] MediaSegmentType? type,
        [FromQuery, DefaultValue(-1)] int typeIndex)
    {
        var list = _mediaSegmentManager.GetAllMediaSegments(itemId, streamIndex, typeIndex, type);

        return list;
    }

    /// <summary>
    /// Create or update a media segment. You can update start/end/action.
    /// </summary>
    /// <param name="startTicks">Start position of segment in Ticks.</param>
    /// <param name="endTicks">End position of segment in Ticks.</param>
    /// <param name="itemId">Segment is associated with MediaSourceId.</param>
    /// <param name="streamIndex">Segment is associated with MediaStreamIndex.</param>
    /// <param name="type">Segment type.</param>
    /// <param name="typeIndex">Optional: If you want to add a type multiple times to the same itemId increment it.</param>
    /// <param name="action">Optional: Creator recommends an action.</param>
    /// <param name="comment">Optional: A comment.</param>
    /// <response code="200">Segments returned.</response>
    /// <response code="400">Missing query parameter.</response>
    /// <returns>An <see cref="OkResult"/>containing the segment.</returns>
    [HttpPost("Segment")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MediaSegment>> PostSegment(
        [FromQuery, Required] long startTicks,
        [FromQuery, Required] long endTicks,
        [FromQuery, Required] Guid itemId,
        [FromQuery, Required] int streamIndex,
        [FromQuery, Required] MediaSegmentType type,
        [FromQuery, DefaultValue(0)] int typeIndex,
        [FromQuery, DefaultValue(MediaSegmentAction.Auto)] MediaSegmentAction action,
        [FromQuery] string comment)
    {
        var newMediaSegment = new MediaSegment()
        {
            StartTicks = startTicks,
            EndTicks = endTicks,
            ItemId = itemId,
            StreamIndex = streamIndex,
            Type = type,
            TypeIndex = typeIndex,
            Action = action,
            Comment = comment
        };

        var segment = await _mediaSegmentManager.CreateMediaSegmentAsync(newMediaSegment).ConfigureAwait(false);

        return segment;
    }

    /// <summary>
    /// Create or update multiple media segments. See /MediaSegment/Segment for required properties.
    /// </summary>
    /// <param name="segments">All segments that should be added.</param>
    /// <response code="200">Segments returned.</response>
    /// <response code="400">Invalid segments.</response>
    /// <returns>An <see cref="OkResult"/>containing the created/updated segments.</returns>
    [HttpPost]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MediaSegment>>> PostSegments(
        [FromBody, Required] IEnumerable<MediaSegment> segments)
    {
        var nsegments = await _mediaSegmentManager.CreateMediaSegmentsAsync(segments).ConfigureAwait(false);

        return nsegments.ToList();
    }

    /// <summary>
    /// Delete media segments. All query parameters can be freely defined.
    /// </summary>
    /// <param name="itemId">Optional: All segments with MediaSourceId.</param>
    /// <param name="streamIndex">Segment is associated with MediaStreamIndex.</param>
    /// <param name="type">Optional: All segments of type.</param>
    /// <param name="typeIndex">Optional: All segments with typeIndex.</param>
    /// <response code="200">Segments returned.</response>
    /// <response code="404">Segments not found.</response>
    /// <returns>An <see cref="OkResult"/>containing the deleted segments.</returns>
    [HttpDelete]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MediaSegment>>> DeleteSegments(
        [FromQuery, Required] Guid itemId,
        [FromQuery] int? streamIndex,
        [FromQuery] MediaSegmentType? type,
        [FromQuery] int? typeIndex)
    {
        var list = await _mediaSegmentManager.DeleteSegmentsAsync(itemId, streamIndex, typeIndex, type).ConfigureAwait(false);

        return list;
    }
}
