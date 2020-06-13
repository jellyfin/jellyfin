using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

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
        /// Initializes a new instance of the <see cref="BaseItemsByNameService{TItemType}" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="dtoService">The dto service.</param>
        protected BaseItemsByNameService(
            ILogger<BaseItemsByNameService<TItemType>> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IUserDataManager userDataRepository,
            IDtoService dtoService,
            IAuthorizationContext authorizationContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            UserManager = userManager;
            LibraryManager = libraryManager;
            UserDataRepository = userDataRepository;
            DtoService = dtoService;
            AuthorizationContext = authorizationContext;
        }

        /// <summary>
        /// Gets the _user manager.
        /// </summary>
        protected IUserManager UserManager { get; }

        /// <summary>
        /// Gets the library manager
        /// </summary>
        protected ILibraryManager LibraryManager { get; }

        protected IUserDataManager UserDataRepository { get; }

        protected IDtoService DtoService { get; }

        protected IAuthorizationContext AuthorizationContext { get; }

        protected BaseItem GetParentItem(GetItemsByName request)
        {
            BaseItem parentItem;

            if (!request.UserId.Equals(Guid.Empty))
            {
                var user = UserManager.GetUserById(request.UserId);
                parentItem = string.IsNullOrEmpty(request.ParentId) ? LibraryManager.GetUserRootFolder() : LibraryManager.GetItemById(request.ParentId);
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

            if (parent is IHasCollectionType collectionFolder)
            {
                return collectionFolder.CollectionType;
            }

            return null;
        }

        protected QueryResult<BaseItemDto> GetResultSlim(GetItemsByName request)
        {
            var dtoOptions = GetDtoOptions(AuthorizationContext, request);

            User user = null;
            BaseItem parentItem;

            if (!request.UserId.Equals(Guid.Empty))
            {
                user = UserManager.GetUserById(request.UserId);
                parentItem = string.IsNullOrEmpty(request.ParentId) ? LibraryManager.GetUserRootFolder() : LibraryManager.GetItemById(request.ParentId);
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
                Tags = request.GetTags(),
                OfficialRatings = request.GetOfficialRatings(),
                Genres = request.GetGenres(),
                GenreIds = GetGuids(request.GenreIds),
                StudioIds = GetGuids(request.StudioIds),
                Person = request.Person,
                PersonIds = GetGuids(request.PersonIds),
                PersonTypes = request.GetPersonTypes(),
                Years = request.GetYears(),
                MinCommunityRating = request.MinCommunityRating,
                DtoOptions = dtoOptions,
                SearchTerm = request.SearchTerm,
                EnableTotalRecordCount = request.EnableTotalRecordCount
            };

            if (!string.IsNullOrWhiteSpace(request.ParentId))
            {
                if (parentItem is Folder)
                {
                    query.AncestorIds = new[] { new Guid(request.ParentId) };
                }
                else
                {
                    query.ItemIds = new[] { new Guid(request.ParentId) };
                }
            }

            // Studios
            if (!string.IsNullOrEmpty(request.Studios))
            {
                query.StudioIds = request.Studios.Split('|').Select(i =>
                {
                    try
                    {
                        return LibraryManager.GetStudio(i);
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(i => i != null).Select(i => i.Id).ToArray();
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

            var dtos = result.Items.Select(i =>
            {
                var dto = DtoService.GetItemByNameDto(i.Item1, dtoOptions, null, user);

                if (!string.IsNullOrWhiteSpace(request.IncludeItemTypes))
                {
                    SetItemCounts(dto, i.Item2);
                }
                return dto;
            });

            return new QueryResult<BaseItemDto>
            {
                Items = dtos.ToArray(),
                TotalRecordCount = result.TotalRecordCount
            };
        }

        protected virtual QueryResult<(BaseItem, ItemCounts)> GetItems(GetItemsByName request, InternalItemsQuery query)
        {
            return new QueryResult<(BaseItem, ItemCounts)>();
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
            dto.ArtistCount = counts.ArtistCount;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        protected QueryResult<BaseItemDto> GetResult(GetItemsByName request)
        {
            var dtoOptions = GetDtoOptions(AuthorizationContext, request);

            User user = null;
            BaseItem parentItem;

            if (!request.UserId.Equals(Guid.Empty))
            {
                user = UserManager.GetUserById(request.UserId);
                parentItem = string.IsNullOrEmpty(request.ParentId) ? LibraryManager.GetUserRootFolder() : LibraryManager.GetItemById(request.ParentId);
            }
            else
            {
                parentItem = string.IsNullOrEmpty(request.ParentId) ? LibraryManager.RootFolder : LibraryManager.GetItemById(request.ParentId);
            }

            IList<BaseItem> items;

            var excludeItemTypes = request.GetExcludeItemTypes();
            var includeItemTypes = request.GetIncludeItemTypes();
            var mediaTypes = request.GetMediaTypes();

            var query = new InternalItemsQuery(user)
            {
                ExcludeItemTypes = excludeItemTypes,
                IncludeItemTypes = includeItemTypes,
                MediaTypes = mediaTypes,
                DtoOptions = dtoOptions
            };

            bool Filter(BaseItem i) => FilterItem(request, i, excludeItemTypes, includeItemTypes, mediaTypes);

            if (parentItem.IsFolder)
            {
                var folder = (Folder)parentItem;

                if (!request.UserId.Equals(Guid.Empty))
                {
                    items = request.Recursive ?
                        folder.GetRecursiveChildren(user, query).ToList() :
                        folder.GetChildren(user, true).Where(Filter).ToList();
                }
                else
                {
                    items = request.Recursive ?
                        folder.GetRecursiveChildren(Filter) :
                        folder.Children.Where(Filter).ToList();
                }
            }
            else
            {
                items = new[] { parentItem }.Where(Filter).ToList();
            }

            var extractedItems = GetAllItems(request, items);

            var filteredItems = LibraryManager.Sort(extractedItems, user, request.GetOrderBy());

            var ibnItemsArray = filteredItems.ToList();

            IEnumerable<BaseItem> ibnItems = ibnItemsArray;

            var result = new QueryResult<BaseItemDto>
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

            var tuples = ibnItems.Select(i => new Tuple<BaseItem, List<BaseItem>>(i, new List<BaseItem>()));

            var dtos = tuples.Select(i => DtoService.GetItemByNameDto(i.Item1, dtoOptions, i.Item2, user));

            result.Items = dtos.Where(i => i != null).ToArray();

            return result;
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
            if (excludeItemTypes.Length > 0 && excludeItemTypes.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // Include item types
            if (includeItemTypes.Length > 0 && !includeItemTypes.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // Include MediaTypes
            if (mediaTypes.Length > 0 && !mediaTypes.Contains(f.MediaType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Task{`0}}.</returns>
        protected abstract IEnumerable<BaseItem> GetAllItems(GetItemsByName request, IList<BaseItem> items);
    }

    /// <summary>
    /// Class GetItemsByName
    /// </summary>
    public class GetItemsByName : BaseItemsRequest, IReturn<QueryResult<BaseItemDto>>
    {
        public GetItemsByName()
        {
            Recursive = true;
        }
    }
}
