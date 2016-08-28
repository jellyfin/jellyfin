using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetGenres
    /// </summary>
    [Route("/Genres", "GET", Summary = "Gets all genres from a given item, folder, or the entire library")]
    public class GetGenres : GetItemsByName
    {
    }

    /// <summary>
    /// Class GetGenre
    /// </summary>
    [Route("/Genres/{Name}", "GET", Summary = "Gets a genre, by name")]
    public class GetGenre : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The genre name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    /// <summary>
    /// Class GenresService
    /// </summary>
    [Authenticated]
    public class GenresService : BaseItemsByNameService<Genre>
    {
        public GenresService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepo, IDtoService dtoService)
            : base(userManager, libraryManager, userDataRepository, itemRepo, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGenre request)
        {
            var result = GetItem(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private BaseItemDto GetItem(GetGenre request)
        {
            var item = GetGenre(request.Name, LibraryManager);

            var dtoOptions = GetDtoOptions(request);

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                var user = UserManager.GetUserById(request.UserId);

                return DtoService.GetBaseItemDto(item, dtoOptions, user);
            }

            return DtoService.GetBaseItemDto(item, dtoOptions);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGenres request)
        {
            var result = GetResultSlim(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        protected override QueryResult<Tuple<BaseItem, ItemCounts>> GetItems(GetItemsByName request, InternalItemsQuery query)
        {
            var viewType = GetParentItemViewType(request);

            if (string.Equals(viewType, CollectionType.Music) || string.Equals(viewType, CollectionType.MusicVideos))
            {
                return LibraryManager.GetMusicGenres(query);
            }

            if (string.Equals(viewType, CollectionType.Games))
            {
                return LibraryManager.GetGameGenres(query);
            }

            return LibraryManager.GetGenres(query);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<BaseItem> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            throw new NotImplementedException();
        }
    }
}
