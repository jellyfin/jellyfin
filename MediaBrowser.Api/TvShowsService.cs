using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetNextUpEpisodes
    /// </summary>
    [Route("/Shows/NextUp", "GET", Summary = "Gets a list of next up episodes")]
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "SeriesId", Description = "Optional. Filter by series id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SeriesId { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }
    }

    [Route("/Shows/Upcoming", "GET", Summary = "Gets a list of upcoming episodes")]
    public class GetUpcomingEpisodes : IReturn<ItemsResult>, IHasItemFields
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }
    }

    [Route("/Shows/{Id}/Similar", "GET", Summary = "Finds tv shows similar to a given one.")]
    public class GetSimilarShows : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Shows/{Id}/Episodes", "GET", Summary = "Gets episodes for a tv season")]
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
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

        [ApiMember(Name = "AdjacentTo", Description = "Optional. Return items that are siblings of a supplied item.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AdjacentTo { get; set; }
    }

    [Route("/Shows/{Id}/Seasons", "GET", Summary = "Gets seasons for a tv series")]
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "Id", Description = "The series id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid Id { get; set; }

        [ApiMember(Name = "IsSpecialSeason", Description = "Optional. Filter by special season.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsSpecialSeason { get; set; }

        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

        [ApiMember(Name = "IsVirtualUnaired", Description = "Optional filter by items that are virtual unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsVirtualUnaired { get; set; }

        [ApiMember(Name = "AdjacentTo", Description = "Optional. Return items that are siblings of a supplied item.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AdjacentTo { get; set; }
    }

    /// <summary>
    /// Class TvShowsService
    /// </summary>
    [Authenticated]
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

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public object Get(GetUpcomingEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = GetAllLibraryItems(request.UserId, _userManager, _libraryManager, request.ParentId)
                .OfType<Episode>();

            var itemsList = _libraryManager.Sort(items, user, new[] { "PremiereDate", "AirTime", "SortName" }, SortOrder.Ascending)
                .Cast<Episode>()
                .ToList();

            var unairedEpisodes = itemsList.Where(i => i.IsUnaired).ToList();

            var minPremiereDate = DateTime.Now.Date.AddDays(-1).ToUniversalTime();
            var previousEpisodes = itemsList.Where(i => !i.IsUnaired && (i.PremiereDate ?? DateTime.MinValue) >= minPremiereDate).ToList();

            previousEpisodes.AddRange(unairedEpisodes);

            var pagedItems = ApplyPaging(previousEpisodes, request.StartIndex, request.Limit);

            var fields = request.GetItemFields().ToList();

            var returnItems = pagedItems.Select(i => _dtoService.GetBaseItemDto(i, fields, user)).ToArray();

            var result = new ItemsResult
            {
                TotalRecordCount = itemsList.Count,
                Items = returnItems
            };

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetNextUpEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var itemsList = GetNextUpEpisodes(request)
                .ToList();

            var pagedItems = ApplyPaging(itemsList, request.StartIndex, request.Limit);

            var fields = request.GetItemFields().ToList();

            var returnItems = pagedItems.Select(i => _dtoService.GetBaseItemDto(i, fields, user)).ToArray();

            var result = new ItemsResult
            {
                TotalRecordCount = itemsList.Count,
                Items = returnItems
            };

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public IEnumerable<Episode> GetNextUpEpisodes(GetNextUpEpisodes request)
        {
            var items = GetAllLibraryItems(request.UserId, _userManager, _libraryManager, request.ParentId)
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
                .Select(i => GetNextUp(i, currentUser))
                .Where(i => i.Item1 != null)
                .OrderByDescending(i =>
                {
                    var episode = i.Item1;

                    var seriesUserData = _userDataManager.GetUserData(user.Id, episode.Series.GetUserDataKey());

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
                .ThenByDescending(i => i.Item2)
                .ThenByDescending(i => i.Item1.PremiereDate ?? DateTime.MinValue)
                .Select(i => i.Item1);
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{Episode}.</returns>
        private Tuple<Episode, DateTime> GetNextUp(Series series, User user)
        {
            // Get them in display order, then reverse
            var allEpisodes = series.GetSeasons(user, true, true)
                .SelectMany(i => i.GetEpisodes(user, true, true))
                .Reverse()
                .ToList();

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
                    if (episode.LocationType != LocationType.Virtual)
                    {
                        nextUp = episode;
                    }
                }
            }

            if (lastWatched != null)
            {
                return new Tuple<Episode, DateTime>(nextUp, lastWatchedDate);
            }

            return new Tuple<Episode, DateTime>(null, lastWatchedDate);
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
        /// <param name="items">The items.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplyPaging(IEnumerable<BaseItem> items, int? startIndex, int? limit)
        {
            // Start at
            if (startIndex.HasValue)
            {
                items = items.Skip(startIndex.Value);
            }

            // Return limit
            if (limit.HasValue)
            {
                items = items.Take(limit.Value);
            }

            return items;
        }

        public object Get(GetSeasons request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var series = _libraryManager.GetItemById(request.Id) as Series;

            if (series == null)
            {
                throw new ResourceNotFoundException("No series exists with Id " + request.Id);
            }

            var seasons = series.GetSeasons(user);

            if (request.IsSpecialSeason.HasValue)
            {
                var val = request.IsSpecialSeason.Value;

                seasons = seasons.Where(i => i.IsSpecialSeason == val);
            }

            seasons = FilterVirtualSeasons(request, seasons);

            // This must be the last filter
            if (!string.IsNullOrEmpty(request.AdjacentTo))
            {
                seasons = ItemsService.FilterForAdjacency(seasons, request.AdjacentTo)
                    .Cast<Season>();
            }

            var fields = request.GetItemFields().ToList();

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

            IEnumerable<Episode> episodes;

            if (string.IsNullOrEmpty(request.SeasonId))
            {
                var series = _libraryManager.GetItemById(request.Id) as Series;

                if (series == null)
                {
                    throw new ResourceNotFoundException("No series exists with Id " + request.Id);
                }

                episodes = series.GetEpisodes(user, request.Season.Value);
            }
            else
            {
                var season = _libraryManager.GetItemById(new Guid(request.SeasonId)) as Season;

                if (season == null)
                {
                    throw new ResourceNotFoundException("No season exists with Id " + request.SeasonId);
                }

                episodes = season.GetEpisodes(user);
            }

            // Filter after the fact in case the ui doesn't want them
            if (request.IsMissing.HasValue)
            {
                var val = request.IsMissing.Value;
                episodes = episodes.Where(i => i.IsMissingEpisode == val);
            }

            // Filter after the fact in case the ui doesn't want them
            if (request.IsVirtualUnaired.HasValue)
            {
                var val = request.IsVirtualUnaired.Value;
                episodes = episodes.Where(i => i.IsVirtualUnaired == val);
            }

            // This must be the last filter
            if (!string.IsNullOrEmpty(request.AdjacentTo))
            {
                episodes = ItemsService.FilterForAdjacency(episodes, request.AdjacentTo)
                    .Cast<Episode>();
            }

            var fields = request.GetItemFields().ToList();

            episodes = _libraryManager.ReplaceVideosWithPrimaryVersions(episodes).Cast<Episode>();

            var returnItems = episodes.Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToArray();

            return new ItemsResult
            {
                TotalRecordCount = returnItems.Length,
                Items = returnItems
            };
        }
    }
}
