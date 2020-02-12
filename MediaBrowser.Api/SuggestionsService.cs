using System;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    [Route("/Users/{UserId}/Suggestions", "GET", Summary = "Gets items based on a query.")]
    public class GetSuggestedItems : IReturn<QueryResult<BaseItemDto>>
    {
        public string MediaType { get; set; }
        public string Type { get; set; }
        public Guid UserId { get; set; }
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

        public SuggestionsService(
            ILogger<SuggestionsService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IDtoService dtoService,
            IAuthorizationContext authContext,
            IUserManager userManager,
            ILibraryManager libraryManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _dtoService = dtoService;
            _authContext = authContext;
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        public object Get(GetSuggestedItems request)
        {
            return GetResultItems(request);
        }

        private QueryResult<BaseItemDto> GetResultItems(GetSuggestedItems request)
        {
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            var dtoOptions = GetDtoOptions(_authContext, request);
            var result = GetItems(request, user, dtoOptions);

            var dtoList = _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user);

            return new QueryResult<BaseItemDto>
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = dtoList
            };
        }

        private QueryResult<BaseItem> GetItems(GetSuggestedItems request, User user, DtoOptions dtoOptions)
        {
            return _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                OrderBy = new[] { ItemSortBy.Random }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray(),
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
