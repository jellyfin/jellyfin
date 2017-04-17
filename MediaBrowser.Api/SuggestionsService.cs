using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Api
{
    [Route("/Users/{UserId}/Suggestions", "GET", Summary = "Gets items based on a query.")]
    public class GetSuggestedItems : IReturn<QueryResult<BaseItem>>
    {
        public string MediaType { get; set; }
        public string Type { get; set; }
        public string UserId { get; set; }
        public bool EnableTotalRecordCount { get; set; }
        public int? StartIndex { get; set; }
        public int? Limit { get; set; }

        public string[] GetMediaTypes()
        {
            return (MediaType ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetIncludeItemTypes()
        {
            return (Type ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public class SuggestionsService : BaseApiService
    {
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        public SuggestionsService(IDtoService dtoService, IAuthorizationContext authContext, IUserManager userManager, ILibraryManager libraryManager)
        {
            _dtoService = dtoService;
            _authContext = authContext;
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        public async Task<object> Get(GetSuggestedItems request)
        {
            var result = await GetResultItems(request).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        private async Task<QueryResult<BaseItemDto>> GetResultItems(GetSuggestedItems request)
        {
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;

            var dtoOptions = GetDtoOptions(_authContext, request);
            var result = GetItems(request, user, dtoOptions);

            var dtoList = await _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user).ConfigureAwait(false);

            if (dtoList == null)
            {
                throw new InvalidOperationException("GetBaseItemDtos returned null");
            }

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = dtoList.ToArray()
            };
        }

        private QueryResult<BaseItem> GetItems(GetSuggestedItems request, User user, DtoOptions dtoOptions)
        {
            return _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                SortBy = new string[] { ItemSortBy.Random },
                MediaTypes = request.GetMediaTypes(),
                IncludeItemTypes = request.GetIncludeItemTypes(),
                IsVirtualItem = false,
                StartIndex = request.StartIndex,
                Limit = request.Limit,
                DtoOptions = dtoOptions,
                EnableTotalRecordCount = request.EnableTotalRecordCount,
                Recursive = true
            });
        }
    }
}
