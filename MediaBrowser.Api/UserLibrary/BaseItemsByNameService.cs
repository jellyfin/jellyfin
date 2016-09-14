using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Dto;

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

        protected BaseItem GetParentItem(GetItemsByName request)
        {
            BaseItem parentItem;

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                var user = UserManager.GetUserById(request.UserId);
                parentItem = string.IsNullOrEmpty(request.ParentId) ? user.RootFolder : LibraryManager.GetItemById(request.ParentId);
            }
            else
            {
                parentItem = string.IsNullOrEmpty(request.ParentId) ? LibraryManager.RootFolder : LibraryManager.GetItemById(request.ParentId);
            }

            return parentItem;
        }

        protected string GetParentItemViewType(GetItemsByName request)
        {
            var parent = GetParentItem(request);

            var collectionFolder = parent as ICollectionFolder;
            if (collectionFolder != null)
            {
                return collectionFolder.CollectionType;
            }

            var view = parent as UserView;
            if (view != null)
            {
                return view.ViewType;
            }

            return null;
        }

        protected ItemsResult GetResultSlim(GetItemsByName request)
        {
            var dtoOptions = GetDtoOptions(request);

            User user = null;
            BaseItem parentItem;

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                user = UserManager.GetUserById(request.UserId);
                parentItem = string.IsNullOrEmpty(request.ParentId) ? user.RootFolder : LibraryManager.GetItemById(request.ParentId);
            }
            else
            {
                parentItem = string.IsNullOrEmpty(request.ParentId) ? LibraryManager.RootFolder : LibraryManager.GetItemById(request.ParentId);
            }

            var excludeItemTypes = request.GetExcludeItemTypes();
            var includeItemTypes = request.GetIncludeItemTypes();
            var mediaTypes = request.GetMediaTypes();

            var query = new InternalItemsQuery(user)
            {
                ExcludeItemTypes = excludeItemTypes,
                IncludeItemTypes = includeItemTypes,
                MediaTypes = mediaTypes,
                StartIndex = request.StartIndex,
                Limit = request.Limit,
                IsFavorite = request.IsFavorite,
                NameLessThan = request.NameLessThan,
                NameStartsWith = request.NameStartsWith,
                NameStartsWithOrGreater = request.NameStartsWithOrGreater,
                AlbumArtistStartsWithOrGreater = request.AlbumArtistStartsWithOrGreater,
                Tags = request.GetTags(),
                OfficialRatings = request.GetOfficialRatings(),
                Genres = request.GetGenres(),
                GenreIds = request.GetGenreIds(),
                Studios = request.GetStudios(),
                StudioIds = request.GetStudioIds(),
                Person = request.Person,
                PersonIds = request.GetPersonIds(),
                PersonTypes = request.GetPersonTypes(),
                Years = request.GetYears(),
                MinCommunityRating = request.MinCommunityRating
            };

            if (!string.IsNullOrWhiteSpace(request.ParentId))
            {
                if (parentItem is Folder)
                {
                    query.AncestorIds = new[] { request.ParentId };
                }
                else
                {
                    query.ItemIds = new[] { request.ParentId };
                }
            }

            foreach (var filter in request.GetFilters())
            {
                switch (filter)
                {
                    case ItemFilter.Dislikes:
                        query.IsLiked = false;
                        break;
                    case ItemFilter.IsFavorite:
                        query.IsFavorite = true;
                        break;
                    case ItemFilter.IsFavoriteOrLikes:
                        query.IsFavoriteOrLiked = true;
                        break;
                    case ItemFilter.IsFolder:
                        query.IsFolder = true;
                        break;
                    case ItemFilter.IsNotFolder:
                        query.IsFolder = false;
                        break;
                    case ItemFilter.IsPlayed:
                        query.IsPlayed = true;
                        break;
                    case ItemFilter.IsResumable:
                        query.IsResumable = true;
                        break;
                    case ItemFilter.IsUnplayed:
                        query.IsPlayed = false;
                        break;
                    case ItemFilter.Likes:
                        query.IsLiked = true;
                        break;
                }
            }

            var result = GetItems(request, query);

            var syncProgess = DtoService.GetSyncedItemProgress(dtoOptions);
            var dtos = result.Items.Select(i =>
            {
                var dto = DtoService.GetItemByNameDto(i.Item1, dtoOptions, null, syncProgess, user);

                if (!string.IsNullOrWhiteSpace(request.IncludeItemTypes))
                {
                    SetItemCounts(dto, i.Item2);
                }
                return dto;
            });

            return new ItemsResult
            {
                Items = dtos.ToArray(),
                TotalRecordCount = result.TotalRecordCount
            };
        }

        protected virtual QueryResult<Tuple<BaseItem, ItemCounts>> GetItems(GetItemsByName request, InternalItemsQuery query)
        {
            return new QueryResult<Tuple<BaseItem, ItemCounts>>();
        }

        private void SetItemCounts(BaseItemDto dto, ItemCounts counts)
        {
            dto.ChildCount = counts.ItemCount;
            dto.ProgramCount = counts.ProgramCount;
            dto.SeriesCount = counts.SeriesCount;
            dto.EpisodeCount = counts.EpisodeCount;
            dto.MovieCount = counts.MovieCount;
            dto.TrailerCount = counts.TrailerCount;
            dto.AlbumCount = counts.AlbumCount;
            dto.SongCount = counts.SongCount;
            dto.GameCount = counts.GameCount;
            dto.ArtistCount = counts.ArtistCount;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        protected ItemsResult GetResult(GetItemsByName request)
        {
            var dtoOptions = GetDtoOptions(request);

            User user = null;
            BaseItem parentItem;
            List<BaseItem> libraryItems = null;

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                user = UserManager.GetUserById(request.UserId);
                parentItem = string.IsNullOrEmpty(request.ParentId) ? user.RootFolder : LibraryManager.GetItemById(request.ParentId);

                if (RequiresLibraryItems(request, dtoOptions))
                {
                    libraryItems = user.RootFolder.GetRecursiveChildren(user).ToList();
                }
            }
            else
            {
                parentItem = string.IsNullOrEmpty(request.ParentId) ? LibraryManager.RootFolder : LibraryManager.GetItemById(request.ParentId);
                if (RequiresLibraryItems(request, dtoOptions))
                {
                    libraryItems = LibraryManager.RootFolder.GetRecursiveChildren().ToList();
                }
            }

            IEnumerable<BaseItem> items;

            var excludeItemTypes = request.GetExcludeItemTypes();
            var includeItemTypes = request.GetIncludeItemTypes();
            var mediaTypes = request.GetMediaTypes();

            var query = new InternalItemsQuery(user)
            {
                ExcludeItemTypes = excludeItemTypes,
                IncludeItemTypes = includeItemTypes,
                MediaTypes = mediaTypes
            };

            Func<BaseItem, bool> filter = i => FilterItem(request, i, excludeItemTypes, includeItemTypes, mediaTypes);

            if (parentItem.IsFolder)
            {
                var folder = (Folder)parentItem;

                if (!string.IsNullOrWhiteSpace(request.UserId))
                {
                    items = request.Recursive ?
                        folder.GetRecursiveChildren(user, query) :
                        folder.GetChildren(user, true).Where(filter);
                }
                else
                {
                    items = request.Recursive ?
                        folder.GetRecursiveChildren(filter) :
                        folder.Children.Where(filter);
                }
            }
            else
            {
                items = new[] { parentItem }.Where(filter);
            }

            var extractedItems = GetAllItems(request, items);

            var filteredItems = FilterItems(request, extractedItems, user);

            filteredItems = FilterByLibraryItems(request, filteredItems.Cast<IItemByName>(), user, libraryItems).Cast<BaseItem>();

            filteredItems = LibraryManager.Sort(filteredItems, user, request.GetOrderBy(), request.SortOrder ?? SortOrder.Ascending);

            var ibnItemsArray = filteredItems.ToList();

            IEnumerable<BaseItem> ibnItems = ibnItemsArray;

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

            IEnumerable<Tuple<BaseItem, List<BaseItem>>> tuples;
            if (dtoOptions.Fields.Contains(ItemFields.ItemCounts))
            {
                tuples = ibnItems.Select(i => new Tuple<BaseItem, List<BaseItem>>(i, ((IItemByName)i).GetTaggedItems(libraryItems).ToList()));
            }
            else
            {
                tuples = ibnItems.Select(i => new Tuple<BaseItem, List<BaseItem>>(i, new List<BaseItem>()));
            }

            var syncProgess = DtoService.GetSyncedItemProgress(dtoOptions);
            var dtos = tuples.Select(i => DtoService.GetItemByNameDto(i.Item1, dtoOptions, i.Item2, syncProgess, user));

            result.Items = dtos.Where(i => i != null).ToArray();

            return result;
        }

        private bool RequiresLibraryItems(GetItemsByName request, DtoOptions options)
        {
            var filters = request.GetFilters().ToList();

            if (filters.Contains(ItemFilter.IsPlayed))
            {
                return true;
            }

            if (filters.Contains(ItemFilter.IsUnplayed))
            {
                return true;
            }

            if (request.IsPlayed.HasValue)
            {
                return true;
            }

            return options.Fields.Contains(ItemFields.ItemCounts);
        }

        private IEnumerable<IItemByName> FilterByLibraryItems(GetItemsByName request, IEnumerable<IItemByName> items, User user, IEnumerable<BaseItem> libraryItems)
        {
            var filters = request.GetFilters().ToList();

            if (filters.Contains(ItemFilter.IsPlayed))
            {
                items = items.Where(i => i.GetTaggedItems(libraryItems).All(l => l.IsPlayed(user)));
            }

            if (filters.Contains(ItemFilter.IsUnplayed))
            {
                items = items.Where(i => i.GetTaggedItems(libraryItems).All(l => l.IsUnplayed(user)));
            }

            if (request.IsPlayed.HasValue)
            {
                var val = request.IsPlayed.Value;

                items = items.Where(i => i.GetTaggedItems(libraryItems).All(l => l.IsPlayed(user)) == val);
            }

            return items;
        }

        /// <summary>
        /// Filters the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{`0}.</returns>
        private IEnumerable<BaseItem> FilterItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            if (!string.IsNullOrEmpty(request.NameStartsWithOrGreater))
            {
                items = items.Where(i => string.Compare(request.NameStartsWithOrGreater, i.SortName, StringComparison.CurrentCultureIgnoreCase) < 1);
            }
            if (!string.IsNullOrEmpty(request.NameStartsWith))
            {
                items = items.Where(i => string.Compare(request.NameStartsWith, i.SortName.Substring(0, 1), StringComparison.CurrentCultureIgnoreCase) == 0);
            }

            if (!string.IsNullOrEmpty(request.NameLessThan))
            {
                items = items.Where(i => string.Compare(request.NameLessThan, i.SortName, StringComparison.CurrentCultureIgnoreCase) == 1);
            }

            var imageTypes = request.GetImageTypes().ToList();
            if (imageTypes.Count > 0)
            {
                items = items.Where(item => imageTypes.Any(item.HasImage));
            }

            var filters = request.GetFilters().ToList();

            if (filters.Contains(ItemFilter.Dislikes))
            {
                items = items.Where(i =>
                    {
                        var userdata = UserDataRepository.GetUserData(user, i);

                        return userdata != null && userdata.Likes.HasValue && !userdata.Likes.Value;
                    });
            }

            if (filters.Contains(ItemFilter.Likes))
            {
                items = items.Where(i =>
                {
                    var userdata = UserDataRepository.GetUserData(user, i);

                    return userdata != null && userdata.Likes.HasValue && userdata.Likes.Value;
                });
            }

            if (filters.Contains(ItemFilter.IsFavoriteOrLikes))
            {
                items = items.Where(i =>
                {
                    var userdata = UserDataRepository.GetUserData(user, i);

                    var likes = userdata.Likes ?? false;
                    var favorite = userdata.IsFavorite;

                    return likes || favorite;
                });
            }

            if (filters.Contains(ItemFilter.IsFavorite))
            {
                items = items.Where(i =>
                {
                    var userdata = UserDataRepository.GetUserData(user, i);

                    return userdata != null && userdata.IsFavorite;
                });
            }

            // Avoid implicitly captured closure
            var currentRequest = request;
            return items.Where(i => ApplyAdditionalFilters(currentRequest, i, user, false));
        }

        private bool ApplyAdditionalFilters(BaseItemsRequest request, BaseItem i, User user, bool isPreFiltered)
        {
            if (!isPreFiltered)
            {
                // Apply tag filter
                var tags = request.GetTags();
                if (tags.Length > 0)
                {
                    if (!tags.Any(v => i.Tags.Contains(v, StringComparer.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                // Apply official rating filter
                var officialRatings = request.GetOfficialRatings();
                if (officialRatings.Length > 0 && !officialRatings.Contains(i.OfficialRating ?? string.Empty))
                {
                    return false;
                }

                // Apply genre filter
                var genres = request.GetGenres();
                if (genres.Length > 0 && !genres.Any(v => i.Genres.Contains(v, StringComparer.OrdinalIgnoreCase)))
                {
                    return false;
                }

                // Apply year filter
                var years = request.GetYears();
                if (years.Length > 0 && !(i.ProductionYear.HasValue && years.Contains(i.ProductionYear.Value)))
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Filters the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="f">The f.</param>
        /// <param name="excludeItemTypes">The exclude item types.</param>
        /// <param name="includeItemTypes">The include item types.</param>
        /// <param name="mediaTypes">The media types.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private bool FilterItem(GetItemsByName request, BaseItem f, string[] excludeItemTypes, string[] includeItemTypes, string[] mediaTypes)
        {
            // Exclude item types
            if (excludeItemTypes.Length > 0)
            {
                if (excludeItemTypes.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Include item types
            if (includeItemTypes.Length > 0)
            {
                if (!includeItemTypes.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Include MediaTypes
            if (mediaTypes.Length > 0)
            {
                if (!mediaTypes.Contains(f.MediaType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Task{`0}}.</returns>
        protected abstract IEnumerable<BaseItem> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items);
    }

    /// <summary>
    /// Class GetItemsByName
    /// </summary>
    public class GetItemsByName : BaseItemsRequest, IReturn<ItemsResult>
    {
        public GetItemsByName()
        {
            Recursive = true;
        }
    }
}
