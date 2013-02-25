using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Connectivity;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Text.Controller;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItem
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}", "GET")]
    [Route("/Users/{UserId}/Items/Root", "GET")]
    public class GetItem : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
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
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UpdateDisplayPreferences
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/DisplayPreferences", "GET")]
    [ServiceStack.ServiceHost.Api(("Updates a user's display preferences for an item"))]
    public class UpdateDisplayPreferences : IReturnVoid, IRequiresRequestStream
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        /// <value>The request stream.</value>
        public Stream RequestStream { get; set; }
    }

    /// <summary>
    /// Class GetVirtualFolders
    /// </summary>
    [Route("/Users/{UserId}/VirtualFolders", "GET")]
    public class GetVirtualFolders : IReturn<List<VirtualFolderInfo>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Class MarkFavoriteItem
    /// </summary>
    [Route("/Users/{UserId}/FavoriteItems/{Id}", "POST")]
    public class MarkFavoriteItem : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UnmarkFavoriteItem
    /// </summary>
    [Route("/Users/{UserId}/FavoriteItems/{Id}", "DELETE")]
    public class UnmarkFavoriteItem : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    /// <summary>
    /// Class ClearUserItemRating
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/Rating", "DELETE")]
    public class DeleteUserItemRating : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UpdateUserItemRating
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/Rating", "POST")]
    public class UpdateUserItemRating : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UpdateUserItemRating" /> is likes.
        /// </summary>
        /// <value><c>true</c> if likes; otherwise, <c>false</c>.</value>
        public bool Likes { get; set; }
    }

    /// <summary>
    /// Class MarkPlayedItem
    /// </summary>
    [Route("/Users/{UserId}/PlayedItems/{Id}", "POST")]
    public class MarkPlayedItem : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    /// <summary>
    /// Class MarkUnplayedItem
    /// </summary>
    [Route("/Users/{UserId}/PlayedItems/{Id}", "DELETE")]
    public class MarkUnplayedItem : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    [Route("/Users/{UserId}/PlayingItems/{Id}", "POST")]
    public class OnPlaybackStart : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    [Route("/Users/{UserId}/PlayingItems/{Id}/Progress", "POST")]
    public class OnPlaybackProgress : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long? PositionTicks { get; set; }
    }

    [Route("/Users/{UserId}/PlayingItems/{Id}", "DELETE")]
    public class OnPlaybackStopped : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long? PositionTicks { get; set; }
    }
    
    /// <summary>
    /// Class GetLocalTrailers
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/LocalTrailers", "GET")]
    public class GetLocalTrailers : IReturn<List<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetSpecialFeatures
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/SpecialFeatures", "GET")]
    public class GetSpecialFeatures : IReturn<List<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }
    }


    /// <summary>
    /// Class UserLibraryService
    /// </summary>
    public class UserLibraryService : BaseRestService
    {
        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLibraryService" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public UserLibraryService(IJsonSerializer jsonSerializer)
            : base()
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSpecialFeatures request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var movie = (Movie)item;

            var dtoBuilder = new DtoBuilder(Logger);

            var items = movie.SpecialFeatures.Select(i => dtoBuilder.GetDtoBaseItem(item, user, fields)).AsParallel().Select(t => t.Result).ToList();

            return ToOptimizedResult(items);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetLocalTrailers request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger);

            var items = item.LocalTrailers.Select(i => dtoBuilder.GetDtoBaseItem(item, user, fields)).AsParallel().Select(t => t.Result).ToList();

            return ToOptimizedResult(items);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItem request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.Id, user.Id);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger);

            var result = dtoBuilder.GetDtoBaseItem(item, user, fields).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetVirtualFolders request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var result = kernel.LibraryManager.GetVirtualFolders(user).ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetIntros request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, user.Id);

            var result = kernel.IntroProviders.SelectMany(i => i.GetIntros(item, user));

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

            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(userId);

            var item = (Folder)DtoBuilder.GetItemByClientId(itemId, user.Id);

            var displayPreferences = _jsonSerializer.DeserializeFromStream<DisplayPreferences>(request.RequestStream);

            var task = kernel.LibraryManager.SaveDisplayPreferencesForFolder(user, item, displayPreferences);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkFavoriteItem request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = (Folder)DtoBuilder.GetItemByClientId(request.Id, user.Id);

            // Get the user data for this item
            var data = item.GetUserData(user, true);

            // Set favorite status
            data.IsFavorite = true;

            var task = kernel.UserDataManager.SaveUserDataForItem(user, item, data);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(UnmarkFavoriteItem request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = (Folder)DtoBuilder.GetItemByClientId(request.Id, user.Id);

            // Get the user data for this item
            var data = item.GetUserData(user, true);

            // Set favorite status
            data.IsFavorite = false;

            var task = kernel.UserDataManager.SaveUserDataForItem(user, item, data);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserItemRating request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = (Folder)DtoBuilder.GetItemByClientId(request.Id, user.Id);

            // Get the user data for this item
            var data = item.GetUserData(user, true);

            data.Rating = null;

            var task = kernel.UserDataManager.SaveUserDataForItem(user, item, data);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateUserItemRating request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = (Folder)DtoBuilder.GetItemByClientId(request.Id, user.Id);

            // Get the user data for this item
            var data = item.GetUserData(user, true);

            data.Likes = request.Likes;

            var task = kernel.UserDataManager.SaveUserDataForItem(user, item, data);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkPlayedItem request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var task = UpdatePlayedStatus(user, request.Id, true);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackStart request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, user.Id);
            
            kernel.UserDataManager.OnPlaybackStart(user, item, ClientType.Other, string.Empty);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackProgress request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, user.Id);

            var task = kernel.UserDataManager.OnPlaybackProgress(user, item, request.PositionTicks, ClientType.Other, string.Empty);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(OnPlaybackStopped request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

            var item = DtoBuilder.GetItemByClientId(request.Id, user.Id);

            var task = kernel.UserDataManager.OnPlaybackStopped(user, item, request.PositionTicks, ClientType.Other, string.Empty);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(MarkUnplayedItem request)
        {
            var kernel = (Kernel)Kernel;

            var user = kernel.GetUserById(request.UserId);

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
            var item = DtoBuilder.GetItemByClientId(itemId, user.Id);

            return item.SetPlayedStatus(user, wasPlayed);
        }
    }
}
