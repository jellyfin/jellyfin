using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

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
        protected readonly IUserDataManager UserDataRepository;
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
        protected BaseItemsByNameService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepository, IDtoService dtoService)
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
        protected ItemsResult GetResult(GetItemsByName request)
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
                    items = request.Recursive ? folder.GetRecursiveChildren() : folder.Children;
                }
            }
            else
            {
                items = new[] { item };
            }

            items = FilterItems(request, items);

            var extractedItems = GetAllItems(request, items);

            var filteredItems = FilterItems(request, extractedItems, user);

            filteredItems = FilterByLibraryItems(request, filteredItems, user);

            filteredItems = ItemsService.ApplySortOrder(request, filteredItems, user, LibraryManager).Cast<TItemType>();

            var ibnItemsArray = filteredItems.ToList();

            IEnumerable<TItemType> ibnItems = ibnItemsArray;

            var result = new ItemsResult
            {
                TotalRecordCount = ibnItemsArray.Count
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

            var dtos = ibnItems.Select(i => GetDto(i, user, fields));

            result.Items = dtos.Where(i => i != null).ToArray();

            return result;
        }

        private IEnumerable<TItemType> FilterByLibraryItems(GetItemsByName request, IEnumerable<TItemType> items, User user)
        {
            var filters = request.GetFilters().ToList();

            if (filters.Contains(ItemFilter.IsPlayed))
            {
                var libraryItems = user.RootFolder.GetRecursiveChildren(user).ToList();

                items = items.Where(i => GetLibraryItems(i, libraryItems).All(l => l.IsPlayed(user)));
            }

            if (filters.Contains(ItemFilter.IsUnplayed))
            {
                var libraryItems = user.RootFolder.GetRecursiveChildren(user).ToList();

                items = items.Where(i => GetLibraryItems(i, libraryItems).All(l => l.IsUnplayed(user)));
            }

            if (request.IsPlayed.HasValue)
            {
                var val = request.IsPlayed.Value;

                var libraryItems = user.RootFolder.GetRecursiveChildren(user).ToList();

                items = items.Where(i => GetLibraryItems(i, libraryItems).All(l => l.IsPlayed(user)) == val);
            }

            return items;
        }

        protected abstract IEnumerable<BaseItem> GetLibraryItems(TItemType item, IEnumerable<BaseItem> libraryItems);

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
                items = items.Where(i => string.Compare(request.NameStartsWithOrGreater, i.SortName, StringComparison.CurrentCultureIgnoreCase) < 1);
            }

            if (!string.IsNullOrEmpty(request.NameLessThan))
            {
                items = items.Where(i => string.Compare(request.NameLessThan, i.SortName, StringComparison.CurrentCultureIgnoreCase) == 1);
            }

            var imageTypes = request.GetImageTypes().ToList();
            if (imageTypes.Count > 0)
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

                    return userdata != null && userdata.IsFavorite;
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
        protected abstract IEnumerable<TItemType> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items);

        /// <summary>
        /// Gets the dto.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <param name="fields">The fields.</param>
        /// <returns>Task{DtoBaseItem}.</returns>
        private BaseItemDto GetDto(TItemType item, User user, List<ItemFields> fields)
        {
            var dto = user == null ? DtoService.GetBaseItemDto(item, fields) :
                 DtoService.GetBaseItemDto(item, fields, user);

            return dto;
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

        [ApiMember(Name = "NameLessThan", Description = "Optional filter by items whose name is sorted less than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameLessThan { get; set; }
        
        public GetItemsByName()
        {
            Recursive = true;
        }
    }
}
