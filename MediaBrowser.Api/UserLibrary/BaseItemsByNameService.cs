using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class BaseItemsByNameService
    /// </summary>
    /// <typeparam name="TItemType">The type of the T item type.</typeparam>
    public abstract class BaseItemsByNameService<TItemType> : BaseApiService
        where TItemType : BaseItem, IItemByName
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        protected readonly IUserManager UserManager;
        /// <summary>
        /// The library manager
        /// </summary>
        protected readonly ILibraryManager LibraryManager;
        protected readonly IUserDataRepository UserDataRepository;
        protected readonly IItemRepository ItemRepository;
        protected IDtoService DtoService { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemsByNameService{TItemType}" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="itemRepository">The item repository.</param>
        /// <param name="dtoService">The dto service.</param>
        protected BaseItemsByNameService(IUserManager userManager, ILibraryManager libraryManager, IUserDataRepository userDataRepository, IItemRepository itemRepository, IDtoService dtoService)
        {
            UserManager = userManager;
            LibraryManager = libraryManager;
            UserDataRepository = userDataRepository;
            ItemRepository = itemRepository;
            DtoService = dtoService;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        protected async Task<ItemsResult> GetResult(GetItemsByName request)
        {
            User user = null;
            BaseItem item;

            if (request.UserId.HasValue)
            {
                user = UserManager.GetUserById(request.UserId.Value);
                item = string.IsNullOrEmpty(request.ParentId) ? user.RootFolder : DtoService.GetItemByDtoId(request.ParentId, user.Id);
            }
            else
            {
                item = string.IsNullOrEmpty(request.ParentId) ? LibraryManager.RootFolder : DtoService.GetItemByDtoId(request.ParentId);
            }

            IEnumerable<BaseItem> items;

            if (item.IsFolder)
            {
                var folder = (Folder)item;

                if (request.UserId.HasValue)
                {
                    items = request.Recursive ? folder.GetRecursiveChildren(user) : folder.GetChildren(user, true);
                }
                else
                {
                    items = request.Recursive ? folder.RecursiveChildren : folder.Children;
                }
            }
            else
            {
                items = new[] { item };
            }

            items = FilterItems(request, items);

            var ibnItemTasks = GetAllItems(request, items);
            var extractedItems = await Task.WhenAll(ibnItemTasks).ConfigureAwait(false);

            var filteredItems = FilterItems(request, extractedItems, user);

            filteredItems = ItemsService.ApplySortOrder(request, filteredItems, user, LibraryManager).Cast<TItemType>();

            var ibnItemsArray = filteredItems.ToArray();

            IEnumerable<TItemType> ibnItems = ibnItemsArray;

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
        /// Filters the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{`0}.</returns>
        private IEnumerable<TItemType> FilterItems(GetItemsByName request, IEnumerable<TItemType> items, User user)
        {
            if (!string.IsNullOrEmpty(request.NameStartsWithOrGreater))
            {
                items = items.Where(i => string.Compare(request.NameStartsWithOrGreater, i.Name, StringComparison.CurrentCultureIgnoreCase) < 1);
            }

            var imageTypes = request.GetImageTypes().ToArray();
            if (imageTypes.Length > 0)
            {
                items = items.Where(item => imageTypes.Any(imageType => ItemsService.HasImage(item, imageType)));
            }

            var filters = request.GetFilters().ToList();

            if (filters.Count == 0)
            {
                return items;
            }

            items = items.AsParallel();

            if (filters.Contains(ItemFilter.Dislikes))
            {
                items = items.Where(i =>
                    {
                        var userdata = UserDataRepository.GetUserData(user.Id, i.GetUserDataKey());

                        return userdata != null && userdata.Likes.HasValue && !userdata.Likes.Value;
                    });
            }

            if (filters.Contains(ItemFilter.Likes))
            {
                items = items.Where(i =>
                {
                    var userdata = UserDataRepository.GetUserData(user.Id, i.GetUserDataKey());

                    return userdata != null && userdata.Likes.HasValue && userdata.Likes.Value;
                });
            }

            if (filters.Contains(ItemFilter.IsFavoriteOrLikes))
            {
                items = items.Where(i =>
                {
                    var userdata = UserDataRepository.GetUserData(user.Id, i.GetUserDataKey());

                    var likes = userdata.Likes ?? false;
                    var favorite = userdata.IsFavorite;

                    return likes || favorite;
                });
            }

            if (filters.Contains(ItemFilter.IsFavorite))
            {
                items = items.Where(i =>
                {
                    var userdata = UserDataRepository.GetUserData(user.Id, i.GetUserDataKey());

                    return userdata != null && userdata.Likes.HasValue && userdata.IsFavorite;
                });
            }

            return items.AsEnumerable();
        }

        /// <summary>
        /// Filters the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected virtual IEnumerable<BaseItem> FilterItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            // Exclude item types
            if (!string.IsNullOrEmpty(request.ExcludeItemTypes))
            {
                var vals = request.ExcludeItemTypes.Split(',');
                items = items.Where(f => !vals.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            // Include item types
            if (!string.IsNullOrEmpty(request.IncludeItemTypes))
            {
                var vals = request.IncludeItemTypes.Split(',');
                items = items.Where(f => vals.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            // Include MediaTypes
            if (!string.IsNullOrEmpty(request.MediaTypes))
            {
                var vals = request.MediaTypes.Split(',');

                items = items.Where(f => vals.Contains(f.MediaType ?? string.Empty, StringComparer.OrdinalIgnoreCase));
            }

            return items;
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Task{`0}}.</returns>
        protected abstract IEnumerable<Task<TItemType>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items);

        /// <summary>
        /// Gets the dto.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <param name="fields">The fields.</param>
        /// <returns>Task{DtoBaseItem}.</returns>
        private async Task<BaseItemDto> GetDto(TItemType item, User user, List<ItemFields> fields)
        {
            var dto = user == null ? await DtoService.GetBaseItemDto(item, fields).ConfigureAwait(false) :
                await DtoService.GetBaseItemDto(item, fields, user).ConfigureAwait(false);

            return dto;
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetItems(Guid? userId)
        {
            if (userId.HasValue)
            {
                var user = UserManager.GetUserById(userId.Value);

                return UserManager.GetUserById(userId.Value).RootFolder.GetRecursiveChildren(user);
            }

            return LibraryManager.RootFolder.RecursiveChildren;
        }
    }

    /// <summary>
    /// Class GetItemsByName
    /// </summary>
    public class GetItemsByName : BaseItemsRequest, IReturn<ItemsResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        [ApiMember(Name = "NameStartsWithOrGreater", Description = "Optional filter by items whose name is sorted equally or greater than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameStartsWithOrGreater { get; set; }

        public GetItemsByName()
        {
            Recursive = true;
        }
    }
}
