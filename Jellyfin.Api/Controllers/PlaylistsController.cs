using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions PlaylistExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder)), ParameterObsolete] IReadOnlyList<Guid> ids,
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
    /// Get a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="200">The playlist.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>
    /// A <see cref="Playlist"/> objects.
    /// </returns>
    [HttpGet("{playlistId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PlaylistDto> GetPlaylist(
        [FromRoute, Required] Guid playlistId)
    {
        var userId = User.GetUserId();

        var playlist = _playlistManager.GetPlaylistForUser(playlistId, userId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        return new PlaylistDto()
        {
            Shares = playlist.Shares,
            OpenAccess = playlist.OpenAccess,
            ItemIds = playlist.GetManageableItems().Select(t => t.Item2.Id).ToList()
        };
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
    /// <param name="position">Optional. 0-based index where to place the items or at the end if <c>null</c>.</param>
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
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] ids,
        [FromQuery] int? position,
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

        await _playlistManager.AddItemToPlaylistAsync(playlistId, ids, position, userId.Value).ConfigureAwait(false);
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

        await _playlistManager.MoveItemAsync(playlistId, itemId, newIndex, callingUserId).ConfigureAwait(false);
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
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] string[] entryIds)
    {
        var callingUserId = User.GetUserId();

        if (!callingUserId.IsEmpty())
        {
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
        }
        else
        {
            var isApiKey = User.GetIsApiKey();

            if (!isApiKey)
            {
                return Forbid();
            }
        }

        try
        {
            await _playlistManager.RemoveItemFromPlaylistAsync(playlistId, entryIds).ConfigureAwait(false);
            return NoContent();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
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
    /// <response code="403">Access forbidden.</response>
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
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableImages,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes)
    {
        var callingUserId = userId ?? User.GetUserId();
        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OpenAccess
            || playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        var user = _userManager.GetUserById(callingUserId);

        // Use the raw LinkedChildren count for total — avoids resolving every item just to count.
        var count = playlist.LinkedChildren.Length;

        // Slice at the LinkedChild array level before resolving so we only hit the DB for
        // the items actually needed for this page.
        var effectiveStart = startIndex ?? 0;
        var effectiveLimit = limit ?? int.MaxValue;
        var items = playlist
            .GetManageableItems(effectiveStart, effectiveLimit)
            .Where(i => i.Item2.IsVisible(user))
            .ToArray();

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var dtos = _dtoService.GetBaseItemDtos(items.Select(i => i.Item2).ToList(), dtoOptions, user);
        for (int index = 0; index < dtos.Count; index++)
        {
            dtos[index].PlaylistItemId = items[index].Item1.ItemId?.ToString("N", CultureInfo.InvariantCulture);
        }

        return new QueryResult<BaseItemDto>(startIndex, count, dtos);
    }

    /// <summary>
    /// Lists playlist entries whose underlying library item can no longer be resolved.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="200">Broken items returned.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>A list of PlaylistItemId strings for entries that are broken.</returns>
    [HttpGet("{playlistId}/Items/Broken")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<string>> GetBrokenPlaylistItems(
        [FromRoute, Required] Guid playlistId)
    {
        var callingUserId = User.GetUserId();
        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        var brokenEntryIds = playlist.LinkedChildren
            .Where(c =>
            {
                if (c.ItemId.HasValue && c.ItemId.Value.IsEmpty())
                {
                    return true;
                }

                if (c.ItemId.HasValue)
                {
                    return _libraryManager.GetItemById(c.ItemId.Value) is null;
                }

                return false;
            })
            .Select(c => c.ItemId?.ToString("N", CultureInfo.InvariantCulture) ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        return brokenEntryIds;
    }

    /// <summary>
    /// Removes all broken playlist entries (items whose underlying library item no longer exists).
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="204">Broken items removed.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpDelete("{playlistId}/Items/Broken")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PurgeBrokenPlaylistItems(
        [FromRoute, Required] Guid playlistId)
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

        await _playlistManager.PurgeBrokenItemsAsync(playlistId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Reorders all items in a playlist in a single request, enabling drag-and-drop
    /// reordering from clients without issuing one move call per item.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="reorderRequest">The new desired item order.</param>
    /// <response code="204">Playlist reordered.</response>
    /// <response code="400">Bad request — EntryIds list is empty.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("{playlistId}/Items/Order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ReorderPlaylistItems(
        [FromRoute, Required] Guid playlistId,
        [FromBody, Required] Models.PlaylistDtos.ReorderPlaylistItemsDto reorderRequest)
    {
        if (reorderRequest.EntryIds.Count == 0)
        {
            return BadRequest("EntryIds must not be empty.");
        }

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

        await _playlistManager.ReorderItemsAsync(playlistId, reorderRequest.EntryIds, callingUserId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Exports a playlist as a downloadable file.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="format">Export format: <c>m3u8</c> (default) for an extended M3U8 file, or <c>json</c> for a
    /// portable Jellyfin JSON export that can be re-imported on any server.</param>
    /// <response code="200">File download returned.</response>
    /// <response code="400">Unsupported format.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>The playlist file.</returns>
    [HttpGet("{playlistId}/Export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ExportPlaylist(
        [FromRoute, Required] Guid playlistId,
        [FromQuery] string format = "m3u8")
    {
        var callingUserId = User.GetUserId();
        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OpenAccess
            || playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        // Sanitise the playlist name for use as a filename.
        var safeName = string.Concat(playlist.Name.Split(System.IO.Path.GetInvalidFileNameChars()));

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            var dto = await _playlistManager.ExportAsJsonAsync(playlistId).ConfigureAwait(false);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, PlaylistExportJsonOptions);
            return File(bytes, "application/json", string.Concat(safeName, ".json"));
        }

        if (string.Equals(format, "m3u8", StringComparison.OrdinalIgnoreCase))
        {
            var content = await _playlistManager.ExportAsM3u8Async(playlistId).ConfigureAwait(false);
            return File(Encoding.UTF8.GetBytes(content), "audio/x-mpegurl", string.Concat(safeName, ".m3u8"));
        }

        return BadRequest(
            string.Format(
                CultureInfo.InvariantCulture,
                "Unsupported export format '{0}'. Supported formats: m3u8, json.",
                format));
    }

    /// <summary>
    /// Imports a playlist from an uploaded file, creating a new playlist.
    /// Supports M3U, M3U8, PLS, WPL, ZPL (matched by path) and Jellyfin JSON exports (matched by provider IDs).
    /// Items that cannot be matched to the local library are silently skipped.
    /// </summary>
    /// <param name="file">The playlist file to import.</param>
    /// <param name="userId">The user who will own the created playlist.</param>
    /// <param name="name">Optional name override; defaults to the uploaded filename without extension.</param>
    /// <response code="200">Playlist created.</response>
    /// <response code="400">Empty file or unsupported format.</response>
    /// <response code="404">User not found.</response>
    /// <returns>The new playlist creation result.</returns>
    [HttpPost("Import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaylistCreationResult>> ImportPlaylist(
        [FromForm, Required] IFormFile file,
        [FromQuery] Guid? userId,
        [FromQuery] string? name)
    {
        userId = RequestHelpers.GetUserId(User, userId);

        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        using var stream = file.OpenReadStream();
        try
        {
            var result = await _playlistManager
                .ImportPlaylistAsync(stream, file.FileName, userId.Value, name)
                .ConfigureAwait(false);
            return result;
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Creates a copy of an existing playlist.
    /// </summary>
    /// <param name="playlistId">The playlist to clone.</param>
    /// <param name="newName">Optional name for the copy; defaults to "{original name} (Copy)".</param>
    /// <response code="200">Clone created.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>The creation result for the cloned playlist.</returns>
    [HttpPost("{playlistId}/Clone")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaylistCreationResult>> ClonePlaylist(
        [FromRoute, Required] Guid playlistId,
        [FromQuery] string? newName)
    {
        var callingUserId = User.GetUserId();
        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OpenAccess
            || playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        return await _playlistManager.ClonePlaylistAsync(playlistId, callingUserId, newName).ConfigureAwait(false);
    }

    /// <summary>
    /// Shuffles the stored order of all items in a playlist.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="204">Playlist shuffled.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("{playlistId}/Items/Shuffle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ShufflePlaylistItems(
        [FromRoute, Required] Guid playlistId)
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

        await _playlistManager.ShuffleItemsAsync(playlistId, callingUserId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Lists duplicate playlist entries (same underlying item appearing more than once).
    /// The first occurrence is considered canonical and is not included in the returned list.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="200">Duplicate entry ids returned.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>PlaylistItemId strings for all duplicate entries.</returns>
    [HttpGet("{playlistId}/Items/Duplicates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<string>> GetDuplicatePlaylistItems(
        [FromRoute, Required] Guid playlistId)
    {
        var callingUserId = User.GetUserId();
        var playlist = _playlistManager.GetPlaylistForUser(playlistId, callingUserId);
        if (playlist is null)
        {
            return NotFound("Playlist not found");
        }

        var isPermitted = playlist.OpenAccess
            || playlist.OwnerUserId.Equals(callingUserId)
            || playlist.Shares.Any(s => s.UserId.Equals(callingUserId));

        if (!isPermitted)
        {
            return Forbid();
        }

        return _playlistManager.GetDuplicateEntryIds(playlistId).ToList();
    }

    /// <summary>
    /// Removes all duplicate entries from a playlist, keeping the first occurrence of each item.
    /// </summary>
    /// <param name="playlistId">The playlist id.</param>
    /// <response code="204">Duplicates removed.</response>
    /// <response code="403">Access forbidden.</response>
    /// <response code="404">Playlist not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpDelete("{playlistId}/Items/Duplicates")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveDuplicatePlaylistItems(
        [FromRoute, Required] Guid playlistId)
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

        await _playlistManager.RemoveDuplicatesAsync(playlistId).ConfigureAwait(false);
        return NoContent();
    }
}
