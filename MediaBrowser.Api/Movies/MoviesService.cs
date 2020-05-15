using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Movies
{
    [Route("/Movies/Recommendations", "GET", Summary = "Gets movie recommendations")]
    public class GetMovieRecommendations : IReturn<RecommendationDto[]>, IHasDtoOptions
    {
        [ApiMember(Name = "CategoryLimit", Description = "The max number of categories to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int CategoryLimit { get; set; }

        [ApiMember(Name = "ItemLimit", Description = "The max number of items to return per category", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int ItemLimit { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        public GetMovieRecommendations()
        {
            CategoryLimit = 5;
            ItemLimit = 8;
        }

        public string Fields { get; set; }
    }

    /// <summary>
    /// Class MoviesService
    /// </summary>
    [Authenticated]
    public class MoviesService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        private readonly ILibraryManager _libraryManager;

        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoviesService" /> class.
        /// </summary>
        public MoviesService(
            ILogger<MoviesService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _authContext = authContext;
        }

        public object Get(GetMovieRecommendations request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var result = GetRecommendationCategories(user, request.ParentId, request.CategoryLimit, request.ItemLimit, dtoOptions);

            return ToOptimizedResult(result);
        }

        public QueryResult<BaseItemDto> GetSimilarItemsResult(BaseGetSimilarItemsFromItem request)
        {
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (!request.UserId.Equals(Guid.Empty) ? _libraryManager.GetUserRootFolder() :
                _libraryManager.RootFolder) : _libraryManager.GetItemById(request.Id);

            var itemTypes = new List<string> { typeof(Movie).Name };
            if (ServerConfigurationManager.Configuration.EnableExternalContentInSuggestions)
            {
                itemTypes.Add(typeof(Trailer).Name);
                itemTypes.Add(typeof(LiveTvProgram).Name);
            }

            var dtoOptions = GetDtoOptions(_authContext, request);

            var itemsResult = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Limit = request.Limit,
                IncludeItemTypes = itemTypes.ToArray(),
                IsMovie = true,
                SimilarTo = item,
                EnableGroupByMetadataKey = true,
                DtoOptions = dtoOptions

            });

            var returnList = _dtoService.GetBaseItemDtos(itemsResult, dtoOptions, user);

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnList,

                TotalRecordCount = itemsResult.Count
            };

            return result;
        }

        private IEnumerable<RecommendationDto> GetRecommendationCategories(User user, string parentId, int categoryLimit, int itemLimit, DtoOptions dtoOptions)
        {
            var categories = new List<RecommendationDto>();

            var parentIdGuid = string.IsNullOrWhiteSpace(parentId) ? Guid.Empty : new Guid(parentId);

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[]
                {
                    typeof(Movie).Name,
                    //typeof(Trailer).Name,
                    //typeof(LiveTvProgram).Name
                },
                // IsMovie = true
                OrderBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.Random }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray(),
                Limit = 7,
                ParentId = parentIdGuid,
                Recursive = true,
                IsPlayed = true,
                DtoOptions = dtoOptions
            };

            var recentlyPlayedMovies = _libraryManager.GetItemList(query);

            var itemTypes = new List<string> { typeof(Movie).Name };
            if (ServerConfigurationManager.Configuration.EnableExternalContentInSuggestions)
            {
                itemTypes.Add(typeof(Trailer).Name);
                itemTypes.Add(typeof(LiveTvProgram).Name);
            }

            var likedMovies = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = itemTypes.ToArray(),
                IsMovie = true,
                OrderBy = new[] { ItemSortBy.Random }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray(),
                Limit = 10,
                IsFavoriteOrLiked = true,
                ExcludeItemIds = recentlyPlayedMovies.Select(i => i.Id).ToArray(),
                EnableGroupByMetadataKey = true,
                ParentId = parentIdGuid,
                Recursive = true,
                DtoOptions = dtoOptions

            });

            var mostRecentMovies = recentlyPlayedMovies.Take(6).ToList();
            // Get recently played directors
            var recentDirectors = GetDirectors(mostRecentMovies)
                .ToList();

            // Get recently played actors
            var recentActors = GetActors(mostRecentMovies)
                .ToList();

            var similarToRecentlyPlayed = GetSimilarTo(user, recentlyPlayedMovies, itemLimit, dtoOptions, RecommendationType.SimilarToRecentlyPlayed).GetEnumerator();
            var similarToLiked = GetSimilarTo(user, likedMovies, itemLimit, dtoOptions, RecommendationType.SimilarToLikedItem).GetEnumerator();

            var hasDirectorFromRecentlyPlayed = GetWithDirector(user, recentDirectors, itemLimit, dtoOptions, RecommendationType.HasDirectorFromRecentlyPlayed).GetEnumerator();
            var hasActorFromRecentlyPlayed = GetWithActor(user, recentActors, itemLimit, dtoOptions, RecommendationType.HasActorFromRecentlyPlayed).GetEnumerator();

            var categoryTypes = new List<IEnumerator<RecommendationDto>>
            {
                // Give this extra weight
                similarToRecentlyPlayed,
                similarToRecentlyPlayed,

                // Give this extra weight
                similarToLiked,
                similarToLiked,

                hasDirectorFromRecentlyPlayed,
                hasActorFromRecentlyPlayed
            };

            while (categories.Count < categoryLimit)
            {
                var allEmpty = true;

                foreach (var category in categoryTypes)
                {
                    if (category.MoveNext())
                    {
                        categories.Add(category.Current);
                        allEmpty = false;

                        if (categories.Count >= categoryLimit)
                        {
                            break;
                        }
                    }
                }

                if (allEmpty)
                {
                    break;
                }
            }

            return categories.OrderBy(i => i.RecommendationType);
        }

        private IEnumerable<RecommendationDto> GetWithDirector(User user, IEnumerable<string> names, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            var itemTypes = new List<string> { typeof(Movie).Name };
            if (ServerConfigurationManager.Configuration.EnableExternalContentInSuggestions)
            {
                itemTypes.Add(typeof(Trailer).Name);
                itemTypes.Add(typeof(LiveTvProgram).Name);
            }

            foreach (var name in names)
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Person = name,
                    // Account for duplicates by imdb id, since the database doesn't support this yet
                    Limit = itemLimit + 2,
                    PersonTypes = new[] { PersonType.Director },
                    IncludeItemTypes = itemTypes.ToArray(),
                    IsMovie = true,
                    EnableGroupByMetadataKey = true,
                    DtoOptions = dtoOptions

                }).GroupBy(i => i.GetProviderId(MetadataProviders.Imdb) ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture))
                .Select(x => x.First())
                .Take(itemLimit)
                .ToList();

                if (items.Count > 0)
                {
                    var returnItems = _dtoService.GetBaseItemDtos(items, dtoOptions, user);

                    yield return new RecommendationDto
                    {
                        BaselineItemName = name,
                        CategoryId = name.GetMD5(),
                        RecommendationType = type,
                        Items = returnItems
                    };
                }
            }
        }

        private IEnumerable<RecommendationDto> GetWithActor(User user, IEnumerable<string> names, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            var itemTypes = new List<string> { typeof(Movie).Name };
            if (ServerConfigurationManager.Configuration.EnableExternalContentInSuggestions)
            {
                itemTypes.Add(typeof(Trailer).Name);
                itemTypes.Add(typeof(LiveTvProgram).Name);
            }

            foreach (var name in names)
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Person = name,
                    // Account for duplicates by imdb id, since the database doesn't support this yet
                    Limit = itemLimit + 2,
                    IncludeItemTypes = itemTypes.ToArray(),
                    IsMovie = true,
                    EnableGroupByMetadataKey = true,
                    DtoOptions = dtoOptions

                }).GroupBy(i => i.GetProviderId(MetadataProviders.Imdb) ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture))
                .Select(x => x.First())
                .Take(itemLimit)
                .ToList();

                if (items.Count > 0)
                {
                    var returnItems = _dtoService.GetBaseItemDtos(items, dtoOptions, user);

                    yield return new RecommendationDto
                    {
                        BaselineItemName = name,
                        CategoryId = name.GetMD5(),
                        RecommendationType = type,
                        Items = returnItems
                    };
                }
            }
        }

        private IEnumerable<RecommendationDto> GetSimilarTo(User user, List<BaseItem> baselineItems, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            var itemTypes = new List<string> { typeof(Movie).Name };
            if (ServerConfigurationManager.Configuration.EnableExternalContentInSuggestions)
            {
                itemTypes.Add(typeof(Trailer).Name);
                itemTypes.Add(typeof(LiveTvProgram).Name);
            }

            foreach (var item in baselineItems)
            {
                var similar = _libraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Limit = itemLimit,
                    IncludeItemTypes = itemTypes.ToArray(),
                    IsMovie = true,
                    SimilarTo = item,
                    EnableGroupByMetadataKey = true,
                    DtoOptions = dtoOptions

                });

                if (similar.Count > 0)
                {
                    var returnItems = _dtoService.GetBaseItemDtos(similar, dtoOptions, user);

                    yield return new RecommendationDto
                    {
                        BaselineItemName = item.Name,
                        CategoryId = item.Id,
                        RecommendationType = type,
                        Items = returnItems
                    };
                }
            }
        }

        private IEnumerable<string> GetActors(List<BaseItem> items)
        {
            var people = _libraryManager.GetPeople(new InternalPeopleQuery
            {
                ExcludePersonTypes = new[]
                {
                    PersonType.Director
                },
                MaxListOrder = 3
            });

            var itemIds = items.Select(i => i.Id).ToList();

            return people
                .Where(i => itemIds.Contains(i.ItemId))
                .Select(i => i.Name)
                .DistinctNames();
        }

        private IEnumerable<string> GetDirectors(List<BaseItem> items)
        {
            var people = _libraryManager.GetPeople(new InternalPeopleQuery
            {
                PersonTypes = new[]
                {
                    PersonType.Director
                }
            });

            var itemIds = items.Select(i => i.Id).ToList();

            return people
                .Where(i => itemIds.Contains(i.ItemId))
                .Select(i => i.Name)
                .DistinctNames();
        }
    }
}
