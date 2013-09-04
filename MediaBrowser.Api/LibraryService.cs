using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/Items/{Id}/File", "GET")]
    [Api(Description = "Gets the original file of an item")]
    public class GetFile 
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
    
    /// <summary>
    /// Class GetCriticReviews
    /// </summary>
    [Route("/Items/{Id}/CriticReviews", "GET")]
    [Api(Description = "Gets critic reviews for an item")]
    public class GetCriticReviews : IReturn<ItemReviewsResult>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }
    }

    /// <summary>
    /// Class GetThemeSongs
    /// </summary>
    [Route("/Items/{Id}/ThemeSongs", "GET")]
    [Api(Description = "Gets theme songs for an item")]
    public class GetThemeSongs : IReturn<ThemeMediaResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "InheritFromParent", Description = "Determines whether or not parent items should be searched for theme media.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool InheritFromParent { get; set; }
    }

    /// <summary>
    /// Class GetThemeVideos
    /// </summary>
    [Route("/Items/{Id}/ThemeVideos", "GET")]
    [Api(Description = "Gets theme videos for an item")]
    public class GetThemeVideos : IReturn<ThemeMediaResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "InheritFromParent", Description = "Determines whether or not parent items should be searched for theme media.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool InheritFromParent { get; set; }
    }

    /// <summary>
    /// Class GetThemeVideos
    /// </summary>
    [Route("/Items/{Id}/ThemeMedia", "GET")]
    [Api(Description = "Gets theme videos and songs for an item")]
    public class GetThemeMedia : IReturn<ThemeMediaResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "InheritFromParent", Description = "Determines whether or not parent items should be searched for theme media.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool InheritFromParent { get; set; }
    }

    [Route("/Library/Refresh", "POST")]
    [Api(Description = "Starts a library scan")]
    public class RefreshLibrary : IReturnVoid
    {
    }

    [Route("/Items/{Id}", "DELETE")]
    [Api(Description = "Deletes an item from the library and file system")]
    public class DeleteItem : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Items/Counts", "GET")]
    [Api(Description = "Gets counts of various item types")]
    public class GetItemCounts : IReturn<ItemCounts>
    {
        [ApiMember(Name = "UserId", Description = "Optional. Get counts from a specific user's library.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }
    }

    [Route("/Items/{Id}/Ancestors", "GET")]
    [Api(Description = "Gets all parents of an item")]
    public class GetAncestors : IReturn<BaseItemDto[]>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }
    
    /// <summary>
    /// Class LibraryService
    /// </summary>
    public class LibraryService : BaseApiService
    {
        /// <summary>
        /// The _item repo
        /// </summary>
        private readonly IItemRepository _itemRepo;

        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryService" /> class.
        /// </summary>
        public LibraryService(IItemRepository itemRepo, ILibraryManager libraryManager, IUserManager userManager,
                              IDtoService dtoService)
        {
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
        }

        public object Get(GetFile request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            if (item.LocationType == LocationType.Remote || item.LocationType == LocationType.Virtual)
            {
                throw new ArgumentException("This command cannot be used for remote or virtual items.");
            }
            if (Directory.Exists(item.Path))
            {
                throw new ArgumentException("This command cannot be used for directories.");
            }

            return ToStaticFileResult(item.Path);
        }
        
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAncestors request)
        {
            var result = GetAncestors(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the ancestors.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto[]}.</returns>
        public async Task<BaseItemDto[]> GetAncestors(GetAncestors request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            var tasks = new List<Task<BaseItemDto>>();

            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            BaseItem parent = item.Parent;

            while (parent != null)
            {
                if (user != null)
                {
                    parent = TranslateParentItem(parent, user);
                }

                tasks.Add(_dtoService.GetBaseItemDto(parent, fields, user));

                if (parent is UserRootFolder)
                {
                    break;
                }

                parent = parent.Parent;
            }

            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private BaseItem TranslateParentItem(BaseItem item, User user)
        {
            if (item.Parent is AggregateFolder)
            {
                return user.RootFolder.GetChildren(user, true).FirstOrDefault(i =>
                {

                    try
                    {
                        return i.LocationType == LocationType.FileSystem &&
                               i.ResolveArgs.PhysicalLocations.Contains(item.Path);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error getting ResolveArgs for {0}", ex, i.Path);
                        return false;
                    }

                });
            }

            return item;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCriticReviews request)
        {
            var result = GetCriticReviewsAsync(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemCounts request)
        {
            var items = GetItems(request.UserId).ToList();

            var counts = new ItemCounts
            {
                AlbumCount = items.OfType<MusicAlbum>().Count(),
                EpisodeCount = items.OfType<Episode>().Count(),
                GameCount = items.OfType<Game>().Count(),
                MovieCount = items.OfType<Movie>().Count(),
                SeriesCount = items.OfType<Series>().Count(),
                SongCount = items.OfType<Audio>().Count(),
                TrailerCount = items.OfType<Trailer>().Count(),
                MusicVideoCount = items.OfType<MusicVideo>().Count(),
                AdultVideoCount = items.OfType<AdultVideo>().Count(),
                BoxSetCount = items.OfType<BoxSet>().Count()
            };

            return ToOptimizedResult(counts);
        }

        protected IEnumerable<BaseItem> GetItems(Guid? userId)
        {
            if (userId.HasValue)
            {
                var user = _userManager.GetUserById(userId.Value);

                return _userManager.GetUserById(userId.Value).RootFolder.GetRecursiveChildren(user);
            }

            return _libraryManager.RootFolder.RecursiveChildren;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public async void Post(RefreshLibrary request)
        {
            try
            {
                await _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None)
                                   .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItem request)
        {
            var task = DeleteItem(request);

            Task.WaitAll(task);
        }

        private async Task DeleteItem(DeleteItem request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            var parent = item.Parent;

            if (item.LocationType == LocationType.Offline)
            {
                throw new InvalidOperationException(string.Format("{0} is currently offline.", item.Name));
            }

            if (item.LocationType == LocationType.FileSystem)
            {
                if (Directory.Exists(item.Path))
                {
                    Directory.Delete(item.Path, true);
                }
                else if (File.Exists(item.Path))
                {
                    File.Delete(item.Path);
                }

                if (parent != null)
                {
                    try
                    {
                        await
                            parent.ValidateChildren(new Progress<double>(), CancellationToken.None)
                                  .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error refreshing item", ex);
                    }
                }
            }
            else if (parent != null)
            {
                try
                {
                    await parent.RemoveChild(item, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error removing item", ex);
                }
            }
            else
            {
                throw new InvalidOperationException("Don't know how to delete " + item.Name);
            }
        }

        /// <summary>
        /// Gets the critic reviews async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemReviewsResult}.</returns>
        private async Task<ItemReviewsResult> GetCriticReviewsAsync(GetCriticReviews request)
        {
            var reviews = await _itemRepo.GetCriticReviews(new Guid(request.Id)).ConfigureAwait(false);

            var reviewsArray = reviews.ToArray();

            var result = new ItemReviewsResult
                {
                    TotalRecordCount = reviewsArray.Length
                };

            if (request.StartIndex.HasValue)
            {
                reviewsArray = reviewsArray.Skip(request.StartIndex.Value).ToArray();
            }
            if (request.Limit.HasValue)
            {
                reviewsArray = reviewsArray.Take(request.Limit.Value).ToArray();
            }

            result.ItemReviews = reviewsArray;

            return result;
        }

        public object Get(GetThemeMedia request)
        {
            var themeSongs = GetThemeSongs(new GetThemeSongs
            {
                InheritFromParent = request.InheritFromParent,
                Id = request.Id,
                UserId = request.UserId

            }).Result;

            var themeVideos = GetThemeVideos(new GetThemeVideos
            {
                InheritFromParent = request.InheritFromParent,
                Id = request.Id,
                UserId = request.UserId

            }).Result;

            return ToOptimizedResult(new AllThemeMediaResult
            {
                ThemeSongsResult = themeSongs,
                ThemeVideosResult = themeVideos
            });
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeSongs request)
        {
            var result = GetThemeSongs(request).Result;

            return ToOptimizedResult(result);
        }

        private async Task<ThemeMediaResult> GetThemeSongs(GetThemeSongs request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (request.UserId.HasValue
                                  ? user.RootFolder
                                  : (Folder)_libraryManager.RootFolder)
                           : _dtoService.GetItemByDtoId(request.Id, request.UserId);

            while (item.ThemeSongIds.Count == 0 && request.InheritFromParent && item.Parent != null)
            {
                item = item.Parent;
            }

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var tasks = item.ThemeSongIds.Select(_itemRepo.RetrieveItem)
                            .OrderBy(i => i.SortName)
                            .Select(i => _dtoService.GetBaseItemDto(i, fields, user, item));

            var items = await Task.WhenAll(tasks).ConfigureAwait(false);

            return new ThemeMediaResult
            {
                Items = items,
                TotalRecordCount = items.Length,
                OwnerId = _dtoService.GetDtoId(item)
            };
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeVideos request)
        {
            var result = GetThemeVideos(request).Result;

            return ToOptimizedResult(result);
        }

        public async Task<ThemeMediaResult> GetThemeVideos(GetThemeVideos request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (request.UserId.HasValue
                                  ? user.RootFolder
                                  : (Folder)_libraryManager.RootFolder)
                           : _dtoService.GetItemByDtoId(request.Id, request.UserId);

            while (item.ThemeVideoIds.Count == 0 && request.InheritFromParent && item.Parent != null)
            {
                item = item.Parent;
            }

            // Get everything
            var fields =
                Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var tasks = item.ThemeVideoIds.Select(_itemRepo.RetrieveItem)
                            .OrderBy(i => i.SortName)
                            .Select(i => _dtoService.GetBaseItemDto(i, fields, user, item));

            var items = await Task.WhenAll(tasks).ConfigureAwait(false);

            return new ThemeMediaResult
            {
                Items = items,
                TotalRecordCount = items.Length,
                OwnerId = _dtoService.GetDtoId(item)
            };
        }
    }
}
