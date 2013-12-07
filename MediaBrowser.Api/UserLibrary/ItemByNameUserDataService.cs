using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack;
using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text.Controller;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class MarkItemByNameFavorite
    /// </summary>
    [Route("/Users/{UserId}/Favorites/Artists/{Name}", "POST")]
    [Route("/Users/{UserId}/Favorites/Persons/{Name}", "POST")]
    [Route("/Users/{UserId}/Favorites/Studios/{Name}", "POST")]
    [Route("/Users/{UserId}/Favorites/Genres/{Name}", "POST")]
    [Route("/Users/{UserId}/Favorites/MusicGenres/{Name}", "POST")]
    [Route("/Users/{UserId}/Favorites/GameGenres/{Name}", "POST")]
    [Api(Description = "Marks something as a favorite")]
    public class MarkItemByNameFavorite : IReturn<UserItemDataDto>
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
    [Route("/Users/{UserId}/Favorites/MusicGenres/{Name}", "DELETE")]
    [Route("/Users/{UserId}/Favorites/GameGenres/{Name}", "DELETE")]
    [Api(Description = "Unmarks something as a favorite")]
    public class UnmarkItemByNameFavorite : IReturn<UserItemDataDto>
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
    [Route("/Users/{UserId}/Ratings/MusicGenres/{Name}", "POST")]
    [Api(Description = "Updates a user's rating for an item")]
    public class UpdateItemByNameRating : IReturn<UserItemDataDto>
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
    [Route("/Users/{UserId}/Ratings/MusicGenres/{Name}", "DELETE")]
    [Route("/Users/{UserId}/Ratings/GameGenres/{Name}", "DELETE")]
    [Api(Description = "Deletes a user's saved personal rating for an item")]
    public class DeleteItemByNameRating : IReturn<UserItemDataDto>
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
        protected readonly IUserDataManager UserDataRepository;

        /// <summary>
        /// The library manager
        /// </summary>
        protected readonly ILibraryManager LibraryManager;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemByNameUserDataService" /> class.
        /// </summary>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public ItemByNameUserDataService(IUserDataManager userDataRepository, ILibraryManager libraryManager, IDtoService dtoService)
        {
            UserDataRepository = userDataRepository;
            LibraryManager = libraryManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Post(MarkItemByNameFavorite request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(3);

            var task = MarkFavorite(request.UserId, type, request.Name, true);

            return ToOptimizedResult(task.Result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Post(UpdateItemByNameRating request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(3);

            var task = MarkLike(request.UserId, type, request.Name, request.Likes);

            return ToOptimizedResult(task.Result);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Delete(UnmarkItemByNameFavorite request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(3);

            var task = MarkFavorite(request.UserId, type, request.Name, false);

            return ToOptimizedResult(task.Result);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public object Delete(DeleteItemByNameRating request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(3);

            var task = MarkLike(request.UserId, type, request.Name, null);

            return ToOptimizedResult(task.Result);
        }

        /// <summary>
        /// Marks the favorite.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        /// <returns>Task.</returns>
        protected async Task<UserItemDataDto> MarkFavorite(Guid userId, string type, string name, bool isFavorite)
        {
            var item = GetItemByName(name, type, LibraryManager);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = UserDataRepository.GetUserData(userId, key);

            // Set favorite status
            data.IsFavorite = isFavorite;

            await UserDataRepository.SaveUserData(userId, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None).ConfigureAwait(false);

            data = UserDataRepository.GetUserData(userId, key);

            return _dtoService.GetUserItemDataDto(data);
        }

        /// <summary>
        /// Marks the like.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task.</returns>
        protected async Task<UserItemDataDto> MarkLike(Guid userId, string type, string name, bool? likes)
        {
            var item = GetItemByName(name, type, LibraryManager);

            var key = item.GetUserDataKey();

            // Get the user data for this item
            var data = UserDataRepository.GetUserData(userId, key);

            data.Likes = likes;

            await UserDataRepository.SaveUserData(userId, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None).ConfigureAwait(false);

            data = UserDataRepository.GetUserData(userId, key);

            return _dtoService.GetUserItemDataDto(data);
        }
    }
}
