using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Class MarkPlayedItem
    /// </summary>
    [Route("/Users/{UserId}/PlayedItems/{Id}", "POST")]
    [Api(Description = "Marks an item as played")]
    public class MarkPlayedItem : IReturn<UserItemDataDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "DatePlayed", Description = "The date the item was played (if any). Format = yyyyMMddHHmmss", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string DatePlayed { get; set; }

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
    public class MarkUnplayedItem : IReturn<UserItemDataDto>
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

    [Route("/Sessions/Playing", "POST")]
    [Api(Description = "Reports playback has started within a session")]
    public class ReportPlaybackStart : PlaybackStartInfo, IReturnVoid
    {
    }

    [Route("/Sessions/Playing/Progress", "POST")]
    [Api(Description = "Reports playback progress within a session")]
    public class ReportPlaybackProgress : PlaybackProgressInfo, IReturnVoid
    {
    }

    [Route("/Sessions/Playing/Stopped", "POST")]
    [Api(Description = "Reports playback has stopped within a session")]
    public class ReportPlaybackStopped : PlaybackStopInfo, IReturnVoid
    {
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

        [ApiMember(Name = "MediaSourceId", Description = "The id of the MediaSource", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MediaSourceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UpdateUserItemRating" /> is likes.
        /// </summary>
        /// <value><c>true</c> if likes; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "CanSeek", Description = "Indicates if the client can seek", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool CanSeek { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "QueueableMediaTypes", Description = "A list of media types that can be queued from this item, comma delimited. Audio,Video,Book,Game", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string QueueableMediaTypes { get; set; }

        [ApiMember(Name = "AudioStreamIndex", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? AudioStreamIndex { get; set; }

        [ApiMember(Name = "SubtitleStreamIndex", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? SubtitleStreamIndex { get; set; }
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

        [ApiMember(Name = "MediaSourceId", Description = "The id of the MediaSource", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MediaSourceId { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        [ApiMember(Name = "PositionTicks", Description = "Optional. The current position, in ticks. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public long? PositionTicks { get; set; }

        [ApiMember(Name = "IsPaused", Description = "Indicates if the player is paused.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool IsPaused { get; set; }

        [ApiMember(Name = "IsMuted", Description = "Indicates if the player is muted.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool IsMuted { get; set; }

        [ApiMember(Name = "AudioStreamIndex", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? AudioStreamIndex { get; set; }

        [ApiMember(Name = "SubtitleStreamIndex", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? SubtitleStreamIndex { get; set; }

        [ApiMember(Name = "VolumeLevel", Description = "Scale of 0-100", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? VolumeLevel { get; set; }
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

        [ApiMember(Name = "MediaSourceId", Description = "The id of the MediaSource", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string MediaSourceId { get; set; }

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


    /// <summary>
    /// Class UserLibraryService
    /// </summary>
    [Authenticated]
    public class UserLibraryService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;
        /// <summary>
        /// The _user data repository
        /// </summary>
        private readonly IUserDataManager _userDataRepository;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly ISessionManager _sessionManager;
        private readonly IDtoService _dtoService;

        private readonly IUserViewManager _userViewManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLibraryService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public UserLibraryService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, ISessionManager sessionManager, IDtoService dtoService, IUserViewManager userViewManager)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _sessionManager = sessionManager;
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

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Post(MarkPlayedItem request)
        {
            var result = MarkPlayed(request).Result;

            return ToOptimizedResult(result);
        }

        private async Task<UserItemDataDto> MarkPlayed(MarkPlayedItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            DateTime? datePlayed = null;

            if (!string.IsNullOrEmpty(request.DatePlayed))
            {
                datePlayed = DateTime.ParseExact(request.DatePlayed, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }

            var session = GetSession();

            var dto = await UpdatePlayedStatus(user, request.Id, true, datePlayed).ConfigureAwait(false);

            foreach (var additionalUserInfo in session.AdditionalUsers)
            {
                var additionalUser = _userManager.GetUserById(new Guid(additionalUserInfo.UserId));

                await UpdatePlayedStatus(additionalUser, request.Id, true, datePlayed).ConfigureAwait(false);
            }

            return dto;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackStart request)
        {
            var queueableMediaTypes = (request.QueueableMediaTypes ?? string.Empty);

            Post(new ReportPlaybackStart
            {
                CanSeek = request.CanSeek,
                ItemId = request.Id,
                QueueableMediaTypes = queueableMediaTypes.Split(',').ToList(),
                MediaSourceId = request.MediaSourceId,
                AudioStreamIndex = request.AudioStreamIndex,
                SubtitleStreamIndex = request.SubtitleStreamIndex
            });
        }

        public void Post(ReportPlaybackStart request)
        {
            request.SessionId = GetSession().Id;

            var task = _sessionManager.OnPlaybackStart(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackProgress request)
        {
            Post(new ReportPlaybackProgress
            {
                ItemId = request.Id,
                PositionTicks = request.PositionTicks,
                IsMuted = request.IsMuted,
                IsPaused = request.IsPaused,
                MediaSourceId = request.MediaSourceId,
                AudioStreamIndex = request.AudioStreamIndex,
                SubtitleStreamIndex = request.SubtitleStreamIndex,
                VolumeLevel = request.VolumeLevel
            });
        }

        public void Post(ReportPlaybackProgress request)
        {
            request.SessionId = GetSession().Id;

            var task = _sessionManager.OnPlaybackProgress(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(OnPlaybackStopped request)
        {
            Post(new ReportPlaybackStopped
            {
                ItemId = request.Id,
                PositionTicks = request.PositionTicks,
                MediaSourceId = request.MediaSourceId
            });
        }

        public void Post(ReportPlaybackStopped request)
        {
            request.SessionId = GetSession().Id;

            var task = _sessionManager.OnPlaybackStopped(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Delete(MarkUnplayedItem request)
        {
            var task = MarkUnplayed(request);

            return ToOptimizedResult(task.Result);
        }

        private async Task<UserItemDataDto> MarkUnplayed(MarkUnplayedItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var session = GetSession();

            var dto = await UpdatePlayedStatus(user, request.Id, false, null).ConfigureAwait(false);

            foreach (var additionalUserInfo in session.AdditionalUsers)
            {
                var additionalUser = _userManager.GetUserById(new Guid(additionalUserInfo.UserId));

                await UpdatePlayedStatus(additionalUser, request.Id, false, null).ConfigureAwait(false);
            }

            return dto;
        }

        /// <summary>
        /// Updates the played status.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="wasPlayed">if set to <c>true</c> [was played].</param>
        /// <param name="datePlayed">The date played.</param>
        /// <returns>Task.</returns>
        private async Task<UserItemDataDto> UpdatePlayedStatus(User user, string itemId, bool wasPlayed, DateTime? datePlayed)
        {
            var item = _libraryManager.GetItemById(itemId);

            if (wasPlayed)
            {
                await item.MarkPlayed(user, datePlayed, _userDataRepository).ConfigureAwait(false);
            }
            else
            {
                await item.MarkUnplayed(user, _userDataRepository).ConfigureAwait(false);
            }

            return _userDataRepository.GetUserDataDto(item, user);
        }
    }
}
