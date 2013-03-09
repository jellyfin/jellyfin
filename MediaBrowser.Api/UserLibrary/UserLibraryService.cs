using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Connectivity;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Server.Implementations.HttpServer;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItem
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}", "GET")]
    [ServiceStack.ServiceHost.Api(Description = "Gets an item from a user's library")]
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
    [ServiceStack.ServiceHost.Api(Description = "Gets the root folder from a user's library")]
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
    [ServiceStack.ServiceHost.Api(("Gets intros to play before the main media item plays"))]
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
    /// Class UpdateDisplayPreferences
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/DisplayPreferences", "POST")]
    [ServiceStack.ServiceHost.Api(("Updates a user's display preferences for an item"))]
    public class UpdateDisplayPreferences : DisplayPreferences, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class MarkFavoriteItem
    /// </summary>
    [Route("/Users/{UserId}/FavoriteItems/{Id}", "POST")]
    [ServiceStack.ServiceHost.Api(Description = "Marks an item as a favorite")]
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
    [ServiceStack.ServiceHost.Api(Description = "Unmarks an item as a favorite")]
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
    [ServiceStack.ServiceHost.Api(Description = "Deletes a user's saved personal rating for an item")]
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
    [ServiceStack.ServiceHost.Api(Description = "Updates a user's rating for an item")]
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
    [ServiceStack.ServiceHost.Api(Description = "Marks an item as played")]
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
    [ServiceStack.ServiceHost.Api(Description = "Marks an item as unplayed")]
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

    [Route("/Users/{UserId}/PlayingItems/{Id}", "POST")]
    [ServiceStack.ServiceHost.Api(Description = "Reports that a user has begun playing an item")]
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

    [Route("/Users/{UserId}/PlayingItems/{Id}/Progress", "POST")]
    [ServiceStack.ServiceHost.Api(Description = "Reports a user's playback progress")]
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
        [ApiMember(Name = "PositionTicks", Description = "Optional. The current position, in ticks. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "DELETE")]
        public long? PositionTicks { get; set; }
    }

    [Route("/Users/{UserId}/PlayingItems/{Id}", "DELETE")]
    [ServiceStack.ServiceHost.Api(Description = "Reports that a user has stopped playing an item")]
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
    [ServiceStack.ServiceHost.Api(Description = "Gets local trailers for an item")]
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
    [ServiceStack.ServiceHost.Api(Description = "Gets special features for a movie")]
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
    public class UserLibraryService : BaseRestService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        private readonly ILibraryManager _libraryManager;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UserLibraryService" /> class.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public UserLibraryService(IUserManager userManager, ILibraryManager libraryManager)
            : base()
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSpecialFeatures request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var movie = (Movie)item;

            var dtoBuilder = new DtoBuilder(Logger);

            var items = movie.SpecialFeatures.Select(i => dtoBuilder.GetBaseItemDto(i, user, fields, _libraryManager)).AsParallel().Select(t => t.Result).ToList();

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

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger);

            var items = item.LocalTrailers.Select(i => dtoBuilder.GetBaseItemDto(i, user, fields, _libraryManager)).AsParallel().Select(t => t.Result).ToList();

            return ToOptimizedResult(items);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger);

            var result = dtoBuilder.GetBaseItemDto(item, user, fields, _libraryManager).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetRootFolder request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = user.RootFolder;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger);

            var result = dtoBuilder.GetBaseItemDto(item, user, fields, _libraryManager).Result;

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

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var result = _libraryManager.GetIntros(item, user);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateDisplayPreferences request)
        {
            // We need to parse this manually because we told service stack not to with IRequiresRequestStream
            // https://code.google.com/p/servicestack/source/browse/trunk/Common/ServiceStack.Text/ServiceStack.Text/Controller/PathInfo.cs
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var userId = new Guid(pathInfo.GetArgumentValue<string>(1));
            var itemId = pathInfo.GetArgumentValue<string>(3);

            var user = _userManager.GetUserById(userId);

            var item = (Folder)DtoBuilder.GetItemByClientId(itemId, _userManager, _libraryManager, user.Id);

            var displayPreferences = request;

            var task = _libraryManager.SaveDisplayPreferencesForFolder(user, item, displayPreferences);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkFavoriteItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = (Folder)DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get the user data for this item
            var data = item.GetUserData(user, true);

            // Set favorite status
            data.IsFavorite = true;

            var task = _userManager.SaveUserDataForItem(user, item, data);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(UnmarkFavoriteItem request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = (Folder)DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get the user data for this item
            var data = item.GetUserData(user, true);

            // Set favorite status
            data.IsFavorite = false;

            var task = _userManager.SaveUserDataForItem(user, item, data);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserItemRating request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = (Folder)DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get the user data for this item
            var data = item.GetUserData(user, true);

            data.Rating = null;

            var task = _userManager.SaveUserDataForItem(user, item, data);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateUserItemRating request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = (Folder)DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            // Get the user data for this item
            var data = item.GetUserData(user, true);

            data.Likes = request.Likes;

            var task = _userManager.SaveUserDataForItem(user, item, data);

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

            _userManager.OnPlaybackStart(user, item, ClientType.Other, string.Empty);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackProgress request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var task = _userManager.OnPlaybackProgress(user, item, request.PositionTicks, ClientType.Other, string.Empty);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackStopped request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, user.Id);

            var task = _userManager.OnPlaybackStopped(user, item, request.PositionTicks, ClientType.Other, string.Empty);

            Task.WaitAll(task);
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

            return item.SetPlayedStatus(user, wasPlayed, _userManager);
        }
    }
}
