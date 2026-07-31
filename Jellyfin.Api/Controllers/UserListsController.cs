using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.UserListDtos;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// User lists controller.
/// </summary>
[Route("")]
[Authorize]
[Tags("UserList")]
public class UserListsController : BaseJellyfinApiController
{
    private readonly IItemListManager _itemListManager;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserListsController"/> class.
    /// </summary>
    /// <param name="itemListManager">Instance of the <see cref="IItemListManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    public UserListsController(
        IItemListManager itemListManager,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService)
    {
        _itemListManager = itemListManager;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
    }

    /// <summary>
    /// Gets the calling user's lists.
    /// </summary>
    /// <response code="200">User lists returned.</response>
    /// <response code="400">The calling user is invalid.</response>
    /// <returns>The calling user's lists.</returns>
    [HttpGet("UserLists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<UserListDto>>> GetUserLists()
    {
        var lists = await _itemListManager.GetListsAsync(User.GetUserId()).ConfigureAwait(false);
        IReadOnlyList<UserListDto> result = lists.Select(ToDto).ToArray();
        return Ok(result);
    }

    /// <summary>
    /// Creates a custom list for the calling user.
    /// </summary>
    /// <param name="request">The list creation request.</param>
    /// <response code="200">User list created.</response>
    /// <response code="400">The request is invalid or the list limit has been reached.</response>
    /// <returns>The created user list.</returns>
    [HttpPost("UserLists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserListDto>> CreateUserList(
        [FromBody, Required] CreateUserListDto request)
    {
        try
        {
            var list = await _itemListManager.CreateListAsync(
                User.GetUserId(),
                request.Name,
                request.AutoRemoveWatched).ConfigureAwait(false);
            return ToDto(list);
        }
        catch (IItemListManager.ItemListLimitExceededException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (IItemListManager.DefaultItemListDeletionException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (IItemListManager.DuplicateItemListNameException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// Updates a list owned by the calling user.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="request">The list update request.</param>
    /// <response code="204">User list updated.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="404">The list is not owned by the calling user.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPatch("UserLists/{listId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateUserList(
        [FromRoute, Required] Guid listId,
        [FromBody, Required] UpdateUserListDto request)
    {
        if (!await IsListOwnedByUserAsync(User.GetUserId(), listId).ConfigureAwait(false))
        {
            return NotFound();
        }

        var errorResult = await ExecuteManagerOperationAsync(
            () => _itemListManager.UpdateListAsync(
                listId,
                request.Name,
                request.SortIndex,
                request.AutoRemoveWatched)).ConfigureAwait(false);

        return errorResult ?? NoContent();
    }

    /// <summary>
    /// Deletes a non-default list owned by the calling user.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <response code="204">User list deleted.</response>
    /// <response code="400">The default list cannot be deleted.</response>
    /// <response code="404">The list is not owned by the calling user.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpDelete("UserLists/{listId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUserList([FromRoute, Required] Guid listId)
    {
        if (!await IsListOwnedByUserAsync(User.GetUserId(), listId).ConfigureAwait(false))
        {
            return NotFound();
        }

        var errorResult = await ExecuteManagerOperationAsync(
            () => _itemListManager.DeleteListAsync(listId)).ConfigureAwait(false);

        return errorResult ?? NoContent();
    }

    /// <summary>
    /// Gets the items in a list owned by the calling user.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="startIndex">Optional. The record index to start at.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="fields">Optional. Additional item fields to include.</param>
    /// <response code="200">User list items returned.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="404">The list is not owned by the calling user.</response>
    /// <returns>The items in the user list.</returns>
    [HttpGet("UserLists/{listId}/Items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetUserListItems(
        [FromRoute, Required] Guid listId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields)
    {
        var userId = User.GetUserId();
        if (!await IsListOwnedByUserAsync(userId, listId).ConfigureAwait(false))
        {
            return NotFound();
        }

        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var dtoOptions = new DtoOptions
        {
            Fields = fields
        };
        var itemsResult = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
        {
            ItemListId = listId,
            StartIndex = startIndex,
            Limit = limit,
            DtoOptions = dtoOptions
        });
        var itemDtos = _dtoService.GetBaseItemDtos(itemsResult.Items, dtoOptions, user);

        return new QueryResult<BaseItemDto>(
            startIndex,
            itemsResult.TotalRecordCount,
            itemDtos);
    }

    /// <summary>
    /// Adds an item to a list owned by the calling user.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="itemId">The item identifier.</param>
    /// <response code="204">Item added to the user list.</response>
    /// <response code="400">The request is invalid or the item limit has been reached.</response>
    /// <response code="404">The list is not owned by the calling user.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("UserLists/{listId}/Items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddUserListItem(
        [FromRoute, Required] Guid listId,
        [FromRoute, Required] Guid itemId)
    {
        if (!await IsListOwnedByUserAsync(User.GetUserId(), listId).ConfigureAwait(false))
        {
            return NotFound();
        }

        var errorResult = await ExecuteManagerOperationAsync(
            () => _itemListManager.AddItemAsync(listId, itemId)).ConfigureAwait(false);

        return errorResult ?? NoContent();
    }

    /// <summary>
    /// Removes an item from a list owned by the calling user.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="itemId">The item identifier.</param>
    /// <response code="204">Item removed from the user list.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="404">The list is not owned by the calling user.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpDelete("UserLists/{listId}/Items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveUserListItem(
        [FromRoute, Required] Guid listId,
        [FromRoute, Required] Guid itemId)
    {
        if (!await IsListOwnedByUserAsync(User.GetUserId(), listId).ConfigureAwait(false))
        {
            return NotFound();
        }

        var errorResult = await ExecuteManagerOperationAsync(
            () => _itemListManager.RemoveItemAsync(listId, itemId)).ConfigureAwait(false);

        return errorResult ?? NoContent();
    }

    /// <summary>
    /// Moves an item within a list owned by the calling user.
    /// </summary>
    /// <param name="listId">The list identifier.</param>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="newSortIndex">The new zero-based item position.</param>
    /// <response code="204">Item moved within the user list.</response>
    /// <response code="400">The requested position is invalid.</response>
    /// <response code="404">The list is not owned by the calling user.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("UserLists/{listId}/Items/{itemId}/Move")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MoveUserListItem(
        [FromRoute, Required] Guid listId,
        [FromRoute, Required] Guid itemId,
        [FromQuery, Required] int newSortIndex)
    {
        if (!await IsListOwnedByUserAsync(User.GetUserId(), listId).ConfigureAwait(false))
        {
            return NotFound();
        }

        var errorResult = await ExecuteManagerOperationAsync(
            () => _itemListManager.MoveItemAsync(listId, itemId, newSortIndex)).ConfigureAwait(false);

        return errorResult ?? NoContent();
    }

    /// <summary>
    /// Adds an item to the calling user's default watchlist.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <response code="204">Item added to the default watchlist.</response>
    /// <response code="400">The request is invalid or the item limit has been reached.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpPost("UserWatchlistItems/{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddUserWatchlistItem([FromRoute, Required] Guid itemId)
    {
        var userId = User.GetUserId();
        var errorResult = await ExecuteManagerOperationAsync(
            async () =>
            {
                var list = await _itemListManager.GetOrCreateDefaultListAsync(userId).ConfigureAwait(false);
                await _itemListManager.AddItemAsync(list.Id, itemId).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return errorResult ?? NoContent();
    }

    /// <summary>
    /// Removes an item from the calling user's default watchlist.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <response code="204">Item removed from the default watchlist.</response>
    /// <response code="400">The request is invalid.</response>
    /// <returns>An <see cref="NoContentResult"/> on success.</returns>
    [HttpDelete("UserWatchlistItems/{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RemoveUserWatchlistItem([FromRoute, Required] Guid itemId)
    {
        var userId = User.GetUserId();
        var errorResult = await ExecuteManagerOperationAsync(
            async () =>
            {
                var list = await _itemListManager.GetOrCreateDefaultListAsync(userId).ConfigureAwait(false);
                await _itemListManager.RemoveItemAsync(list.Id, itemId).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return errorResult ?? NoContent();
    }

    private static UserListDto ToDto(ItemList list)
    {
        return new UserListDto
        {
            Id = list.Id,
            Name = list.Name,
            Kind = list.ListType,
            IsDefault = list.IsDefault,
            AutoRemoveWatched = list.AutoRemoveWatched,
            SortIndex = list.SortIndex,
            DateCreated = list.DateCreated,
            DateModified = list.DateModified
        };
    }

    private async Task<bool> IsListOwnedByUserAsync(Guid userId, Guid listId)
    {
        var lists = await _itemListManager.GetListsAsync(userId).ConfigureAwait(false);
        return lists.Any(list => list.Id.Equals(listId));
    }

    private static async Task<ActionResult?> ExecuteManagerOperationAsync(Func<Task> operation)
    {
        try
        {
            await operation().ConfigureAwait(false);
            return null;
        }
        catch (IItemListManager.ItemListLimitExceededException exception)
        {
            return new BadRequestObjectResult(exception.Message);
        }
        catch (IItemListManager.DefaultItemListDeletionException exception)
        {
            return new BadRequestObjectResult(exception.Message);
        }
        catch (IItemListManager.DuplicateItemListNameException exception)
        {
            return new BadRequestObjectResult(exception.Message);
        }
    }
}
