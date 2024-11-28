using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaSegments;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Media Segments api.
/// </summary>
[Authorize]
public class MediaSegmentsController : BaseJellyfinApiController
{
    private readonly IMediaSegmentManager _mediaSegmentManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentsController"/> class.
    /// </summary>
    /// <param name="mediaSegmentManager">MediaSegments Manager.</param>
    /// <param name="libraryManager">The Library manager.</param>
    public MediaSegmentsController(IMediaSegmentManager mediaSegmentManager, ILibraryManager libraryManager)
    {
        _mediaSegmentManager = mediaSegmentManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Gets all media segments based on an itemId.
    /// </summary>
    /// <param name="itemId">The ItemId.</param>
    /// <param name="includeSegmentTypes">Optional filter of requested segment types.</param>
    /// <returns>A list of media segment objects related to the requested itemId.</returns>
    [HttpGet("{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QueryResult<MediaSegmentDto>>> GetItemSegments(
        [FromRoute, Required] Guid itemId,
        [FromQuery] IEnumerable<MediaSegmentType>? includeSegmentTypes = null)
    {
        var item = _libraryManager.GetItemById<BaseItem>(itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        var items = await _mediaSegmentManager.GetSegmentsAsync(item, includeSegmentTypes).ConfigureAwait(false);
        return Ok(new QueryResult<MediaSegmentDto>(items.ToArray()));
    }
}
