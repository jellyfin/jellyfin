using System;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetStudios
    /// </summary>
    [Route("/Studios", "GET", Summary = "Gets all studios from a given item, folder, or the entire library")]
    public class GetStudios : GetItemsByName
    {
    }

    /// <summary>
    /// Class GetStudio
    /// </summary>
    [Route("/Studios/{Name}", "GET", Summary = "Gets a studio, by name")]
    public class GetStudio : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The studio name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    /// <summary>
    /// Class StudiosService
    /// </summary>
    [Authenticated]
    public class StudiosService : BaseItemsByNameService<Studio>
    {
        public StudiosService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepo, IDtoService dtoService)
            : base(userManager, libraryManager, userDataRepository, itemRepo, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetStudio request)
        {
            var result = GetItem(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private BaseItemDto GetItem(GetStudio request)
        {
            var item = GetStudio(request.Name, LibraryManager);

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
        public object Get(GetStudios request)
        {
            var result = GetResultSlim(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        protected override QueryResult<Tuple<BaseItem, ItemCounts>> GetItems(GetItemsByName request, InternalItemsQuery query)
        {
            return LibraryManager.GetStudios(query);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<BaseItem> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            var itemsList = items.Where(i => i.Studios != null).ToList();

            return itemsList
                .SelectMany(i => i.Studios)
                .DistinctNames()
                .Select(name => LibraryManager.GetStudio(name));
        }
    }
}
