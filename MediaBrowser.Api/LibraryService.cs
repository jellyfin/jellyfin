using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
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
    public class GetThemeSongs : IReturn<ThemeSongsResult>
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
    /// Class GetThemeVideos
    /// </summary>
    [Route("/Items/{Id}/ThemeVideos", "GET")]
    [Api(Description = "Gets video backdrops for an item")]
    public class GetThemeVideos : IReturn<ThemeVideosResult>
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

    [Route("/Library/Refresh", "POST")]
    [Api(Description = "Starts a library scan")]
    public class RefreshLibrary : IReturnVoid
    {
    }

    [Route("/Items/Counts", "GET")]
    [Api(Description = "Gets counts of various item types")]
    public class GetItemCounts : IReturn<ItemCounts>
    {
        [ApiMember(Name = "UserId", Description = "Optional. Get counts from a specific user's library.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }
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
        private readonly IUserDataRepository _userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryService" /> class.
        /// </summary>
        /// <param name="itemRepo">The item repo.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        public LibraryService(IItemRepository itemRepo, ILibraryManager libraryManager, IUserManager userManager, IUserDataRepository userDataRepository)
        {
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepository = userDataRepository;
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
                GameCount = items.OfType<BaseGame>().Count(),
                MovieCount = items.OfType<Movie>().Count(),
                SeriesCount = items.OfType<Series>().Count(),
                SongCount = items.OfType<Audio>().Count(),
                TrailerCount = items.OfType<Trailer>().Count()
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
        public void Post(RefreshLibrary request)
        {
            _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
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

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeSongs request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (request.UserId.HasValue ? user.RootFolder : 
                (Folder)_libraryManager.RootFolder) : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, request.UserId);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var items = _itemRepo.GetItems(item.ThemeSongIds).OrderBy(i => i.SortName).Select(i => dtoBuilder.GetBaseItemDto(i, fields, user)).Select(t => t.Result).ToArray();

            var result = new ThemeSongsResult
            {
                Items = items,
                TotalRecordCount = items.Length,
                OwnerId = DtoBuilder.GetClientItemId(item)
            };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeVideos request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (request.UserId.HasValue ? user.RootFolder :
                (Folder)_libraryManager.RootFolder) : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, request.UserId);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var items = _itemRepo.GetItems(item.ThemeVideoIds).OrderBy(i => i.SortName).Select(i => dtoBuilder.GetBaseItemDto(i, fields, user)).Select(t => t.Result).ToArray();

            var result = new ThemeVideosResult
            {
                Items = items,
                TotalRecordCount = items.Length,
                OwnerId = DtoBuilder.GetClientItemId(item)
            };

            return ToOptimizedResult(result);
        }
    }
}
