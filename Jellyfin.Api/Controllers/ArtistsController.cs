using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The artists controller.
/// </summary>
[Route("Artists")]
[Authorize]
public class ArtistsController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistsController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    public ArtistsController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDtoService dtoService)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
    }

    /// <summary>
    /// Gets all artists from a given item, folder, or the entire library.
    /// </summary>
    /// <param name="minCommunityRating">Optional filter by minimum community rating.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="searchTerm">Optional. Search term.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="excludeItemTypes">Optional. If specified, results will be filtered out based on item type. This allows multiple, comma delimited.</param>
    /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
    /// <param name="filters">Optional. Specify additional filters to apply.</param>
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
    /// <param name="personIds">Optional. If specified, results will be filtered to include only those containing the specified person ids.</param>
    /// <param name="personTypes">Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited.</param>
    /// <param name="studios">Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimited.</param>
    /// <param name="studioIds">Optional. If specified, results will be filtered based on studio id. This allows multiple, pipe delimited.</param>
    /// <param name="userId">User id.</param>
    /// <param name="nameStartsWithOrGreater">Optional filter by items whose name is sorted equally or greater than a given input string.</param>
    /// <param name="nameStartsWith">Optional filter by items whose name is sorted equally than a given input string.</param>
    /// <param name="nameLessThan">Optional filter by items whose name is equally or lesser than a given input string.</param>
    /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited.</param>
    /// <param name="sortOrder">Sort Order - Ascending,Descending.</param>
    /// <param name="enableImages">Optional, include image information in output.</param>
    /// <param name="enableTotalRecordCount">Total record count.</param>
    /// <response code="200">Artists returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the artists.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetArtists(
        [FromQuery] double? minCommunityRating,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFilter[] filters,
        [FromQuery] bool? isFavorite,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] MediaType[] mediaTypes,
        [FromQuery, ModelBinder(typeof(PipeDelimitedCollectionModelBinder))] string[] genres,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] genreIds,
        [FromQuery, ModelBinder(typeof(PipeDelimitedCollectionModelBinder))] string[] officialRatings,
        [FromQuery, ModelBinder(typeof(PipeDelimitedCollectionModelBinder))] string[] tags,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] int[] years,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] string? person,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] personIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] string[] personTypes,
        [FromQuery, ModelBinder(typeof(PipeDelimitedCollectionModelBinder))] string[] studios,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] studioIds,
        [FromQuery] Guid? userId,
        [FromQuery] string? nameStartsWithOrGreater,
        [FromQuery] string? nameStartsWith,
        [FromQuery] string? nameLessThan,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemSortBy[] sortBy,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] SortOrder[] sortOrder,
        [FromQuery] bool? enableImages = true,
        [FromQuery] bool enableTotalRecordCount = true)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        User? user = null;
        BaseItem parentItem = _libraryManager.GetParentItem(parentId, userId);

        if (!userId.IsNullOrEmpty())
        {
            user = _userManager.GetUserById(userId.Value);
        }

        var query = new InternalItemsQuery(user)
        {
            ExcludeItemTypes = excludeItemTypes,
            IncludeItemTypes = includeItemTypes,
            MediaTypes = mediaTypes,
            StartIndex = startIndex,
            Limit = limit,
            IsFavorite = isFavorite,
            NameLessThan = nameLessThan,
            NameStartsWith = nameStartsWith,
            NameStartsWithOrGreater = nameStartsWithOrGreater,
            Tags = tags,
            OfficialRatings = officialRatings,
            Genres = genres,
            GenreIds = genreIds,
            StudioIds = studioIds,
            Person = person,
            PersonIds = personIds,
            PersonTypes = personTypes,
            Years = years,
            MinCommunityRating = minCommunityRating,
            DtoOptions = dtoOptions,
            SearchTerm = searchTerm,
            EnableTotalRecordCount = enableTotalRecordCount,
            OrderBy = RequestHelpers.GetOrderBy(sortBy, sortOrder)
        };

        if (parentId.HasValue)
        {
            if (parentItem is Folder)
            {
                query.AncestorIds = new[] { parentId.Value };
            }
            else
            {
                query.ItemIds = new[] { parentId.Value };
            }
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

        var result = _libraryManager.GetArtists(query);

        var dtos = result.Items.Select(i =>
        {
            var (baseItem, itemCounts) = i;
            var dto = _dtoService.GetItemByNameDto(baseItem, dtoOptions, null, user);

            if (includeItemTypes.Length != 0)
            {
                dto.ChildCount = itemCounts.ItemCount;
                dto.ProgramCount = itemCounts.ProgramCount;
                dto.SeriesCount = itemCounts.SeriesCount;
                dto.EpisodeCount = itemCounts.EpisodeCount;
                dto.MovieCount = itemCounts.MovieCount;
                dto.TrailerCount = itemCounts.TrailerCount;
                dto.AlbumCount = itemCounts.AlbumCount;
                dto.SongCount = itemCounts.SongCount;
                dto.ArtistCount = itemCounts.ArtistCount;
            }

            return dto;
        });

        return new QueryResult<BaseItemDto>(
            query.StartIndex,
            result.TotalRecordCount,
            dtos.ToArray());
    }

    /// <summary>
    /// Gets all album artists from a given item, folder, or the entire library.
    /// </summary>
    /// <param name="minCommunityRating">Optional filter by minimum community rating.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="searchTerm">Optional. Search term.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="excludeItemTypes">Optional. If specified, results will be filtered out based on item type. This allows multiple, comma delimited.</param>
    /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
    /// <param name="filters">Optional. Specify additional filters to apply.</param>
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
    /// <param name="personIds">Optional. If specified, results will be filtered to include only those containing the specified person ids.</param>
    /// <param name="personTypes">Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited.</param>
    /// <param name="studios">Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimited.</param>
    /// <param name="studioIds">Optional. If specified, results will be filtered based on studio id. This allows multiple, pipe delimited.</param>
    /// <param name="userId">User id.</param>
    /// <param name="nameStartsWithOrGreater">Optional filter by items whose name is sorted equally or greater than a given input string.</param>
    /// <param name="nameStartsWith">Optional filter by items whose name is sorted equally than a given input string.</param>
    /// <param name="nameLessThan">Optional filter by items whose name is equally or lesser than a given input string.</param>
    /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited.</param>
    /// <param name="sortOrder">Sort Order - Ascending,Descending.</param>
    /// <param name="enableImages">Optional, include image information in output.</param>
    /// <param name="enableTotalRecordCount">Total record count.</param>
    /// <response code="200">Album artists returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the album artists.</returns>
    [HttpGet("AlbumArtists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetAlbumArtists(
        [FromQuery] double? minCommunityRating,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFilter[] filters,
        [FromQuery] bool? isFavorite,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] MediaType[] mediaTypes,
        [FromQuery, ModelBinder(typeof(PipeDelimitedCollectionModelBinder))] string[] genres,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] genreIds,
        [FromQuery, ModelBinder(typeof(PipeDelimitedCollectionModelBinder))] string[] officialRatings,
        [FromQuery, ModelBinder(typeof(PipeDelimitedCollectionModelBinder))] string[] tags,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] int[] years,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] string? person,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] personIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] string[] personTypes,
        [FromQuery, ModelBinder(typeof(PipeDelimitedCollectionModelBinder))] string[] studios,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] studioIds,
        [FromQuery] Guid? userId,
        [FromQuery] string? nameStartsWithOrGreater,
        [FromQuery] string? nameStartsWith,
        [FromQuery] string? nameLessThan,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemSortBy[] sortBy,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] SortOrder[] sortOrder,
        [FromQuery] bool? enableImages = true,
        [FromQuery] bool enableTotalRecordCount = true)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        User? user = null;
        BaseItem parentItem = _libraryManager.GetParentItem(parentId, userId);

        if (!userId.IsNullOrEmpty())
        {
            user = _userManager.GetUserById(userId.Value);
        }

        var query = new InternalItemsQuery(user)
        {
            ExcludeItemTypes = excludeItemTypes,
            IncludeItemTypes = includeItemTypes,
            MediaTypes = mediaTypes,
            StartIndex = startIndex,
            Limit = limit,
            IsFavorite = isFavorite,
            NameLessThan = nameLessThan,
            NameStartsWith = nameStartsWith,
            NameStartsWithOrGreater = nameStartsWithOrGreater,
            Tags = tags,
            OfficialRatings = officialRatings,
            Genres = genres,
            GenreIds = genreIds,
            StudioIds = studioIds,
            Person = person,
            PersonIds = personIds,
            PersonTypes = personTypes,
            Years = years,
            MinCommunityRating = minCommunityRating,
            DtoOptions = dtoOptions,
            SearchTerm = searchTerm,
            EnableTotalRecordCount = enableTotalRecordCount,
            OrderBy = RequestHelpers.GetOrderBy(sortBy, sortOrder)
        };

        if (parentId.HasValue)
        {
            if (parentItem is Folder)
            {
                query.AncestorIds = new[] { parentId.Value };
            }
            else
            {
                query.ItemIds = new[] { parentId.Value };
            }
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

        var result = _libraryManager.GetAlbumArtists(query);

        var dtos = result.Items.Select(i =>
        {
            var (baseItem, itemCounts) = i;
            var dto = _dtoService.GetItemByNameDto(baseItem, dtoOptions, null, user);

            if (includeItemTypes.Length != 0)
            {
                dto.ChildCount = itemCounts.ItemCount;
                dto.ProgramCount = itemCounts.ProgramCount;
                dto.SeriesCount = itemCounts.SeriesCount;
                dto.EpisodeCount = itemCounts.EpisodeCount;
                dto.MovieCount = itemCounts.MovieCount;
                dto.TrailerCount = itemCounts.TrailerCount;
                dto.AlbumCount = itemCounts.AlbumCount;
                dto.SongCount = itemCounts.SongCount;
                dto.ArtistCount = itemCounts.ArtistCount;
            }

            return dto;
        });

        return new QueryResult<BaseItemDto>(
            query.StartIndex,
            result.TotalRecordCount,
            dtos.ToArray());
    }

    /// <summary>
    /// Gets an artist by name.
    /// </summary>
    /// <param name="name">Studio name.</param>
    /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
    /// <response code="200">Artist returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the artist.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<BaseItemDto> GetArtistByName([FromRoute, Required] string name, [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions().AddClientFields(User);

        var item = _libraryManager.GetArtist(name, dtoOptions);

        if (!userId.IsNullOrEmpty())
        {
            var user = _userManager.GetUserById(userId.Value);

            return _dtoService.GetBaseItemDto(item, dtoOptions, user);
        }

        return _dtoService.GetBaseItemDto(item, dtoOptions);
    }
}
