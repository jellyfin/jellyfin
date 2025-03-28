using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.ModelBinders;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The collection controller.
/// </summary>
[Route("Collections")]
[Authorize(Policy = Policies.CollectionManagement)]
public class CollectionController : BaseJellyfinApiController
{
    private readonly ICollectionManager _collectionManager;
    private readonly IDtoService _dtoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionController"/> class.
    /// </summary>
    /// <param name="collectionManager">Instance of <see cref="ICollectionManager"/> interface.</param>
    /// <param name="dtoService">Instance of <see cref="IDtoService"/> interface.</param>
    public CollectionController(
        ICollectionManager collectionManager,
        IDtoService dtoService)
    {
        _collectionManager = collectionManager;
        _dtoService = dtoService;
    }

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <param name="ids">Item Ids to add to the collection.</param>
    /// <param name="parentId">Optional. Create the collection within a specific folder.</param>
    /// <param name="isLocked">Whether or not to lock the new collection.</param>
    /// <response code="200">Collection created.</response>
    /// <returns>A <see cref="CollectionCreationOptions"/> with information about the new collection.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionCreationResult>> CreateCollection(
        [FromQuery] string? name,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] string[] ids,
        [FromQuery] Guid? parentId,
        [FromQuery] bool isLocked = false)
    {
        var userId = User.GetUserId();

        var item = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
        {
            IsLocked = isLocked,
            Name = name,
            ParentId = parentId,
            ItemIdList = ids,
            UserIds = new[] { userId }
        }).ConfigureAwait(false);

        var dtoOptions = new DtoOptions().AddClientFields(User);

        var dto = _dtoService.GetBaseItemDto(item, dtoOptions);

        return new CollectionCreationResult
        {
            Id = dto.Id
        };
    }

    /// <summary>
    /// Adds items to a collection.
    /// </summary>
    /// <param name="collectionId">The collection id.</param>
    /// <param name="ids">Item ids, comma delimited.</param>
    /// <response code="204">Items added to collection.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("{collectionId}/Items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> AddToCollection(
        [FromRoute, Required] Guid collectionId,
        [FromQuery, Required, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] ids)
    {
        await _collectionManager.AddToCollectionAsync(collectionId, ids).ConfigureAwait(true);
        return NoContent();
    }

    /// <summary>
    /// Removes items from a collection.
    /// </summary>
    /// <param name="collectionId">The collection id.</param>
    /// <param name="ids">Item ids, comma delimited.</param>
    /// <response code="204">Items removed from collection.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpDelete("{collectionId}/Items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> RemoveFromCollection(
        [FromRoute, Required] Guid collectionId,
        [FromQuery, Required, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] ids)
    {
        await _collectionManager.RemoveFromCollectionAsync(collectionId, ids).ConfigureAwait(false);
        return NoContent();
    }
}
