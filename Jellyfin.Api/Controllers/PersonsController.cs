﻿using System;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Entities;
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
    /// Persons controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class PersonsController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonsController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        public PersonsController(
            ILibraryManager libraryManager,
            IDtoService dtoService,
            IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets all persons from a given item, folder, or the entire library.
        /// </summary>
        /// <param name="minCommunityRating">Optional filter by minimum community rating.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
        /// <param name="excludeItemTypes">Optional. If specified, results will be filtered out based on item type. This allows multiple, comma delimited.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be filtered in based on item type. This allows multiple, comma delimited.</param>
        /// <param name="filters">Optional. Specify additional filters to apply. This allows multiple, comma delimited. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes.</param>
        /// <param name="isFavorite">Optional filter by items that are marked as favorite, or not.</param>
        /// <param name="mediaTypes">Optional filter by MediaType. Allows multiple, comma delimited.</param>
        /// <param name="genres">Optional. If specified, results will be filtered based on genre. This allows multiple, pipe delimited.</param>
        /// <param name="genreIds">Optional. If specified, results will be filtered based on genre id. This allows multiple, pipe delimited.</param>
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
        /// <param name="studioIds">Optional. If specified, results will be filtered based on studio id. This allows multiple, pipe delimited.</param>
        /// <param name="userId">User id.</param>
        /// <param name="nameStartsWithOrGreater">Optional filter by items whose name is sorted equally or greater than a given input string.</param>
        /// <param name="nameStartsWith">Optional filter by items whose name is sorted equally than a given input string.</param>
        /// <param name="nameLessThan">Optional filter by items whose name is equally or lesser than a given input string.</param>
        /// <param name="enableImages">Optional, include image information in output.</param>
        /// <param name="enableTotalRecordCount">Optional. Include total record count.</param>
        /// <response code="200">Persons returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the queryresult of persons.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetPersons(
            [FromQuery] double? minCommunityRating,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string? searchTerm,
            [FromQuery] string? parentId,
            [FromQuery] string? fields,
            [FromQuery] string? excludeItemTypes,
            [FromQuery] string? includeItemTypes,
            [FromQuery] string? filters,
            [FromQuery] bool? isFavorite,
            [FromQuery] string? mediaTypes,
            [FromQuery] string? genres,
            [FromQuery] string? genreIds,
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
            [FromQuery] string? studioIds,
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
                .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

            User? user = null;
            BaseItem parentItem;

            if (userId.HasValue && !userId.Equals(Guid.Empty))
            {
                user = _userManager.GetUserById(userId.Value);
                parentItem = string.IsNullOrEmpty(parentId) ? _libraryManager.GetUserRootFolder() : _libraryManager.GetItemById(parentId);
            }
            else
            {
                parentItem = string.IsNullOrEmpty(parentId) ? _libraryManager.RootFolder : _libraryManager.GetItemById(parentId);
            }

            var query = new InternalItemsQuery(user)
            {
                ExcludeItemTypes = RequestHelpers.Split(excludeItemTypes, ',', true),
                IncludeItemTypes = RequestHelpers.Split(includeItemTypes, ',', true),
                MediaTypes = RequestHelpers.Split(mediaTypes, ',', true),
                StartIndex = startIndex,
                Limit = limit,
                IsFavorite = isFavorite,
                NameLessThan = nameLessThan,
                NameStartsWith = nameStartsWith,
                NameStartsWithOrGreater = nameStartsWithOrGreater,
                Tags = RequestHelpers.Split(tags, '|', true),
                OfficialRatings = RequestHelpers.Split(officialRatings, '|', true),
                Genres = RequestHelpers.Split(genres, '|', true),
                GenreIds = RequestHelpers.GetGuids(genreIds),
                StudioIds = RequestHelpers.GetGuids(studioIds),
                Person = person,
                PersonIds = RequestHelpers.GetGuids(personIds),
                PersonTypes = RequestHelpers.Split(personTypes, ',', true),
                Years = RequestHelpers.Split(years, ',', true).Select(y => Convert.ToInt32(y, CultureInfo.InvariantCulture)).ToArray(),
                MinCommunityRating = minCommunityRating,
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

            // Studios
            if (!string.IsNullOrEmpty(studios))
            {
                query.StudioIds = studios.Split('|')
                    .Select(i =>
                    {
                        try
                        {
                            return _libraryManager.GetStudio(i);
                        }
                        catch
                        {
                            return null;
                        }
                    }).Where(i => i != null)
                    .Select(i => i!.Id)
                    .ToArray();
            }

            foreach (var filter in RequestHelpers.GetFilters(filters))
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

            var result = new QueryResult<(BaseItem, ItemCounts)>();

            var dtos = result.Items.Select(i =>
            {
                var (baseItem, counts) = i;
                var dto = _dtoService.GetItemByNameDto(baseItem, dtoOptions, null, user);

                if (!string.IsNullOrWhiteSpace(includeItemTypes))
                {
                    dto.ChildCount = counts.ItemCount;
                    dto.ProgramCount = counts.ProgramCount;
                    dto.SeriesCount = counts.SeriesCount;
                    dto.EpisodeCount = counts.EpisodeCount;
                    dto.MovieCount = counts.MovieCount;
                    dto.TrailerCount = counts.TrailerCount;
                    dto.AlbumCount = counts.AlbumCount;
                    dto.SongCount = counts.SongCount;
                    dto.ArtistCount = counts.ArtistCount;
                }

                return dto;
            });

            return new QueryResult<BaseItemDto>
            {
                Items = dtos.ToArray(),
                TotalRecordCount = result.TotalRecordCount
            };
        }

        /// <summary>
        /// Get person by name.
        /// </summary>
        /// <param name="name">Person name.</param>
        /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
        /// <response code="200">Person returned.</response>
        /// <response code="404">Person not found.</response>
        /// <returns>An <see cref="OkResult"/> containing the person on success,
        /// or a <see cref="NotFoundResult"/> if person not found.</returns>
        [HttpGet("{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<BaseItemDto> GetPerson([FromRoute] string name, [FromQuery] Guid? userId)
        {
            var dtoOptions = new DtoOptions()
                .AddClientFields(Request);

            var item = _libraryManager.GetPerson(name);
            if (item == null)
            {
                return NotFound();
            }

            if (userId.HasValue && !userId.Equals(Guid.Empty))
            {
                var user = _userManager.GetUserById(userId.Value);
                return _dtoService.GetBaseItemDto(item, dtoOptions, user);
            }

            return _dtoService.GetBaseItemDto(item, dtoOptions);
        }
    }
}
