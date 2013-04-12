using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class BaseItemsByNameService
    /// </summary>
    /// <typeparam name="TItemType">The type of the T item type.</typeparam>
    public abstract class BaseItemsByNameService<TItemType> : BaseApiService
        where TItemType : BaseItem
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        protected readonly IUserManager UserManager;
        /// <summary>
        /// The library manager
        /// </summary>
        protected readonly ILibraryManager LibraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemsByNameService{TItemType}" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        protected BaseItemsByNameService(IUserManager userManager, ILibraryManager libraryManager)
        {
            UserManager = userManager;
            LibraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        protected async Task<ItemsResult> GetResult(GetItemsByName request)
        {
            var user = UserManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.ParentId) ? user.RootFolder : DtoBuilder.GetItemByClientId(request.ParentId, UserManager, LibraryManager, user.Id);

            IEnumerable<BaseItem> items;

            if (item.IsFolder)
            {
                var folder = (Folder)item;

                items = request.Recursive ? folder.GetRecursiveChildren(user) : folder.GetChildren(user);
            }
            else
            {
                items = new[] { item };
            }

            items = FilterItems(request, items, user);

            var extractedItems = GetAllItems(request, items, user);
            var ibnItemsArray = SortItems(request, extractedItems).ToArray();
      
            IEnumerable<Tuple<string, Func<IEnumerable<BaseItem>>>> ibnItems = ibnItemsArray;

            var result = new ItemsResult
            {
                TotalRecordCount = ibnItemsArray.Length
            };

            if (request.StartIndex.HasValue || request.Limit.HasValue)
            {
                if (request.StartIndex.HasValue)
                {
                    ibnItems = ibnItems.Skip(request.StartIndex.Value);
                }

                if (request.Limit.HasValue)
                {
                    ibnItems = ibnItems.Take(request.Limit.Value);
                }

            }

            var fields = request.GetItemFields().ToList();

            var tasks = ibnItems.Select(i => GetDto(i, user, fields));

            var resultItems = await Task.WhenAll(tasks).ConfigureAwait(false);

            result.Items = resultItems.Where(i => i != null).ToArray();

            return result;
        }

        /// <summary>
        /// Sorts the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<Tuple<string, Func<IEnumerable<BaseItem>>>> SortItems(GetItemsByName request, IEnumerable<Tuple<string, Func<IEnumerable<BaseItem>>>> items)
        {
            if (string.Equals(request.SortBy, "SortName", StringComparison.OrdinalIgnoreCase))
            {
                if (request.SortOrder.HasValue && request.SortOrder.Value == Model.Entities.SortOrder.Descending)
                {
                    items = items.OrderByDescending(i => i.Item1);
                }
                else
                {
                    items = items.OrderBy(i => i.Item1);
                }
            }

            return items;
        }

        /// <summary>
        /// Filters the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> FilterItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            items = items.AsParallel();

            items = ItemsService.ApplyAdditionalFilters(request, items);

            // Apply filters
            // Run them starting with the ones that are likely to reduce the list the most
            foreach (var filter in request.GetFilters().OrderByDescending(f => (int)f))
            {
                items = ItemsService.ApplyFilter(items, filter, user, UserManager);
            }

            items = items.AsEnumerable();

            return items;
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected abstract IEnumerable<Tuple<string, Func<IEnumerable<BaseItem>>>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user);

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{BaseItem}.</returns>
        protected abstract Task<TItemType> GetEntity(string name);

        /// <summary>
        /// Gets the dto.
        /// </summary>
        /// <param name="stub">The stub.</param>
        /// <param name="user">The user.</param>
        /// <param name="fields">The fields.</param>
        /// <returns>Task{DtoBaseItem}.</returns>
        private async Task<BaseItemDto> GetDto(Tuple<string, Func<IEnumerable<BaseItem>>> stub, User user, List<ItemFields> fields)
        {
            BaseItem item;

            try
            {
                item = await GetEntity(stub.Item1).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting IBN item {0}", ex, stub.Item1);
                return null;
            }

            var dto = await new DtoBuilder(Logger, LibraryManager, UserManager).GetBaseItemDto(item, user, fields).ConfigureAwait(false);

            if (fields.Contains(ItemFields.ItemCounts))
            {
                var items = stub.Item2().ToList();

                dto.ChildCount = items.Count;
                dto.RecentlyAddedItemCount = items.Count(i => i.IsRecentlyAdded(user));
            }

            return dto;
        }
    }

    /// <summary>
    /// Class GetItemsByName
    /// </summary>
    public class GetItemsByName : BaseItemsRequest, IReturn<ItemsResult>
    {
        /// <summary>
        /// What to sort the results by
        /// </summary>
        /// <value>The sort by.</value>
        [ApiMember(Name = "SortBy", Description = "Optional. Options: SortName", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SortBy { get; set; }
    }
}
