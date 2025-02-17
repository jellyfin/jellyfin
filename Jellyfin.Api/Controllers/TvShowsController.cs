using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The tv shows controller.
/// </summary>
[Route("Shows")]
[Authorize]
public class TvShowsController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;
    private readonly ITVSeriesManager _tvSeriesManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TvShowsController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="tvSeriesManager">Instance of the <see cref="ITVSeriesManager"/> interface.</param>
    public TvShowsController(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        ITVSeriesManager tvSeriesManager)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _tvSeriesManager = tvSeriesManager;
    }

    /// <summary>
    /// Gets a list of next up episodes.
    /// </summary>
    /// <param name="userId">The user id of the user to get the next up episodes for.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="seriesId">Optional. Filter by series id.</param>
    /// <param name="parentId">Optional. Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="nextUpDateCutoff">Optional. Starting date of shows to show in Next Up section.</param>
    /// <param name="enableTotalRecordCount">Whether to enable the total records count. Defaults to true.</param>
    /// <param name="disableFirstEpisode">Whether to disable sending the first episode in a series as next up.</param>
    /// <param name="enableResumable">Whether to include resumable episodes in next up results.</param>
    /// <param name="enableRewatching">Whether to include watched episodes in next up results.</param>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the next up episodes.</returns>
    [HttpGet("NextUp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetNextUp(
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] Guid? seriesId,
        [FromQuery] Guid? parentId,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] bool? enableUserData,
        [FromQuery] DateTime? nextUpDateCutoff,
        [FromQuery] bool enableTotalRecordCount = true,
        [FromQuery] bool disableFirstEpisode = false,
        [FromQuery] bool enableResumable = true,
        [FromQuery] bool enableRewatching = false)
    {
        var user = _userManager.GetUserById(RequestHelpers.GetUserId(User, userId));
        if (user is null)
        {
            return NotFound();
        }

        var options = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var result = _tvSeriesManager.GetNextUp(
            new NextUpQuery
            {
                Limit = limit,
                ParentId = parentId,
                SeriesId = seriesId,
                StartIndex = startIndex,
                User = user,
                EnableTotalRecordCount = enableTotalRecordCount,
                DisableFirstEpisode = disableFirstEpisode,
                NextUpDateCutoff = nextUpDateCutoff ?? DateTime.MinValue,
                EnableResumable = enableResumable,
                EnableRewatching = enableRewatching
            },
            options);

        var returnItems = _dtoService.GetBaseItemDtos(result.Items, options, user);

        return new QueryResult<BaseItemDto>(
            startIndex,
            result.TotalRecordCount,
            returnItems);
    }

    /// <summary>
    /// Gets a list of upcoming episodes.
    /// </summary>
    /// <param name="userId">The user id of the user to get the upcoming episodes for.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="parentId">Optional. Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the upcoming episodes.</returns>
    [HttpGet("Upcoming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetUpcomingEpisodes(
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] Guid? parentId,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] bool? enableUserData)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var minPremiereDate = DateTime.UtcNow.Date.AddDays(-1);

        var parentIdGuid = parentId ?? Guid.Empty;

        var options = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var itemsResult = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { BaseItemKind.Episode },
            OrderBy = new[] { (ItemSortBy.PremiereDate, SortOrder.Ascending), (ItemSortBy.SortName, SortOrder.Ascending) },
            MinPremiereDate = minPremiereDate,
            StartIndex = startIndex,
            Limit = limit,
            ParentId = parentIdGuid,
            Recursive = true,
            DtoOptions = options
        });

        var returnItems = _dtoService.GetBaseItemDtos(itemsResult, options, user);

        return new QueryResult<BaseItemDto>(
            startIndex,
            itemsResult.Count,
            returnItems);
    }

    /// <summary>
    /// Gets episodes for a tv season.
    /// </summary>
    /// <param name="seriesId">The series id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls.</param>
    /// <param name="season">Optional filter by season number.</param>
    /// <param name="seasonId">Optional. Filter by season id.</param>
    /// <param name="isMissing">Optional. Filter by items that are missing episodes or not.</param>
    /// <param name="adjacentTo">Optional. Return items that are siblings of a supplied item.</param>
    /// <param name="startItemId">Optional. Skip through the list until a given item is found.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="enableImages">Optional, include image information in output.</param>
    /// <param name="imageTypeLimit">Optional, the max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime.</param>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the episodes on success or a <see cref="NotFoundResult"/> if the series was not found.</returns>
    [HttpGet("{seriesId}/Episodes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueryResult<BaseItemDto>> GetEpisodes(
        [FromRoute, Required] Guid seriesId,
        [FromQuery] Guid? userId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] int? season,
        [FromQuery] Guid? seasonId,
        [FromQuery] bool? isMissing,
        [FromQuery] Guid? adjacentTo,
        [FromQuery] Guid? startItemId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] bool? enableUserData,
        [FromQuery] ItemSortBy? sortBy)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        List<BaseItem> episodes;

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);
        var shouldIncludeMissingEpisodes = (user is not null && user.DisplayMissingEpisodes) || User.GetIsApiKey();

        if (seasonId.HasValue) // Season id was supplied. Get episodes by season id.
        {
            var item = _libraryManager.GetItemById<BaseItem>(seasonId.Value);
            if (item is not Season seasonItem)
            {
                return NotFound("No season exists with Id " + seasonId);
            }

            episodes = seasonItem.GetEpisodes(user, dtoOptions, shouldIncludeMissingEpisodes);
        }
        else if (season.HasValue) // Season number was supplied. Get episodes by season number
        {
            var series = _libraryManager.GetItemById<Series>(seriesId);
            if (series is null)
            {
                return NotFound("Series not found");
            }

            var seasonItem = series
                .GetSeasons(user, dtoOptions)
                .FirstOrDefault(i => i.IndexNumber == season.Value);

            episodes = seasonItem is null ?
                new List<BaseItem>()
                : ((Season)seasonItem).GetEpisodes(user, dtoOptions, shouldIncludeMissingEpisodes);
        }
        else // No season number or season id was supplied. Returning all episodes.
        {
            if (_libraryManager.GetItemById<BaseItem>(seriesId) is not Series series)
            {
                return NotFound("Series not found");
            }

            episodes = series.GetEpisodes(user, dtoOptions, shouldIncludeMissingEpisodes).ToList();
        }

        // Filter after the fact in case the ui doesn't want them
        if (isMissing.HasValue)
        {
            var val = isMissing.Value;
            episodes = episodes
                .Where(i => ((Episode)i).IsMissingEpisode == val)
                .ToList();
        }

        if (startItemId.HasValue)
        {
            episodes = episodes
                .SkipWhile(i => !startItemId.Value.Equals(i.Id))
                .ToList();
        }

        // This must be the last filter
        if (!adjacentTo.IsNullOrEmpty())
        {
            episodes = UserViewBuilder.FilterForAdjacency(episodes, adjacentTo.Value).ToList();
        }

        if (sortBy == ItemSortBy.Random)
        {
            episodes.Shuffle();
        }

        var returnItems = episodes;

        if (startIndex.HasValue || limit.HasValue)
        {
            returnItems = ApplyPaging(episodes, startIndex, limit).ToList();
        }

        var dtos = _dtoService.GetBaseItemDtos(returnItems, dtoOptions, user);

        return new QueryResult<BaseItemDto>(
            startIndex,
            episodes.Count,
            dtos);
    }

    /// <summary>
    /// Gets seasons for a tv series.
    /// </summary>
    /// <param name="seriesId">The series id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls.</param>
    /// <param name="isSpecialSeason">Optional. Filter by special season.</param>
    /// <param name="isMissing">Optional. Filter by items that are missing episodes or not.</param>
    /// <param name="adjacentTo">Optional. Return items that are siblings of a supplied item.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> on success or a <see cref="NotFoundResult"/> if the series was not found.</returns>
    [HttpGet("{seriesId}/Seasons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueryResult<BaseItemDto>> GetSeasons(
        [FromRoute, Required] Guid seriesId,
        [FromQuery] Guid? userId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] bool? isSpecialSeason,
        [FromQuery] bool? isMissing,
        [FromQuery] Guid? adjacentTo,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] bool? enableUserData)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var item = _libraryManager.GetItemById<Series>(seriesId, user);
        if (item is null)
        {
            return NotFound();
        }

        var seasons = item.GetItemList(new InternalItemsQuery(user)
        {
            IsMissing = isMissing,
            IsSpecialSeason = isSpecialSeason,
            AdjacentTo = adjacentTo
        });

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var returnItems = _dtoService.GetBaseItemDtos(seasons, dtoOptions, user);

        return new QueryResult<BaseItemDto>(returnItems);
    }

    /// <summary>
    /// Applies the paging.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="limit">The limit.</param>
    /// <returns>IEnumerable{BaseItem}.</returns>
    private IEnumerable<BaseItem> ApplyPaging(IEnumerable<BaseItem> items, int? startIndex, int? limit)
    {
        // Start at
        if (startIndex.HasValue)
        {
            items = items.Skip(startIndex.Value);
        }

        // Return limit
        if (limit.HasValue)
        {
            items = items.Take(limit.Value);
        }

        return items;
    }
}
