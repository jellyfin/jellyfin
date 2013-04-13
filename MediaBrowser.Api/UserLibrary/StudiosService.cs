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
    [Route("/Users/{UserId}/Items/{ParentId}/Studios", "GET")]
    [Route("/Users/{UserId}/Items/Root/Studios", "GET")]
    [Api(Description = "Gets all studios from a given item, folder, or the entire library")]
    public class GetStudios : GetItemsByName
    {
    }

    [Route("/Users/{UserId}/FavoriteStudios/{Name}", "POST")]
    [Api(Description = "Marks a studio as a favorite")]
    public class MarkFavoriteStudio : IReturnVoid
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
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Name { get; set; }
    }

    [Route("/Users/{UserId}/FavoriteStudios/{Name}", "DELETE")]
    [Api(Description = "Unmarks a studio as a favorite")]
    public class UnmarkFavoriteStudio : IReturnVoid
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
        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Name { get; set; }
    }
    
    /// <summary>
    /// Class StudiosService
    /// </summary>
    public class StudiosService : BaseItemsByNameService<Studio>
    {
        public StudiosService(IUserManager userManager, ILibraryManager libraryManager)
            : base(userManager, libraryManager)
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
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(MarkFavoriteStudio request)
        {
            var task = MarkFavorite(() => LibraryManager.GetStudio(request.Name), request.UserId, true);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(UnmarkFavoriteStudio request)
        {
            var task = MarkFavorite(() => LibraryManager.GetStudio(request.Name), request.UserId, false);

            Task.WaitAll(task);
        }
        
        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<Tuple<string, Func<IEnumerable<BaseItem>>>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            var itemsList = items.Where(i => i.Studios != null).ToList();

            return itemsList
                .SelectMany(i => i.Studios)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new Tuple<string, Func<IEnumerable<BaseItem>>>(name, () => itemsList.Where(i => i.Studios.Contains(name, StringComparer.OrdinalIgnoreCase))));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Studio}.</returns>
        protected override Task<Studio> GetEntity(string name)
        {
            return LibraryManager.GetStudio(name);
        }
    }
}
