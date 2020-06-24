using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Years controller.
    /// </summary>
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
        /// <param name="maxOfficialRating">Optional. Filter by maximum official rating (PG, PG-13, TV-MA, etc).</param>
        /// <param name="hasThemeSong">Optional. Filter by items with theme songs.</param>
        /// <param name="hasThemeVideo">Optional. Filter by items with theme videos.</param>
        /// <param name="hasSubtitles">Optional. Filter by items with subtitles.</param>
        /// <param name="hasSpecialFeatures">Optional. Filter by items with special features.</param>
        /// <param name="hasTrailer">Optional. Filter by items with trailers.</param>
        /// <param name="adjacentTo">Optional. Return items that are siblings of a supplied item.</param>
        /// <param name="minIndexNumber">Optional. Filter by minimum index number.</param>
        /// <param name="parentIndexNumber">Optional. Filter by parent index number.</param>
        /// <param name="hasParentalRating">Optional. filter by items that have or do not have a parental rating.</param>
        /// <param name="isHd">Optional. Filter by items that are HD or not.</param>
        /// <param name="is4k">Optional. Filter by items that are 4K or not.</param>
        /// <param name="locationTypes">Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimited.</param>
        /// <param name="excludeLocationTypes">Optional. If specified, results will be excluded based on LocationType. This allows multiple, comma delimited.</param>
        /// <param name="isMissing">Optional. Filter by items that are missing episodes or not.</param>
        /// <param name="isUnaired">Optional.  Filter by items that are unaired episodes or not.</param>
        /// <param name="minCommunityRating">Optional. Filter by minimum community rating.</param>
        /// <param name="minCriticRating">Optional. Filter by minimum critic rating.</param>
        /// <param name="airedDuringSeason">Gets all episodes that aired during a season, including specials.</param>
        /// <param name="minPremiereDate">Optional. The minimum premiere date.</param>
        /// <param name="minDateLastSaved">Optional. The minimum last saved date.</param>
        /// <param name="minDateLastSavedForUser">Optional. The minimum last saved date for user.</param>
        /// <param name="maxPremiereDate">Optional. The maximum premiere date.</param>
        /// <param name="hasOverview">Optional. Filter by items that have an overview or not.</param>
        /// <param name="hasImdbId">Optional. Filter by items that have an imdb id or not.</param>
        /// <param name="hasTmdbId">Optional. Filter by items that have a tmdb id or not.</param>
        /// <param name="hasTvdbId">Optional. Filter by items that have a tvdb id or not.</param>
        /// <param name="excludeItemIds">Optional. If specified, results will be filtered by excluding item ids. This allows multiple, comma delimited.</param>
        /// <param name="startIndex">Skips over a given number of items within the results. Use for paging.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="searchTerm">Optional. Search term.</param>
        /// <param name="sortOrder">Sort Order - Ascending,Descending.</param>
        /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
        /// <param name="excludeItemTypes">Optional. If specified, results will be excluded based on item type. This allows multiple, comma delimited.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be included based on item type. This allows multiple, comma delimited.</param>
        /// <param name="filters">Optional. Specify additional filters to apply. This allows multiple, comma delimited. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes.</param>
        /// <param name="isFavorite">Optional. Filter by items that are marked as favorite, or not.</param>
        /// <param name="mediaTypes">Optional. Filter by MediaType. Allows multiple, comma delimited.</param>
        /// <param name="imageTypes">Optional. If specified, results will be filtered based on those containing image types. This allows multiple, comma delimited.</param>
        /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime.</param>
        /// <param name="isPlayed">Optional. Filter by items that are played, or not.</param>
        /// <param name="genres">Optional. If specified, results will be filtered based on genre. This allows multiple, pipe delimited.</param>
        /// <param name="genreIds">Optional. If specified, results will be filtered based on genre id. This allows multiple, pipe delimited.</param>
        /// <param name="officialRatings">Optional. If specified, results will be filtered based on OfficialRating. This allows multiple, pipe delimited.</param>
        /// <param name="tags">Optional. If specified, results will be filtered based on tag. This allows multiple, pipe delimited.</param>
        /// <param name="years">Optional. If specified, results will be filtered based on production year. This allows multiple, comma delimited.</param>
        /// <param name="enableUserData">Optional. Include user data.</param>
        /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
        /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
        /// <param name="person">Optional. If specified, results will be filtered to include only those containing the specified person.</param>
        /// <param name="personIds">Optional. If specified, results will be filtered to include only those containing the specified person ids.</param>
        /// <param name="personTypes">Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited.</param>
        /// <param name="studios">Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimited.</param>
        /// <param name="studioIds">Optional. If specified, results will be filtered based on studio id. This allows multiple, pipe delimited.</param>
        /// <param name="artists">Optional. If specified, results will be filtered based on artist. This allows multiple, pipe delimited.</param>
        /// <param name="excludeArtistIds">Optional. If specified, results will be excluded based on artist id. This allows multiple, pipe delimited.</param>
        /// <param name="artistIds">Optional. If specified, results will be filtered based on artist id. This allows multiple, pipe delimited.</param>
        /// <param name="albumArtistIds">Optional. If specified, results will be filtered based on album artist id. This allows multiple, pipe delimited.</param>
        /// <param name="contributingArtistIds">Optional. If specified, results will be filtered based on contributing artist id. This allows multiple, pipe delimited.</param>
        /// <param name="albums">Optional. If specified, results will be filtered based on album. This allows multiple, pipe delimited.</param>
        /// <param name="albumIds">Optional. If specified, results will be filtered based on album id. This allows multiple, pipe delimited.</param>
        /// <param name="ids">Optional. If specific items are needed, specify a list of item id's to retrieve. This allows multiple, comma delimited.</param>
        /// <param name="videoTypes">Optional. Filter by VideoType (videofile, dvd, bluray, iso). Allows multiple, comma delimited.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="minOfficialRating">Optional. Filter by minimum official rating (PG, PG-13, TV-MA, etc).</param>
        /// <param name="isLocked">Optional. Filter by items that are locked.</param>
        /// <param name="isPlaceholder">Optional. Filter by items that are placeholders.</param>
        /// <param name="hasOfficialRating">Optional. Filter by items that have official ratings.</param>
        /// <param name="collapseBoxSetItems">Whether or not to hide items behind their boxsets.</param>
        /// <param name="minWidth">Min width.</param>
        /// <param name="minHeight">Min height.</param>
        /// <param name="maxWidth">Max width.</param>
        /// <param name="maxHeight">Max height.</param>
        /// <param name="is3d">Optional. Filter by items that are 3D, or not.</param>
        /// <param name="seriesStatus">Optional. Filter by Series Status. Allows multiple, comma delimited.</param>
        /// <param name="nameStartsWithOrGreater">Optional. Filter by items whose name is sorted equally or greater than a given input string.</param>
        /// <param name="nameStartsWith">Optional. Filter by items whose name is sorted equally than a given input string.</param>
        /// <param name="nameLessThan">Optional. Filter by items whose name is equally or lesser than a given input string.</param>
        /// <param name="recursive">Search recursively.</param>
        /// <param name="enableImages">Optional. Include image information in output.</param>
        /// <param name="enableTotalRecordCount">Return total record count.</param>
        /// <response code="200">Year query returned.</response>
        /// <returns> A <see cref="QueryResult{BaseItemDto}"/> containing the year result.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "maxOfficialRating", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasThemeSong", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasThemeVideo", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasSubtitles", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasSpecialFeatures", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasTrailer", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "adjacentTo", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "minIndexNumber", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "parentIndexNumber", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasParentalRating", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isHd", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "is4k", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "locationTypes", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "excludeLocationTypes", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isMissing", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isUnaired", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "minCommunityRating", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "minCriticRating", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "airedDuringSeason", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "minPremiereDate", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "minDateLastSaved", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "minDateLastSavedForUser", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "maxPremiereDate", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasOverview", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasImdbId", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasTmdbId", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasTvdbId", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "excludeItemIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "searchTerm", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "filters", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isFavorite", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "imageTypes", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isPlayed", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "genres", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "genreIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "officialRatings", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "tags", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "years", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "person", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "personIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "personTypes", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "studios", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "studioIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "artists", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "excludeArtistIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "artistIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "albumArtistIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "contributingArtistIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "personIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "personTypes", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "studios", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "studioIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "artists", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "excludeArtistIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "artistIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "albumArtistIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "contributingArtistIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "albums", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "albumIds", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "ids", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "videoTypes", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isLocked", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isPlaceholder", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "hasOfficialRating", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "collapseBoxSetItems", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "minWidth", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "minHeight", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "maxWidth", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "maxHeight", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "is3d", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "seriesStatus", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "nameStartsWithOrGreater", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "nameStartsWith", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "nameLessThan", Justification = "Imported from ServiceStack")]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "enableTotalRecordCount", Justification = "Imported from ServiceStack")]
        public ActionResult<QueryResult<BaseItemDto>> GetYears(
            [FromQuery] string maxOfficialRating,
            [FromQuery] bool? hasThemeSong,
            [FromQuery] bool? hasThemeVideo,
            [FromQuery] bool? hasSubtitles,
            [FromQuery] bool? hasSpecialFeatures,
            [FromQuery] bool? hasTrailer,
            [FromQuery] string adjacentTo,
            [FromQuery] int? minIndexNumber,
            [FromQuery] int? parentIndexNumber,
            [FromQuery] bool? hasParentalRating,
            [FromQuery] bool? isHd,
            [FromQuery] bool? is4k,
            [FromQuery] string locationTypes,
            [FromQuery] string excludeLocationTypes,
            [FromQuery] bool? isMissing,
            [FromQuery] bool? isUnaired,
            [FromQuery] double? minCommunityRating,
            [FromQuery] double? minCriticRating,
            [FromQuery] int? airedDuringSeason,
            [FromQuery] DateTime? minPremiereDate,
            [FromQuery] DateTime? minDateLastSaved,
            [FromQuery] DateTime? minDateLastSavedForUser,
            [FromQuery] DateTime? maxPremiereDate,
            [FromQuery] bool? hasOverview,
            [FromQuery] bool? hasImdbId,
            [FromQuery] bool? hasTmdbId,
            [FromQuery] bool? hasTvdbId,
            [FromQuery] string excludeItemIds,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string searchTerm,
            [FromQuery] string sortOrder,
            [FromQuery] string parentId,
            [FromQuery] string fields,
            [FromQuery] string excludeItemTypes,
            [FromQuery] string includeItemTypes,
            [FromQuery] string filters,
            [FromQuery] bool? isFavorite,
            [FromQuery] string mediaTypes,
            [FromQuery] string imageTypes,
            [FromQuery] string sortBy,
            [FromQuery] bool? isPlayed,
            [FromQuery] string genres,
            [FromQuery] string genreIds,
            [FromQuery] string officialRatings,
            [FromQuery] string tags,
            [FromQuery] string years,
            [FromQuery] bool? enableUserData,
            [FromQuery] int? imageTypeLimit,
            [FromQuery] string enableImageTypes,
            [FromQuery] string person,
            [FromQuery] string personIds,
            [FromQuery] string personTypes,
            [FromQuery] string studios,
            [FromQuery] string studioIds,
            [FromQuery] string artists,
            [FromQuery] string excludeArtistIds,
            [FromQuery] string artistIds,
            [FromQuery] string albumArtistIds,
            [FromQuery] string contributingArtistIds,
            [FromQuery] string albums,
            [FromQuery] string albumIds,
            [FromQuery] string ids,
            [FromQuery] string videoTypes,
            [FromQuery] Guid userId,
            [FromQuery] string minOfficialRating,
            [FromQuery] bool? isLocked,
            [FromQuery] bool? isPlaceholder,
            [FromQuery] bool? hasOfficialRating,
            [FromQuery] bool? collapseBoxSetItems,
            [FromQuery] int? minWidth,
            [FromQuery] int? minHeight,
            [FromQuery] int? maxWidth,
            [FromQuery] int? maxHeight,
            [FromQuery] bool? is3d,
            [FromQuery] string seriesStatus,
            [FromQuery] string nameStartsWithOrGreater,
            [FromQuery] string nameStartsWith,
            [FromQuery] string nameLessThan,
            [FromQuery] bool recursive = true,
            [FromQuery] bool? enableImages = true,
            [FromQuery] bool enableTotalRecordCount = true)
        {
            var dtoOptions = new DtoOptions()
                .AddItemFields(fields)
                .AddClientFields(Request)
                .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

            User? user = null;
            BaseItem parentItem;

            if (!userId.Equals(Guid.Empty))
            {
                user = _userManager.GetUserById(userId);
                parentItem = string.IsNullOrEmpty(parentId) ? _libraryManager.GetUserRootFolder() : _libraryManager.GetItemById(parentId);
            }
            else
            {
                parentItem = string.IsNullOrEmpty(parentId) ? _libraryManager.RootFolder : _libraryManager.GetItemById(parentId);
            }

            IList<BaseItem> items;

            var excludeItemTypesArr = RequestHelpers.Split(excludeItemTypes, ',', true);
            var includeItemTypesArr = RequestHelpers.Split(includeItemTypes, ',', true);
            var mediaTypesArr = RequestHelpers.Split(mediaTypes, ',', true);

            var query = new InternalItemsQuery(user)
            {
                ExcludeItemTypes = excludeItemTypesArr,
                IncludeItemTypes = includeItemTypesArr,
                MediaTypes = mediaTypesArr,
                DtoOptions = dtoOptions
            };

            bool Filter(BaseItem i) => FilterItem(i, excludeItemTypesArr, includeItemTypesArr, mediaTypesArr);

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
        public ActionResult<BaseItemDto> GetYear([FromRoute] int year, [FromQuery] Guid userId)
        {
            var item = _libraryManager.GetYear(year);
            if (item == null)
            {
                return NotFound();
            }

            var dtoOptions = new DtoOptions()
                .AddClientFields(Request);

            if (!userId.Equals(Guid.Empty))
            {
                var user = _userManager.GetUserById(userId);
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
