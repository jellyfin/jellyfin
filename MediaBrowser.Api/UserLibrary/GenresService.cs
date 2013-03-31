using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetGenres
    /// </summary>
    [Route("/Users/{UserId}/Items/{ParentId}/Genres", "GET")]
    [Route("/Users/{UserId}/Items/Root/Genres", "GET")]
    [Api(Description = "Gets all genres from a given item, folder, or the entire library")]
    public class GetGenres : GetItemsByName
    {
    }

    /// <summary>
    /// Class GenresService
    /// </summary>
    public class GenresService : BaseItemsByNameService<Genre>
    {
        public GenresService(IUserManager userManager, ILibraryManager libraryManager)
            : base(userManager, libraryManager)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetGenres request)
        {
            var result = GetResult(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<Tuple<string, Func<int>>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            var itemsList = items.Where(i => i.Genres != null).ToList();

            return itemsList
                .SelectMany(i => i.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new Tuple<string, Func<int>>(name, () => itemsList.Count(i => i.Genres.Contains(name, StringComparer.OrdinalIgnoreCase))));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        protected override Task<Genre> GetEntity(string name)
        {
            return LibraryManager.GetGenre(name);
        }
    }
}
