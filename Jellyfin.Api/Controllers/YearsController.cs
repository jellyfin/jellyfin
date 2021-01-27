using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Years controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class YearsController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="YearsController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        public YearsController(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDtoService dtoService)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Get years.
        /// </summary>
        /// <param name="startIndex">Skips over a given number of items within the results. Use for paging.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="sortOrder">Sort Order - Ascending,Descending.</param>
        /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
        /// <param name="excludeItemTypes">Optional. If specified, results will be excluded based on item type. This allows multiple, comma delimited.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be included based on item type. This allows multiple, comma delimited.</param>
        /// <param name="mediaTypes">Optional. Filter by MediaType. Allows multiple, comma delimited.</param>
        /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime.</param>
        /// <param name="enableUserData">Optional. Include user data.</param>
        /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
        /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="recursive">Search recursively.</param>
        /// <param name="enableImages">Optional. Include image information in output.</param>
        /// <response code="200">Year query returned.</response>
        /// <returns> A <see cref="QueryResult{BaseItemDto}"/> containing the year result.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetYears(
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] SortOrder[] sortOrder,
            [FromQuery] Guid? parentId,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] excludeItemTypes,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] includeItemTypes,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] mediaTypes,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] sortBy,
            [FromQuery] bool? enableUserData,
            [FromQuery] int? imageTypeLimit,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
            [FromQuery] Guid? userId,
            [FromQuery] bool recursive = true,
            [FromQuery] bool? enableImages = true)
        {
            var dtoOptions = new DtoOptions { Fields = fields }
                .AddClientFields(Request)
                .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

            User? user = null;
            BaseItem parentItem = _libraryManager.GetParentItem(parentId, userId);

            if (userId.HasValue && !userId.Equals(Guid.Empty))
            {
                user = _userManager.GetUserById(userId.Value);
            }

            IList<BaseItem> items;

            var query = new InternalItemsQuery(user)
            {
                ExcludeItemTypes = excludeItemTypes,
                IncludeItemTypes = includeItemTypes,
                MediaTypes = mediaTypes,
                DtoOptions = dtoOptions
            };

            bool Filter(BaseItem i) => FilterItem(i, excludeItemTypes, includeItemTypes, mediaTypes);

            if (parentItem.IsFolder)
            {
                var folder = (Folder)parentItem;

                if (!userId.Equals(Guid.Empty))
                {
                    items = recursive ? folder.GetRecursiveChildren(user, query).ToList() : folder.GetChildren(user, true).Where(Filter).ToList();
                }
                else
                {
                    items = recursive ? folder.GetRecursiveChildren(Filter) : folder.Children.Where(Filter).ToList();
                }
            }
            else
            {
                items = new[] { parentItem }.Where(Filter).ToList();
            }

            var extractedItems = GetAllItems(items);

            var filteredItems = _libraryManager.Sort(extractedItems, user, RequestHelpers.GetOrderBy(sortBy, sortOrder));

            var ibnItemsArray = filteredItems.ToList();

            IEnumerable<BaseItem> ibnItems = ibnItemsArray;

            var result = new QueryResult<BaseItemDto> { TotalRecordCount = ibnItemsArray.Count };

            if (startIndex.HasValue || limit.HasValue)
            {
                if (startIndex.HasValue)
                {
                    ibnItems = ibnItems.Skip(startIndex.Value);
                }

                if (limit.HasValue)
                {
                    ibnItems = ibnItems.Take(limit.Value);
                }
            }

            var tuples = ibnItems.Select(i => new Tuple<BaseItem, List<BaseItem>>(i, new List<BaseItem>()));

            var dtos = tuples.Select(i => _dtoService.GetItemByNameDto(i.Item1, dtoOptions, i.Item2, user));

            result.Items = dtos.Where(i => i != null).ToArray();

            return result;
        }

        /// <summary>
        /// Gets a year.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
        /// <response code="200">Year returned.</response>
        /// <response code="404">Year not found.</response>
        /// <returns>
        /// An <see cref="OkResult"/> containing the year,
        /// or a <see cref="NotFoundResult"/> if year not found.
        /// </returns>
        [HttpGet("{year}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<BaseItemDto> GetYear([FromRoute, Required] int year, [FromQuery] Guid? userId)
        {
            var item = _libraryManager.GetYear(year);
            if (item == null)
            {
                return NotFound();
            }

            var dtoOptions = new DtoOptions()
                .AddClientFields(Request);

            if (userId.HasValue && !userId.Equals(Guid.Empty))
            {
                var user = _userManager.GetUserById(userId.Value);
                return _dtoService.GetBaseItemDto(item, dtoOptions, user);
            }

            return _dtoService.GetBaseItemDto(item, dtoOptions);
        }

        private bool FilterItem(BaseItem f, IReadOnlyCollection<string> excludeItemTypes, IReadOnlyCollection<string> includeItemTypes, IReadOnlyCollection<string> mediaTypes)
        {
            // Exclude item types
            if (excludeItemTypes.Count > 0 && excludeItemTypes.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // Include item types
            if (includeItemTypes.Count > 0 && !includeItemTypes.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // Include MediaTypes
            if (mediaTypes.Count > 0 && !mediaTypes.Contains(f.MediaType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private IEnumerable<BaseItem> GetAllItems(IEnumerable<BaseItem> items)
        {
            return items
                .Select(i => i.ProductionYear ?? 0)
                .Where(i => i > 0)
                .Distinct()
                .Select(year => _libraryManager.GetYear(year));
        }
    }
}
