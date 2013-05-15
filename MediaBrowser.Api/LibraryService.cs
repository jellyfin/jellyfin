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

    [Route("/Items/{Id}/Similar", "GET")]
    [Api(Description = "Gets items similar to a given input item.")]
    public class GetSimilarItems : IReturn<ItemsResult>
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

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }
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

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSimilarItems request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (request.UserId.HasValue ? user.RootFolder :
                (Folder)_libraryManager.RootFolder) : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, request.UserId);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true)).ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var inputItems = user == null
                                 ? _libraryManager.RootFolder.RecursiveChildren
                                 : user.RootFolder.GetRecursiveChildren(user);

            var items = GetSimilaritems(item, inputItems).ToArray();

            var result = new ItemsResult
            {
                Items = items.Take(request.Limit ?? items.Length).Select(i => dtoBuilder.GetBaseItemDto(i, fields, user)).Select(t => t.Result).ToArray(),

                TotalRecordCount = items.Length
            };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the similiar items.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="inputItems">The input items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> GetSimilaritems(BaseItem item, IEnumerable<BaseItem> inputItems)
        {
            if (item is Movie || item is Trailer)
            {
                inputItems = inputItems.Where(i => i is Movie || i is Trailer);
            }
            else if (item is Series)
            {
                inputItems = inputItems.Where(i => i is Series);
            }
            else if (item is BaseGame)
            {
                inputItems = inputItems.Where(i => i is BaseGame);
            }
            else if (item is MusicAlbum)
            {
                inputItems = inputItems.Where(i => i is MusicAlbum);
            }
            else if (item is Audio)
            {
                inputItems = inputItems.Where(i => i is Audio);
            }

            // Avoid implicitly captured closure
            var currentItem = item;

            return inputItems.Where(i => i.Id != currentItem.Id)
                .Select(i => new Tuple<BaseItem, int>(i, GetSimiliarityScore(item, i)))
                .Where(i => i.Item2 > 0)
                .OrderByDescending(i => i.Item2)
                .ThenByDescending(i => i.Item1.CriticRating ?? 0)
                .Select(i => i.Item1);
        }

        /// <summary>
        /// Gets the similiarity score.
        /// </summary>
        /// <param name="item1">The item1.</param>
        /// <param name="item2">The item2.</param>
        /// <returns>System.Int32.</returns>
        private int GetSimiliarityScore(BaseItem item1, BaseItem item2)
        {
            var points = 0;

            if (!string.IsNullOrEmpty(item1.OfficialRating) && string.Equals(item1.OfficialRating, item2.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                points += 1;
            }

            // Find common genres
            points += item1.Genres.Where(i => item2.Genres.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 5);

            // Find common tags
            points += item1.Tags.Where(i => item2.Tags.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 5);

            // Find common studios
            points += item1.Studios.Where(i => item2.Studios.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 3);

            var item2PeopleNames = item2.People.Select(i => i.Name).ToList();

            points += item1.People.Where(i => item2PeopleNames.Contains(i.Name, StringComparer.OrdinalIgnoreCase)).Sum(i =>
            {
                if (string.Equals(i.Name, PersonType.Director, StringComparison.OrdinalIgnoreCase))
                {
                    return 5;
                }
                if (string.Equals(i.Name, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Name, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Name, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Name, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
                {
                    return 2;
                }

                return 1;
            });

            if (item1.ProductionYear.HasValue && item2.ProductionYear.HasValue)
            {
                var diff = Math.Abs(item1.ProductionYear.Value - item2.ProductionYear.Value);

                // Add a point if they came out within the same decade
                if (diff < 10)
                {
                    points += 1;
                }

                // And another if within five years
                if (diff < 5)
                {
                    points += 1;
                }
            }

            var album = item1 as MusicAlbum;

            if (album != null)
            {
                points += GetAlbumSimilarityScore(album, (MusicAlbum)item2);
            }

            return points;
        }

        /// <summary>
        /// Gets the album similarity score.
        /// </summary>
        /// <param name="item1">The item1.</param>
        /// <param name="item2">The item2.</param>
        /// <returns>System.Int32.</returns>
        private int GetAlbumSimilarityScore(MusicAlbum item1, MusicAlbum item2)
        {
            var artists1 = item1.RecursiveChildren
                .OfType<Audio>()
                .SelectMany(i => new[]{i.AlbumArtist, i.Artist})
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var artists2 = item2.RecursiveChildren
                .OfType<Audio>()
                .SelectMany(i => new[] { i.AlbumArtist, i.Artist })
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return artists1.Where(i => artists2.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 5);
        }
    }
}
