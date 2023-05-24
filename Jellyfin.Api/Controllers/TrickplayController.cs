using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Mime;
using System.Text;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/>.</param>
    /// <param name="trickplayManager">Instance of <see cref="ITrickplayManager"/>.</param>
    public TrickplayController(
        ILibraryManager libraryManager,
        ITrickplayManager trickplayManager)
    {
        _libraryManager = libraryManager;
        _trickplayManager = trickplayManager;
    }

    /// <summary>
    /// Gets an image tiles playlist for trickplay.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="width">The width of a single tile.</param>
    /// <param name="mediaSourceId">The media version id, if using an alternate version.</param>
    /// <response code="200">Tiles stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the trickplay tiles file.</returns>
    [HttpGet("Videos/{itemId}/Trickplay/{width}/tiles.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesPlaylistFile]
    public ActionResult GetTrickplayHlsPlaylist(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int width,
        [FromQuery] Guid? mediaSourceId)
    {
        return GetTrickplayPlaylistInternal(width, mediaSourceId ?? itemId);
    }

    /// <summary>
    /// Gets a trickplay tile grid image.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="width">The width of a single tile.</param>
    /// <param name="index">The index of the desired tile grid.</param>
    /// <param name="mediaSourceId">The media version id, if using an alternate version.</param>
    /// <response code="200">Tiles image returned.</response>
    /// <response code="200">Tiles image not found at specified index.</response>
    /// <returns>A <see cref="FileResult"/> containing the trickplay tiles image.</returns>
    [HttpGet("Videos/{itemId}/Trickplay/{width}/{index}.jpg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    public ActionResult GetTrickplayHlsPlaylist(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int width,
        [FromRoute, Required] int index,
        [FromQuery] Guid? mediaSourceId)
    {
        var item = _libraryManager.GetItemById(mediaSourceId ?? itemId);
        if (item is null)
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

    private ActionResult GetTrickplayPlaylistInternal(int width, Guid mediaSourceId)
    {
        var tilesResolutions = _trickplayManager.GetTilesResolutions(mediaSourceId);
        if (tilesResolutions is not null && tilesResolutions.TryGetValue(width, out var tilesInfo))
        {
            var builder = new StringBuilder(128);

            if (tilesInfo.TileCount > 0)
            {
                const string urlFormat = "Trickplay/{0}/{1}.jpg?MediaSourceId={2}&api_key={3}";
                const string decimalFormat = "{0:0.###}";

                var resolution = $"{tilesInfo.Width}x{tilesInfo.Height}";
                var layout = $"{tilesInfo.TileWidth}x{tilesInfo.TileHeight}";
                var tilesPerGrid = tilesInfo.TileWidth * tilesInfo.TileHeight;
                var tileDuration = tilesInfo.Interval / 1000d;
                var infDuration = tileDuration * tilesPerGrid;
                var tileGridCount = (int)Math.Ceiling((decimal)tilesInfo.TileCount / tilesPerGrid);

                builder
                    .AppendLine("#EXTM3U")
                    .Append("#EXT-X-TARGETDURATION:")
                    .AppendLine(tileGridCount.ToString(CultureInfo.InvariantCulture))
                    .AppendLine("#EXT-X-VERSION:7")
                    .AppendLine("#EXT-X-MEDIA-SEQUENCE:1")
                    .AppendLine("#EXT-X-PLAYLIST-TYPE:VOD")
                    .AppendLine("#EXT-X-IMAGES-ONLY");

                for (int i = 0; i < tileGridCount; i++)
                {
                    // All tile grids before the last one must contain full amount of tiles.
                    // The final grid will be 0 < count <= maxTiles
                    if (i == tileGridCount - 1)
                    {
                        tilesPerGrid = tilesInfo.TileCount - (i * tilesPerGrid);
                        infDuration = tileDuration * tilesPerGrid;
                    }

                    // EXTINF
                    builder
                        .Append("#EXTINF:")
                        .AppendFormat(CultureInfo.InvariantCulture, decimalFormat, infDuration)
                        .AppendLine(",");

                    // EXT-X-TILES
                    builder
                        .Append("#EXT-X-TILES:RESOLUTION=")
                        .Append(resolution)
                        .Append(",LAYOUT=")
                        .Append(layout)
                        .Append(",DURATION=")
                        .AppendFormat(CultureInfo.InvariantCulture, decimalFormat, tileDuration)
                        .AppendLine();

                    // URL
                    builder
                        .AppendFormat(
                            CultureInfo.InvariantCulture,
                            urlFormat,
                            width.ToString(CultureInfo.InvariantCulture),
                            i.ToString(CultureInfo.InvariantCulture),
                            mediaSourceId.ToString("N"),
                            User.GetToken())
                        .AppendLine();
                }

                builder.AppendLine("#EXT-X-ENDLIST");
                return new FileContentResult(Encoding.UTF8.GetBytes(builder.ToString()), MimeTypes.GetMimeType("playlist.m3u8"));
            }
        }

        return NotFound();
    }
}
