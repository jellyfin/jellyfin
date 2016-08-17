using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItem
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}", "GET", Summary = "Gets an item from a user's library")]
    public class GetItem : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetItem
    /// </summary>
    [Route("/Users/{UserId}/Items/Root", "GET", Summary = "Gets the root folder from a user's library")]
    public class GetRootFolder : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }
    }

    /// <summary>
    /// Class GetIntros
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/Intros", "GET", Summary = "Gets intros to play before the main media item plays")]
    public class GetIntros : IReturn<ItemsResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class MarkFavoriteItem
    /// </summary>
    [Route("/Users/{UserId}/FavoriteItems/{Id}", "POST", Summary = "Marks an item as a favorite")]
    public class MarkFavoriteItem : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UnmarkFavoriteItem
    /// </summary>
    [Route("/Users/{UserId}/FavoriteItems/{Id}", "DELETE", Summary = "Unmarks an item as a favorite")]
    public class UnmarkFavoriteItem : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class ClearUserItemRating
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/Rating", "DELETE", Summary = "Deletes a user's saved personal rating for an item")]
    public class DeleteUserItemRating : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UpdateUserItemRating
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/Rating", "POST", Summary = "Updates a user's rating for an item")]
    public class UpdateUserItemRating : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UpdateUserItemRating" /> is likes.
        /// </summary>
        /// <value><c>true</c> if likes; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "Likes", Description = "Whether the user likes the item or not. true/false", IsRequired = true, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool Likes { get; set; }
    }

    /// <summary>
    /// Class GetLocalTrailers
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/LocalTrailers", "GET", Summary = "Gets local trailers for an item")]
    public class GetLocalTrailers : IReturn<List<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetSpecialFeatures
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/SpecialFeatures", "GET", Summary = "Gets special features for an item")]
    public class GetSpecialFeatures : IReturn<List<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Movie Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Users/{UserId}/Items/Latest", "GET", Summary = "Gets latest media")]
    public class GetLatestMedia : IReturn<List<BaseItemDto>>, IHasDtoOptions
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "Limit", Description = "Limit", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int Limit { get; set; }

        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }

        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }

        [ApiMember(Name = "IsFolder", Description = "Filter by items that are folders, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsFolder { get; set; }

        [ApiMember(Name = "IsPlayed", Description = "Filter by items that are played, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsPlayed { get; set; }

        [ApiMember(Name = "GroupItems", Description = "Whether or not to group items into a parent container.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool GroupItems { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        public GetLatestMedia()
        {
            Limit = 20;
            GroupItems = true;
        }
    }

    /// <summary>
    /// Class UserLibraryService
    /// </summary>
    [Authenticated]
    public class UserLibraryService : BaseApiService
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IUserViewManager _userViewManager;
        private readonly IFileSystem _fileSystem;

        public UserLibraryService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IDtoService dtoService, IUserViewManager userViewManager, IFileSystem fileSystem)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _dtoService = dtoService;
            _userViewManager = userViewManager;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSpecialFeatures request)
        {
            var result = GetAsync(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public object Get(GetLatestMedia request)
        {
            var user = _userManager.GetUserById(request.UserId);

            if (!request.IsPlayed.HasValue)
            {
                if (user.Configuration.HidePlayedInLatest)
                {
                    request.IsPlayed = false;
                }
            }

            var list = _userViewManager.GetLatestItems(new LatestItemsQuery
            {
                GroupItems = request.GroupItems,
                IncludeItemTypes = (request.IncludeItemTypes ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray(),
                IsPlayed = request.IsPlayed,
                Limit = request.Limit,
                ParentId = request.ParentId,
                UserId = request.UserId
            });

            var options = GetDtoOptions(request);

            var dtos = list.Select(i =>
            {
                var item = i.Item2[0];
                var childCount = 0;

                if (i.Item1 != null && i.Item2.Count > 0)
                {
                    item = i.Item1;
                    childCount = i.Item2.Count;
                }

                var dto = _dtoService.GetBaseItemDto(item, options, user);

                dto.ChildCount = childCount;

                return dto;
            });

            return ToOptimizedResult(dtos.ToList());
        }

        private List<BaseItemDto> GetAsync(GetSpecialFeatures request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ?
                user.RootFolder :
                _libraryManager.GetItemById(request.Id);

            var series = item as Series;

            // Get them from the child tree
            if (series != null)
            {
                var dtoOptions = GetDtoOptions(request);

                // Avoid implicitly captured closure
                var currentUser = user;

                var dtos = series
                    .GetRecursiveChildren(i => i is Episode && i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == 0)
                    .OrderBy(i =>
                    {
                        if (i.PremiereDate.HasValue)
                        {
                            return i.PremiereDate.Value;
                        }

                        if (i.ProductionYear.HasValue)
                        {
                            return new DateTime(i.ProductionYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        }
                        return DateTime.MinValue;
                    })
                    .ThenBy(i => i.SortName)
                    .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, currentUser));

                return dtos.ToList();
            }

            var movie = item as IHasSpecialFeatures;

            // Get them from the db
            if (movie != null)
            {
                var dtoOptions = GetDtoOptions(request);

                var dtos = movie.SpecialFeatureIds
                    .Select(_libraryManager.GetItemById)
                    .OrderBy(i => i.SortName)
                    .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, item));

                return dtos.ToList();
            }

            return new List<BaseItemDto>();
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetLocalTrailers request)
        {
            var result = GetAsync(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        private List<BaseItemDto> GetAsync(GetLocalTrailers request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _libraryManager.GetItemById(request.Id);

            var trailerIds = new List<Guid>();

            var hasTrailers = item as IHasTrailers;
            if (hasTrailers != null)
            {
                trailerIds = hasTrailers.GetTrailerIds();
            }

            var dtoOptions = GetDtoOptions(request);

            var dtos = trailerIds
                .Select(_libraryManager.GetItemById)
                .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, item));

            return dtos.ToList();
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _libraryManager.GetItemById(request.Id);

            await RefreshItemOnDemandIfNeeded(item).ConfigureAwait(false);

            var dtoOptions = GetDtoOptions(request);

            var result = _dtoService.GetBaseItemDto(item, dtoOptions, user);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        private async Task RefreshItemOnDemandIfNeeded(BaseItem item)
        {
            if (item is Person)
            {
                var hasMetdata = !string.IsNullOrWhiteSpace(item.Overview) && item.HasImage(ImageType.Primary);
                var performFullRefresh = !hasMetdata && (DateTime.UtcNow - item.DateLastRefreshed).TotalDays >= 3;

                if (!hasMetdata)
                {
                    var options = new MetadataRefreshOptions(_fileSystem)
                    {
                        MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                        ImageRefreshMode = ImageRefreshMode.FullRefresh,
                        ForceSave = performFullRefresh
                    };

                    await item.RefreshMetadata(options, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetRootFolder request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = user.RootFolder;

            var dtoOptions = GetDtoOptions(request);

            var result = _dtoService.GetBaseItemDto(item, dtoOptions, user);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetIntros request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _libraryManager.GetItemById(request.Id);

            var items = await _libraryManager.GetIntros(item, user).ConfigureAwait(false);

            var dtoOptions = GetDtoOptions(request);

            var dtos = items.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user))
                .ToArray();

            var result = new ItemsResult
            {
                Items = dtos,
                TotalRecordCount = dtos.Length
            };

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<object> Post(MarkFavoriteItem request)
        {
            var dto = await MarkFavorite(request.UserId, request.Id, true).ConfigureAwait(false);

            return ToOptimizedResult(dto);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Delete(UnmarkFavoriteItem request)
        {
            var dto = MarkFavorite(request.UserId, request.Id, false).Result;

            return ToOptimizedResult(dto);
        }

        /// <summary>
        /// Marks the favorite.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        private async Task<UserItemDataDto> MarkFavorite(string userId, string itemId, bool isFavorite)
        {
            var user = _userManager.GetUserById(userId);

            var item = string.IsNullOrEmpty(itemId) ? user.RootFolder : _libraryManager.GetItemById(itemId);

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user, item);

            // Set favorite status
            data.IsFavorite = isFavorite;

            await _userDataRepository.SaveUserData(user.Id, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None).ConfigureAwait(false);

            return await _userDataRepository.GetUserDataDto(item, user).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Delete(DeleteUserItemRating request)
        {
            var dto = UpdateUserItemRating(request.UserId, request.Id, null).Result;

            return ToOptimizedResult(dto);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<object> Post(UpdateUserItemRating request)
        {
            var dto = await UpdateUserItemRating(request.UserId, request.Id, request.Likes).ConfigureAwait(false);

            return ToOptimizedResult(dto);
        }

        /// <summary>
        /// Updates the user item rating.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        private async Task<UserItemDataDto> UpdateUserItemRating(string userId, string itemId, bool? likes)
        {
            var user = _userManager.GetUserById(userId);

            var item = string.IsNullOrEmpty(itemId) ? user.RootFolder : _libraryManager.GetItemById(itemId);

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user, item);

            data.Likes = likes;

            await _userDataRepository.SaveUserData(user.Id, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None).ConfigureAwait(false);

            return await _userDataRepository.GetUserDataDto(item, user).ConfigureAwait(false);
        }
    }
}
