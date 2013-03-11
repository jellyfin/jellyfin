using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Server.Implementations.HttpServer;
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
    public abstract class BaseItemsByNameService<TItemType> : BaseRestService
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

            var ibnItemsArray = GetAllItems(request, items, user).ToArray();
            IEnumerable<Tuple<string, Func<int>>> ibnItems = ibnItemsArray;

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

            var fields = GetItemFields(request).ToList();

            var tasks = ibnItems.Select(i => GetDto(i, user, fields));

            var resultItems = await Task.WhenAll(tasks).ConfigureAwait(false);

            result.Items = resultItems.Where(i => i != null).ToArray();

            return result;
        }

        /// <summary>
        /// Gets the item fields.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{ItemFields}.</returns>
        private IEnumerable<ItemFields> GetItemFields(GetItemsByName request)
        {
            var val = request.Fields;

            if (string.IsNullOrEmpty(val))
            {
                return new ItemFields[] { };
            }

            return val.Split(',').Select(v => (ItemFields)Enum.Parse(typeof(ItemFields), v, true));
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected abstract IEnumerable<Tuple<string, Func<int>>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user);

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
        private async Task<BaseItemDto> GetDto(Tuple<string, Func<int>> stub, User user, List<ItemFields> fields)
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

            var dto = await new DtoBuilder(Logger).GetBaseItemDto(item, user, fields, LibraryManager).ConfigureAwait(false);

            dto.ChildCount = stub.Item2();

            return dto;
        }
    }

    /// <summary>
    /// Class GetItemsByName
    /// </summary>
    public class GetItemsByName : BaseItemsRequest, IReturn<ItemsResult>
    {
    }
}
