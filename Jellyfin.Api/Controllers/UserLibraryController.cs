using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// User library controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class UserLibraryController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IUserViewManager _userViewManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILyricManager _lyricManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLibraryController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataRepository">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        /// <param name="userViewManager">Instance of the <see cref="IUserViewManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="lyricManager">Instance of the <see cref="ILyricManager"/> interface.</param>
        public UserLibraryController(
            IUserManager userManager,
            IUserDataManager userDataRepository,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            IUserViewManager userViewManager,
            IFileSystem fileSystem,
            ILyricManager lyricManager)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _userViewManager = userViewManager;
            _fileSystem = fileSystem;
            _lyricManager = lyricManager;
        }

        /// <summary>
        /// Gets an item from a user's library.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Item returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the d item.</returns>
        [HttpGet("Users/{userId}/Items/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<BaseItemDto>> GetItem([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            var user = _userManager.GetUserById(userId);

            var item = itemId.Equals(default)
                ? _libraryManager.GetUserRootFolder()
                : _libraryManager.GetItemById(itemId);

            await RefreshItemOnDemandIfNeeded(item).ConfigureAwait(false);

            var dtoOptions = new DtoOptions().AddClientFields(User);

            return _dtoService.GetBaseItemDto(item, dtoOptions, user);
        }

        /// <summary>
        /// Gets the root folder from a user's library.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <response code="200">Root folder returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the user's root folder.</returns>
        [HttpGet("Users/{userId}/Items/Root")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<BaseItemDto> GetRootFolder([FromRoute, Required] Guid userId)
        {
            var user = _userManager.GetUserById(userId);
            var item = _libraryManager.GetUserRootFolder();
            var dtoOptions = new DtoOptions().AddClientFields(User);
            return _dtoService.GetBaseItemDto(item, dtoOptions, user);
        }

        /// <summary>
        /// Gets intros to play before the main media item plays.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Intros returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the intros to play.</returns>
        [HttpGet("Users/{userId}/Items/{itemId}/Intros")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<QueryResult<BaseItemDto>>> GetIntros([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            var user = _userManager.GetUserById(userId);

            var item = itemId.Equals(default)
                ? _libraryManager.GetUserRootFolder()
                : _libraryManager.GetItemById(itemId);

            var items = await _libraryManager.GetIntros(item, user).ConfigureAwait(false);
            var dtoOptions = new DtoOptions().AddClientFields(User);
            var dtos = items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user)).ToArray();

            return new QueryResult<BaseItemDto>(dtos);
        }

        /// <summary>
        /// Marks an item as a favorite.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Item marked as favorite.</response>
        /// <returns>An <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>.</returns>
        [HttpPost("Users/{userId}/FavoriteItems/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<UserItemDataDto> MarkFavoriteItem([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            return MarkFavorite(userId, itemId, true);
        }

        /// <summary>
        /// Unmarks item as a favorite.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Item unmarked as favorite.</response>
        /// <returns>An <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>.</returns>
        [HttpDelete("Users/{userId}/FavoriteItems/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<UserItemDataDto> UnmarkFavoriteItem([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            return MarkFavorite(userId, itemId, false);
        }

        /// <summary>
        /// Deletes a user's saved personal rating for an item.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Personal rating removed.</response>
        /// <returns>An <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>.</returns>
        [HttpDelete("Users/{userId}/Items/{itemId}/Rating")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<UserItemDataDto> DeleteUserItemRating([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            return UpdateUserItemRatingInternal(userId, itemId, null);
        }

        /// <summary>
        /// Updates a user's rating for an item.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <param name="likes">Whether this <see cref="UpdateUserItemRating" /> is likes.</param>
        /// <response code="200">Item rating updated.</response>
        /// <returns>An <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>.</returns>
        [HttpPost("Users/{userId}/Items/{itemId}/Rating")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<UserItemDataDto> UpdateUserItemRating([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId, [FromQuery] bool? likes)
        {
            return UpdateUserItemRatingInternal(userId, itemId, likes);
        }

        /// <summary>
        /// Gets local trailers for an item.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">An <see cref="OkResult"/> containing the item's local trailers.</response>
        /// <returns>The items local trailers.</returns>
        [HttpGet("Users/{userId}/Items/{itemId}/LocalTrailers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<BaseItemDto>> GetLocalTrailers([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            var user = _userManager.GetUserById(userId);

            var item = itemId.Equals(default)
                ? _libraryManager.GetUserRootFolder()
                : _libraryManager.GetItemById(itemId);

            var dtoOptions = new DtoOptions().AddClientFields(User);

            if (item is IHasTrailers hasTrailers)
            {
                var trailers = hasTrailers.LocalTrailers;
                return Ok(_dtoService.GetBaseItemDtos(trailers, dtoOptions, user, item));
            }

            return Ok(item.GetExtras()
                .Where(e => e.ExtraType == ExtraType.Trailer)
                .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, item)));
        }

        /// <summary>
        /// Gets special features for an item.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Special features returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the special features.</returns>
        [HttpGet("Users/{userId}/Items/{itemId}/SpecialFeatures")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<BaseItemDto>> GetSpecialFeatures([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            var user = _userManager.GetUserById(userId);

            var item = itemId.Equals(default)
                ? _libraryManager.GetUserRootFolder()
                : _libraryManager.GetItemById(itemId);

            var dtoOptions = new DtoOptions().AddClientFields(User);

            return Ok(item
                .GetExtras()
                .Where(i => i.ExtraType.HasValue && BaseItem.DisplayExtraTypes.Contains(i.ExtraType.Value))
                .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, item)));
        }

        /// <summary>
        /// Gets latest media.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
        /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
        /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
        /// <param name="isPlayed">Filter by items that are played, or not.</param>
        /// <param name="enableImages">Optional. include image information in output.</param>
        /// <param name="imageTypeLimit">Optional. the max number of images to return, per image type.</param>
        /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
        /// <param name="enableUserData">Optional. include user data.</param>
        /// <param name="limit">Return item limit.</param>
        /// <param name="groupItems">Whether or not to group items into a parent container.</param>
        /// <response code="200">Latest media returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the latest media.</returns>
        [HttpGet("Users/{userId}/Items/Latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<BaseItemDto>> GetLatestMedia(
            [FromRoute, Required] Guid userId,
            [FromQuery] Guid? parentId,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] BaseItemKind[] includeItemTypes,
            [FromQuery] bool? isPlayed,
            [FromQuery] bool? enableImages,
            [FromQuery] int? imageTypeLimit,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
            [FromQuery] bool? enableUserData,
            [FromQuery] int limit = 20,
            [FromQuery] bool groupItems = true)
        {
            var user = _userManager.GetUserById(userId);

            if (!isPlayed.HasValue)
            {
                if (user.HidePlayedInLatest)
                {
                    isPlayed = false;
                }
            }

            var dtoOptions = new DtoOptions { Fields = fields }
                .AddClientFields(User)
                .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

            var list = _userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    GroupItems = groupItems,
                    IncludeItemTypes = includeItemTypes,
                    IsPlayed = isPlayed,
                    Limit = limit,
                    ParentId = parentId ?? Guid.Empty,
                    UserId = userId,
                },
                dtoOptions);

            var dtos = list.Select(i =>
            {
                var item = i.Item2[0];
                var childCount = 0;

                if (i.Item1 != null && (i.Item2.Count > 1 || i.Item1 is MusicAlbum))
                {
                    item = i.Item1;
                    childCount = i.Item2.Count;
                }

                var dto = _dtoService.GetBaseItemDto(item, dtoOptions, user);

                dto.ChildCount = childCount;

                return dto;
            });

            return Ok(dtos);
        }

        private async Task RefreshItemOnDemandIfNeeded(BaseItem item)
        {
            if (item is Person)
            {
                var hasMetdata = !string.IsNullOrWhiteSpace(item.Overview) && item.HasImage(ImageType.Primary);
                var performFullRefresh = !hasMetdata && (DateTime.UtcNow - item.DateLastRefreshed).TotalDays >= 3;

                if (!hasMetdata)
                {
                    var options = new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                        ImageRefreshMode = MetadataRefreshMode.FullRefresh,
                        ForceSave = performFullRefresh
                    };

                    await item.RefreshMetadata(options, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Marks the favorite.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        private UserItemDataDto MarkFavorite(Guid userId, Guid itemId, bool isFavorite)
        {
            var user = _userManager.GetUserById(userId);

            var item = itemId.Equals(default) ? _libraryManager.GetUserRootFolder() : _libraryManager.GetItemById(itemId);

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user, item);

            // Set favorite status
            data.IsFavorite = isFavorite;

            _userDataRepository.SaveUserData(user, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None);

            return _userDataRepository.GetUserDataDto(item, user);
        }

        /// <summary>
        /// Updates the user item rating.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        private UserItemDataDto UpdateUserItemRatingInternal(Guid userId, Guid itemId, bool? likes)
        {
            var user = _userManager.GetUserById(userId);

            var item = itemId.Equals(default) ? _libraryManager.GetUserRootFolder() : _libraryManager.GetItemById(itemId);

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user, item);

            data.Likes = likes;

            _userDataRepository.SaveUserData(user, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None);

            return _userDataRepository.GetUserDataDto(item, user);
        }

        /// <summary>
        /// Gets an item's lyrics.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="itemId">Item id.</param>
        /// <response code="200">Lyrics returned.</response>
        /// <response code="404">Something went wrong. No Lyrics will be returned.</response>
        /// <returns>An <see cref="OkResult"/> containing the item's lyrics.</returns>
        [HttpGet("Users/{userId}/Items/{itemId}/Lyrics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<LyricResponse>> GetLyrics([FromRoute, Required] Guid userId, [FromRoute, Required] Guid itemId)
        {
            var user = _userManager.GetUserById(userId);

            if (user == null)
            {
                return NotFound();
            }

            var item = itemId.Equals(default)
                ? _libraryManager.GetUserRootFolder()
                : _libraryManager.GetItemById(itemId);

            if (item == null)
            {
                return NotFound();
            }

            var result = await _lyricManager.GetLyrics(item).ConfigureAwait(false);
            if (result is not null)
            {
                return Ok(result);
            }

            return NotFound();
        }
    }
}
