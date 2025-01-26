using System;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The trailers controller.
/// </summary>
[Authorize]
public class TrailersController : BaseJellyfinApiController
{
    private readonly ItemsController _itemsController;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrailersController"/> class.
    /// </summary>
    /// <param name="itemsController">Instance of <see cref="ItemsController"/>.</param>
    public TrailersController(ItemsController itemsController)
    {
        _itemsController = itemsController;
    }

    /// <summary>
    /// Finds movies and trailers similar to a given trailer.
    /// </summary>
    /// <param name="userId">The user id supplied as query parameter; this is required when not using an API key.</param>
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
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the trailers.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetTrailers(
        [FromQuery] Guid? userId,
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
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] studios,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] artists,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] excludeArtistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] artistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] albumArtistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] contributingArtistIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] albums,
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
        var includeItemTypes = new[] { BaseItemKind.Trailer };

        return _itemsController
            .GetItems(
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
    }
}
