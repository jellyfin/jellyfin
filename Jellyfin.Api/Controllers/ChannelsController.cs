#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
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

namespace Jellyfin.Api.Controllers
{
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
        /// <param name="userId">User Id.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="supportsLatestItems">Optional. Filter by channels that support getting latest items.</param>
        /// <param name="supportsMediaDeletion">Optional. Filter by channels that support media deletion.</param>
        /// <param name="isFavorite">Optional. Filter by channels that are favorite.</param>
        /// <response code="200">Channels returned.</response>
        /// <returns>Channels.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetChannels(
            [FromQuery] Guid userId,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] bool? supportsLatestItems,
            [FromQuery] bool? supportsMediaDeletion,
            [FromQuery] bool? isFavorite)
        {
            return _channelManager.GetChannels(new ChannelQuery
            {
                Limit = limit,
                StartIndex = startIndex,
                UserId = userId,
                SupportsLatestItems = supportsLatestItems,
                SupportsMediaDeletion = supportsMediaDeletion,
                IsFavorite = isFavorite
            });
        }

        /// <summary>
        /// Get all channel features.
        /// </summary>
        /// <response code="200">All channel features returned.</response>
        /// <returns>Channel features.</returns>
        [HttpGet("Features")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<ChannelFeatures> GetAllChannelFeatures()
        {
            return _channelManager.GetAllChannelFeatures();
        }

        /// <summary>
        /// Get channel features.
        /// </summary>
        /// <param name="id">Channel id.</param>
        /// <response code="200">Channel features returned.</response>
        /// <returns>Channel features.</returns>
        [HttpGet("{Id}/Features")]
        public ActionResult<ChannelFeatures> GetChannelFeatures([FromRoute] string id)
        {
            return _channelManager.GetChannelFeatures(id);
        }

        /// <summary>
        /// Get channel items.
        /// </summary>
        /// <param name="id">Channel Id.</param>
        /// <param name="folderId">Folder Id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="sortOrder">Sort Order - Ascending,Descending.</param>
        /// <param name="filters">Optional. Specify additional filters to apply. This allows multiple, comma delimited. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes.</param>
        /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
        /// <response code="200">Channel items returned.</response>
        /// <returns>Channel items.</returns>
        [HttpGet("{Id}/Items")]
        public async Task<ActionResult<QueryResult<BaseItemDto>>> GetChannelItems(
            [FromRoute] Guid id,
            [FromQuery] Guid? folderId,
            [FromQuery] Guid? userId,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string sortOrder,
            [FromQuery] string filters,
            [FromQuery] string sortBy,
            [FromQuery] string fields)
        {
            var user = userId == null
                ? null
                : _userManager.GetUserById(userId.Value);

            var query = new InternalItemsQuery(user)
            {
                Limit = limit,
                StartIndex = startIndex,
                ChannelIds = new[] { id },
                ParentId = folderId ?? Guid.Empty,
                OrderBy = RequestExtensions.GetOrderBy(sortBy, sortOrder),
                DtoOptions = new DtoOptions { Fields = RequestExtensions.GetItemFields(fields) }
            };

            foreach (var filter in RequestExtensions.GetFilters(filters))
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
        /// <param name="userId">User Id.</param>
        /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
        /// <param name="limit">Optional. The maximum number of records to return.</param>
        /// <param name="filters">Optional. Specify additional filters to apply. This allows multiple, comma delimited. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimited. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines.</param>
        /// <param name="channelIds">Optional. Specify one or more channel id's, comma delimited.</param>
        /// <response code="200">Latest channel items returned.</response>
        /// <returns>Latest channel items.</returns>
        [HttpGet("Items/Latest")]
        public async Task<ActionResult<QueryResult<BaseItemDto>>> GetLatestChannelItems(
            [FromQuery] Guid? userId,
            [FromQuery] int? startIndex,
            [FromQuery] int? limit,
            [FromQuery] string filters,
            [FromQuery] string fields,
            [FromQuery] string channelIds)
        {
            var user = userId == null
                ? null
                : _userManager.GetUserById(userId.Value);

            var query = new InternalItemsQuery(user)
            {
                Limit = limit,
                StartIndex = startIndex,
                ChannelIds =
                    (channelIds ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => new Guid(i)).ToArray(),
                DtoOptions = new DtoOptions { Fields = RequestExtensions.GetItemFields(fields) }
            };

            foreach (var filter in RequestExtensions.GetFilters(filters))
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
}
