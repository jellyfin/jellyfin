using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetNextUpEpisodes
    /// </summary>
    [Route("/Shows/NextUp", "GET", Summary = "Gets a list of next up episodes")]
    public class GetNextUpEpisodes : IReturn<QueryResult<BaseItemDto>>, IHasDtoOptions
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
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
        public bool EnableTotalRecordCount { get; set; }

        public GetNextUpEpisodes()
        {
            EnableTotalRecordCount = true;
        }
    }

    [Route("/Shows/Upcoming", "GET", Summary = "Gets a list of upcoming episodes")]
    public class GetUpcomingEpisodes : IReturn<QueryResult<BaseItemDto>>, IHasDtoOptions
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
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

    [Route("/Shows/{Id}/Episodes", "GET", Summary = "Gets episodes for a tv season")]
    public class GetEpisodes : IReturn<QueryResult<BaseItemDto>>, IHasItemFields, IHasDtoOptions
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "Id", Description = "The series id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Season", Description = "Optional filter by season number.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public int? Season { get; set; }

        [ApiMember(Name = "SeasonId", Description = "Optional. Filter by season id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SeasonId { get; set; }

        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

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

        [ApiMember(Name = "SortBy", Description = "Optional. Specify one or more sort orders, comma delimeted. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SortBy { get; set; }

        [ApiMember(Name = "SortOrder", Description = "Sort Order - Ascending,Descending", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public SortOrder? SortOrder { get; set; }
    }

    [Route("/Shows/{Id}/Seasons", "GET", Summary = "Gets seasons for a tv series")]
    public class GetSeasons : IReturn<QueryResult<BaseItemDto>>, IHasItemFields, IHasDtoOptions
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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "Id", Description = "The series id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "IsSpecialSeason", Description = "Optional. Filter by special season.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsSpecialSeason { get; set; }

        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

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
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly IDtoService _dtoService;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly IAuthorizationContext _authContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvShowsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataManager">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public TvShowsService(
            ILogger<TvShowsService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            ITVSeriesManager tvSeriesManager,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _tvSeriesManager = tvSeriesManager;
            _authContext = authContext;
        }

        public object Get(GetUpcomingEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var minPremiereDate = DateTime.Now.Date.ToUniversalTime().AddDays(-1);

            var parentIdGuid = string.IsNullOrWhiteSpace(request.ParentId) ? Guid.Empty : new Guid(request.ParentId);

            var options = GetDtoOptions(_authContext, request);

            var itemsResult = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Episode).Name },
                OrderBy = new[] { ItemSortBy.PremiereDate, ItemSortBy.SortName }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Ascending)).ToArray(),
                MinPremiereDate = minPremiereDate,
                StartIndex = request.StartIndex,
                Limit = request.Limit,
                ParentId = parentIdGuid,
                Recursive = true,
                DtoOptions = options

            });

            var returnItems = _dtoService.GetBaseItemDtos(itemsResult, options, user);

            var result = new QueryResult<BaseItemDto>
            {
                TotalRecordCount = itemsResult.Count,
                Items = returnItems
            };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetNextUpEpisodes request)
        {
            var options = GetDtoOptions(_authContext, request);

            var result = _tvSeriesManager.GetNextUp(new NextUpQuery
            {
                Limit = request.Limit,
                ParentId = request.ParentId,
                SeriesId = request.SeriesId,
                StartIndex = request.StartIndex,
                UserId = request.UserId,
                EnableTotalRecordCount = request.EnableTotalRecordCount
            }, options);

            var user = _userManager.GetUserById(request.UserId);

            var returnItems = _dtoService.GetBaseItemDtos(result.Items, options, user);

            return ToOptimizedResult(new QueryResult<BaseItemDto>
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

        public object Get(GetSeasons request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var series = GetSeries(request.Id, user);

            if (series == null)
            {
                throw new ResourceNotFoundException("Series not found");
            }

            var seasons = series.GetItemList(new InternalItemsQuery(user)
            {
                IsMissing = request.IsMissing,
                IsSpecialSeason = request.IsSpecialSeason,
                AdjacentTo = request.AdjacentTo

            });

            var dtoOptions = GetDtoOptions(_authContext, request);

            var returnItems = _dtoService.GetBaseItemDtos(seasons, dtoOptions, user);

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = returnItems.Count,
                Items = returnItems
            };
        }

        private Series GetSeries(string seriesId, User user)
        {
            if (!string.IsNullOrWhiteSpace(seriesId))
            {
                return _libraryManager.GetItemById(seriesId) as Series;
            }

            return null;
        }

        public object Get(GetEpisodes request)
        {
            var user = _userManager.GetUserById(request.UserId);

            List<BaseItem> episodes;

            var dtoOptions = GetDtoOptions(_authContext, request);

            if (!string.IsNullOrWhiteSpace(request.SeasonId))
            {
                if (!(_libraryManager.GetItemById(new Guid(request.SeasonId)) is Season season))
                {
                    throw new ResourceNotFoundException("No season exists with Id " + request.SeasonId);
                }

                episodes = season.GetEpisodes(user, dtoOptions);
            }
            else if (request.Season.HasValue)
            {
                var series = GetSeries(request.Id, user);

                if (series == null)
                {
                    throw new ResourceNotFoundException("Series not found");
                }

                var season = series.GetSeasons(user, dtoOptions).FirstOrDefault(i => i.IndexNumber == request.Season.Value);

                episodes = season == null ? new List<BaseItem>() : ((Season)season).GetEpisodes(user, dtoOptions);
            }
            else
            {
                var series = GetSeries(request.Id, user);

                if (series == null)
                {
                    throw new ResourceNotFoundException("Series not found");
                }

                episodes = series.GetEpisodes(user, dtoOptions).ToList();
            }

            // Filter after the fact in case the ui doesn't want them
            if (request.IsMissing.HasValue)
            {
                var val = request.IsMissing.Value;
                episodes = episodes.Where(i => ((Episode)i).IsMissingEpisode == val).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.StartItemId))
            {
                episodes = episodes.SkipWhile(i => !string.Equals(i.Id.ToString("N", CultureInfo.InvariantCulture), request.StartItemId, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // This must be the last filter
            if (!string.IsNullOrEmpty(request.AdjacentTo))
            {
                episodes = UserViewBuilder.FilterForAdjacency(episodes, request.AdjacentTo).ToList();
            }

            if (string.Equals(request.SortBy, ItemSortBy.Random, StringComparison.OrdinalIgnoreCase))
            {
                episodes.Shuffle();
            }

            var returnItems = episodes;

            if (request.StartIndex.HasValue || request.Limit.HasValue)
            {
                returnItems = ApplyPaging(episodes, request.StartIndex, request.Limit).ToList();
            }

            var dtos = _dtoService.GetBaseItemDtos(returnItems, dtoOptions, user);

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = episodes.Count,
                Items = dtos
            };
        }
    }
}
