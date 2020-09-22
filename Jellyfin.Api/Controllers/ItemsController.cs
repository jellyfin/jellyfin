using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The items controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class ItemsController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;
        private readonly IDtoService _dtoService;
        private readonly ILogger<ItemsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        public ItemsController(
            IUserManager userManager,
            ILibraryManager libraryManager,
            ILocalizationManager localization,
            IDtoService dtoService,
            ILogger<ItemsController> logger)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _localization = localization;
            _dtoService = dtoService;
            _logger = logger;
        }

        /// <summary>
        /// Gets items based on a query.
        /// </summary>
        /// <param name="uId">The user id supplied in the /Users/{uid}/Items.</param>
        /// <param name="userId">The user id supplied as query parameter.</param>
        /// <param name="maxOfficialRating">Optional filter by maximum official rating (PG, PG-13, TV-MA, etc).</param>
        /// <param name="hasThemeSong">Optional filter by items with theme songs.</param>
        /// <param name="hasThemeVideo">Optional filter by items with theme videos.</param>
        /// <param name="hasSubtitles">Optional filter by items with subtitles.</param>
        /// <param name="hasSpecialFeature">Optional filter by items with special features.</param>
        /// <param name="hasTrailer">Optional filter by items with trailers.</param>
        /// <param name="adjacentTo">Optional. Return items that are siblings of a supplied item.</param>
        /// <param name="parentIndexNumber">Optional filter by parent index number.</param>
        /// <param name="hasParentalRating">Optional filter by items that have or do not have a parental rating.</param>
        /// <param name="isHd">Optional filter by items that are HD or not.</param>
        /// <param name="is4K">Optional filter by items that are 4K or not.</param>
        /// <param name="locationTypes">Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimeted.</param>
        /// <param name="excludeLocationTypes">Optional. If specified, results will be filtered based on the LocationType. This allows multiple, comma delimeted.</param>
        /// <param name="isMissing">Optional filter by items that are missing episodes or not.</param>
        /// <param name="isUnaired">Optional filter by items that are unaired episodes or not.</param>
        /// <param name="minCommunityRating">Optional filter by minimum community rating.</param>
        /// <param name="minCriticRating">Optional filter by minimum critic rating.</param>
        /// <param name="minPremiereDate">Optional. The minimum premiere date. Format = ISO.</param>
        /// <param name="minDateLastSaved">Optional. The minimum last saved date. Format = ISO.</param>
        /// <param name="minDateLastSavedForUser">Optional. The minimum last saved date for the current user. Format = ISO.</param>
        /// <param name="maxPremiereDate">Optional. The maximum premiere date. Format = ISO.</param>
        /// <param name="hasOverview">Optional filter by items that have an overview or not.</param>
        /// <param name="hasImdbId">Optional filter by items that have an imdb id or not.</param>
        /// <param name="hasTmdbId">Optional filter by items that have a tmdb id or not.</param>
        /// <param name="hasTvdbId">Optional filter by items that have a tvdb id or not.</param>
        /// <param name="excludeItemIds">Optional. If specified, results will be filtered by exxcluding item ids. This allows multiple, comma delimeted.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="recursive">When searching within folders, this determines whether or not the search will be recursive. true/false.</param>
        /// <param name="searchTerm">Optional. Filter based on a search term.</param>
        /// <param name="sortOrder">Sort Order - Ascending,Descending.</param>
        /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
        /// <param name="excludeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on the item type. This allows multiple, comma delimeted.</param>
        /// <param name="filters">Optional. Specify additional filters to apply. This allows multiple, comma delimeted. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes.</param>
        /// <param name="isFavorite">Optional filter by items that are marked as favorite, or not.</param>
        /// <param name="mediaTypes">Optional filter by MediaType. Allows multiple, comma delimited.</param>
        /// <param name="imageTypes">Optional. If specified, results will be filtered based on those containing image types. This allows multiple, comma delimited.</param>
        /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimeted. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime.</param>
        /// <param name="isPlayed">Optional filter by items that are played, or not.</param>
        /// <param name="genres">Optional. If specified, results will be filtered based on genre. This allows multiple, pipe delimeted.</param>
        /// <param name="officialRatings">Optional. If specified, results will be filtered based on OfficialRating. This allows multiple, pipe delimeted.</param>
        /// <param name="tags">Optional. If specified, results will be filtered based on tag. This allows multiple, pipe delimeted.</param>
        /// <param name="years">Optional. If specified, results will be filtered based on production year. This allows multiple, comma delimeted.</param>
        /// <param name="enableUserData">Optional, include user data.</param>
        /// <param name="imageTypeLimit">Optional, the max number of images to return, per image type.</param>
        /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
        /// <param name="person">Optional. If specified, results will be filtered to include only those containing the specified person.</param>
        /// <param name="personIds">Optional. If specified, results will be filtered to include only those containing the specified person id.</param>
        /// <param name="personTypes">Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited.</param>
        /// <param name="studios">Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimeted.</param>
        /// <param name="artists">Optional. If specified, results will be filtered based on artists. This allows multiple, pipe delimeted.</param>
        /// <param name="excludeArtistIds">Optional. If specified, results will be filtered based on artist id. This allows multiple, pipe delimeted.</param>
        /// <param name="artistIds">Optional. If specified, results will be filtered to include only those containing the specified artist id.</param>
        /// <param name="albumArtistIds">Optional. If specified, results will be filtered to include only those containing the specified album artist id.</param>
        /// <param name="contributingArtistIds">Optional. If specified, results will be filtered to include only those containing the specified contributing artist id.</param>
        /// <param name="albums">Optional. If specified, results will be filtered based on album. This allows multiple, pipe delimeted.</param>
        /// <param name="albumIds">Optional. If specified, results will be filtered based on album id. This allows multiple, pipe delimeted.</param>
        /// <param name="ids">Optional. If specific items are needed, specify a list of item id's to retrieve. This allows multiple, comma delimited.</param>
        /// <param name="videoTypes">Optional filter by VideoType (videofile, dvd, bluray, iso). Allows multiple, comma delimeted.</param>
        /// <param name="minOfficialRating">Optional filter by minimum official rating (PG, PG-13, TV-MA, etc).</param>
        /// <param name="isLocked">Optional filter by items that are locked.</param>
        /// <param name="isPlaceHolder">Optional filter by items that are placeholders.</param>
        /// <param name="hasOfficialRating">Optional filter by items that have official ratings.</param>
        /// <param name="collapseBoxSetItems">Whether or not to hide items behind their boxsets.</param>
        /// <param name="minWidth">Optional. Filter by the minimum width of the item.</param>
        /// <param name="minHeight">Optional. Filter by the minimum height of the item.</param>
        /// <param name="maxWidth">Optional. Filter by the maximum width of the item.</param>
        /// <param name="maxHeight">Optional. Filter by the maximum height of the item.</param>
        /// <param name="is3D">Optional filter by items that are 3D, or not.</param>
        /// <param name="seriesStatus">Optional filter by Series Status. Allows multiple, comma delimeted.</param>
        /// <param name="nameStartsWithOrGreater">Optional filter by items whose name is sorted equally or greater than a given input string.</param>
        /// <param name="nameStartsWith">Optional filter by items whose name is sorted equally than a given input string.</param>
        /// <param name="nameLessThan">Optional filter by items whose name is equally or lesser than a given input string.</param>
        /// <param name="studioIds">Optional. If specified, results will be filtered based on studio id. This allows multiple, pipe delimeted.</param>
        /// <param name="genreIds">Optional. If specified, results will be filtered based on genre id. This allows multiple, pipe delimeted.</param>
        /// <param name="enableTotalRecordCount">Optional. Enable the total record count.</param>
        /// <param name="enableImages">Optional, include image information in output.</param>
        /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the items.</returns>
        [HttpGet("Items")]
        [HttpGet("Users/{uId}/Items", Name = "GetItems_2")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetItems(
            [FromRoute] Guid? uId,
            [FromQuery] Guid? userId,
            [FromQuery] string? maxOfficialRating,
            [FromQuery] bool? hasThemeSong,
            [FromQuery] bool? hasThemeVideo,
            [FromQuery] bool? hasSubtitles,
            [FromQuery] bool? hasSpecialFeature,
            [FromQuery] bool? hasTrailer,
            [FromQuery] string? adjacentTo,
            [FromQuery] int? parentIndexNumber,
            [FromQuery] bool? hasParentalRating,
            [FromQuery] bool? isHd,
            [FromQuery] bool? is4K,
            [FromQuery] string? locationTypes,
            [FromQuery] string? excludeLocationTypes,
            [FromQuery] bool? isMissing,
            [FromQuery] bool? isUnaired,
            [FromQuery] double? minCommunityRating,
            [FromQuery] double? minCriticRating,
            [FromQuery] DateTime? minPremiereDate,
            [FromQuery] DateTime? minDateLastSaved,
            [FromQuery] DateTime? minDateLastSavedForUser,
            [FromQuery] DateTime? maxPremiereDate,
            [FromQuery] bool? hasOverview,
            [FromQuery] bool? hasImdbId,
            [FromQuery] bool? hasTmdbId,
            [FromQuery] bool? hasTvdbId,
            [FromQuery] string? excludeItemIds,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] bool? recursive,
            [FromQuery] string? searchTerm,
            [FromQuery] string? sortOrder,
            [FromQuery] string? parentId,
            [FromQuery] string? fields,
            [FromQuery] string? excludeItemTypes,
            [FromQuery] string? includeItemTypes,
            [FromQuery] string? filters,
            [FromQuery] bool? isFavorite,
            [FromQuery] string? mediaTypes,
            [FromQuery] string? imageTypes,
            [FromQuery] string? sortBy,
            [FromQuery] bool? isPlayed,
            [FromQuery] string? genres,
            [FromQuery] string? officialRatings,
            [FromQuery] string? tags,
            [FromQuery] string? years,
            [FromQuery] bool? enableUserData,
            [FromQuery] int? imageTypeLimit,
            [FromQuery] string? enableImageTypes,
            [FromQuery] string? person,
            [FromQuery] string? personIds,
            [FromQuery] string? personTypes,
            [FromQuery] string? studios,
            [FromQuery] string? artists,
            [FromQuery] string? excludeArtistIds,
            [FromQuery] string? artistIds,
            [FromQuery] string? albumArtistIds,
            [FromQuery] string? contributingArtistIds,
            [FromQuery] string? albums,
            [FromQuery] string? albumIds,
            [FromQuery] string? ids,
            [FromQuery] string? videoTypes,
            [FromQuery] string? minOfficialRating,
            [FromQuery] bool? isLocked,
            [FromQuery] bool? isPlaceHolder,
            [FromQuery] bool? hasOfficialRating,
            [FromQuery] bool? collapseBoxSetItems,
            [FromQuery] int? minWidth,
            [FromQuery] int? minHeight,
            [FromQuery] int? maxWidth,
            [FromQuery] int? maxHeight,
            [FromQuery] bool? is3D,
            [FromQuery] string? seriesStatus,
            [FromQuery] string? nameStartsWithOrGreater,
            [FromQuery] string? nameStartsWith,
            [FromQuery] string? nameLessThan,
            [FromQuery] string? studioIds,
            [FromQuery] string? genreIds,
            [FromQuery] bool enableTotalRecordCount = true,
            [FromQuery] bool? enableImages = true)
        {
            // use user id route parameter over query parameter
            userId = uId ?? userId;

            var user = userId.HasValue && !userId.Equals(Guid.Empty)
                ? _userManager.GetUserById(userId.Value)
                : null;
            var dtoOptions = new DtoOptions()
                .AddItemFields(fields)
                .AddClientFields(Request)
                .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

            if (string.Equals(includeItemTypes, "Playlist", StringComparison.OrdinalIgnoreCase)
                || string.Equals(includeItemTypes, "BoxSet", StringComparison.OrdinalIgnoreCase))
            {
                parentId = null;
            }

            BaseItem? item = null;
            QueryResult<BaseItem> result;
            if (!string.IsNullOrEmpty(parentId))
            {
                item = _libraryManager.GetItemById(parentId);
            }

            item ??= _libraryManager.GetUserRootFolder();

            if (!(item is Folder folder))
            {
                folder = _libraryManager.GetUserRootFolder();
            }

            if (folder is IHasCollectionType hasCollectionType
                && string.Equals(hasCollectionType.CollectionType, CollectionType.Playlists, StringComparison.OrdinalIgnoreCase))
            {
                recursive = true;
                includeItemTypes = "Playlist";
            }

            bool isInEnabledFolder = user!.GetPreference(PreferenceKind.EnabledFolders).Any(i => new Guid(i) == item.Id)
                                     // Assume all folders inside an EnabledChannel are enabled
                                     || user.GetPreference(PreferenceKind.EnabledChannels).Any(i => new Guid(i) == item.Id)
                                     // Assume all items inside an EnabledChannel are enabled
                                     || user.GetPreference(PreferenceKind.EnabledChannels).Any(i => new Guid(i) == item.ChannelId);

            var collectionFolders = _libraryManager.GetCollectionFolders(item);
            foreach (var collectionFolder in collectionFolders)
            {
                if (user.GetPreference(PreferenceKind.EnabledFolders).Contains(
                    collectionFolder.Id.ToString("N", CultureInfo.InvariantCulture),
                    StringComparer.OrdinalIgnoreCase))
                {
                    isInEnabledFolder = true;
                }
            }

            if (!(item is UserRootFolder)
                && !isInEnabledFolder
                && !user.HasPermission(PermissionKind.EnableAllFolders)
                && !user.HasPermission(PermissionKind.EnableAllChannels))
            {
                _logger.LogWarning("{UserName} is not permitted to access Library {ItemName}.", user.Username, item.Name);
                return Unauthorized($"{user.Username} is not permitted to access Library {item.Name}.");
            }

            if ((recursive.HasValue && recursive.Value) || !string.IsNullOrEmpty(ids) || !(item is UserRootFolder))
            {
                var query = new InternalItemsQuery(user!)
                {
                    IsPlayed = isPlayed,
                    MediaTypes = RequestHelpers.Split(mediaTypes, ',', true),
                    IncludeItemTypes = RequestHelpers.Split(includeItemTypes, ',', true),
                    ExcludeItemTypes = RequestHelpers.Split(excludeItemTypes, ',', true),
                    Recursive = recursive ?? false,
                    OrderBy = RequestHelpers.GetOrderBy(sortBy, sortOrder),
                    IsFavorite = isFavorite,
                    Limit = limit,
                    StartIndex = startIndex,
                    IsMissing = isMissing,
                    IsUnaired = isUnaired,
                    CollapseBoxSetItems = collapseBoxSetItems,
                    NameLessThan = nameLessThan,
                    NameStartsWith = nameStartsWith,
                    NameStartsWithOrGreater = nameStartsWithOrGreater,
                    HasImdbId = hasImdbId,
                    IsPlaceHolder = isPlaceHolder,
                    IsLocked = isLocked,
                    MinWidth = minWidth,
                    MinHeight = minHeight,
                    MaxWidth = maxWidth,
                    MaxHeight = maxHeight,
                    Is3D = is3D,
                    HasTvdbId = hasTvdbId,
                    HasTmdbId = hasTmdbId,
                    HasOverview = hasOverview,
                    HasOfficialRating = hasOfficialRating,
                    HasParentalRating = hasParentalRating,
                    HasSpecialFeature = hasSpecialFeature,
                    HasSubtitles = hasSubtitles,
                    HasThemeSong = hasThemeSong,
                    HasThemeVideo = hasThemeVideo,
                    HasTrailer = hasTrailer,
                    IsHD = isHd,
                    Is4K = is4K,
                    Tags = RequestHelpers.Split(tags, '|', true),
                    OfficialRatings = RequestHelpers.Split(officialRatings, '|', true),
                    Genres = RequestHelpers.Split(genres, '|', true),
                    ArtistIds = RequestHelpers.GetGuids(artistIds),
                    AlbumArtistIds = RequestHelpers.GetGuids(albumArtistIds),
                    ContributingArtistIds = RequestHelpers.GetGuids(contributingArtistIds),
                    GenreIds = RequestHelpers.GetGuids(genreIds),
                    StudioIds = RequestHelpers.GetGuids(studioIds),
                    Person = person,
                    PersonIds = RequestHelpers.GetGuids(personIds),
                    PersonTypes = RequestHelpers.Split(personTypes, ',', true),
                    Years = RequestHelpers.Split(years, ',', true).Select(int.Parse).ToArray(),
                    ImageTypes = RequestHelpers.Split(imageTypes, ',', true).Select(v => Enum.Parse<ImageType>(v, true)).ToArray(),
                    VideoTypes = RequestHelpers.Split(videoTypes, ',', true).Select(v => Enum.Parse<VideoType>(v, true)).ToArray(),
                    AdjacentTo = adjacentTo,
                    ItemIds = RequestHelpers.GetGuids(ids),
                    MinCommunityRating = minCommunityRating,
                    MinCriticRating = minCriticRating,
                    ParentId = string.IsNullOrWhiteSpace(parentId) ? Guid.Empty : new Guid(parentId),
                    ParentIndexNumber = parentIndexNumber,
                    EnableTotalRecordCount = enableTotalRecordCount,
                    ExcludeItemIds = RequestHelpers.GetGuids(excludeItemIds),
                    DtoOptions = dtoOptions,
                    SearchTerm = searchTerm,
                    MinDateLastSaved = minDateLastSaved?.ToUniversalTime(),
                    MinDateLastSavedForUser = minDateLastSavedForUser?.ToUniversalTime(),
                    MinPremiereDate = minPremiereDate?.ToUniversalTime(),
                    MaxPremiereDate = maxPremiereDate?.ToUniversalTime(),
                };

                if (!string.IsNullOrWhiteSpace(ids) || !string.IsNullOrWhiteSpace(searchTerm))
                {
                    query.CollapseBoxSetItems = false;
                }

                foreach (var filter in RequestHelpers.GetFilters(filters!))
                {
                    switch (filter)
                    {
                        case ItemFilter.Dislikes:
                            query.IsLiked = false;
                            break;
                        case ItemFilter.IsFavorite:
                            query.IsFavorite = true;
                            break;
                        case ItemFilter.IsFavoriteOrLikes:
                            query.IsFavoriteOrLiked = true;
                            break;
                        case ItemFilter.IsFolder:
                            query.IsFolder = true;
                            break;
                        case ItemFilter.IsNotFolder:
                            query.IsFolder = false;
                            break;
                        case ItemFilter.IsPlayed:
                            query.IsPlayed = true;
                            break;
                        case ItemFilter.IsResumable:
                            query.IsResumable = true;
                            break;
                        case ItemFilter.IsUnplayed:
                            query.IsPlayed = false;
                            break;
                        case ItemFilter.Likes:
                            query.IsLiked = true;
                            break;
                    }
                }

                // Filter by Series Status
                if (!string.IsNullOrEmpty(seriesStatus))
                {
                    query.SeriesStatuses = seriesStatus.Split(',').Select(d => (SeriesStatus)Enum.Parse(typeof(SeriesStatus), d, true)).ToArray();
                }

                // ExcludeLocationTypes
                if (!string.IsNullOrEmpty(excludeLocationTypes))
                {
                    if (excludeLocationTypes.Split(',').Select(d => (LocationType)Enum.Parse(typeof(LocationType), d, true)).ToArray().Contains(LocationType.Virtual))
                    {
                        query.IsVirtualItem = false;
                    }
                }

                if (!string.IsNullOrEmpty(locationTypes))
                {
                    var requestedLocationTypes = locationTypes.Split(',');
                    if (requestedLocationTypes.Length > 0 && requestedLocationTypes.Length < 4)
                    {
                        query.IsVirtualItem = requestedLocationTypes.Contains(LocationType.Virtual.ToString());
                    }
                }

                // Min official rating
                if (!string.IsNullOrWhiteSpace(minOfficialRating))
                {
                    query.MinParentalRating = _localization.GetRatingLevel(minOfficialRating);
                }

                // Max official rating
                if (!string.IsNullOrWhiteSpace(maxOfficialRating))
                {
                    query.MaxParentalRating = _localization.GetRatingLevel(maxOfficialRating);
                }

                // Artists
                if (!string.IsNullOrEmpty(artists))
                {
                    query.ArtistIds = artists.Split('|').Select(i =>
                    {
                        try
                        {
                            return _libraryManager.GetArtist(i, new DtoOptions(false));
                        }
                        catch
                        {
                            return null;
                        }
                    }).Where(i => i != null).Select(i => i!.Id).ToArray();
                }

                // ExcludeArtistIds
                if (!string.IsNullOrWhiteSpace(excludeArtistIds))
                {
                    query.ExcludeArtistIds = RequestHelpers.GetGuids(excludeArtistIds);
                }

                if (!string.IsNullOrWhiteSpace(albumIds))
                {
                    query.AlbumIds = RequestHelpers.GetGuids(albumIds);
                }

                // Albums
                if (!string.IsNullOrEmpty(albums))
                {
                    query.AlbumIds = albums.Split('|').SelectMany(i =>
                    {
                        return _libraryManager.GetItemIds(new InternalItemsQuery { IncludeItemTypes = new[] { nameof(MusicAlbum) }, Name = i, Limit = 1 });
                    }).ToArray();
                }

                // Studios
                if (!string.IsNullOrEmpty(studios))
                {
                    query.StudioIds = studios.Split('|').Select(i =>
                    {
                        try
                        {
                            return _libraryManager.GetStudio(i);
                        }
                        catch
                        {
                            return null;
                        }
                    }).Where(i => i != null).Select(i => i!.Id).ToArray();
                }

                // Apply default sorting if none requested
                if (query.OrderBy.Count == 0)
                {
                    // Albums by artist
                    if (query.ArtistIds.Length > 0 && query.IncludeItemTypes.Length == 1 && string.Equals(query.IncludeItemTypes[0], "MusicAlbum", StringComparison.OrdinalIgnoreCase))
                    {
                        query.OrderBy = new[] { new ValueTuple<string, SortOrder>(ItemSortBy.ProductionYear, SortOrder.Descending), new ValueTuple<string, SortOrder>(ItemSortBy.SortName, SortOrder.Ascending) };
                    }
                }

                result = folder.GetItems(query);
            }
            else
            {
                var itemsArray = folder.GetChildren(user, true);
                result = new QueryResult<BaseItem> { Items = itemsArray, TotalRecordCount = itemsArray.Count, StartIndex = 0 };
            }

            return new QueryResult<BaseItemDto> { StartIndex = startIndex.GetValueOrDefault(), TotalRecordCount = result.TotalRecordCount, Items = _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user) };
        }

        /// <summary>
        /// Gets items based on a query.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The item limit.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
        /// <param name="mediaTypes">Optional. Filter by MediaType. Allows multiple, comma delimited.</param>
        /// <param name="enableUserData">Optional. Include user data.</param>
        /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
        /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
        /// <param name="excludeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on the item type. This allows multiple, comma delimeted.</param>
        /// <param name="enableTotalRecordCount">Optional. Enable the total record count.</param>
        /// <param name="enableImages">Optional. Include image information in output.</param>
        /// <response code="200">Items returned.</response>
        /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the items that are resumable.</returns>
        [HttpGet("Users/{userId}/Items/Resume")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetResumeItems(
            [FromRoute, Required] Guid userId,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string? searchTerm,
            [FromQuery] string? parentId,
            [FromQuery] string? fields,
            [FromQuery] string? mediaTypes,
            [FromQuery] bool? enableUserData,
            [FromQuery] int? imageTypeLimit,
            [FromQuery] string? enableImageTypes,
            [FromQuery] string? excludeItemTypes,
            [FromQuery] string? includeItemTypes,
            [FromQuery] bool enableTotalRecordCount = true,
            [FromQuery] bool? enableImages = true)
        {
            var user = _userManager.GetUserById(userId);
            var parentIdGuid = string.IsNullOrWhiteSpace(parentId) ? Guid.Empty : new Guid(parentId);
            var dtoOptions = new DtoOptions()
                .AddItemFields(fields)
                .AddClientFields(Request)
                .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

            var ancestorIds = Array.Empty<Guid>();

            var excludeFolderIds = user.GetPreference(PreferenceKind.LatestItemExcludes);
            if (parentIdGuid.Equals(Guid.Empty) && excludeFolderIds.Length > 0)
            {
                ancestorIds = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                    .Where(i => i is Folder)
                    .Where(i => !excludeFolderIds.Contains(i.Id.ToString("N", CultureInfo.InvariantCulture)))
                    .Select(i => i.Id)
                    .ToArray();
            }

            var itemsResult = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                IsResumable = true,
                StartIndex = startIndex,
                Limit = limit,
                ParentId = parentIdGuid,
                Recursive = true,
                DtoOptions = dtoOptions,
                MediaTypes = RequestHelpers.Split(mediaTypes, ',', true),
                IsVirtualItem = false,
                CollapseBoxSetItems = false,
                EnableTotalRecordCount = enableTotalRecordCount,
                AncestorIds = ancestorIds,
                IncludeItemTypes = RequestHelpers.Split(includeItemTypes, ',', true),
                ExcludeItemTypes = RequestHelpers.Split(excludeItemTypes, ',', true),
                SearchTerm = searchTerm
            });

            var returnItems = _dtoService.GetBaseItemDtos(itemsResult.Items, dtoOptions, user);

            return new QueryResult<BaseItemDto>
            {
                StartIndex = startIndex.GetValueOrDefault(),
                TotalRecordCount = itemsResult.TotalRecordCount,
                Items = returnItems
            };
        }
    }
}
