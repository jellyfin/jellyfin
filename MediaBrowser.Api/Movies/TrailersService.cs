using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Movies
{
    [Route("/Trailers", "GET", Summary = "Finds movies and trailers similar to a given trailer.")]
    public class Getrailers : BaseItemsRequest, IReturn<ItemsResult>
    {
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

        private readonly IDtoService _dtoService;
        private readonly IChannelManager _channelManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrailersService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        public TrailersService(IUserManager userManager, IUserDataManager userDataRepository, ILibraryManager libraryManager, IDtoService dtoService, IChannelManager channelManager)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _channelManager = channelManager;
        }

        public async Task<object> Get(Getrailers request)
        {
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;
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

            var dtoOptions = GetDtoOptions(request);

            var returnItems = _dtoService.GetBaseItemDtos(pagedItems, dtoOptions, user).ToArray();

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
