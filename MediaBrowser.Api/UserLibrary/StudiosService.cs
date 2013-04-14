using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
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
    [Route("/Users/{UserId}/Items/{ParentId}/Studios", "GET")]
    [Route("/Users/{UserId}/Items/Root/Studios", "GET")]
    [Api(Description = "Gets all studios from a given item, folder, or the entire library")]
    public class GetStudios : GetItemsByName
    {
    }
    
    /// <summary>
    /// Class StudiosService
    /// </summary>
    public class StudiosService : BaseItemsByNameService<Studio>
    {
        public StudiosService(IUserManager userManager, ILibraryManager libraryManager, IUserDataRepository userDataRepository)
            : base(userManager, libraryManager, userDataRepository)
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
        protected override IEnumerable<IbnStub<Studio>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            var itemsList = items.Where(i => i.Studios != null).ToList();

            return itemsList
                .SelectMany(i => i.Studios)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new IbnStub<Studio>(name, () => itemsList.Where(i => i.Studios.Contains(name, StringComparer.OrdinalIgnoreCase)), GetEntity));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Studio}.</returns>
        protected Task<Studio> GetEntity(string name)
        {
            return LibraryManager.GetStudio(name);
        }
    }
}
