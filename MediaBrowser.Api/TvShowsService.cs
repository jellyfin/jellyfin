using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
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
    public class GetNextUpEpisodes : IReturn<ItemsResult>, IHasItemFields
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, OverviewHtml, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "SeriesId", Description = "Optional. Filter by series id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SeriesId { get; set; }
    }

    [Route("/Shows/{Id}/Similar", "GET")]
    [Api(Description = "Finds tv shows similar to a given one.")]
    public class GetSimilarShows : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Shows/{Id}/Episodes", "GET")]
    [Api(Description = "Gets episodes for a tv season")]
    public class GetEpisodes : IReturn<ItemsResult>, IHasItemFields
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, OverviewHtml, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "Id", Description = "The series id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid Id { get; set; }

        [ApiMember(Name = "Season", Description = "Optional filter by season number.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public int? Season { get; set; }

        [ApiMember(Name = "SeasonId", Description = "Optional. Filter by season id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SeasonId { get; set; }
        
        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

        [ApiMember(Name = "IsVirtualUnaired", Description = "Optional filter by items that are virtual unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsVirtualUnaired { get; set; }
    }

    [Route("/Shows/{Id}/Seasons", "GET")]
    [Api(Description = "Gets seasons for a tv series")]
    public class GetSeasons : IReturn<ItemsResult>, IHasItemFields
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, OverviewHtml, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "Id", Description = "The series id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid Id { get; set; }

        [ApiMember(Name = "IsSpecialSeason", Description = "Optional. Filter by special season.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsSpecialSeason { get; set; }

        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

        [ApiMember(Name = "IsVirtualUnaired", Description = "Optional filter by items that are virtual unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsVirtualUnaired { get; set; }
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
        private readonly IUserDataManager _userDataManager;
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
        /// <param name="userDataManager">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public TvShowsService(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, IItemRepository itemRepo, IDtoService dtoService)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
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
                _userDataManager,
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
            var result = GetNextUpEpisodeItemsResult(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the next up episodes.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        private ItemsResult GetNextUpEpisodeItemsResult(GetNextUpEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var itemsList = GetNextUpEpisodes(request)
                .ToList();

            var pagedItems = ApplyPaging(request, itemsList);

            var fields = request.GetItemFields().ToList();

            var returnItems = pagedItems.Select(i => _dtoService.GetBaseItemDto(i, fields, user)).ToArray();

            return new ItemsResult
            {
                TotalRecordCount = itemsList.Count,
                Items = returnItems
            };
        }

        public IEnumerable<Episode> GetNextUpEpisodes(GetNextUpEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = user.RootFolder
                .GetRecursiveChildren(user)
                .OfType<Series>();

            // Avoid implicitly captured closure
            return GetNextUpEpisodes(request, items);
        }

        public IEnumerable<Episode> GetNextUpEpisodes(GetNextUpEpisodes request, IEnumerable<Series> series)
        {
            var user = _userManager.GetUserById(request.UserId);

            // Avoid implicitly captured closure
            var currentUser = user;

            return FilterSeries(request, series)
                .AsParallel()
                .Select(i => GetNextUp(i, currentUser, request).Item1)
                .Where(i => i != null)
                .OrderByDescending(i =>
                {
                    var seriesUserData = _userDataManager.GetUserData(user.Id, i.Series.GetUserDataKey());

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
                .ThenByDescending(i => i.PremiereDate ?? DateTime.MinValue);
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="user">The user.</param>
        /// <param name="request">The request.</param>
        /// <returns>Task{Episode}.</returns>
        private Tuple<Episode, DateTime> GetNextUp(Series series, User user, GetNextUpEpisodes request)
        {
            var allEpisodes = series.GetRecursiveChildren(user)
                .OfType<Episode>()
                .OrderByDescending(i => i.PremiereDate ?? DateTime.MinValue)
                .ThenByDescending(i => i.IndexNumber ?? 0)
                .ToList();

            allEpisodes = FilterItems(request, allEpisodes).ToList();

            Episode lastWatched = null;
            var lastWatchedDate = DateTime.MinValue;
            Episode nextUp = null;

            // Go back starting with the most recent episodes
            foreach (var episode in allEpisodes)
            {
                var userData = _userDataManager.GetUserData(user.Id, episode.GetUserDataKey());

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


        private IEnumerable<Episode> FilterItems(GetNextUpEpisodes request, IEnumerable<Episode> items)
        {
            // Make this configurable when needed
            items = items.Where(i => i.LocationType != LocationType.Virtual);

            return items;
        }

        private IEnumerable<Series> FilterSeries(GetNextUpEpisodes request, IEnumerable<Series> items)
        {
            if (!string.IsNullOrWhiteSpace(request.SeriesId))
            {
                var id = new Guid(request.SeriesId);

                items = items.Where(i => i.Id == id);
            }

            return items;
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

        public object Get(GetSeasons request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var series = _libraryManager.GetItemById(request.Id) as Series;

            var fields = request.GetItemFields().ToList();

            var seasons = series.GetChildren(user, true)
                .OfType<Season>();

            var sortOrder = ItemSortBy.SortName;

            if (request.IsSpecialSeason.HasValue)
            {
                var val = request.IsSpecialSeason.Value;

                seasons = seasons.Where(i => i.IsSpecialSeason == val);
            }

            var config = user.Configuration;

            if (!config.DisplayMissingEpisodes && !config.DisplayUnairedEpisodes)
            {
                seasons = seasons.Where(i => !i.IsMissingOrVirtualUnaired);
            }
            else
            {
                if (!config.DisplayMissingEpisodes)
                {
                    seasons = seasons.Where(i => !i.IsMissingSeason);
                }
                if (!config.DisplayUnairedEpisodes)
                {
                    seasons = seasons.Where(i => !i.IsVirtualUnaired);
                }
            }

            seasons = FilterVirtualSeasons(request, seasons);

            seasons = _libraryManager.Sort(seasons, user, new[] { sortOrder }, SortOrder.Ascending)
                .Cast<Season>();

            var returnItems = seasons.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToArray();

            return new ItemsResult
            {
                TotalRecordCount = returnItems.Length,
                Items = returnItems
            };
        }

        private IEnumerable<Season> FilterVirtualSeasons(GetSeasons request, IEnumerable<Season> items)
        {
            if (request.IsMissing.HasValue && request.IsVirtualUnaired.HasValue)
            {
                var isMissing = request.IsMissing.Value;
                var isVirtualUnaired = request.IsVirtualUnaired.Value;

                if (!isMissing && !isVirtualUnaired)
                {
                    return items.Where(i => !i.IsMissingOrVirtualUnaired);
                }
            }

            if (request.IsMissing.HasValue)
            {
                var val = request.IsMissing.Value;
                items = items.Where(i => i.IsMissingSeason == val);
            }

            if (request.IsVirtualUnaired.HasValue)
            {
                var val = request.IsVirtualUnaired.Value;
                items = items.Where(i => i.IsVirtualUnaired == val);
            }

            return items;
        }
        
        public object Get(GetEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var series = _libraryManager.GetItemById(request.Id) as Series;

            var fields = request.GetItemFields().ToList();

            var episodes = series.GetRecursiveChildren(user)
                .OfType<Episode>();

            var sortOrder = ItemSortBy.SortName;

            if (!string.IsNullOrEmpty(request.SeasonId))
            {
                var season = _libraryManager.GetItemById(request.Id) as Season;

                if (season.IndexNumber.HasValue)
                {
                    episodes = FilterEpisodesBySeason(episodes, season.IndexNumber.Value, true);

                    sortOrder = ItemSortBy.AiredEpisodeOrder;
                }
                else
                {
                    episodes = season.RecursiveChildren.OfType<Episode>();

                    sortOrder = ItemSortBy.SortName;
                }
            }

            else if (request.Season.HasValue)
            {
                episodes = FilterEpisodesBySeason(episodes, request.Season.Value, true);

                sortOrder = ItemSortBy.AiredEpisodeOrder;
            }

            var config = user.Configuration;

            if (!config.DisplayMissingEpisodes)
            {
                episodes = episodes.Where(i => !i.IsMissingEpisode);
            }
            if (!config.DisplayUnairedEpisodes)
            {
                episodes = episodes.Where(i => !i.IsVirtualUnaired);
            }

            if (request.IsMissing.HasValue)
            {
                var val = request.IsMissing.Value;
                episodes = episodes.Where(i => i.IsMissingEpisode == val);
            }

            if (request.IsVirtualUnaired.HasValue)
            {
                var val = request.IsVirtualUnaired.Value;
                episodes = episodes.Where(i => i.IsVirtualUnaired == val);
            }

            episodes = _libraryManager.Sort(episodes, user, new[] { sortOrder }, SortOrder.Ascending)
                .Cast<Episode>();

            var returnItems = episodes.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToArray();

            return new ItemsResult
            {
                TotalRecordCount = returnItems.Length,
                Items = returnItems
            };
        }

        internal static IEnumerable<Episode> FilterEpisodesBySeason(IEnumerable<Episode> episodes, int seasonNumber, bool includeSpecials)
        {
            if (!includeSpecials || seasonNumber < 1)
            {
                return episodes.Where(i => (i.PhysicalSeasonNumber ?? -1) == seasonNumber);
            }

            return episodes.Where(i =>
            {
                var episode = i;

                if (episode != null)
                {
                    var currentSeasonNumber = episode.AiredSeasonNumber;

                    return currentSeasonNumber.HasValue && currentSeasonNumber.Value == seasonNumber;
                }

                return false;
            });
        }
    }
}
