using System;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The tv shows controller.
    /// </summary>
    [Route("/Shows")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class TvShowsController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly IAuthorizationContext _authContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvShowsController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        /// <param name="tvSeriesManager">Instance of the <see cref="ITVSeriesManager"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        public TvShowsController(
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            ITVSeriesManager tvSeriesManager,
            IAuthorizationContext authContext)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _tvSeriesManager = tvSeriesManager;
            _authContext = authContext;
        }

        /// <summary>
        /// Gets a list of next up episodes.
        /// </summary>
        /// <param name="userId">The user id of the user to get the next up episodes for.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls.</param>
        /// <param name="seriesId">Optional. Filter by series id.</param>
        /// <param name="parentId">Optional. Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="enableImges">Optional. Include image information in output.</param>
        /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
        /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
        /// <param name="enableUserData">Optional. Include user data.</param>
        /// <param name="enableTotalRecordCount">Whether to enable the total records count. Defaults to true.</param>
        /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the next up episodes.</returns>
        [HttpGet("NextUp")]
        public ActionResult<QueryResult<BaseItemDto>> GetNextUp(
            [FromQuery] Guid userId,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string? fields,
            [FromQuery] string? seriesId,
            [FromQuery] string? parentId,
            [FromQuery] bool? enableImges,
            [FromQuery] int? imageTypeLimit,
            [FromQuery] string enableImageTypes,
            [FromQuery] bool? enableUserData,
            [FromQuery] bool enableTotalRecordCount = true)
        {
            var options = new DtoOptions()
                .AddItemFields(fields)
                .AddClientFields(Request)
                .AddAdditionalDtoOptions(enableImges, enableUserData, imageTypeLimit, enableImageTypes);

            var result = _tvSeriesManager.GetNextUp(
                new NextUpQuery
                {
                    Limit = limit,
                    ParentId = parentId,
                    SeriesId = seriesId,
                    StartIndex = startIndex,
                    UserId = userId,
                    EnableTotalRecordCount = enableTotalRecordCount
                },
                options);

            var user = _userManager.GetUserById(userId);

            var returnItems = _dtoService.GetBaseItemDtos(result.Items, options, user);

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = returnItems
            };
        }

        /// <summary>
        /// Gets a list of upcoming episodes.
        /// </summary>
        /// <param name="userId">The user id of the user to get the upcoming episodes for.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls.</param>
        /// <param name="seriesId">Optional. Filter by series id.</param>
        /// <param name="parentId">Optional. Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="enableImges">Optional. Include image information in output.</param>
        /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
        /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
        /// <param name="enableUserData">Optional. Include user data.</param>
        /// <param name="enableTotalRecordCount">Whether to enable the total records count. Defaults to true.</param>
        /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the next up episodes.</returns>
        [HttpGet("Upcoming")]
        public ActionResult<QueryResult<BaseItemDto>> GetUpcomingEpisodes(
            [FromQuery] Guid userId,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string? fields,
            [FromQuery] string? seriesId,
            [FromQuery] string? parentId,
            [FromQuery] bool? enableImges,
            [FromQuery] int? imageTypeLimit,
            [FromQuery] string enableImageTypes,
            [FromQuery] bool? enableUserData,
            [FromQuery] bool enableTotalRecordCount = true)
        {
            var user = _userManager.GetUserById(userId);

            var minPremiereDate = DateTime.Now.Date.ToUniversalTime().AddDays(-1);

            var parentIdGuid = string.IsNullOrWhiteSpace(parentId) ? Guid.Empty : new Guid(parentId);

            var options = new DtoOptions()
                .AddItemFields(fields)
                .AddClientFields(Request)
                .AddAdditionalDtoOptions(enableImges, enableUserData, imageTypeLimit, enableImageTypes);

            var itemsResult = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { nameof(Episode) },
                OrderBy = new[] { ItemSortBy.PremiereDate, ItemSortBy.SortName }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Ascending)).ToArray(),
                MinPremiereDate = minPremiereDate,
                StartIndex = startIndex,
                Limit = limit,
                ParentId = parentIdGuid,
                Recursive = true,
                DtoOptions = options
            });

            var returnItems = _dtoService.GetBaseItemDtos(itemsResult, options, user);

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = itemsResult.Count,
                Items = returnItems
            };
        }
    }
}
