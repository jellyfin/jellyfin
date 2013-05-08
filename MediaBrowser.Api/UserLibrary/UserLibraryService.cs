using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
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
    public class GetIntros : IReturn<List<string>>
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
    public class MarkFavoriteItem : IReturnVoid
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
    public class UnmarkFavoriteItem : IReturnVoid
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
    public class DeleteUserItemRating : IReturnVoid
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
    public class UpdateUserItemRating : IReturnVoid
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
    /// Class MarkPlayedItem
    /// </summary>
    [Route("/Users/{UserId}/PlayedItems/{Id}", "POST")]
    [Api(Description = "Marks an item as played")]
    public class MarkPlayedItem : IReturnVoid
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
    /// Class MarkUnplayedItem
    /// </summary>
    [Route("/Users/{UserId}/PlayedItems/{Id}", "DELETE")]
    [Api(Description = "Marks an item as unplayed")]
    public class MarkUnplayedItem : IReturnVoid
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
    /// Class OnPlaybackStart
    /// </summary>
    [Route("/Users/{UserId}/PlayingItems/{Id}", "POST")]
    [Api(Description = "Reports that a user has begun playing an item")]
    public class OnPlaybackStart : IReturnVoid
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
    /// Class OnPlaybackProgress
    /// </summary>
    [Route("/Users/{UserId}/PlayingItems/{Id}/Progress", "POST")]
    [Api(Description = "Reports a user's playback progress")]
    public class OnPlaybackProgress : IReturnVoid
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
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        [ApiMember(Name = "PositionTicks", Description = "Optional. The current position, in ticks. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public long? PositionTicks { get; set; }
    }

    /// <summary>
    /// Class OnPlaybackStopped
    /// </summary>
    [Route("/Users/{UserId}/PlayingItems/{Id}", "DELETE")]
    [Api(Description = "Reports that a user has stopped playing an item")]
    public class OnPlaybackStopped : IReturnVoid
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

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        [ApiMember(Name = "PositionTicks", Description = "Optional. The position, in ticks, where playback stopped. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "DELETE")]
        public long? PositionTicks { get; set; }
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
    /// Class GetThemeSongs
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/ThemeSongs", "GET")]
    [Api(Description = "Gets theme songs for an item")]
    public class GetThemeSongs : IReturn<ThemeSongsResult>
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
    /// Class GetThemeVideos
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/ThemeVideos", "GET")]
    [Api(Description = "Gets video backdrops for an item")]
    public class GetThemeVideos : IReturn<ThemeVideosResult>
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
    [Api(Description = "Gets special features for a movie")]
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


    /// <summary>
    /// Class UserLibraryService
    /// </summary>
    public class UserLibraryService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;
        /// <summary>
        /// The _user data repository
        /// </summary>
        private readonly IUserDataRepository _userDataRepository;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly IItemRepository _itemRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLibraryService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="itemRepo">The item repo.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public UserLibraryService(IUserManager userManager, ILibraryManager libraryManager, IUserDataRepository userDataRepository, IItemRepository itemRepo)
            : base()
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _itemRepo = itemRepo;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSpecialFeatures request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var movie = (Movie)item;

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var items = _itemRepo.GetItems(movie.SpecialFeatureIds).OrderBy(i => i.SortName).Select(i => dtoBuilder.GetBaseItemDto(i, user, fields)).Select(t => t.Result).ToList();

            return ToOptimizedResult(items);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetLocalTrailers request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var items = _itemRepo.GetItems(item.LocalTrailerIds).OrderBy(i => i.SortName).Select(i => dtoBuilder.GetBaseItemDto(i, user, fields)).Select(t => t.Result).ToList();

            return ToOptimizedResult(items);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeSongs request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var items = _itemRepo.GetItems(item.ThemeSongIds).OrderBy(i => i.SortName).Select(i => dtoBuilder.GetBaseItemDto(i, user, fields)).Select(t => t.Result).ToArray();

            var result = new ThemeSongsResult
            {
                Items = items,
                TotalRecordCount = items.Length,
                OwnerId = DtoBuilder.GetClientItemId(item)
            };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeVideos request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var items = _itemRepo.GetItems(item.ThemeVideoIds).OrderBy(i => i.SortName).Select(i => dtoBuilder.GetBaseItemDto(i, user, fields)).Select(t => t.Result).ToArray();

            var result = new ThemeVideosResult
            {
                Items = items,
                TotalRecordCount = items.Length,
                OwnerId = DtoBuilder.GetClientItemId(item)
            };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var result = dtoBuilder.GetBaseItemDto(item, user, fields).Result;

            return ToOptimizedResult(result);
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

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var result = dtoBuilder.GetBaseItemDto(item, user, fields).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetIntros request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var result = _libraryManager.GetIntros(item, user);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkFavoriteItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get the user data for this item
            var key = item.GetUserDataKey();

            var data = _userDataRepository.GetUserData(user.Id, key).Result;

            // Set favorite status
            data.IsFavorite = true;

            var task = _userDataRepository.SaveUserData(user.Id, key, data, CancellationToken.None);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(UnmarkFavoriteItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user.Id, key).Result;

            // Set favorite status
            data.IsFavorite = false;

            var task = _userDataRepository.SaveUserData(user.Id, key, data, CancellationToken.None);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserItemRating request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user.Id, key).Result;

            data.Rating = null;

            var task = _userDataRepository.SaveUserData(user.Id, key, data, CancellationToken.None);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateUserItemRating request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user.Id, key).Result;

            data.Likes = request.Likes;

            var task = _userDataRepository.SaveUserData(user.Id, key, data, CancellationToken.None);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkPlayedItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var task = UpdatePlayedStatus(user, request.Id, true);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackStart request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var auth = RequestFilterAttribute.GetAuthorization(RequestContext);

            if (auth != null)
            {
                _userManager.OnPlaybackStart(user, item, auth["Client"], auth["DeviceId"], auth["Device"] ?? string.Empty);
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackProgress request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var auth = RequestFilterAttribute.GetAuthorization(RequestContext);

            if (auth != null)
            {
                var task = _userManager.OnPlaybackProgress(user, item, request.PositionTicks, auth["Client"], auth["DeviceId"], auth["Device"] ?? string.Empty);

                Task.WaitAll(task);
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(OnPlaybackStopped request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var auth = RequestFilterAttribute.GetAuthorization(RequestContext);

            if (auth != null)
            {
                var task = _userManager.OnPlaybackStopped(user, item, request.PositionTicks, auth["Client"], auth["DeviceId"], auth["Device"] ?? string.Empty);

                Task.WaitAll(task);
            }
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(MarkUnplayedItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var task = UpdatePlayedStatus(user, request.Id, false);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Updates the played status.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="wasPlayed">if set to <c>true</c> [was played].</param>
        /// <returns>Task.</returns>
        private Task UpdatePlayedStatus(User user, string itemId, bool wasPlayed)
        {
            var item = DtoBuilder.GetItemByClientId(itemId, _userManager, _libraryManager, user.Id);

            return item.SetPlayedStatus(user, wasPlayed, _userDataRepository);
        }
    }
}
