#pragma warning disable CS0618
#pragma warning disable CA1708

using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.Entities;
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
[Tags("TrickPlay")]
public class TrickPlayController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly ITrickplayManager _trickPlayManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickPlayController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/>.</param>
    /// <param name="trickPlayManager">Instance of <see cref="ITrickplayManager"/>.</param>
    public TrickPlayController(
        ILibraryManager libraryManager,
        ITrickplayManager trickPlayManager)
    {
        _libraryManager = libraryManager;
        _trickPlayManager = trickPlayManager;
    }

    /// <summary>
    /// Gets an image tiles playlist for trick play.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="width">The width of a single tile.</param>
    /// <param name="mediaSourceId">The media version ID when using an alternate version.</param>
    /// <response code="200">Tiles playlist returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the trick play playlist file.</returns>
    [HttpGet("Videos/{itemId}/TrickPlay/{width}/tiles.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesPlaylistFile]
    public Task<ActionResult> GetTrickPlayHlsPlaylist(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int width,
        [FromQuery] Guid? mediaSourceId)
        => GetTrickplayHlsPlaylist(itemId, width, mediaSourceId);

    /// <summary>
    /// Gets a trick play tile image.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="width">The width of a single tile.</param>
    /// <param name="index">The index of the desired tile.</param>
    /// <param name="mediaSourceId">The media version ID when using an alternate version.</param>
    /// <response code="200">Tile image returned.</response>
    /// <response code="200">Tile image not found at specified index.</response>
    /// <returns>A <see cref="FileResult"/> containing the trick play tiles image.</returns>
    [HttpGet("Videos/{itemId}/TrickPlay/{width}/{index}.jpg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public Task<ActionResult> GetTrickPlayTileImage(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int width,
        [FromRoute, Required] int index,
        [FromQuery] Guid? mediaSourceId)
        => GetTrickplayTileImage(itemId, width, index, mediaSourceId);

    /// <summary>
    /// Gets an image tiles playlist for trickplay.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="width">The width of a single tile.</param>
    /// <param name="mediaSourceId">The media version id, if using an alternate version.</param>
    /// <response code="200">Tiles playlist returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the trickplay playlist file.</returns>
    [HttpGet("Videos/{itemId}/Trickplay/{width}/tiles.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesPlaylistFile]
    [Obsolete("Use GetTrickPlayHlsPlaylist")]
    public async Task<ActionResult> GetTrickplayHlsPlaylist(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int width,
        [FromQuery] Guid? mediaSourceId)
    {
        string? playlist = await _trickPlayManager.GetHlsPlaylist(mediaSourceId ?? itemId, width, User.GetToken()).ConfigureAwait(false);

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
    /// <returns>A <see cref="FileResult"/> containing the trickplay tiles image.</returns>
    [HttpGet("Videos/{itemId}/Trickplay/{width}/{index}.jpg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    [Obsolete("Use GetTrickPlayTileImage")]
    public async Task<ActionResult> GetTrickplayTileImage(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int width,
        [FromRoute, Required] int index,
        [FromQuery] Guid? mediaSourceId)
    {
        var item = _libraryManager.GetItemById<BaseItem>(mediaSourceId ?? itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        var saveWithMedia = _libraryManager.GetLibraryOptions(item).SaveTrickplayWithMedia;
        var path = await _trickPlayManager.GetTrickplayTilePathAsync(item, width, index, saveWithMedia).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
        {
            Response.Headers.ContentDisposition = "attachment";
            return PhysicalFile(path, MediaTypeNames.Image.Jpeg);
        }

        return NotFound();
    }
}
