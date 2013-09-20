using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetNextUpEpisodes
    /// </summary>
    [Route("/Shows/NextUp", "GET")]
    [Api(("Gets a list of currently installed plugins"))]
    public class GetNextUpEpisodes : IReturn<ItemsResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

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

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, ItemCounts, IndexOptions, MediaStreams, Overview, OverviewHtml, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        /// <summary>
        /// Gets the item fields.
        /// </summary>
        /// <returns>IEnumerable{ItemFields}.</returns>
        public IEnumerable<ItemFields> GetItemFields()
        {
            var val = Fields;

            if (string.IsNullOrEmpty(val))
            {
                return new ItemFields[] { };
            }

            return val.Split(',').Select(v =>
            {
                ItemFields value;

                if (Enum.TryParse(v, true, out value))
                {
                    return (ItemFields?)value;
                }
                return null;

            }).Where(i => i.HasValue).Select(i => i.Value);
        }
    }

    [Route("/Shows/{Id}/Similar", "GET")]
    [Api(Description = "Finds tv shows similar to a given one.")]
    public class GetSimilarShows : BaseGetSimilarItemsFromItem
    {
    }

    /// <summary>
    /// Class TvShowsService
    /// </summary>
    public class TvShowsService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _user data repository
        /// </summary>
        private readonly IUserDataRepository _userDataRepository;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly IItemRepository _itemRepo;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvShowsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public TvShowsService(IUserManager userManager, IUserDataRepository userDataRepository, ILibraryManager libraryManager, IItemRepository itemRepo, IDtoService dtoService)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSimilarShows request)
        {
            var result = SimilarItemsHelper.GetSimilarItemsResult(_userManager,
                _itemRepo,
                _libraryManager,
                _userDataRepository,
                _dtoService,
                Logger,
                request, item => item is Series,
                SimilarItemsHelper.GetSimiliarityScore);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetNextUpEpisodes request)
        {
            var result = GetNextUpEpisodes(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the next up episodes.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        private ItemsResult GetNextUpEpisodes(GetNextUpEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var itemsList = user.RootFolder
                .GetRecursiveChildren(user)
                .OfType<Series>()
                .AsParallel()
                .Select(i => GetNextUp(i, user))
                .ToList();

            itemsList = itemsList
                .Where(i => i.Item1 != null)
                .OrderByDescending(i =>
                {
                    var seriesUserData =
                        _userDataRepository.GetUserData(user.Id, i.Item1.Series.GetUserDataKey());

                    if (seriesUserData.IsFavorite)
                    {
                        return 2;
                    }

                    if (seriesUserData.Likes.HasValue)
                    {
                        return seriesUserData.Likes.Value ? 1 : -1;
                    }

                    return 0;
                })
                .ThenByDescending(i => i.Item1.PremiereDate ?? DateTime.MinValue)
                .ToList();

            var pagedItems = ApplyPaging(request, itemsList.Select(i => i.Item1));

            var fields = request.GetItemFields().ToList();

            var returnItems = pagedItems.Select(i => _dtoService.GetBaseItemDto(i, fields, user)).ToArray();

            return new ItemsResult
            {
                TotalRecordCount = itemsList.Count,
                Items = returnItems
            };
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{Episode}.</returns>
        private Tuple<Episode, DateTime> GetNextUp(Series series, User user)
        {
            var allEpisodes = series.GetRecursiveChildren(user)
                .OfType<Episode>()
                .OrderByDescending(i => i.PremiereDate ?? DateTime.MinValue)
                .ThenByDescending(i => i.IndexNumber ?? 0)
                .ToList();

            Episode lastWatched = null;
            var lastWatchedDate = DateTime.MinValue;
            Episode nextUp = null;

            // Go back starting with the most recent episodes
            foreach (var episode in allEpisodes)
            {
                var userData = _userDataRepository.GetUserData(user.Id, episode.GetUserDataKey());

                if (userData.Played)
                {
                    if (lastWatched != null || nextUp == null)
                    {
                        break;
                    }

                    lastWatched = episode;
                    lastWatchedDate = userData.LastPlayedDate ?? DateTime.MinValue;
                }
                else
                {
                    nextUp = episode;
                }
            }

            if (lastWatched != null)
            {
                return new Tuple<Episode, DateTime>(nextUp, lastWatchedDate);
            }

            return new Tuple<Episode, DateTime>(null, lastWatchedDate);
        }

        /// <summary>
        /// Applies the paging.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplyPaging(GetNextUpEpisodes request, IEnumerable<BaseItem> items)
        {
            // Start at
            if (request.StartIndex.HasValue)
            {
                items = items.Skip(request.StartIndex.Value);
            }

            // Return limit
            if (request.Limit.HasValue)
            {
                items = items.Take(request.Limit.Value);
            }

            return items;
        }
    }
}
