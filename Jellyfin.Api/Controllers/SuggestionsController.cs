using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The suggestions controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class SuggestionsController : BaseJellyfinApiController
    {
        private readonly IDtoService _dtoService;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuggestionsController"/> class.
        /// </summary>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public SuggestionsController(
            IDtoService dtoService,
            IUserManager userManager,
            ILibraryManager libraryManager)
        {
            _dtoService = dtoService;
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets suggestions.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="mediaType">The media types.</param>
        /// <param name="type">The type.</param>
        /// <param name="startIndex">Optional. The start index.</param>
        /// <param name="limit">Optional. The limit.</param>
        /// <param name="enableTotalRecordCount">Whether to enable the total record count.</param>
        /// <response code="200">Suggestions returned.</response>
        /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the suggestions.</returns>
        [HttpGet("Users/{userId}/Suggestions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetSuggestions(
            [FromRoute, Required] Guid userId,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] mediaType,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] type,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] bool enableTotalRecordCount = false)
        {
            var user = !userId.Equals(Guid.Empty) ? _userManager.GetUserById(userId) : null;

            var dtoOptions = new DtoOptions().AddClientFields(Request);
            var result = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                OrderBy = new[] { ItemSortBy.Random }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray(),
                MediaTypes = mediaType,
                IncludeItemTypes = type,
                IsVirtualItem = false,
                StartIndex = startIndex,
                Limit = limit,
                DtoOptions = dtoOptions,
                EnableTotalRecordCount = enableTotalRecordCount,
                Recursive = true
            });

            var dtoList = _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user);

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = dtoList
            };
        }
    }
}
