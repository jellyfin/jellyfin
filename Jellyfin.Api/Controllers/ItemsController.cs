using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The items controller.
/// </summary>
[Route("")]
[Authorize]
public class ItemsController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly IDtoService _dtoService;
    private readonly ILogger<ItemsController> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly IUserDataManager _userDataRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemsController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="userDataRepository">Instance of the <see cref="IUserDataManager"/> interface.</param>
    public ItemsController(
        IUserManager userManager,
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        IDtoService dtoService,
        ILogger<ItemsController> logger,
        ISessionManager sessionManager,
        IUserDataManager userDataRepository)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _localization = localization;
        _dtoService = dtoService;
        _logger = logger;
        _sessionManager = sessionManager;
        _userDataRepository = userDataRepository;
    }

    /// <summary>
    /// Gets items based on a query.
    /// </summary>
    /// <param name="userId">The user id supplied as query parameter; this is required when not using an API key.</param>
    /// <param name="maxOfficialRating">Optional filter by maximum official rating (PG, PG-13, TV-MA, etc).</param>
    /// <param name="hasThemeSong">Optional filter by items with theme songs.</param>
    /// <param name="hasThemeVideo">Optional filter by items with theme videos.</param>
    /// <param name="hasSubtitles">Optional filter by items with subtitles.</param>
    /// <param name="hasSpecialFeature">Optional filter by items with special features.</param>
    /// <param name="hasTrailer">Optional filter by items with trailers.</param>
    /// <param name="adjacentTo">Optional. Return items that are siblings of a supplied item.</param>
    /// <param name="indexNumber">Optional filter by index number.</param>
    /// <param name="parentIndexNumber">Optional filter by parent index number.</param>
    /// <param name="hasParentalRating">Optional filter by items that have or do not have a parental rating.</param>
    /// <param name="isHd">Optional filter by items that are HD or not.</param>
    /// <param name="is4K">Optional filter by items that are 4K or not.</param>
    /// <param name="locationTypes">Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimited.</param>
    /// <param name="excludeLocationTypes">Optional. If specified, results will be filtered based on the LocationType. This allows multiple, comma delimited.</param>
    /// <param name="isMissing">Optional filter by items that are missing episodes or not.</param>
    /// <param name="isUnaired">Optional filter by items that are unaired episodes or not.</param>
    /// <param name="minCommunityRating">Optional filter by minimum community rating.</param>
    /// <param name="minCriticRating">Optional filter by minimum critic rating.</param>
    /// <param name="minPremiereDate">Optional. The minimum premiere date. Format = ISO.</param>
    /// <param name="minDateLastSaved">Optional. The minimum last saved date. Format = ISO.</param>
    /// <param name="minDateLastSavedForUser">Optional. The minimum last saved date for the current user. Format = ISO.</param>
    /// <param name="maxPremiereDate">Optional. The maximum premiere date. Format = ISO.</param>
    /// <param name="hasOverview">Optional filter by items that have an overview or not.</param>
    /// <param name="hasImdbId">Optional filter by items that have an IMDb id or not.</param>
    /// <param name="hasTmdbId">Optional filter by items that have a TMDb id or not.</param>
    /// <param name="hasTvdbId">Optional filter by items that have a TVDb id or not.</param>
    /// <param name="isMovie">Optional filter for live tv movies.</param>
    /// <param name="isSeries">Optional filter for live tv series.</param>
    /// <param name="isNews">Optional filter for live tv news.</param>
    /// <param name="isKids">Optional filter for live tv kids.</param>
    /// <param name="isSports">Optional filter for live tv sports.</param>
    /// <param name="excludeItemIds">Optional. If specified, results will be filtered by excluding item ids. This allows multiple, comma delimited.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="recursive">When searching within folders, this determines whether or not the search will be recursive. true/false.</param>
    /// <param name="searchTerm">Optional. Filter based on a search term.</param>
    /// <param name="sortOrder">Sort Order - Ascending, Descending.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
    /// <param name="excludeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
    /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on the item type. This allows multiple, comma delimited.</param>
    /// <param name="filters">Optional. Specify additional filters to apply. This allows multiple, comma delimited. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes.</param>
    /// <param name="isFavorite">Optional filter by items that are marked as favorite, or not.</param>
    /// <param name="mediaTypes">Optional filter by MediaType. Allows multiple, comma delimited.</param>
    /// <param name="imageTypes">Optional. If specified, results will be filtered based on those containing image types. This allows multiple, comma delimited.</param>
    /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime.</param>
    /// <param name="isPlayed">Optional filter by items that are played, or not.</param>
    /// <param name="genres">Optional. If specified, results will be filtered based on genre. This allows multiple, pipe delimited.</param>
    /// <param name="officialRatings">Optional. If specified, results will be filtered based on OfficialRating. This allows multiple, pipe delimited.</param>
    /// <param name="tags">Optional. If specified, results will be filtered based on tag. This allows multiple, pipe delimited.</param>
    /// <param name="years">Optional. If specified, results will be filtered based on production year. This allows multiple, comma delimited.</param>
    /// <param name="enableUserData">Optional, include user data.</param>
    /// <param name="imageTypeLimit">Optional, the max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="person">Optional. If specified, results will be filtered to include only those containing the specified person.</param>
    /// <param name="personIds">Optional. If specified, results will be filtered to include only those containing the specified person id.</param>
    /// <param name="personTypes">Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited.</param>
    /// <param name="studios">Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimited.</param>
    /// <param name="artists">Optional. If specified, results will be filtered based on artists. This allows multiple, pipe delimited.</param>
    /// <param name="excludeArtistIds">Optional. If specified, results will be filtered based on artist id. This allows multiple, pipe delimited.</param>
    /// <param name="artistIds">Optional. If specified, results will be filtered to include only those containing the specified artist id.</param>
    /// <param name="albumArtistIds">Optional. If specified, results will be filtered to include only those containing the specified album artist id.</param>
    /// <param name="contributingArtistIds">Optional. If specified, results will be filtered to include only those containing the specified contributing artist id.</param>
    /// <param name="albums">Optional. If specified, results will be filtered based on album. This allows multiple, pipe delimited.</param>
    /// <param name="albumIds">Optional. If specified, results will be filtered based on album id. This allows multiple, pipe delimited.</param>
    /// <param name="ids">Optional. If specific items are needed, specify a list of item id's to retrieve. This allows multiple, comma delimited.</param>
    /// <param name="videoTypes">Optional filter by VideoType (videofile, dvd, bluray, iso). Allows multiple, comma delimited.</param>
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
    /// <param name="seriesStatus">Optional filter by Series Status. Allows multiple, comma delimited.</param>
    /// <param name="nameStartsWithOrGreater">Optional filter by items whose name is sorted equally or greater than a given input string.</param>
    /// <param name="nameStartsWith">Optional filter by items whose name is sorted equally than a given input string.</param>
    /// <param name="nameLessThan">Optional filter by items whose name is equally or lesser than a given input string.</param>
    /// <param name="studioIds">Optional. If specified, results will be filtered based on studio id. This allows multiple, pipe delimited.</param>
    /// <param name="genreIds">Optional. If specified, results will be filtered based on genre id. This allows multiple, pipe delimited.</param>
    /// <param name="enableTotalRecordCount">Optional. Enable the total record count.</param>
    /// <param name="enableImages">Optional, include image information in output.</param>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the items.</returns>
    [HttpGet("Items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetItems(
        [FromQuery] Guid? userId,
        [FromQuery] string? maxOfficialRating,
        [FromQuery] bool? hasThemeSong,
        [FromQuery] bool? hasThemeVideo,
        [FromQuery] bool? hasSubtitles,
        [FromQuery] bool? hasSpecialFeature,
        [FromQuery] bool? hasTrailer,
        [FromQuery] Guid? adjacentTo,
        [FromQuery] int? indexNumber,
        [FromQuery] int? parentIndexNumber,
        [FromQuery] bool? hasParentalRating,
        [FromQuery] bool? isHd,
        [FromQuery] bool? is4K,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] LocationType[] locationTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] LocationType[] excludeLocationTypes,
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
        [FromQuery] bool? isMovie,
        [FromQuery] bool? isSeries,
        [FromQuery] bool? isNews,
        [FromQuery] bool? isKids,
        [FromQuery] bool? isSports,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] excludeItemIds,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] bool? recursive,
        [FromQuery] string? searchTerm,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] SortOrder[] sortOrder,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFilter[] filters,
        [FromQuery] bool? isFavorite,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] MediaType[] mediaTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] imageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemSortBy[] sortBy,
        [FromQuery] bool? isPlayed,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] genres,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] officialRatings,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] tags,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] int[] years,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] string? person,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] personIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] personTypes,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] studios,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] artists,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] excludeArtistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] artistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] albumArtistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] contributingArtistIds,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] albums,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] albumIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] ids,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] VideoType[] videoTypes,
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
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] SeriesStatus[] seriesStatus,
        [FromQuery] string? nameStartsWithOrGreater,
        [FromQuery] string? nameStartsWith,
        [FromQuery] string? nameLessThan,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] studioIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] genreIds,
        [FromQuery] bool enableTotalRecordCount = true,
        [FromQuery] bool? enableImages = true)
    {
        var isApiKey = User.GetIsApiKey();
        // if api key is used (auth.IsApiKey == true), then `user` will be null throughout this method
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value) ?? throw new ResourceNotFoundException();

        // beyond this point, we're either using an api key or we have a valid user
        if (!isApiKey && user is null)
        {
            return BadRequest("userId is required");
        }

        if (user is not null
            && user.GetPreference(PreferenceKind.AllowedTags).Length != 0
            && !fields.Contains(ItemFields.Tags))
        {
            fields = [..fields, ItemFields.Tags];
        }

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        if (includeItemTypes.Length == 1
            && includeItemTypes[0] == BaseItemKind.BoxSet)
        {
            parentId = null;
        }

        var item = _libraryManager.GetParentItem(parentId, userId);
        QueryResult<BaseItem> result;

        if (item is not Folder folder)
        {
            folder = _libraryManager.GetUserRootFolder();
        }

        CollectionType? collectionType = null;
        if (folder is IHasCollectionType hasCollectionType)
        {
            collectionType = hasCollectionType.CollectionType;
        }

        if (collectionType == CollectionType.playlists)
        {
            recursive = true;
            includeItemTypes = new[] { BaseItemKind.Playlist };
        }

        if (item is not UserRootFolder
            // api keys can always access all folders
            && !isApiKey
            // check the item is visible for the user
            && !item.IsVisible(user))
        {
            _logger.LogWarning("{UserName} is not permitted to access Library {ItemName}", user!.Username, item.Name);
            return Unauthorized($"{user.Username} is not permitted to access Library {item.Name}.");
        }

        if ((recursive.HasValue && recursive.Value) || ids.Length != 0 || item is not UserRootFolder)
        {
            var query = new InternalItemsQuery(user)
            {
                IsPlayed = isPlayed,
                MediaTypes = mediaTypes,
                IncludeItemTypes = includeItemTypes,
                ExcludeItemTypes = excludeItemTypes,
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
                IsMovie = isMovie,
                IsSeries = isSeries,
                IsNews = isNews,
                IsKids = isKids,
                IsSports = isSports,
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
                Tags = tags,
                OfficialRatings = officialRatings,
                Genres = genres,
                ArtistIds = artistIds,
                AlbumArtistIds = albumArtistIds,
                ContributingArtistIds = contributingArtistIds,
                GenreIds = genreIds,
                StudioIds = studioIds,
                Person = person,
                PersonIds = personIds,
                PersonTypes = personTypes,
                Years = years,
                ImageTypes = imageTypes,
                VideoTypes = videoTypes,
                AdjacentTo = adjacentTo,
                ItemIds = ids,
                MinCommunityRating = minCommunityRating,
                MinCriticRating = minCriticRating,
                ParentId = parentId ?? Guid.Empty,
                IndexNumber = indexNumber,
                ParentIndexNumber = parentIndexNumber,
                EnableTotalRecordCount = enableTotalRecordCount,
                ExcludeItemIds = excludeItemIds,
                DtoOptions = dtoOptions,
                SearchTerm = searchTerm,
                MinDateLastSaved = minDateLastSaved?.ToUniversalTime(),
                MinDateLastSavedForUser = minDateLastSavedForUser?.ToUniversalTime(),
                MinPremiereDate = minPremiereDate?.ToUniversalTime(),
                MaxPremiereDate = maxPremiereDate?.ToUniversalTime(),
            };

            if (ids.Length != 0 || !string.IsNullOrWhiteSpace(searchTerm))
            {
                query.CollapseBoxSetItems = false;
            }

            foreach (var filter in filters)
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
            if (seriesStatus.Length != 0)
            {
                query.SeriesStatuses = seriesStatus;
            }

            // Exclude Blocked Unrated Items
            var blockedUnratedItems = user?.GetPreferenceValues<UnratedItem>(PreferenceKind.BlockUnratedItems);
            if (blockedUnratedItems is not null)
            {
                query.BlockUnratedItems = blockedUnratedItems;
            }

            // ExcludeLocationTypes
            if (excludeLocationTypes.Any(t => t == LocationType.Virtual))
            {
                query.IsVirtualItem = false;
            }

            if (locationTypes.Length > 0 && locationTypes.Length < 4)
            {
                query.IsVirtualItem = locationTypes.Contains(LocationType.Virtual);
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
            if (artists.Length != 0)
            {
                query.ArtistIds = artists.Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetArtist(i, new DtoOptions(false));
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(i => i is not null).Select(i => i!.Id).ToArray();
            }

            // ExcludeArtistIds
            if (excludeArtistIds.Length != 0)
            {
                query.ExcludeArtistIds = excludeArtistIds;
            }

            if (albumIds.Length != 0)
            {
                query.AlbumIds = albumIds;
            }

            // Albums
            if (albums.Length != 0)
            {
                query.AlbumIds = albums.SelectMany(i =>
                {
                    return _libraryManager.GetItemIds(new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.MusicAlbum }, Name = i, Limit = 1 });
                }).ToArray();
            }

            // Studios
            if (studios.Length != 0)
            {
                query.StudioIds = studios.Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetStudio(i);
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(i => i is not null).Select(i => i!.Id).ToArray();
            }

            // Apply default sorting if none requested
            if (query.OrderBy.Count == 0)
            {
                // Albums by artist
                if (query.ArtistIds.Length > 0 && query.IncludeItemTypes.Length == 1 && query.IncludeItemTypes[0] == BaseItemKind.MusicAlbum)
                {
                    query.OrderBy = new[] { (ItemSortBy.ProductionYear, SortOrder.Descending), (ItemSortBy.SortName, SortOrder.Ascending) };
                }
            }

            query.Parent = null;
            result = folder.GetItems(query);
        }
        else
        {
            var itemsArray = folder.GetChildren(user, true);
            result = new QueryResult<BaseItem>(itemsArray);
        }

        return new QueryResult<BaseItemDto>(
            startIndex,
            result.TotalRecordCount,
            _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user));
    }

    /// <summary>
    /// Gets items based on a query.
    /// </summary>
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
    /// <param name="locationTypes">Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimited.</param>
    /// <param name="excludeLocationTypes">Optional. If specified, results will be filtered based on the LocationType. This allows multiple, comma delimited.</param>
    /// <param name="isMissing">Optional filter by items that are missing episodes or not.</param>
    /// <param name="isUnaired">Optional filter by items that are unaired episodes or not.</param>
    /// <param name="minCommunityRating">Optional filter by minimum community rating.</param>
    /// <param name="minCriticRating">Optional filter by minimum critic rating.</param>
    /// <param name="minPremiereDate">Optional. The minimum premiere date. Format = ISO.</param>
    /// <param name="minDateLastSaved">Optional. The minimum last saved date. Format = ISO.</param>
    /// <param name="minDateLastSavedForUser">Optional. The minimum last saved date for the current user. Format = ISO.</param>
    /// <param name="maxPremiereDate">Optional. The maximum premiere date. Format = ISO.</param>
    /// <param name="hasOverview">Optional filter by items that have an overview or not.</param>
    /// <param name="hasImdbId">Optional filter by items that have an IMDb id or not.</param>
    /// <param name="hasTmdbId">Optional filter by items that have a TMDb id or not.</param>
    /// <param name="hasTvdbId">Optional filter by items that have a TVDb id or not.</param>
    /// <param name="isMovie">Optional filter for live tv movies.</param>
    /// <param name="isSeries">Optional filter for live tv series.</param>
    /// <param name="isNews">Optional filter for live tv news.</param>
    /// <param name="isKids">Optional filter for live tv kids.</param>
    /// <param name="isSports">Optional filter for live tv sports.</param>
    /// <param name="excludeItemIds">Optional. If specified, results will be filtered by excluding item ids. This allows multiple, comma delimited.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="recursive">When searching within folders, this determines whether or not the search will be recursive. true/false.</param>
    /// <param name="searchTerm">Optional. Filter based on a search term.</param>
    /// <param name="sortOrder">Sort Order - Ascending, Descending.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
    /// <param name="excludeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
    /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on the item type. This allows multiple, comma delimited.</param>
    /// <param name="filters">Optional. Specify additional filters to apply. This allows multiple, comma delimited. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes.</param>
    /// <param name="isFavorite">Optional filter by items that are marked as favorite, or not.</param>
    /// <param name="mediaTypes">Optional filter by MediaType. Allows multiple, comma delimited.</param>
    /// <param name="imageTypes">Optional. If specified, results will be filtered based on those containing image types. This allows multiple, comma delimited.</param>
    /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime.</param>
    /// <param name="isPlayed">Optional filter by items that are played, or not.</param>
    /// <param name="genres">Optional. If specified, results will be filtered based on genre. This allows multiple, pipe delimited.</param>
    /// <param name="officialRatings">Optional. If specified, results will be filtered based on OfficialRating. This allows multiple, pipe delimited.</param>
    /// <param name="tags">Optional. If specified, results will be filtered based on tag. This allows multiple, pipe delimited.</param>
    /// <param name="years">Optional. If specified, results will be filtered based on production year. This allows multiple, comma delimited.</param>
    /// <param name="enableUserData">Optional, include user data.</param>
    /// <param name="imageTypeLimit">Optional, the max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="person">Optional. If specified, results will be filtered to include only those containing the specified person.</param>
    /// <param name="personIds">Optional. If specified, results will be filtered to include only those containing the specified person id.</param>
    /// <param name="personTypes">Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited.</param>
    /// <param name="studios">Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimited.</param>
    /// <param name="artists">Optional. If specified, results will be filtered based on artists. This allows multiple, pipe delimited.</param>
    /// <param name="excludeArtistIds">Optional. If specified, results will be filtered based on artist id. This allows multiple, pipe delimited.</param>
    /// <param name="artistIds">Optional. If specified, results will be filtered to include only those containing the specified artist id.</param>
    /// <param name="albumArtistIds">Optional. If specified, results will be filtered to include only those containing the specified album artist id.</param>
    /// <param name="contributingArtistIds">Optional. If specified, results will be filtered to include only those containing the specified contributing artist id.</param>
    /// <param name="albums">Optional. If specified, results will be filtered based on album. This allows multiple, pipe delimited.</param>
    /// <param name="albumIds">Optional. If specified, results will be filtered based on album id. This allows multiple, pipe delimited.</param>
    /// <param name="ids">Optional. If specific items are needed, specify a list of item id's to retrieve. This allows multiple, comma delimited.</param>
    /// <param name="videoTypes">Optional filter by VideoType (videofile, dvd, bluray, iso). Allows multiple, comma delimited.</param>
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
    /// <param name="seriesStatus">Optional filter by Series Status. Allows multiple, comma delimited.</param>
    /// <param name="nameStartsWithOrGreater">Optional filter by items whose name is sorted equally or greater than a given input string.</param>
    /// <param name="nameStartsWith">Optional filter by items whose name is sorted equally than a given input string.</param>
    /// <param name="nameLessThan">Optional filter by items whose name is equally or lesser than a given input string.</param>
    /// <param name="studioIds">Optional. If specified, results will be filtered based on studio id. This allows multiple, pipe delimited.</param>
    /// <param name="genreIds">Optional. If specified, results will be filtered based on genre id. This allows multiple, pipe delimited.</param>
    /// <param name="enableTotalRecordCount">Optional. Enable the total record count.</param>
    /// <param name="enableImages">Optional, include image information in output.</param>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the items.</returns>
    [HttpGet("Users/{userId}/Items")]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetItemsByUserIdLegacy(
        [FromRoute] Guid userId,
        [FromQuery] string? maxOfficialRating,
        [FromQuery] bool? hasThemeSong,
        [FromQuery] bool? hasThemeVideo,
        [FromQuery] bool? hasSubtitles,
        [FromQuery] bool? hasSpecialFeature,
        [FromQuery] bool? hasTrailer,
        [FromQuery] Guid? adjacentTo,
        [FromQuery] int? parentIndexNumber,
        [FromQuery] bool? hasParentalRating,
        [FromQuery] bool? isHd,
        [FromQuery] bool? is4K,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] LocationType[] locationTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] LocationType[] excludeLocationTypes,
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
        [FromQuery] bool? isMovie,
        [FromQuery] bool? isSeries,
        [FromQuery] bool? isNews,
        [FromQuery] bool? isKids,
        [FromQuery] bool? isSports,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] excludeItemIds,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] bool? recursive,
        [FromQuery] string? searchTerm,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] SortOrder[] sortOrder,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFilter[] filters,
        [FromQuery] bool? isFavorite,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] MediaType[] mediaTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] imageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemSortBy[] sortBy,
        [FromQuery] bool? isPlayed,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] genres,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] officialRatings,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] tags,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] int[] years,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] string? person,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] personIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] personTypes,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] studios,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] artists,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] excludeArtistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] artistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] albumArtistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] contributingArtistIds,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] albums,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] albumIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] ids,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] VideoType[] videoTypes,
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
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] SeriesStatus[] seriesStatus,
        [FromQuery] string? nameStartsWithOrGreater,
        [FromQuery] string? nameStartsWith,
        [FromQuery] string? nameLessThan,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] studioIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] genreIds,
        [FromQuery] bool enableTotalRecordCount = true,
        [FromQuery] bool? enableImages = true)
        => GetItems(
            userId,
            maxOfficialRating,
            hasThemeSong,
            hasThemeVideo,
            hasSubtitles,
            hasSpecialFeature,
            hasTrailer,
            adjacentTo,
            null,
            parentIndexNumber,
            hasParentalRating,
            isHd,
            is4K,
            locationTypes,
            excludeLocationTypes,
            isMissing,
            isUnaired,
            minCommunityRating,
            minCriticRating,
            minPremiereDate,
            minDateLastSaved,
            minDateLastSavedForUser,
            maxPremiereDate,
            hasOverview,
            hasImdbId,
            hasTmdbId,
            hasTvdbId,
            isMovie,
            isSeries,
            isNews,
            isKids,
            isSports,
            excludeItemIds,
            startIndex,
            limit,
            recursive,
            searchTerm,
            sortOrder,
            parentId,
            fields,
            excludeItemTypes,
            includeItemTypes,
            filters,
            isFavorite,
            mediaTypes,
            imageTypes,
            sortBy,
            isPlayed,
            genres,
            officialRatings,
            tags,
            years,
            enableUserData,
            imageTypeLimit,
            enableImageTypes,
            person,
            personIds,
            personTypes,
            studios,
            artists,
            excludeArtistIds,
            artistIds,
            albumArtistIds,
            contributingArtistIds,
            albums,
            albumIds,
            ids,
            videoTypes,
            minOfficialRating,
            isLocked,
            isPlaceHolder,
            hasOfficialRating,
            collapseBoxSetItems,
            minWidth,
            minHeight,
            maxWidth,
            maxHeight,
            is3D,
            seriesStatus,
            nameStartsWithOrGreater,
            nameStartsWith,
            nameLessThan,
            studioIds,
            genreIds,
            enableTotalRecordCount,
            enableImages);

    /// <summary>
    /// Gets items based on a query.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="limit">The item limit.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
    /// <param name="mediaTypes">Optional. Filter by MediaType. Allows multiple, comma delimited.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="excludeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
    /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on the item type. This allows multiple, comma delimited.</param>
    /// <param name="enableTotalRecordCount">Optional. Enable the total record count.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="excludeActiveSessions">Optional. Whether to exclude the currently active sessions.</param>
    /// <response code="200">Items returned.</response>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the items that are resumable.</returns>
    [HttpGet("UserItems/Resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetResumeItems(
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] MediaType[] mediaTypes,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery] bool enableTotalRecordCount = true,
        [FromQuery] bool? enableImages = true,
        [FromQuery] bool excludeActiveSessions = false)
    {
        var requestUserId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(requestUserId);
        if (user is null)
        {
            return NotFound();
        }

        var parentIdGuid = parentId ?? Guid.Empty;
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var ancestorIds = Array.Empty<Guid>();

        var excludeFolderIds = user.GetPreferenceValues<Guid>(PreferenceKind.LatestItemExcludes);
        if (parentIdGuid.IsEmpty() && excludeFolderIds.Length > 0)
        {
            ancestorIds = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                .Where(i => i is Folder)
                .Where(i => !excludeFolderIds.Contains(i.Id))
                .Select(i => i.Id)
                .ToArray();
        }

        var excludeItemIds = Array.Empty<Guid>();
        if (excludeActiveSessions)
        {
            excludeItemIds = _sessionManager.Sessions
                .Where(s => s.UserId.Equals(requestUserId) && s.NowPlayingItem is not null)
                .Select(s => s.NowPlayingItem.Id)
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
            MediaTypes = mediaTypes,
            IsVirtualItem = false,
            CollapseBoxSetItems = false,
            EnableTotalRecordCount = enableTotalRecordCount,
            AncestorIds = ancestorIds,
            IncludeItemTypes = includeItemTypes,
            ExcludeItemTypes = excludeItemTypes,
            SearchTerm = searchTerm,
            ExcludeItemIds = excludeItemIds
        });

        var returnItems = _dtoService.GetBaseItemDtos(itemsResult.Items, dtoOptions, user);

        return new QueryResult<BaseItemDto>(
            startIndex,
            itemsResult.TotalRecordCount,
            returnItems);
    }

    /// <summary>
    /// Gets items based on a query.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="limit">The item limit.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
    /// <param name="mediaTypes">Optional. Filter by MediaType. Allows multiple, comma delimited.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="excludeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
    /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on the item type. This allows multiple, comma delimited.</param>
    /// <param name="enableTotalRecordCount">Optional. Enable the total record count.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="excludeActiveSessions">Optional. Whether to exclude the currently active sessions.</param>
    /// <response code="200">Items returned.</response>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the items that are resumable.</returns>
    [HttpGet("Users/{userId}/Items/Resume")]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetResumeItemsLegacy(
        [FromRoute, Required] Guid userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] MediaType[] mediaTypes,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery] bool enableTotalRecordCount = true,
        [FromQuery] bool? enableImages = true,
        [FromQuery] bool excludeActiveSessions = false)
    => GetResumeItems(
        userId,
        startIndex,
        limit,
        searchTerm,
        parentId,
        fields,
        mediaTypes,
        enableUserData,
        imageTypeLimit,
        enableImageTypes,
        excludeItemTypes,
        includeItemTypes,
        enableTotalRecordCount,
        enableImages,
        excludeActiveSessions);

    /// <summary>
    /// Get Item User Data.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="itemId">The item id.</param>
    /// <response code="200">return item user data.</response>
    /// <response code="404">Item is not found.</response>
    /// <returns>Return <see cref="UserItemDataDto"/>.</returns>
    [HttpGet("UserItems/{itemId}/UserData")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserItemDataDto?> GetItemUserData(
        [FromQuery] Guid? userId,
        [FromRoute, Required] Guid itemId)
    {
        var requestUserId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(requestUserId);
        if (user is null)
        {
            return NotFound();
        }

        if (!RequestHelpers.AssertCanUpdateUser(User, user, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to view this item user data.");
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        return _userDataRepository.GetUserDataDto(item, user);
    }

    /// <summary>
    /// Get Item User Data.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="itemId">The item id.</param>
    /// <response code="200">return item user data.</response>
    /// <response code="404">Item is not found.</response>
    /// <returns>Return <see cref="UserItemDataDto"/>.</returns>
    [HttpGet("Users/{userId}/Items/{itemId}/UserData")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<UserItemDataDto?> GetItemUserDataLegacy(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] Guid itemId)
        => GetItemUserData(userId, itemId);

    /// <summary>
    /// Update Item User Data.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="userDataDto">New user data object.</param>
    /// <response code="200">return updated user item data.</response>
    /// <response code="404">Item is not found.</response>
    /// <returns>Return <see cref="UserItemDataDto"/>.</returns>
    [HttpPost("UserItems/{itemId}/UserData")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserItemDataDto?> UpdateItemUserData(
        [FromQuery] Guid? userId,
        [FromRoute, Required] Guid itemId,
        [FromBody, Required] UpdateUserItemDataDto userDataDto)
    {
        var requestUserId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(requestUserId);
        if (user is null)
        {
            return NotFound();
        }

        if (!RequestHelpers.AssertCanUpdateUser(User, user, true))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "User is not allowed to update this item user data.");
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        _userDataRepository.SaveUserData(user, item, userDataDto, UserDataSaveReason.UpdateUserData);

        return _userDataRepository.GetUserDataDto(item, user);
    }

    /// <summary>
    /// Update Item User Data.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="userDataDto">New user data object.</param>
    /// <response code="200">return updated user item data.</response>
    /// <response code="404">Item is not found.</response>
    /// <returns>Return <see cref="UserItemDataDto"/>.</returns>
    [HttpPost("Users/{userId}/Items/{itemId}/UserData")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<UserItemDataDto?> UpdateItemUserDataLegacy(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] Guid itemId,
        [FromBody, Required] UpdateUserItemDataDto userDataDto)
        => UpdateItemUserData(userId, itemId, userDataDto);
}
