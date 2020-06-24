using System;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    public class YearsController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;

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
        /// <param name="isHd"></param>
        /// <param name="is4k"></param>
        /// <param name="locationTypes"></param>
        /// <param name="excludeLocationTypes"></param>
        /// <param name="isMissing"></param>
        /// <param name="isUnaired"></param>
        /// <param name="minCommunityRating"></param>
        /// <param name="minCriticRating"></param>
        /// <param name="airedDuringSeason"></param>
        /// <param name="minPremiereDate"></param>
        /// <param name="minDateLastSaved"></param>
        /// <param name="minDateLastSavedForUser"></param>
        /// <param name="maxPremiereDate"></param>
        /// <param name="hasOverview"></param>
        /// <param name="hasImdbId"></param>
        /// <param name="hasTmdbId"></param>
        /// <param name="hasTvdbId"></param>
        /// <param name="excludeItemIds"></param>
        /// <param name="startIndex"></param>
        /// <param name="limit"></param>
        /// <param name="searchTerm"></param>
        /// <param name="sortOrder"></param>
        /// <param name="parentId"></param>
        /// <param name="fields"></param>
        /// <param name="excludeItemTypes"></param>
        /// <param name="includeItemTypes"></param>
        /// <param name="filters"></param>
        /// <param name="isFavorite"></param>
        /// <param name="mediaTypes"></param>
        /// <param name="imageTypes"></param>
        /// <param name="sortBy"></param>
        /// <param name="isPlayed"></param>
        /// <param name="genres"></param>
        /// <param name="genreIds"></param>
        /// <param name="officialRatings"></param>
        /// <param name="tags"></param>
        /// <param name="years"></param>
        /// <param name="enableUserData"></param>
        /// <param name="imageTypeLimit"></param>
        /// <param name="enableImageTypes"></param>
        /// <param name="person"></param>
        /// <param name="personIds"></param>
        /// <param name="personTypes"></param>
        /// <param name="studios"></param>
        /// <param name="studioIds"></param>
        /// <param name="artists"></param>
        /// <param name="excludeArtistIds"></param>
        /// <param name="artistIds"></param>
        /// <param name="albumArtistIds"></param>
        /// <param name="contributingArtistIds"></param>
        /// <param name="albums"></param>
        /// <param name="albumIds"></param>
        /// <param name="ids"></param>
        /// <param name="videoTypes"></param>
        /// <param name="userId"></param>
        /// <param name="minOfficialRating"></param>
        /// <param name="isLocked"></param>
        /// <param name="isPlaceholder"></param>
        /// <param name="hasOfficialRating"></param>
        /// <param name="collapseBoxSetItems"></param>
        /// <param name="minWidth"></param>
        /// <param name="minHeight"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <param name="is3d"></param>
        /// <param name="seriesStatus"></param>
        /// <param name="nameStartsWithOrGreater"></param>
        /// <param name="nameStartsWith"></param>
        /// <param name="nameLessThan"></param>
        /// <param name="recursive"></param>
        /// <param name="enableImages"></param>
        /// <param name="enableTotalRecordCount"></param>
        /// <returns></returns>
        [HttpGet]
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
    }
}
