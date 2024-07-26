using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Media Segments api.
/// </summary>
[Route("")]
[Authorize]
public class MediaSegmentApiController : BaseJellyfinApiController
{
    private readonly IMediaSegmentManager _mediaSegmentManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentApiController"/> class.
    /// </summary>
    /// <param name="mediaSegmentManager">MediaSegments Manager.</param>
    /// <param name="libraryManager">The Library manager.</param>
    public MediaSegmentApiController(IMediaSegmentManager mediaSegmentManager, ILibraryManager libraryManager)
    {
        _mediaSegmentManager = mediaSegmentManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Gets all media segments based on an itemId.
    /// </summary>
    /// <param name="itemId">The ItemId.</param>
    /// <returns>A list of media segement objects related to the requested itemId</returns>
    [HttpGet("MediaSegments/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IAsyncEnumerable<MediaSegment>> GetSegmentsAsync([FromRoute, Required] Guid itemId)
    {
        var item = _libraryManager.GetItemById<BaseItem>(itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        var items = _mediaSegmentManager.GetSegmentsAsync(item.Id);
        return Ok(items);
    }
}
