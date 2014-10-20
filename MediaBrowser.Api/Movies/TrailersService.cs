using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Movies
{
    /// <summary>
    /// Class GetSimilarTrailers
    /// </summary>
    [Route("/Trailers/{Id}/Similar", "GET", Summary = "Finds movies and trailers similar to a given trailer.")]
    public class GetSimilarTrailers : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Trailers", "GET", Summary = "Finds movies and trailers similar to a given trailer.")]
    public class Getrailers : BaseItemsRequest, IReturn<ItemsResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Class TrailersService
    /// </summary>
    [Authenticated]
    public class TrailersService : BaseApiService
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
        private readonly IChannelManager _channelManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrailersService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public TrailersService(IUserManager userManager, IUserDataManager userDataRepository, ILibraryManager libraryManager, IItemRepository itemRepo, IDtoService dtoService, IChannelManager channelManager)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _dtoService = dtoService;
            _channelManager = channelManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSimilarTrailers request)
        {
            var result = SimilarItemsHelper.GetSimilarItemsResult(_userManager,
                _itemRepo,
                _libraryManager,
                _userDataRepository,
                _dtoService,
                Logger,

                // Strip out secondary versions
                request, item => (item is Movie || item is Trailer) && !((Video)item).PrimaryVersionId.HasValue,

                SimilarItemsHelper.GetSimiliarityScore);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public async Task<object> Get(Getrailers request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;
            var result = await GetAllTrailers(user).ConfigureAwait(false);

            IEnumerable<BaseItem> items = result.Items;

            // Apply filters
            // Run them starting with the ones that are likely to reduce the list the most
            foreach (var filter in request.GetFilters().OrderByDescending(f => (int)f))
            {
                items = ItemsService.ApplyFilter(items, filter, user, _userDataRepository);
            }

            items = _libraryManager.Sort(items, user, request.GetOrderBy(), request.SortOrder ?? SortOrder.Ascending);

            var itemsArray = items.ToList();

            var pagedItems = ApplyPaging(request, itemsArray);

            var fields = request.GetItemFields().ToList();

            var returnItems = pagedItems.Select(i => _dtoService.GetBaseItemDto(i, fields, user)).ToArray();

            return new ItemsResult
            {
                TotalRecordCount = itemsArray.Count,
                Items = returnItems
            };
        }

        private IEnumerable<BaseItem> ApplyPaging(Getrailers request, IEnumerable<BaseItem> items)
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

        private async Task<QueryResult<BaseItem>> GetAllTrailers(User user)
        {
            var trailerResult = await _channelManager.GetAllMediaInternal(new AllChannelMediaQuery
            {
                ContentTypes = new[] { ChannelMediaContentType.MovieExtra },
                ExtraTypes = new[] { ExtraType.Trailer },
                UserId = user.Id.ToString("N")

            }, CancellationToken.None).ConfigureAwait(false);


            return new QueryResult<BaseItem>
            {
                Items = trailerResult.Items,
                TotalRecordCount = trailerResult.TotalRecordCount
            };
        }
    }
}
