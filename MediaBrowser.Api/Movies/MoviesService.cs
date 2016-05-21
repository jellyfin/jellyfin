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
            var result = await GetSimilarItemsResult(
                request, SimilarItemsHelper.GetSimiliarityScore).ConfigureAwait(false);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public async Task<object> Get(GetSimilarTrailers request)
        {
            var result = await GetSimilarItemsResult(
                request, SimilarItemsHelper.GetSimiliarityScore).ConfigureAwait(false);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public async Task<object> Get(GetMovieRecommendations request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Movie).Name }
            };

            if (user.Configuration.IncludeTrailersInSuggestions)
            {
                var includeList = query.IncludeItemTypes.ToList();
                includeList.Add(typeof(Trailer).Name);
                query.IncludeItemTypes = includeList.ToArray();
            }

            var parentIds = string.IsNullOrWhiteSpace(request.ParentId) ? new string[] { } : new[] { request.ParentId };
            var movies = _libraryManager.GetItemList(query, parentIds)
                .OrderBy(i => (int)i.SourceType);

            var listEligibleForCategories = new List<BaseItem>();
            var listEligibleForSuggestion = new List<BaseItem>();

            var list = movies.ToList();

            listEligibleForCategories.AddRange(list);
            listEligibleForSuggestion.AddRange(list);

            listEligibleForCategories = listEligibleForCategories
                // Exclude trailers from the suggestion categories
                .Where(i => i is Movie)
                .DistinctBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .DistinctBy(i => i.GetProviderId(MetadataProviders.Imdb) ?? Guid.NewGuid().ToString(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            listEligibleForSuggestion = listEligibleForSuggestion
                .DistinctBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .DistinctBy(i => i.GetProviderId(MetadataProviders.Imdb) ?? Guid.NewGuid().ToString(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var dtoOptions = GetDtoOptions(request);

            dtoOptions.Fields = request.GetItemFields().ToList();

            var result = GetRecommendationCategories(user, listEligibleForCategories, listEligibleForSuggestion, request.CategoryLimit, request.ItemLimit, dtoOptions);

            return ToOptimizedResult(result);
        }

        private async Task<ItemsResult> GetSimilarItemsResult(BaseGetSimilarItemsFromItem request, Func<BaseItem, List<PersonInfo>, List<PersonInfo>, BaseItem, int> getSimilarityScore)
        {
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (!string.IsNullOrWhiteSpace(request.UserId) ? user.RootFolder :
                _libraryManager.RootFolder) : _libraryManager.GetItemById(request.Id);
            
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Movie).Name }
            };

            if (user == null || user.Configuration.IncludeTrailersInSuggestions)
            {
                var includeList = query.IncludeItemTypes.ToList();
                includeList.Add(typeof(Trailer).Name);
                query.IncludeItemTypes = includeList.ToArray();
            }

            var list = _libraryManager.GetItemList(query)
                .OrderBy(i => (int)i.SourceType)
                .DistinctBy(i => i.GetProviderId(MetadataProviders.Imdb) ?? Guid.NewGuid().ToString("N"))
                .ToList();

            if (item is Video)
            {
                var imdbId = item.GetProviderId(MetadataProviders.Imdb);

                // Use imdb id to try to filter duplicates of the same item
                if (!string.IsNullOrWhiteSpace(imdbId))
                {
                    list = list
                        .Where(i => !string.Equals(imdbId, i.GetProviderId(MetadataProviders.Imdb), StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            var items = SimilarItemsHelper.GetSimilaritems(item, _libraryManager, list, getSimilarityScore).ToList();

            IEnumerable<BaseItem> returnItems = items;

            if (request.Limit.HasValue)
            {
                returnItems = returnItems.Take(request.Limit.Value);
            }

            var dtoOptions = GetDtoOptions(request);

            var result = new ItemsResult
            {
                Items = _dtoService.GetBaseItemDtos(returnItems, dtoOptions, user).ToArray(),

                TotalRecordCount = items.Count
            };

            return result;
        }

        private IEnumerable<RecommendationDto> GetRecommendationCategories(User user, List<BaseItem> allMoviesForCategories, List<BaseItem> allMovies, int categoryLimit, int itemLimit, DtoOptions dtoOptions)
        {
            var categories = new List<RecommendationDto>();

            var recentlyPlayedMovies = allMoviesForCategories
                .Select(i =>
                {
                    var userdata = _userDataRepository.GetUserData(user, i);
                    return new Tuple<BaseItem, bool, DateTime>(i, userdata.Played, userdata.LastPlayedDate ?? DateTime.MinValue);
                })
                .Where(i => i.Item2)
                .OrderByDescending(i => i.Item3)
                .Select(i => i.Item1)
                .ToList();

            var excludeFromLiked = recentlyPlayedMovies.Take(10);
            var likedMovies = allMovies
                .Select(i =>
                {
                    var score = 0;
                    var userData = _userDataRepository.GetUserData(user, i);

                    if (userData.IsFavorite)
                    {
                        score = 2;
                    }
                    else
                    {
                        score = userData.Likes.HasValue ? userData.Likes.Value ? 1 : -1 : 0;
                    }

                    return new Tuple<BaseItem, int>(i, score);
                })
                .OrderByDescending(i => i.Item2)
                .ThenBy(i => Guid.NewGuid())
                .Where(i => i.Item2 > 0)
                .Select(i => i.Item1)
                .Where(i => !excludeFromLiked.Contains(i));

            var mostRecentMovies = recentlyPlayedMovies.Take(6).ToList();
            // Get recently played directors
            var recentDirectors = GetDirectors(mostRecentMovies)
                .OrderBy(i => Guid.NewGuid())
                .ToList();

            // Get recently played actors
            var recentActors = GetActors(mostRecentMovies)
                .OrderBy(i => Guid.NewGuid())
                .ToList();

            var similarToRecentlyPlayed = GetSimilarTo(user, allMovies, recentlyPlayedMovies.Take(7).OrderBy(i => Guid.NewGuid()), itemLimit, dtoOptions, RecommendationType.SimilarToRecentlyPlayed).GetEnumerator();
            var similarToLiked = GetSimilarTo(user, allMovies, likedMovies, itemLimit, dtoOptions, RecommendationType.SimilarToLikedItem).GetEnumerator();

            var hasDirectorFromRecentlyPlayed = GetWithDirector(user, allMovies, recentDirectors, itemLimit, dtoOptions, RecommendationType.HasDirectorFromRecentlyPlayed).GetEnumerator();
            var hasActorFromRecentlyPlayed = GetWithActor(user, allMovies, recentActors, itemLimit, dtoOptions, RecommendationType.HasActorFromRecentlyPlayed).GetEnumerator();

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

        private IEnumerable<RecommendationDto> GetWithDirector(User user, List<BaseItem> allMovies, IEnumerable<string> directors, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            var userId = user.Id;

            foreach (var director in directors)
            {
                var items = allMovies
                    .Where(i => _libraryManager.GetPeople(i).Any(p => string.Equals(p.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase) && string.Equals(p.Name, director, StringComparison.OrdinalIgnoreCase)))
                    .Take(itemLimit)
                    .ToList();

                if (items.Count > 0)
                {
                    yield return new RecommendationDto
                    {
                        BaselineItemName = director,
                        CategoryId = director.GetMD5().ToString("N"),
                        RecommendationType = type,
                        Items = _dtoService.GetBaseItemDtos(items, dtoOptions, user).ToArray()
                    };
                }
            }
        }

        private IEnumerable<RecommendationDto> GetWithActor(User user, List<BaseItem> allMovies, IEnumerable<string> names, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            foreach (var name in names)
            {
                var itemsWithActor = _libraryManager.GetItemIds(new InternalItemsQuery(user)
                {
                    Person = name

                });

                var items = allMovies
                    .Where(i => itemsWithActor.Contains(i.Id))
                    .Take(itemLimit)
                    .ToList();

                if (items.Count > 0)
                {
                    yield return new RecommendationDto
                    {
                        BaselineItemName = name,
                        CategoryId = name.GetMD5().ToString("N"),
                        RecommendationType = type,
                        Items = _dtoService.GetBaseItemDtos(items, dtoOptions, user).ToArray()
                    };
                }
            }
        }

        private IEnumerable<RecommendationDto> GetSimilarTo(User user, List<BaseItem> allMovies, IEnumerable<BaseItem> baselineItems, int itemLimit, DtoOptions dtoOptions, RecommendationType type)
        {
            foreach (var item in baselineItems)
            {
                var similar = SimilarItemsHelper
                    .GetSimilaritems(item, _libraryManager, allMovies, SimilarItemsHelper.GetSimiliarityScore)
                    .Take(itemLimit)
                    .ToList();

                if (similar.Count > 0)
                {
                    yield return new RecommendationDto
                    {
                        BaselineItemName = item.Name,
                        CategoryId = item.Id.ToString("N"),
                        RecommendationType = type,
                        Items = _dtoService.GetBaseItemDtos(similar, dtoOptions, user).ToArray()
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
