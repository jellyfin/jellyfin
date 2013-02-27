using MediaBrowser.Controller;
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
    /// Class GetStudios
    /// </summary>
    [Route("/Users/{UserId}/Items/{Id}/Studios", "GET")]
    [Route("/Users/{UserId}/Items/Root/Studios", "GET")]
    public class GetStudios : GetItemsByName
    {
    }

    /// <summary>
    /// Class StudiosService
    /// </summary>
    public class StudiosService : BaseItemsByNameService<Studio>
    {
        public StudiosService(IUserManager userManager)
            : base(userManager)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetStudios request)
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
            var itemsList = items.Where(i => i.Studios != null).ToList();

            return itemsList
                .SelectMany(i => i.Studios)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new Tuple<string, Func<int>>(name, () => itemsList.Count(i => i.Studios.Contains(name, StringComparer.OrdinalIgnoreCase))));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Studio}.</returns>
        protected override Task<Studio> GetEntity(string name)
        {
            var kernel = (Kernel)Kernel;

            return kernel.LibraryManager.GetStudio(name);
        }
    }
}
