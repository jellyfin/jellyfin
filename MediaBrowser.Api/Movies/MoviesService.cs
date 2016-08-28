using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MoreLinq;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;

namespace MediaBrowser.Api.Movies
{
    /// <summary>
    /// Class GetSimilarMovies
    /// </summary>
    [Route("/Movies/{Id}/Similar", "GET", Summary = "Finds movies and trailers similar to a given movie.")]
    public class GetSimilarMovies : BaseGetSimilarItemsFromItem
    {
    }

    /// <summary>
    /// Class GetSimilarTrailers
    /// </summary>
    [Route("/Trailers/{Id}/Similar", "GET", Summary = "Finds movies and trailers similar to a given trailer.")]
    public class GetSimilarTrailers : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Movies/Recommendations", "GET", Summary = "Gets movie recommendations")]
    public class GetMovieRecommendations : IReturn<RecommendationDto[]>, IHasItemFields
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
        public string UserId { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }

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

        /// <summary>
        /// The _user data repository
        /// </summary>
        private readonly IUserDataManager _userDataRepository;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly IItemRepository _itemRepo;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoviesService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="itemRepo">The item repo.</param>
        /// <param name="dtoService">The dto service.</param>
        public MoviesService(IUserManager userManager, IUserDataManager userDataRepository, ILibraryManager libraryManager, IItemRepository itemRepo, IDtoService dtoService)
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
        public async Task<object> Get(GetSimilarMovies request)
        {
            var result = await GetSimilarItemsResult(request).ConfigureAwait(false);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public async Task<object> Get(GetSimilarTrailers request)
        {
            var result = await GetSimilarItemsResult(request).ConfigureAwait(false);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public async Task<object> Get(GetMovieRecommendations request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(request);

            dtoOptions.Fields = request.GetItemFields().ToList();

            var result = GetRecommendationCategories(user, request.ParentId, request.CategoryLimit, request.ItemLimit, dtoOptions);

            return ToOptimizedResult(result);
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
                        typeof(Movie).Name,
                        typeof(Trailer).Name,
                        typeof(LiveTvProgram).Name
                },
                IsMovie = true,
                SimilarTo = item,
                EnableGroupByMetadataKey = true

            }).ToList();

            var dtoOptions = GetDtoOptions(request);

            var result = new QueryResult<BaseItemDto>
            {
                Items = (await _dtoService.GetBaseItemDtos(itemsResult, dtoOptions, user).ConfigureAwait(false)).ToArray(),

                TotalRecordCount = itemsResult.Count
            };

