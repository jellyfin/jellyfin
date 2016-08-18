using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetNextUpEpisodes
    /// </summary>
    [Route("/Shows/NextUp", "GET", Summary = "Gets a list of next up episodes")]
    public class GetNextUpEpisodes : IReturn<ItemsResult>, IHasDtoOptions
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

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

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }
    }

    [Route("/Shows/Upcoming", "GET", Summary = "Gets a list of upcoming episodes")]
    public class GetUpcomingEpisodes : IReturn<ItemsResult>, IHasDtoOptions
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

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

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }
    }

    [Route("/Shows/{Id}/Similar", "GET", Summary = "Finds tv shows similar to a given one.")]
    public class GetSimilarShows : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Shows/{Id}/Episodes", "GET", Summary = "Gets episodes for a tv season")]
    public class GetEpisodes : IReturn<ItemsResult>, IHasItemFields, IHasDtoOptions
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "Id", Description = "The series id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }

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

        [ApiMember(Name = "StartItemId", Description = "Optional. Skip through the list until a given item is found.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string StartItemId { get; set; }

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

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

    }

    [Route("/Shows/{Id}/Seasons", "GET", Summary = "Gets seasons for a tv series")]
    public class GetSeasons : IReturn<ItemsResult>, IHasItemFields, IHasDtoOptions
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "Id", Description = "The series id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "IsSpecialSeason", Description = "Optional. Filter by special season.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsSpecialSeason { get; set; }

        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

        [ApiMember(Name = "IsVirtualUnaired", Description = "Optional filter by items that are virtual unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsVirtualUnaired { get; set; }

        [ApiMember(Name = "AdjacentTo", Description = "Optional. Return items that are siblings of a supplied item.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AdjacentTo { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

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
        private readonly ITVSeriesManager _tvSeriesManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvShowsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataManager">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public TvShowsService(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, IItemRepository itemRepo, IDtoService dtoService, ITVSeriesManager tvSeriesManager)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _dtoService = dtoService;
            _tvSeriesManager = tvSeriesManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetSimilarShows request)
        {
            var result = await GetSimilarItemsResult(request).ConfigureAwait(false);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        private async Task<QueryResult<BaseItemDto>> GetSimilarItemsResult(BaseGetSimilarItemsFromItem request)
        {
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (!string.IsNullOrWhiteSpace(request.UserId) ? user.RootFolder :
                _libraryManager.RootFolder) : _libraryManager.GetItemById(request.Id);

            var itemsResult = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Limit = request.Limit,
                IncludeItemTypes = new[]
                {
                        typeof(Series).Name
                },
                SimilarTo = item

            }).ToList();

            var dtoOptions = GetDtoOptions(request);

            var result = new QueryResult<BaseItemDto>
            {
                Items = (await _dtoService.GetBaseItemDtos(itemsResult, dtoOptions, user).ConfigureAwait(false)).ToArray(),

                TotalRecordCount = itemsResult.Count
            };

            return result;
        }

        public async Task<object> Get(GetUpcomingEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var minPremiereDate = DateTime.Now.Date.ToUniversalTime().AddDays(-1);

            var parentIdGuid = string.IsNullOrWhiteSpace(request.ParentId) ? (Guid?)null : new Guid(request.ParentId);

            var itemsResult = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Episode).Name },
                SortBy = new[] { "PremiereDate", "AirTime", "SortName" },
                SortOrder = SortOrder.Ascending,
                MinPremiereDate = minPremiereDate,
                StartIndex = request.StartIndex,
                Limit = request.Limit,
                ParentId = parentIdGuid,
                Recursive = true

            }).ToList();

            var options = GetDtoOptions(request);

            var returnItems = (await _dtoService.GetBaseItemDtos(itemsResult, options, user).ConfigureAwait(false)).ToArray();

            var result = new ItemsResult
            {
                TotalRecordCount = itemsResult.Count,
                Items = returnItems
            };

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetNextUpEpisodes request)
        {
            var result = _tvSeriesManager.GetNextUp(new NextUpQuery
            {
                Limit = request.Limit,
                ParentId = request.ParentId,
                SeriesId = request.SeriesId,
                StartIndex = request.StartIndex,
                UserId = request.UserId
            });

            var user = _userManager.GetUserById(request.UserId);

            var options = GetDtoOptions(request);

            var returnItems = (await _dtoService.GetBaseItemDtos(result.Items, options, user).ConfigureAwait(false)).ToArray();

            return ToOptimizedSerializedResultUsingCache(new ItemsResult
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = returnItems
            });
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

        public async Task<object> Get(GetSeasons request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var series = _libraryManager.GetItemById(request.Id) as Series;

            if (series == null)
            {
                throw new ResourceNotFoundException("No series exists with Id " + request.Id);
            }

            var seasons = (await series.GetItems(new InternalItemsQuery(user)
            {
                IsMissing = request.IsMissing,
                IsVirtualUnaired = request.IsVirtualUnaired,
                IsSpecialSeason = request.IsSpecialSeason,
                AdjacentTo = request.AdjacentTo

            }).ConfigureAwait(false)).Items.OfType<Season>();

            var dtoOptions = GetDtoOptions(request);

            var returnItems = (await _dtoService.GetBaseItemDtos(seasons, dtoOptions, user).ConfigureAwait(false))
                .ToArray();

            return new ItemsResult
            {
                TotalRecordCount = returnItems.Length,
                Items = returnItems
            };
        }

        public async Task<object> Get(GetEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            IEnumerable<Episode> episodes;

            if (!string.IsNullOrWhiteSpace(request.SeasonId))
            {
                var season = _libraryManager.GetItemById(new Guid(request.SeasonId)) as Season;

                if (season == null)
                {
                    throw new ResourceNotFoundException("No season exists with Id " + request.SeasonId);
                }

                episodes = season.GetEpisodes(user);
            }
            else if (request.Season.HasValue)
            {
                var series = _libraryManager.GetItemById(request.Id) as Series;

                if (series == null)
                {
                    throw new ResourceNotFoundException("No series exists with Id " + request.Id);
                }

                var season = series.GetSeasons(user).FirstOrDefault(i => i.IndexNumber == request.Season.Value);

                if (season == null)
                {
                    episodes = new List<Episode>();
                }
                else
                {
                    episodes = series.GetSeasonEpisodes(user, season);
                }
            }
            else
            {
                var series = _libraryManager.GetItemById(request.Id) as Series;

                if (series == null)
                {
                    throw new ResourceNotFoundException("No series exists with Id " + request.Id);
                }

                episodes = series.GetEpisodes(user);
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

            if (!string.IsNullOrWhiteSpace(request.StartItemId))
            {
                episodes = episodes.SkipWhile(i => !string.Equals(i.Id.ToString("N"), request.StartItemId, StringComparison.OrdinalIgnoreCase));
            }

            IEnumerable<BaseItem> returnItems = episodes;

            // This must be the last filter
            if (!string.IsNullOrEmpty(request.AdjacentTo))
            {
                returnItems = UserViewBuilder.FilterForAdjacency(returnItems, request.AdjacentTo);
            }

            var returnList = returnItems.ToList();

            var pagedItems = ApplyPaging(returnList, request.StartIndex, request.Limit);

            var dtoOptions = GetDtoOptions(request);

            var dtos = (await _dtoService.GetBaseItemDtos(pagedItems, dtoOptions, user).ConfigureAwait(false))
                .ToArray();

            return new ItemsResult
            {
                TotalRecordCount = returnList.Count,
                Items = dtos
            };
        }
    }
}
