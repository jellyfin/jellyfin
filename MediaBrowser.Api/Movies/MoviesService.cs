using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace MediaBrowser.Api.Movies
{
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
    }
}
