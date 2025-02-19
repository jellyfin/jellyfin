using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Channels Controller.
/// </summary>
[Authorize]
public class ChannelsController : BaseJellyfinApiController
{
    private readonly IChannelManager _channelManager;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelsController"/> class.
    /// </summary>
    /// <param name="channelManager">Instance of the <see cref="IChannelManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public ChannelsController(IChannelManager channelManager, IUserManager userManager)
    {
        _channelManager = channelManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets available channels.
    /// </summary>
    /// <param name="userId">User Id to filter by. Use <see cref="Guid.Empty"/> to not filter by user.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="supportsLatestItems">Optional. Filter by channels that support getting latest items.</param>
    /// <param name="supportsMediaDeletion">Optional. Filter by channels that support media deletion.</param>
    /// <param name="isFavorite">Optional. Filter by channels that are favorite.</param>
    /// <response code="200">Channels returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the channels.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetChannels(
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] bool? supportsLatestItems,
        [FromQuery] bool? supportsMediaDeletion,
        [FromQuery] bool? isFavorite)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        return await _channelManager.GetChannelsAsync(new ChannelQuery
        {
            Limit = limit,
            StartIndex = startIndex,
            UserId = userId.Value,
            SupportsLatestItems = supportsLatestItems,
            SupportsMediaDeletion = supportsMediaDeletion,
            IsFavorite = isFavorite
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Get all channel features.
    /// </summary>
    /// <response code="200">All channel features returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the channel features.</returns>
    [HttpGet("Features")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ChannelFeatures>> GetAllChannelFeatures()
    {
        return _channelManager.GetAllChannelFeatures();
    }

    /// <summary>
    /// Get channel features.
    /// </summary>
    /// <param name="channelId">Channel id.</param>
    /// <response code="200">Channel features returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the channel features.</returns>
    [HttpGet("{channelId}/Features")]
    public ActionResult<ChannelFeatures> GetChannelFeatures([FromRoute, Required] Guid channelId)
    {
        return _channelManager.GetChannelFeatures(channelId);
    }

    /// <summary>
    /// Get channel items.
    /// </summary>
    /// <param name="channelId">Channel Id.</param>
    /// <param name="folderId">Optional. Folder Id.</param>
    /// <param name="userId">Optional. User Id.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="sortOrder">Optional. Sort Order - Ascending,Descending.</param>
    /// <param name="filters">Optional. Specify additional filters to apply.</param>
    /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <response code="200">Channel items returned.</response>
    /// <returns>
    /// A <see cref="Task"/> representing the request to get the channel items.
    /// The task result contains an <see cref="OkResult"/> containing the channel items.
    /// </returns>
    [HttpGet("{channelId}/Items")]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetChannelItems(
        [FromRoute, Required] Guid channelId,
        [FromQuery] Guid? folderId,
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] SortOrder[] sortOrder,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFilter[] filters,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemSortBy[] sortBy,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var query = new InternalItemsQuery(user)
        {
            Limit = limit,
            StartIndex = startIndex,
            ChannelIds = new[] { channelId },
            ParentId = folderId ?? Guid.Empty,
            OrderBy = RequestHelpers.GetOrderBy(sortBy, sortOrder),
            DtoOptions = new DtoOptions { Fields = fields }
        };

        foreach (var filter in filters)
        {
            switch (filter)
            {
                case ItemFilter.IsFolder:
                    query.IsFolder = true;
                    break;
                case ItemFilter.IsNotFolder:
                    query.IsFolder = false;
                    break;
                case ItemFilter.IsUnplayed:
                    query.IsPlayed = false;
                    break;
                case ItemFilter.IsPlayed:
                    query.IsPlayed = true;
                    break;
                case ItemFilter.IsFavorite:
                    query.IsFavorite = true;
                    break;
                case ItemFilter.IsResumable:
                    query.IsResumable = true;
                    break;
                case ItemFilter.Likes:
                    query.IsLiked = true;
                    break;
                case ItemFilter.Dislikes:
                    query.IsLiked = false;
                    break;
                case ItemFilter.IsFavoriteOrLikes:
                    query.IsFavoriteOrLiked = true;
                    break;
            }
        }

        return await _channelManager.GetChannelItems(query, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets latest channel items.
    /// </summary>
    /// <param name="userId">Optional. User Id.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="filters">Optional. Specify additional filters to apply.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="channelIds">Optional. Specify one or more channel id's, comma delimited.</param>
    /// <response code="200">Latest channel items returned.</response>
    /// <returns>
    /// A <see cref="Task"/> representing the request to get the latest channel items.
    /// The task result contains an <see cref="OkResult"/> containing the latest channel items.
    /// </returns>
    [HttpGet("Items/Latest")]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetLatestChannelItems(
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFilter[] filters,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] channelIds)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var query = new InternalItemsQuery(user)
        {
            Limit = limit,
            StartIndex = startIndex,
            ChannelIds = channelIds,
            DtoOptions = new DtoOptions { Fields = fields }
        };

        foreach (var filter in filters)
        {
            switch (filter)
            {
                case ItemFilter.IsFolder:
                    query.IsFolder = true;
                    break;
                case ItemFilter.IsNotFolder:
                    query.IsFolder = false;
                    break;
                case ItemFilter.IsUnplayed:
                    query.IsPlayed = false;
                    break;
                case ItemFilter.IsPlayed:
                    query.IsPlayed = true;
                    break;
                case ItemFilter.IsFavorite:
                    query.IsFavorite = true;
                    break;
                case ItemFilter.IsResumable:
                    query.IsResumable = true;
                    break;
                case ItemFilter.Likes:
                    query.IsLiked = true;
                    break;
                case ItemFilter.Dislikes:
                    query.IsLiked = false;
                    break;
                case ItemFilter.IsFavoriteOrLikes:
                    query.IsFavoriteOrLiked = true;
                    break;
            }
        }

        return await _channelManager.GetLatestChannelItems(query, CancellationToken.None).ConfigureAwait(false);
    }
}