            return result;
        }

        private IEnumerable<RecommendationDto> GetRecommendationCategories(User user, string parentId, int categoryLimit, int itemLimit, DtoOptions dtoOptions)
        {
            var categories = new List<RecommendationDto>();

            var parentIdGuid = string.IsNullOrWhiteSpace(parentId) ? (Guid?)null : new Guid(parentId);

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[]
                {
                    typeof(Movie).Name,
                    //typeof(Trailer).Name,
                    //typeof(LiveTvProgram).Name
                },
                // IsMovie = true
                SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.Random },
                SortOrder = SortOrder.Descending,
                Limit = 7,
                ParentId = parentIdGuid,
                Recursive = true,
                IsPlayed = true
            };

            var recentlyPlayedMovies = _libraryManager.GetItemList(query).ToList();

            var likedMovies = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[]
                {
                   typeof(Movie).Name,
                   typeof(Trailer).Name,
                   typeof(LiveTvProgram).Name
                },
                IsMovie = true,
                SortBy = new[] { ItemSortBy.Random },
                SortOrder = SortOrder.Descending,
                Limit = 10,
                IsFavoriteOrLiked = true,
                ExcludeItemIds = recentlyPlayedMovies.Select(i => i.Id.ToString("N")).ToArray(),
                EnableGroupByMetadataKey = true,
                ParentId = parentIdGuid,
                Recursive = true

            }).ToList();

            var mostRecentMovies = recentlyPlayedMovies.Take(6).ToList();
            // Get recently played directors
            var recentDirectors = GetDirectors(mostRecentMovies)
                .OrderBy(i => Guid.NewGuid())
                .ToList();

            // Get recently played actors
            var recentActors = GetActors(mostRecentMovies)
                .OrderBy(i => Guid.NewGuid())
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

            return categories.OrderBy(i => i.RecommendationType).ThenBy(i => Guid.NewGuid());
        }

        private IEnumerable<RecommendationDto> GetWithDirector(User user, IEnumerable<string> names, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            foreach (var name in names)
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Person = name,
                    // Account for duplicates by imdb id, since the database doesn't support this yet
                    Limit = itemLimit + 2,
                    PersonTypes = new[] { PersonType.Director },
                    IncludeItemTypes = new[]
                    {
                        typeof(Movie).Name,
                        typeof(Trailer).Name,
                        typeof(LiveTvProgram).Name
                    },
                    IsMovie = true,
                    EnableGroupByMetadataKey = true

                }).DistinctBy(i => i.GetProviderId(MetadataProviders.Imdb) ?? Guid.NewGuid().ToString("N"))
                .Take(itemLimit)
                .ToList();

                if (items.Count > 0)
                {
                    yield return new RecommendationDto
                    {
                        BaselineItemName = name,
                        CategoryId = name.GetMD5().ToString("N"),
                        RecommendationType = type,
                        Items = _dtoService.GetBaseItemDtos(items, dtoOptions, user).Result.ToArray()
                    };
                }
            }
        }

        private IEnumerable<RecommendationDto> GetWithActor(User user, IEnumerable<string> names, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            foreach (var name in names)
            {
                var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Person = name,
                    // Account for duplicates by imdb id, since the database doesn't support this yet
                    Limit = itemLimit + 2,
                    IncludeItemTypes = new[]
                    {
                        typeof(Movie).Name,
                        typeof(Trailer).Name,
                        typeof(LiveTvProgram).Name
                    },
                    IsMovie = true,
                    EnableGroupByMetadataKey = true

                }).DistinctBy(i => i.GetProviderId(MetadataProviders.Imdb) ?? Guid.NewGuid().ToString("N"))
                .Take(itemLimit)
                .ToList();

                if (items.Count > 0)
                {
                    yield return new RecommendationDto
                    {
                        BaselineItemName = name,
                        CategoryId = name.GetMD5().ToString("N"),
                        RecommendationType = type,
                        Items = _dtoService.GetBaseItemDtos(items, dtoOptions, user).Result.ToArray()
                    };
                }
            }
        }

        private IEnumerable<RecommendationDto> GetSimilarTo(User user, List<BaseItem> baselineItems, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            foreach (var item in baselineItems)
            {
                var similar = _libraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Limit = itemLimit,
                    IncludeItemTypes = new[]
                    {
                        typeof(Movie).Name,
                        typeof(Trailer).Name,
                        typeof(LiveTvProgram).Name
                    },
                    IsMovie = true,
                    SimilarTo = item,
                    EnableGroupByMetadataKey = true

                }).ToList();

                if (similar.Count > 0)
                {
                    yield return new RecommendationDto
                    {
                        BaselineItemName = item.Name,
                        CategoryId = item.Id.ToString("N"),
                        RecommendationType = type,
                        Items = _dtoService.GetBaseItemDtos(similar, dtoOptions, user).Result.ToArray()
                    };
                }
            }
        }

        private IEnumerable<string> GetActors(IEnumerable<BaseItem> items)
        {
            var people = _libraryManager.GetPeople(new InternalPeopleQuery
            {
                ExcludePersonTypes = new List<string>
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

        private IEnumerable<string> GetDirectors(IEnumerable<BaseItem> items)
        {
            var people = _libraryManager.GetPeople(new InternalPeopleQuery
            {
                PersonTypes = new List<string>
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
