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
using System.Linq;

namespace MediaBrowser.Api.UserLibrary
{
    [Route("/GameGenres", "GET", Summary = "Gets all Game genres from a given item, folder, or the entire library")]
    public class GetGameGenres : GetItemsByName
    {
        public GetGameGenres()
        {
            MediaTypes = MediaType.Game;
        }
    }

    [Route("/GameGenres/{Name}", "GET", Summary = "Gets a Game genre, by name")]
    public class GetGameGenre : IReturn<BaseItemDto>
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

    [Authenticated]
    public class GameGenresService : BaseItemsByNameService<GameGenre>
    {
        public GameGenresService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepo, IDtoService dtoService)
            : base(userManager, libraryManager, userDataRepository, itemRepo, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGameGenre request)
        {
            var result = GetItem(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private BaseItemDto GetItem(GetGameGenre request)
        {
            var item = GetGameGenre(request.Name, LibraryManager);

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
        public object Get(GetGameGenres request)
        {
            var result = GetResult(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<BaseItem> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            return items
                .SelectMany(i => i.Genres)
                .DistinctNames()
                .Select(name =>
                {
                    try
                    {
                        return LibraryManager.GetGameGenre(name);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error getting genre {0}", ex, name);
                        return null;
                    }
                })
                .Where(i => i != null);
        }
    }
}
