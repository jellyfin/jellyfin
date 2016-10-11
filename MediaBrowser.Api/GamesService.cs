using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSimilarGames
    /// </summary>
    [Route("/Games/{Id}/Similar", "GET", Summary = "Finds games similar to a given game.")]
    public class GetSimilarGames : BaseGetSimilarItemsFromItem
    {
    }

    /// <summary>
    /// Class GetGameSystemSummaries
    /// </summary>
    [Route("/Games/SystemSummaries", "GET", Summary = "Finds games similar to a given game.")]
    public class GetGameSystemSummaries : IReturn<List<GameSystemSummary>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    /// <summary>
    /// Class GetGameSystemSummaries
    /// </summary>
    [Route("/Games/PlayerIndex", "GET", Summary = "Gets an index of players (1-x) and the number of games listed under each")]
    public class GetPlayerIndex : IReturn<List<ItemIndex>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    /// <summary>
    /// Class GamesService
    /// </summary>
    [Authenticated]
    public class GamesService : BaseApiService
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

        /// <summary>
        /// The _item repo
        /// </summary>
        private readonly IItemRepository _itemRepo;
        /// <summary>
        /// The _dto service
        /// </summary>
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="itemRepo">The item repo.</param>
        /// <param name="dtoService">The dto service.</param>
        public GamesService(IUserManager userManager, IUserDataManager userDataRepository, ILibraryManager libraryManager, IItemRepository itemRepo, IDtoService dtoService)
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
        public object Get(GetGameSystemSummaries request)
        {
            var user = request.UserId == null ? null : _userManager.GetUserById(request.UserId);
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(GameSystem).Name }
            };
            var gameSystems = _libraryManager.GetItemList(query)
                .Cast<GameSystem>()
                .ToList();

            var result = gameSystems
                .Select(i => GetSummary(i, user))
                .ToList();

            return ToOptimizedSerializedResultUsingCache(result);
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public object Get(GetPlayerIndex request)
        {
            var user = request.UserId == null ? null : _userManager.GetUserById(request.UserId);
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Game).Name }
            };
            var games = _libraryManager.GetItemList(query)
                .Cast<Game>()
                .ToList();

            var lookup = games
                .ToLookup(i => i.PlayersSupported ?? -1)
                .OrderBy(i => i.Key)
                .Select(i => new ItemIndex
                {
                    ItemCount = i.Count(),
                    Name = i.Key == -1 ? string.Empty : i.Key.ToString(UsCulture)
                })
                .ToList();

            return ToOptimizedSerializedResultUsingCache(lookup);
        }

        /// <summary>
        /// Gets the summary.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <param name="user">The user.</param>
        /// <returns>GameSystemSummary.</returns>
        private GameSystemSummary GetSummary(GameSystem system, User user)
        {
            var summary = new GameSystemSummary
            {
                Name = system.GameSystemName,
                DisplayName = system.Name
            };

            var items = user == null ? 
                system.GetRecursiveChildren(i => i is Game) :
                system.GetRecursiveChildren(user, new InternalItemsQuery(user)
                {
                    IncludeItemTypes = new[] { typeof(Game).Name }
                });

            var games = items.Cast<Game>().ToList();

            summary.ClientInstalledGameCount = games.Count(i => i.IsPlaceHolder);

            summary.GameCount = games.Count;

            summary.GameFileExtensions = games.Where(i => !i.IsPlaceHolder).Select(i => Path.GetExtension(i.Path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return summary;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetSimilarGames request)
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

            var dtoOptions = GetDtoOptions(request);

            var itemsResult = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                Limit = request.Limit,
                IncludeItemTypes = new[]
                {
                        typeof(Game).Name
                },
                SimilarTo = item,
                DtoOptions = dtoOptions

            }).ToList();

            var result = new QueryResult<BaseItemDto>
            {
                Items = (await _dtoService.GetBaseItemDtos(itemsResult, dtoOptions, user).ConfigureAwait(false)).ToArray(),

                TotalRecordCount = itemsResult.Count
            };

            return result;
        }
    }
}
