using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Trickplay controller.
/// </summary>
[Route("")]
[Authorize]
public class TrickplayController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly ITrickplayManager _trickplayManager;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/>.</param>
    /// <param name="trickplayManager">Instance of <see cref="ITrickplayManager"/>.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public TrickplayController(
        ILibraryManager libraryManager,
        ITrickplayManager trickplayManager,
        IUserManager userManager)
    {
        _libraryManager = libraryManager;
        _trickplayManager = trickplayManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets an image tiles playlist for trickplay.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="width">The width of a single tile.</param>
    /// <param name="mediaSourceId">The media version id, if using an alternate version.</param>
    /// <response code="200">Tiles playlist returned.</response>
    /// <response code="400">Requested item not found.</response>
    /// <response code="401">Requested item not authorized to be edited by user.</response>
    /// <returns>A <see cref="FileResult"/> containing the trickplay playlist file.</returns>
    [HttpGet("Videos/{itemId}/Trickplay/{width}/tiles.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesPlaylistFile]
    public async Task<ActionResult> GetTrickplayHlsPlaylist(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int width,
        [FromQuery] Guid? mediaSourceId)
    {
        var isApiKey = User.GetIsApiKey();
        var userId = User.GetUserId();
        var user = !isApiKey && !userId.IsEmpty()
            ? _userManager.GetUserById(userId) ?? throw new ResourceNotFoundException()
            : null;
        if (!isApiKey && user is null)
        {
            return Unauthorized("Unauthorized access");
        }

        string? playlist = await _trickplayManager.GetHlsPlaylist(mediaSourceId ?? itemId, width, user, User.GetToken()).ConfigureAwait(false);

        if (string.IsNullOrEmpty(playlist))
        {
            return NotFound();
        }

        return Content(playlist, MimeTypes.GetMimeType("playlist.m3u8"), Encoding.UTF8);
    }

    /// <summary>
    /// Gets a trickplay tile image.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="width">The width of a single tile.</param>
    /// <param name="index">The index of the desired tile.</param>
    /// <param name="mediaSourceId">The media version id, if using an alternate version.</param>
    /// <response code="200">Tile image returned.</response>
    /// <response code="200">Tile image not found at specified index.</response>
    /// <response code="401">Requested item not authorized to be visible to the user.</response>
    /// <returns>A <see cref="FileResult"/> containing the trickplay tiles image.</returns>
    [HttpGet("Videos/{itemId}/Trickplay/{width}/{index}.jpg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesImageFile]
    public ActionResult GetTrickplayTileImage(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int width,
        [FromRoute, Required] int index,
        [FromQuery] Guid? mediaSourceId)
    {
        var isApiKey = User.GetIsApiKey();
        var userId = User.GetUserId();
        var user = !isApiKey && !userId.IsEmpty()
            ? _userManager.GetUserById(userId) ?? throw new ResourceNotFoundException()
            : null;
        if (!isApiKey && user is null)
        {
            return Unauthorized("Unauthorized access");
        }

        var item = _libraryManager.GetItemById(mediaSourceId ?? itemId);
        if (item is null)
        {
            return NotFound();
        }

        if (!isApiKey && !item.IsVisible(user))
        {
            return NotFound();
        }

        var path = _trickplayManager.GetTrickplayTilePath(item, width, index);
        if (System.IO.File.Exists(path))
        {
            return PhysicalFile(path, MediaTypeNames.Image.Jpeg);
        }

        return NotFound();
    }
}
