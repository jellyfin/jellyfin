using System;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The collection controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    [Route("/Collections")]
    public class CollectionController : BaseJellyfinApiController
    {
        private readonly ICollectionManager _collectionManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionController"/> class.
        /// </summary>
        /// <param name="collectionManager">Instance of <see cref="ICollectionManager"/> interface.</param>
        /// <param name="dtoService">Instance of <see cref="IDtoService"/> interface.</param>
        /// <param name="authContext">Instance of <see cref="IAuthorizationContext"/> interface.</param>
        public CollectionController(
            ICollectionManager collectionManager,
            IDtoService dtoService,
            IAuthorizationContext authContext)
        {
            _collectionManager = collectionManager;
            _dtoService = dtoService;
            _authContext = authContext;
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
        public ActionResult<CollectionCreationResult> CreateCollection(
            [FromQuery] string? name,
            [FromQuery] string? ids,
            [FromQuery] Guid? parentId,
            [FromQuery] bool isLocked = false)
        {
            var userId = _authContext.GetAuthorizationInfo(Request).UserId;

            var item = _collectionManager.CreateCollection(new CollectionCreationOptions
            {
                IsLocked = isLocked,
                Name = name,
                ParentId = parentId,
                ItemIdList = RequestHelpers.Split(ids, ',', true),
                UserIds = new[] { userId }
            });

            var dtoOptions = new DtoOptions().AddClientFields(Request);

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
        /// <param name="itemIds">Item ids, comma delimited.</param>
        /// <response code="204">Items added to collection.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("{collectionId}/Items")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult AddToCollection([FromRoute] Guid collectionId, [FromQuery] string? itemIds)
        {
            _collectionManager.AddToCollection(collectionId, RequestHelpers.Split(itemIds, ',', true));
            return NoContent();
        }

        /// <summary>
        /// Removes items from a collection.
        /// </summary>
        /// <param name="collectionId">The collection id.</param>
        /// <param name="itemIds">Item ids, comma delimited.</param>
        /// <response code="204">Items removed from collection.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpDelete("{collectionId}/Items")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult RemoveFromCollection([FromRoute] Guid collectionId, [FromQuery] string? itemIds)
        {
            _collectionManager.RemoveFromCollection(collectionId, RequestHelpers.Split(itemIds, ',', true));
            return NoContent();
        }
    }
}
