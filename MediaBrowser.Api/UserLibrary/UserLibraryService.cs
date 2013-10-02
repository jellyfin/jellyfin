using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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

        [ApiMember(Name = "DatePlayed", Description = "The date the item was played (if any)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public DateTime? DatePlayed { get; set; }

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

    /// <summary>
    /// Class OnPlaybackStart
    /// </summary>
    [Route("/Users/{UserId}/PlayingItems/{Id}", "POST")]
    [Api(Description = "Reports that a user has begun playing an item")]
    public class OnPlaybackStart : IReturnVoid
    {
        public OnPlaybackStart()
        {
            // Have to default these until all clients have a chance to incorporate them
            CanSeek = true;
            QueueableMediaTypes = "Audio,Video,Book,Game";
        }

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
        [ApiMember(Name = "CanSeek", Description = "Indicates if the client can seek", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool CanSeek { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "QueueableMediaTypes", Description = "A list of media types that can be queued from this item, comma delimited. Audio,Video,Book,Game", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string QueueableMediaTypes { get; set; }
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

        [ApiMember(Name = "IsPaused", Description = "Indicates if the player is paused.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool IsPaused { get; set; }

        [ApiMember(Name = "IsMuted", Description = "Indicates if the player is muted.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool IsMuted { get; set; }
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

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLibraryService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public UserLibraryService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, ISessionManager sessionManager, IDtoService dtoService)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _sessionManager = sessionManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSpecialFeatures request)
        {
            var result = GetAsync(request);

            return ToOptimizedResult(result);
        }

        private List<BaseItemDto> GetAsync(GetSpecialFeatures request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _dtoService.GetItemByDtoId(request.Id, user.Id);

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
                    .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

                return dtos.ToList();
            }

            throw new ArgumentException("The item does not support special features");
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetLocalTrailers request)
        {
            var result = GetAsync(request);

            return ToOptimizedResult(result);
        }

        private List<BaseItemDto> GetAsync(GetLocalTrailers request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _dtoService.GetItemByDtoId(request.Id, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtos = item.LocalTrailerIds
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

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _dtoService.GetItemByDtoId(request.Id, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var result = _dtoService.GetBaseItemDto(item, fields, user);

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

            var result = _dtoService.GetBaseItemDto(item, fields, user);

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

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : _dtoService.GetItemByDtoId(request.Id, user.Id);

            var result = _libraryManager.GetIntros(item, user);

            return ToOptimizedResult(result);
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

        private async Task<UserItemDataDto> MarkFavorite(Guid userId, string itemId, bool isFavorite)
        {
            var user = _userManager.GetUserById(userId);

            var item = string.IsNullOrEmpty(itemId) ? user.RootFolder : _dtoService.GetItemByDtoId(itemId, user.Id);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user.Id, key);

            // Set favorite status
            data.IsFavorite = isFavorite;

            await _userDataRepository.SaveUserData(user.Id, key, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None).ConfigureAwait(false);

            data = _userDataRepository.GetUserData(user.Id, key);

            return _dtoService.GetUserItemDataDto(data);
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

        private async Task<UserItemDataDto> UpdateUserItemRating(Guid userId, string itemId, bool? likes)
        {
            var user = _userManager.GetUserById(userId);

            var item = string.IsNullOrEmpty(itemId) ? user.RootFolder : _dtoService.GetItemByDtoId(itemId, user.Id);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = _userDataRepository.GetUserData(user.Id, key);

            data.Likes = likes;

            await _userDataRepository.SaveUserData(user.Id, key, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None).ConfigureAwait(false);

            data = _userDataRepository.GetUserData(user.Id, key);

            return _dtoService.GetUserItemDataDto(data);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Post(MarkPlayedItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var task = UpdatePlayedStatus(user, request.Id, true, request.DatePlayed);

            return ToOptimizedResult(task.Result);
        }

        private SessionInfo GetSession()
        {
            var auth = AuthorizationRequestFilterAttribute.GetAuthorization(RequestContext);

            return _sessionManager.Sessions.First(i => string.Equals(i.DeviceId, auth.DeviceId) &&
                string.Equals(i.Client, auth.Client) &&
                string.Equals(i.ApplicationVersion, auth.Version));
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackStart request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = _dtoService.GetItemByDtoId(request.Id, user.Id);

            var queueableMediaTypes = (request.QueueableMediaTypes ?? string.Empty);

            var info = new PlaybackInfo
            {
                CanSeek = request.CanSeek,
                Item = item,
                SessionId = GetSession().Id,
                QueueableMediaTypes = queueableMediaTypes.Split(',').ToList()
            };

            _sessionManager.OnPlaybackStart(info);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackProgress request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = _dtoService.GetItemByDtoId(request.Id, user.Id);

            var info = new PlaybackProgressInfo
            {
                Item = item,
                PositionTicks = request.PositionTicks,
                IsMuted = request.IsMuted,
                IsPaused = request.IsPaused,
                SessionId = GetSession().Id
            };

            var task = _sessionManager.OnPlaybackProgress(info);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(OnPlaybackStopped request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = _dtoService.GetItemByDtoId(request.Id, user.Id);

            // Kill the encoding
            ApiEntryPoint.Instance.KillSingleTranscodingJob(item.Path);

            var session = GetSession();

            var info = new PlaybackStopInfo
            {
                Item = item,
                PositionTicks = request.PositionTicks,
                SessionId = session.Id
            };

            var task = _sessionManager.OnPlaybackStopped(info);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Delete(MarkUnplayedItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var task = UpdatePlayedStatus(user, request.Id, false, null);

            return ToOptimizedResult(task.Result);
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
            var item = _dtoService.GetItemByDtoId(itemId, user.Id);

            if (wasPlayed)
            {
                await item.MarkPlayed(user, datePlayed, _userDataRepository).ConfigureAwait(false);
            }
            else
            {
                await item.MarkUnplayed(user, _userDataRepository).ConfigureAwait(false);
            }

            return _dtoService.GetUserItemDataDto(_userDataRepository.GetUserData(user.Id, item.GetUserDataKey()));
        }
    }
}
