using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class MarkItemByNameFavorite
    /// </summary>
    [Route("/Users/{UserId}/Favorites/Artists/{Name}", "POST")]
    [Route("/Users/{UserId}/Favorites/Persons/{Name}", "POST")]
    [Route("/Users/{UserId}/Favorites/Studios/{Name}", "POST")]
    [Route("/Users/{UserId}/Favorites/Genres/{Name}", "POST")]
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
        [ApiMember(Name = "Name", Description = "The name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class UnmarkItemByNameFavorite
    /// </summary>
    [Route("/Users/{UserId}/Favorites/Artists/{Name}", "DELETE")]
    [Route("/Users/{UserId}/Favorites/Persons/{Name}", "DELETE")]
    [Route("/Users/{UserId}/Favorites/Studios/{Name}", "DELETE")]
    [Route("/Users/{UserId}/Favorites/Genres/{Name}", "DELETE")]
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
        [ApiMember(Name = "Name", Description = "The name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class UpdateItemByNameRating
    /// </summary>
    [Route("/Users/{UserId}/Ratings/Artists/{Name}", "POST")]
    [Route("/Users/{UserId}/Ratings/Persons/{Name}", "POST")]
    [Route("/Users/{UserId}/Ratings/Studios/{Name}", "POST")]
    [Route("/Users/{UserId}/Ratings/Genres/{Name}", "POST")]
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
        [ApiMember(Name = "Name", Description = "The name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UpdateUserItemRating" /> is likes.
        /// </summary>
        /// <value><c>true</c> if likes; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "Likes", Description = "Whether the user likes the item or not. true/false", IsRequired = true, DataType = "boolean", ParameterType = "query", Verb = "POST")]
        public bool Likes { get; set; }
    }

    /// <summary>
    /// Class DeleteItemByNameRating
    /// </summary>
    [Route("/Users/{UserId}/Ratings/Artists/{Name}", "DELETE")]
    [Route("/Users/{UserId}/Ratings/Persons/{Name}", "DELETE")]
    [Route("/Users/{UserId}/Ratings/Studios/{Name}", "DELETE")]
    [Route("/Users/{UserId}/Ratings/Genres/{Name}", "DELETE")]
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
        [ApiMember(Name = "Name", Description = "The item name (genre, person, year, studio, artist)", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
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
        /// The library manager
        /// </summary>
        protected readonly ILibraryManager LibraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemByNameUserDataService" /> class.
        /// </summary>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public ItemByNameUserDataService(IUserDataRepository userDataRepository, ILibraryManager libraryManager)
        {
            UserDataRepository = userDataRepository;
            LibraryManager = libraryManager;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkItemByNameFavorite request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(3);

            var task = MarkFavorite(request.UserId, type, request.Name, true);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateItemByNameRating request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(3);

            var task = MarkLike(request.UserId, type, request.Name, request.Likes);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(UnmarkItemByNameFavorite request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(3);

            var task = MarkFavorite(request.UserId, type, request.Name, false);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItemByNameRating request)
        {
            var pathInfo = PathInfo.Parse(RequestContext.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(3);

            var task = MarkLike(request.UserId, type, request.Name, null);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Marks the favorite.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        /// <returns>Task.</returns>
        protected async Task MarkFavorite(Guid userId, string type, string name, bool isFavorite)
        {
            BaseItem item;

            if (string.Equals(type, "Persons"))
            {
                item = await LibraryManager.GetPerson(name).ConfigureAwait(false);
            }
            else if (string.Equals(type, "Artists"))
            {
                item = await LibraryManager.GetArtist(name).ConfigureAwait(false);
            }
            else if (string.Equals(type, "Genres"))
            {
                item = await LibraryManager.GetGenre(name).ConfigureAwait(false);
            }
            else if (string.Equals(type, "Studios"))
            {
                item = await LibraryManager.GetStudio(name).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException();
            }

            var key = item.GetUserDataKey();
            
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
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task.</returns>
        protected async Task MarkLike(Guid userId, string type, string name, bool? likes)
        {
            BaseItem item;

            if (string.Equals(type, "Persons"))
            {
                item = await LibraryManager.GetPerson(name).ConfigureAwait(false);
            }
            else if (string.Equals(type, "Artists"))
            {
                item = await LibraryManager.GetArtist(name).ConfigureAwait(false);
            }
            else if (string.Equals(type, "Genres"))
            {
                item = await LibraryManager.GetGenre(name).ConfigureAwait(false);
            }
            else if (string.Equals(type, "Studios"))
            {
                item = await LibraryManager.GetStudio(name).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException();
            }

            var key = item.GetUserDataKey();
            
            // Get the user data for this item
            var data = await UserDataRepository.GetUserData(userId, key).ConfigureAwait(false);

            data.Likes = likes;

            await UserDataRepository.SaveUserData(userId, key, data, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
