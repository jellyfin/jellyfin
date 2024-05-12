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
            Public = createPlaylistRequest?.IsPublic
        }).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Updates a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="updatePlaylistRequest">The <see cref="UpdatePlaylistDto"/> id.</param>
    /// <response code="204">Playlist updated.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>
    /// A <see cref="Task" /> that represents the asynchronous operation to update a playlist.
    /// The task result contains an <see cref="OkResult"/> indicating success.
    /// </returns>
    [HttpPost("{playlistId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdatePlaylist(
        [FromRoute, Required] Guid playlistId,
        [FromBody, Required] UpdatePlaylistDto updatePlaylistRequest)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        await _playlistManager.UpdatePlaylist(new PlaylistUpdateRequest
        {
            UserId = callingUserId,
            Id = playlistId,
            Name = updatePlaylistRequest.Name,
            Ids = updatePlaylistRequest.Ids,
            Users = updatePlaylistRequest.Users,
            Public = updatePlaylistRequest.IsPublic
        }).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Get a playlist's users.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="200">Found shares.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>
    /// A list of <see cref="PlaylistUserPermissions"/> objects.
    /// </returns>
    [HttpGet("{playlistId}/Users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<PlaylistUserPermissions>> GetPlaylistUsers(
        [FromRoute, Required] Guid playlistId)
    {
        var userId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylistForUser(playlistId, userId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(userId);

        return isPermitted ? playlist.Shares.ToList() : Forbid();
    }

    /// <summary>
    /// Get a playlist user.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="userId">The user id.</param>
    /// <response code="200">User permission found.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>
    /// <see cref="PlaylistUserPermissions"/>.
    /// </returns>
    [HttpGet("{playlistId}/Users/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PlaylistUserPermissions?> GetPlaylistUser(
        [FromRoute, Required] Guid playlistId,
        [FromRoute, Required] Guid userId)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        if (playlist.OwnerUserId.Equals(callingUserId))
        {
            return new PlaylistUserPermissions(callingUserId, true);
        }

        var userPermission = playlist.Shares.FirstOrDefault(s => s.UserId.Equals(userId));
        var isPermitted = playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(callingUserId))
            || userId.Equals(callingUserId);

        if (!isPermitted)
        {
            return Forbid();
        }

        if (userPermission is not null)
        {
            return userPermission;
        }

        return NotFound("User permissions not found");
    }

    /// <summary>
    /// Modify a user of a playlist's users.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="updatePlaylistUserRequest">The <see cref="UpdatePlaylistUserDto"/>.</param>
    /// <response code="204">User's permissions modified.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>
    /// A <see cref="Task" /> that represents the asynchronous operation to modify an user's playlist permissions.
    /// The task result contains an <see cref="OkResult"/> indicating success.
    /// </returns>
    [HttpPost("{playlistId}/Users/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdatePlaylistUser(
        [FromRoute, Required] Guid playlistId,
        [FromRoute, Required] Guid userId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow), Required] UpdatePlaylistUserDto updatePlaylistUserRequest)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId);

        if (!isPermitted)
        {
            return Forbid();
        }

        await _playlistManager.AddUserToShares(new PlaylistUserUpdateRequest
        {
            Id = playlistId,
            UserId = userId,
            CanEdit = updatePlaylistUserRequest.CanEdit
        }).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Remove a user from a playlist's users.
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
    [HttpDelete("{playlistId}/Users/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveUserFromPlaylist(
        [FromRoute, Required] Guid playlistId,
        [FromRoute, Required] Guid userId)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        var share = playlist.Shares.FirstOrDefault(s => s.UserId.Equals(userId));
        if (share is null)
        {
            return NotFound("User permissions not found");
        }

        await _playlistManager.RemoveUserFromShares(playlistId, callingUserId, share).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Adds items to a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="ids">Item id, comma delimited.</param>
    /// <param name="userId">The userId.</param>
    /// <response code="204">Items added to playlist.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("{playlistId}/Items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddItemToPlaylist(
        [FromRoute, Required] Guid playlistId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] ids,
        [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var playlist = _playlistManager.GetPlaylistForUser(playlistId, userId.Value);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(userId.Value)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(userId.Value));

        if (!isPermitted)
        {
            return Forbid();
        }

        await _playlistManager.AddItemToPlaylistAsync(playlistId, ids, userId.Value).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Moves a playlist item.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="newIndex">The new index.</param>
    /// <response code="204">Item moved to new index.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("{playlistId}/Items/{itemId}/Move/{newIndex}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MoveItem(
        [FromRoute, Required] string playlistId,
        [FromRoute, Required] string itemId,
        [FromRoute, Required] int newIndex)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylistForUser(Guid.Parse(playlistId), callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        await _playlistManager.MoveItemAsync(playlistId, itemId, newIndex).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Removes items from a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="entryIds">The item ids, comma delimited.</param>
    /// <response code="204">Items removed.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpDelete("{playlistId}/Items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveItemFromPlaylist(
        [FromRoute, Required] string playlistId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] entryIds)
    {
        var callingUserId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylistForUser(Guid.Parse(playlistId), callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.CanEdit && s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        await _playlistManager.RemoveItemFromPlaylistAsync(playlistId, entryIds).ConfigureAwait(false);
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
    /// <response code="404">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>The original playlist items.</returns>
    [HttpGet("{playlistId}/Items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        var playlist = _playlistManager.GetPlaylistForUser(playlistId, userId.Value);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OpenAccess
            || playlist.OwnerUserId.Equals(userId.Value)
            || playlist.Shares.Any(s => s.UserId.Equals(userId.Value));

        if (!isPermitted)
        {
            return Forbid();
        }

        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var item = _libraryManager.GetItemById<Playlist>(playlistId, user);
        if (item is null)
        {
            return NotFound();
        }

        var items = item.GetManageableItems().ToArray();
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
