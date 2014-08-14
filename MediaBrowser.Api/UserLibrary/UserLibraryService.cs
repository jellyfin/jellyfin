using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItem
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}", "GET")]
    [Api(Description = "Gets an item from a user's library")]
    public class GetItem : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Users/{UserId}/Views", "GET")]
    public class GetUserViews : IReturn<QueryResult<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "IncludeExternalContent", Description = "Whether or not to include external views such as channels or live tv", IsRequired = true, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool? IncludeExternalContent { get; set; }
    }

    /// <summary>
    /// Class GetItem
    /// </summary>
    [Route("/Users/{UserId}/Items/Root", "GET")]
    [Api(Description = "Gets the root folder from a user's library")]
    public class GetRootFolder : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Class GetIntros
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/Intros", "GET")]
    [Api(("Gets intros to play before the main media item plays"))]
    public class GetIntros : IReturn<ItemsResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }

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
    [Route("/Users/{UserId}/FavoriteItems/{Id}", "POST")]
    [Api(Description = "Marks an item as a favorite")]
    public class MarkFavoriteItem : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

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
    [Route("/Users/{UserId}/FavoriteItems/{Id}", "DELETE")]
    [Api(Description = "Unmarks an item as a favorite")]
    public class UnmarkFavoriteItem : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid UserId { get; set; }

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
    [Route("/Users/{UserId}/Items/{Id}/Rating", "DELETE")]
    [Api(Description = "Deletes a user's saved personal rating for an item")]
    public class DeleteUserItemRating : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid UserId { get; set; }

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
    [Route("/Users/{UserId}/Items/{Id}/Rating", "POST")]
    [Api(Description = "Updates a user's rating for an item")]
    public class UpdateUserItemRating : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

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
    [Route("/Users/{UserId}/Items/{Id}/LocalTrailers", "GET")]
    [Api(Description = "Gets local trailers for an item")]
    public class GetLocalTrailers : IReturn<List<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }

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
    [Route("/Users/{UserId}/Items/{Id}/SpecialFeatures", "GET")]
    [Api(Description = "Gets special features for an item")]
    public class GetSpecialFeatures : IReturn<List<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Movie Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Users/{UserId}/Items/Latest", "GET", Summary = "Gets latest media")]
    public class GetLatestMedia : IReturn<List<BaseItemDto>>, IHasItemFields
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLibraryService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <param name="userViewManager">The user view manager.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public UserLibraryService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IDtoService dtoService, IUserViewManager userViewManager)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _dtoService = dtoService;
            _userViewManager = userViewManager;
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

            // Avoid implicitly captured closure
            var libraryItems = GetAllLibraryItems(request.UserId, _userManager, _libraryManager, request.ParentId)
                .OrderByDescending(i => i.DateCreated)
                .Where(i => i.LocationType != LocationType.Virtual);


            //if (request.IsFolder.HasValue)
            //{
                //var val = request.IsFolder.Value;
                libraryItems = libraryItems.Where(f => f.IsFolder == false);
            //}
            
            if (!string.IsNullOrEmpty(request.IncludeItemTypes))
            {
                var vals = request.IncludeItemTypes.Split(',');
                libraryItems = libraryItems.Where(f => vals.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            var currentUser = user;

            if (request.IsPlayed.HasValue)
            {
                var takeLimit = request.Limit * 20;

                var val = request.IsPlayed.Value;
                libraryItems = libraryItems.Where(f => f.IsPlayed(currentUser) == val)
                    .Take(takeLimit);
            }
            
            // Avoid implicitly captured closure
            var items = libraryItems
                .ToList();

            var list = new List<Tuple<BaseItem, List<BaseItem>>>();

            foreach (var item in items)
            {
                // Only grab the index container for media
                var container = item.IsFolder || !request.GroupItems ? null : item.LatestItemsIndexContainer;

                if (container == null)
                {
                    list.Add(new Tuple<BaseItem, List<BaseItem>>(null, new List<BaseItem> { item }));
                }
                else
                {
                    var current = list.FirstOrDefault(i => i.Item1 != null && i.Item1.Id == container.Id);

                    if (current != null)
                    {
                        current.Item2.Add(item);
                    }
                    else
                    {
                        list.Add(new Tuple<BaseItem, List<BaseItem>>(container, new List<BaseItem> { item }));
                    }
                }

                if (list.Count >= request.Limit)
                {
                    break;
                }
            }

            var fields = request.GetItemFields().ToList();

            var dtos = list.Select(i =>
            {
                var item = i.Item2[0];
                var childCount = 0;

                if (i.Item1 != null && i.Item2.Count > 0)
                {
                    item = i.Item1;
                    childCount = i.Item2.Count;
                }

                var dto = _dtoService.GetBaseItemDto(item, fields, user);

                dto.ChildCount = childCount;

                return dto;
            });

            return ToOptimizedResult(dtos.ToList());
        }

        public object Get(GetUserViews request)
        {
            var user = _userManager.GetUserById(new Guid(request.UserId));

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var query = new UserViewQuery
            {
                UserId = request.UserId

            };

            if (request.IncludeExternalContent.HasValue)
            {
                query.IncludeExternalContent = request.IncludeExternalContent.Value;
            }

            var folders = _userViewManager.GetUserViews(query, CancellationToken.None).Result;

            var dtos = folders.OrderBy(i => i.SortName)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToArray();

            var result = new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = dtos.Length
            };

            return ToOptimizedResult(result);
        }

        private List<BaseItemDto> GetAsync(GetSpecialFeatures request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ?
                user.RootFolder :
                _libraryManager.GetItemById(request.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var movie = item as Movie;

            // Get them from the db
            if (movie != null)
            {
                // Avoid implicitly captured closure
                var movie1 = movie;

                var dtos = movie.SpecialFeatureIds
                    .Select(_libraryManager.GetItemById)
                    .OrderBy(i => i.SortName)
                    .Select(i => _dtoService.GetBaseItemDto(i, fields, user, movie1));

                return dtos.ToList();
            }

            var series = item as Series;

            // Get them from the child tree
            if (series != null)
            {
                var dtos = series
                    .GetRecursiveChildren()
                    .Where(i => i is Episode && i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == 0)
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
                    .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

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

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var trailerIds = new List<Guid>();

            var hasTrailers = item as IHasTrailers;
            if (hasTrailers != null)
            {
                trailerIds = hasTrailers.LocalTrailerIds;
            }

            var dtos = trailerIds
                .Select(_libraryManager.GetItemById)
                .OrderBy(i => i.SortName)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user, item));

            return dtos.ToList();
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _libraryManager.GetItemById(request.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var result = _dtoService.GetBaseItemDto(item, fields, user);

            return ToOptimizedSerializedResultUsingCache(result);
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

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var result = _dtoService.GetBaseItemDto(item, fields, user);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetIntros request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _libraryManager.GetItemById(request.Id);

            var items = _libraryManager.GetIntros(item, user);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                .ToList();

            var dtos = items.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
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
        public object Post(MarkFavoriteItem request)
        {
            var dto = MarkFavorite(request.UserId, request.Id, true).Result;

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
        private async Task<UserItemDataDto> MarkFavorite(Guid userId, string itemId, bool isFavorite)
        {
            var user = _userManager.GetUserById(userId);

            var item = string.IsNullOrEmpty(itemId) ? user.RootFolder : _libraryManager.GetItemById(itemId);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user.Id, key);

            // Set favorite status
            data.IsFavorite = isFavorite;

            await _userDataRepository.SaveUserData(user.Id, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None).ConfigureAwait(false);

            return _userDataRepository.GetUserDataDto(item, user);
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
        public object Post(UpdateUserItemRating request)
        {
            var dto = UpdateUserItemRating(request.UserId, request.Id, request.Likes).Result;

            return ToOptimizedResult(dto);
        }

        /// <summary>
        /// Updates the user item rating.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        private async Task<UserItemDataDto> UpdateUserItemRating(Guid userId, string itemId, bool? likes)
        {
            var user = _userManager.GetUserById(userId);

            var item = string.IsNullOrEmpty(itemId) ? user.RootFolder : _libraryManager.GetItemById(itemId);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user.Id, key);

            data.Likes = likes;

            await _userDataRepository.SaveUserData(user.Id, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None).ConfigureAwait(false);

            return _userDataRepository.GetUserDataDto(item, user);
        }
    }
}
