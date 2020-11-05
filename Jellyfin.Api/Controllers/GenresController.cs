using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Genre = MediaBrowser.Controller.Entities.Genre;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The genres controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class GenresController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenresController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        public GenresController(
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets all genres from a given item, folder, or the entire library.
        /// </summary>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
        /// <param name="excludeItemTypes">Optional. If specified, results will be filtered out based on item type. This allows multiple, comma delimited.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be filtered in based on item type. This allows multiple, comma delimited.</param>
        /// <param name="isFavorite">Optional filter by items that are marked as favorite, or not.</param>
        /// <param name="imageTypeLimit">Optional, the max number of images to return, per image type.</param>
        /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
        /// <param name="userId">User id.</param>
        /// <param name="nameStartsWithOrGreater">Optional filter by items whose name is sorted equally or greater than a given input string.</param>
        /// <param name="nameStartsWith">Optional filter by items whose name is sorted equally than a given input string.</param>
        /// <param name="nameLessThan">Optional filter by items whose name is equally or lesser than a given input string.</param>
        /// <param name="enableImages">Optional, include image information in output.</param>
        /// <param name="enableTotalRecordCount">Optional. Include total record count.</param>
        /// <response code="200">Genres returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the queryresult of genres.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetGenres(
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string? searchTerm,
            [FromQuery] string? parentId,
            [FromQuery] string? fields,
            [FromQuery] string? excludeItemTypes,
            [FromQuery] string? includeItemTypes,
            [FromQuery] bool? isFavorite,
            [FromQuery] int? imageTypeLimit,
            [FromQuery] ImageType[] enableImageTypes,
            [FromQuery] Guid? userId,
            [FromQuery] string? nameStartsWithOrGreater,
            [FromQuery] string? nameStartsWith,
            [FromQuery] string? nameLessThan,
            [FromQuery] bool? enableImages = true,
            [FromQuery] bool enableTotalRecordCount = true)
        {
            var dtoOptions = new DtoOptions()
                .AddItemFields(fields)
                .AddClientFields(Request)
                .AddAdditionalDtoOptions(enableImages, false, imageTypeLimit, enableImageTypes);

            User? user = userId.HasValue && userId != Guid.Empty ? _userManager.GetUserById(userId.Value) : null;

            var parentItem = _libraryManager.GetParentItem(parentId, userId);

            var query = new InternalItemsQuery(user)
            {
                ExcludeItemTypes = RequestHelpers.Split(excludeItemTypes, ',', true),
                IncludeItemTypes = RequestHelpers.Split(includeItemTypes, ',', true),
                StartIndex = startIndex,
                Limit = limit,
                IsFavorite = isFavorite,
                NameLessThan = nameLessThan,
                NameStartsWith = nameStartsWith,
                NameStartsWithOrGreater = nameStartsWithOrGreater,
                DtoOptions = dtoOptions,
                SearchTerm = searchTerm,
                EnableTotalRecordCount = enableTotalRecordCount
            };

            if (!string.IsNullOrWhiteSpace(parentId))
            {
                if (parentItem is Folder)
                {
                    query.AncestorIds = new[] { new Guid(parentId) };
                }
                else
                {
                    query.ItemIds = new[] { new Guid(parentId) };
                }
            }

            QueryResult<(BaseItem, ItemCounts)> result;
            if (parentItem is ICollectionFolder parentCollectionFolder
                && (string.Equals(parentCollectionFolder.CollectionType, CollectionType.Music)
                || string.Equals(parentCollectionFolder.CollectionType, CollectionType.MusicVideos)))
            {
                result = _libraryManager.GetMusicGenres(query);
            }
            else
            {
                result = _libraryManager.GetGenres(query);
            }

            var shouldIncludeItemTypes = !string.IsNullOrEmpty(includeItemTypes);
            return RequestHelpers.CreateQueryResult(result, dtoOptions, _dtoService, shouldIncludeItemTypes, user);
        }

        /// <summary>
        /// Gets a genre, by name.
        /// </summary>
        /// <param name="genreName">The genre name.</param>
        /// <param name="userId">The user id.</param>
        /// <response code="200">Genres returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the genre.</returns>
        [HttpGet("{genreName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<BaseItemDto> GetGenre([FromRoute, Required] string genreName, [FromQuery] Guid? userId)
        {
            var dtoOptions = new DtoOptions()
                .AddClientFields(Request);

            Genre item = new Genre();
            if (genreName.IndexOf(BaseItem.SlugChar, StringComparison.OrdinalIgnoreCase) != -1)
            {
                var result = GetItemFromSlugName<Genre>(_libraryManager, genreName, dtoOptions);

                if (result != null)
                {
                    item = result;
                }
            }
            else
            {
                item = _libraryManager.GetGenre(genreName);
            }

            if (userId.HasValue && !userId.Equals(Guid.Empty))
            {
                var user = _userManager.GetUserById(userId.Value);

                return _dtoService.GetBaseItemDto(item, dtoOptions, user);
            }

            return _dtoService.GetBaseItemDto(item, dtoOptions);
        }

        private T GetItemFromSlugName<T>(ILibraryManager libraryManager, string name, DtoOptions dtoOptions)
            where T : BaseItem, new()
        {
            var result = libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '&'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions
            }).OfType<T>().FirstOrDefault();

            result ??= libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '/'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions
            }).OfType<T>().FirstOrDefault();

            result ??= libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '?'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions
            }).OfType<T>().FirstOrDefault();

            return result;
        }
    }
}
