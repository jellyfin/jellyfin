using MediaBrowser.Controller.Persistence;
using ServiceStack.ServiceHost;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItemByNameUserData
    /// </summary>
    [Route("/Users/{UserId}/ItemsByName/{Name}/UserData", "GET")]
    [Api(Description = "Gets user data for an item")]
    public class GetItemByNameUserData : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The item name (genre, person, year, studio, artist, album)", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class MarkItemByNameFavorite
    /// </summary>
    [Route("/Users/{UserId}/ItemsByName/Favorites/{Name}", "POST")]
    [Api(Description = "Marks something as a favorite")]
    public class MarkItemByNameFavorite : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The item name (genre, person, year, studio, artist, album)", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class UnmarkItemByNameFavorite
    /// </summary>
    [Route("/Users/{UserId}/ItemsByName/Favorites/{Name}", "DELETE")]
    [Api(Description = "Unmarks something as a favorite")]
    public class UnmarkItemByNameFavorite : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The item name (genre, person, year, studio, artist, album)", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Name { get; set; }
    }

    [Route("/Users/{UserId}/ItemsByName/{Name}/Rating", "POST")]
    [Api(Description = "Updates a user's rating for an item")]
    public class UpdateItemByNameRating : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The item name (genre, person, year, studio, artist, album)", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UpdateUserItemRating" /> is likes.
        /// </summary>
        /// <value><c>true</c> if likes; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "Likes", Description = "Whether the user likes the item or not. true/false", IsRequired = true, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool Likes { get; set; }
    }

    [Route("/Users/{UserId}/ItemsByName/{Name}/Rating", "DELETE")]
    [Api(Description = "Deletes a user's saved personal rating for an item")]
    public class DeleteItemByNameRating : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The item name (genre, person, year, studio, artist, album)", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Name { get; set; }
    }
    
    /// <summary>
    /// Class ItemByNameUserDataService
    /// </summary>
    public class ItemByNameUserDataService : BaseApiService
    {
        /// <summary>
        /// The user data repository
        /// </summary>
        protected readonly IUserDataRepository UserDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemByNameUserDataService" /> class.
        /// </summary>
        /// <param name="userDataRepository">The user data repository.</param>
        public ItemByNameUserDataService(IUserDataRepository userDataRepository)
        {
            UserDataRepository = userDataRepository;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemByNameUserData request)
        {
            // Get the user data for this item
            var data = UserDataRepository.GetUserData(request.UserId, request.Name).Result;

            return ToOptimizedResult(data);
        }
        
        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkItemByNameFavorite request)
        {
            var task = MarkFavorite(request.UserId, request.Name, true);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateItemByNameRating request)
        {
            var task = MarkLike(request.UserId, request.Name, request.Likes);

            Task.WaitAll(task);
        }
        
        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(MarkItemByNameFavorite request)
        {
            var task = MarkFavorite(request.UserId, request.Name, false);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItemByNameRating request)
        {
            var task = MarkLike(request.UserId, request.Name, null);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Marks the favorite.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        /// <returns>Task.</returns>
        protected async Task MarkFavorite(Guid userId, string key, bool isFavorite)
        {
            // Get the user data for this item
            var data = await UserDataRepository.GetUserData(userId, key).ConfigureAwait(false);

            // Set favorite status
            data.IsFavorite = isFavorite;

            await UserDataRepository.SaveUserData(userId, key, data, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Marks the like.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task.</returns>
        protected async Task MarkLike(Guid userId, string key, bool? likes)
        {
            // Get the user data for this item
            var data = await UserDataRepository.GetUserData(userId, key).ConfigureAwait(false);

            data.Likes = likes;

            await UserDataRepository.SaveUserData(userId, key, data, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
