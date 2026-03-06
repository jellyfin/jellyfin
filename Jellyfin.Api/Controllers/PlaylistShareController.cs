using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Playlist share controller for anonymous access via share tokens.
/// </summary>
[Authorize(Policy = Policies.PlaylistShareAccess)]
public class PlaylistShareController : BaseJellyfinApiController
{
    private readonly IPlaylistManager _playlistManager;
    private readonly IDtoService _dtoService;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistShareController"/> class.
    /// </summary>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="playlistManager">Instance of the <see cref="IPlaylistManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    public PlaylistShareController(
        IDtoService dtoService,
        IPlaylistManager playlistManager,
        ILibraryManager libraryManager)
    {
        _dtoService = dtoService;
        _playlistManager = playlistManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Gets a playlist by share token.
    /// </summary>
    /// <param name="shareToken">The share token.</param>
    /// <response code="200">The playlist.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>The playlist information.</returns>
    [HttpGet("{shareToken}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BaseItemDto> GetPlaylistByShareToken(
        [FromRoute, Required] string shareToken)
    {
        var playlist = HttpContext.Items["SharedPlaylist"] as Playlist;
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var dtoOptions = new DtoOptions(false);
        var dto = _dtoService.GetBaseItemDto(playlist, dtoOptions);

        return dto;
    }

    /// <summary>
    /// Gets the items in a playlist by share token.
    /// </summary>
    /// <param name="shareToken">The share token.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <response code="200">Playlist items returned.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>The playlist items.</returns>
    [HttpGet("{shareToken}/Items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueryResult<BaseItemDto>> GetPlaylistItemsByShareToken(
        [FromRoute, Required] string shareToken,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableImages,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes)
    {
        var playlist = HttpContext.Items["SharedPlaylist"] as Playlist;
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var manageableItems = playlist.GetManageableItems().ToArray();
        var items = manageableItems.Select(i => i.Item2).ToArray();
        var count = items.Length;

        if (startIndex.HasValue)
        {
            items = items.Skip(startIndex.Value).ToArray();
        }

        if (limit.HasValue)
        {
            items = items.Take(limit.Value).ToArray();
        }

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var dtos = _dtoService.GetBaseItemDtos(items.ToList(), dtoOptions, null);
        for (int index = 0; index < dtos.Count; index++)
        {
            var originalIndex = (startIndex ?? 0) + index;
            if (originalIndex < manageableItems.Length)
            {
                dtos[index].PlaylistItemId = manageableItems[originalIndex].Item1.ItemId?.ToString("N", CultureInfo.InvariantCulture);
            }
        }

        var result = new QueryResult<BaseItemDto>(
            startIndex,
            count,
            dtos);

        return result;
    }
}
