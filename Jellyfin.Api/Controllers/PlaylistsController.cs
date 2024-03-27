using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.PlaylistDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Playlists controller.
/// </summary>
[Authorize]
public class PlaylistsController : BaseJellyfinApiController
{
    private readonly IPlaylistManager _playlistManager;
    private readonly IDtoService _dtoService;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistsController"/> class.
    /// </summary>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="playlistManager">Instance of the <see cref="IPlaylistManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    public PlaylistsController(
        IDtoService dtoService,
        IPlaylistManager playlistManager,
        IUserManager userManager,
        ILibraryManager libraryManager)
    {
        _dtoService = dtoService;
        _playlistManager = playlistManager;
        _userManager = userManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Creates a new playlist.
    /// </summary>
    /// <remarks>
    /// For backwards compatibility parameters can be sent via Query or Body, with Query having higher precedence.
    /// Query parameters are obsolete.
    /// </remarks>
    /// <param name="name">The playlist name.</param>
    /// <param name="ids">The item ids.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="createPlaylistRequest">The create playlist payload.</param>
    /// <response code="200">Playlist created.</response>
    /// <returns>
    /// A <see cref="Task" /> that represents the asynchronous operation to create a playlist.
    /// The task result contains an <see cref="OkResult"/> indicating success.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlaylistCreationResult>> CreatePlaylist(
        [FromQuery, ParameterObsolete] string? name,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder)), ParameterObsolete] IReadOnlyList<Guid> ids,
        [FromQuery, ParameterObsolete] Guid? userId,
        [FromQuery, ParameterObsolete] MediaType? mediaType,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreatePlaylistDto? createPlaylistRequest)
    {
        if (ids.Count == 0)
        {
            ids = createPlaylistRequest?.Ids ?? Array.Empty<Guid>();
        }

        userId ??= createPlaylistRequest?.UserId ?? default;
        userId = RequestHelpers.GetUserId(User, userId);
        var result = await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
        {
            Name = name ?? createPlaylistRequest?.Name,
            ItemIdList = ids,
            UserId = userId.Value,
            MediaType = mediaType ?? createPlaylistRequest?.MediaType,
            Users = createPlaylistRequest?.Users.ToArray() ?? [],
            Public = createPlaylistRequest?.Public
        }).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Get a playlist's users.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="200">Found shares.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>
    /// A list of <see cref="UserPermissions"/> objects.
    /// </returns>
    [HttpGet("{playlistId}/User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<UserPermissions>> GetPlaylistUsers(
        [FromRoute, Required] Guid playlistId)
    {
        var userId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylist(userId, playlistId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(userId)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(userId));

        return isPermitted ? playlist.Shares.ToList() : Unauthorized("Unauthorized Access");
    }

    /// <summary>
    /// Toggles public access of a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="204">Public access toggled.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>
    /// A <see cref="Task" /> that represents the asynchronous operation to toggle public access of a playlist.
    /// The task result contains an <see cref="OkResult"/> indicating success.
    /// </returns>
    [HttpPost("{playlistId}/TogglePublic")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> TogglePublicAccess(
        [FromRoute, Required] Guid playlistId)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylist(callingUserId, playlistId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Unauthorized("Unauthorized access");
        }

        await _playlistManager.ToggleOpenAccess(playlistId, callingUserId).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Modify a user to a playlist's users.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="canEdit">Edit permission.</param>
    /// <response code="204">User's permissions modified.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>
    /// A <see cref="Task" /> that represents the asynchronous operation to modify an user's playlist permissions.
    /// The task result contains an <see cref="OkResult"/> indicating success.
    /// </returns>
    [HttpPost("{playlistId}/User/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ModifyPlaylistUserPermissions(
        [FromRoute, Required] Guid playlistId,
        [FromRoute, Required] Guid userId,
        [FromBody] bool canEdit)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylist(callingUserId, playlistId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Unauthorized("Unauthorized access");
        }

        await _playlistManager.AddToShares(playlistId, callingUserId, new UserPermissions(userId.ToString(), canEdit)).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Remove a user from a playlist's shares.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="userId">The user id.</param>
    /// <response code="204">User permissions removed from playlist.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">No playlist or user permissions found.</response>
    /// <returns>
    /// A <see cref="Task" /> that represents the asynchronous operation to delete a user from a playlist's shares.
    /// The task result contains an <see cref="OkResult"/> indicating success.
    /// </returns>
    [HttpDelete("{playlistId}/User/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveUserFromPlaylist(
        [FromRoute, Required] Guid playlistId,
        [FromRoute, Required] Guid userId)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylist(callingUserId, playlistId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Unauthorized("Unauthorized access");
        }

        var share = playlist.Shares.FirstOrDefault(s => s.UserId.Equals(userId));
        if (share is null)
        {
            return NotFound("User permissions not found");
        }

        await _playlistManager.RemoveFromShares(playlistId, callingUserId, share).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Adds items to a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="ids">Item id, comma delimited.</param>
    /// <param name="userId">The userId.</param>
    /// <response code="204">Items added to playlist.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("{playlistId}/Items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> AddToPlaylist(
        [FromRoute, Required] Guid playlistId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] ids,
        [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        await _playlistManager.AddToPlaylistAsync(playlistId, ids, userId.Value).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Moves a playlist item.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="newIndex">The new index.</param>
    /// <response code="204">Item moved to new index.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("{playlistId}/Items/{itemId}/Move/{newIndex}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> MoveItem(
        [FromRoute, Required] string playlistId,
        [FromRoute, Required] string itemId,
        [FromRoute, Required] int newIndex)
    {
        await _playlistManager.MoveItemAsync(playlistId, itemId, newIndex).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Removes items from a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="entryIds">The item ids, comma delimited.</param>
    /// <response code="204">Items removed.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpDelete("{playlistId}/Items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> RemoveFromPlaylist(
        [FromRoute, Required] string playlistId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] entryIds)
    {
        await _playlistManager.RemoveFromPlaylistAsync(playlistId, entryIds).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Gets the original items of a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="userId">User id.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <response code="200">Original playlist returned.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>The original playlist items.</returns>
    [HttpGet("{playlistId}/Items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueryResult<BaseItemDto>> GetPlaylistItems(
        [FromRoute, Required] Guid playlistId,
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableImages,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var playlist = (Playlist)_libraryManager.GetItemById(playlistId);
        if (playlist is null)
        {
            return NotFound();
        }

        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var items = playlist.GetManageableItems().ToArray();
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
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var dtos = _dtoService.GetBaseItemDtos(items.Select(i => i.Item2).ToList(), dtoOptions, user);
        for (int index = 0; index < dtos.Count; index++)
        {
            dtos[index].PlaylistItemId = items[index].Item1.Id;
        }

        var result = new QueryResult<BaseItemDto>(
            startIndex,
            count,
            dtos);

        return result;
    }
}
