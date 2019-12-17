using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Movies
{
    [Route("/Trailers", "GET", Summary = "Finds movies and trailers similar to a given trailer.")]
    public class Getrailers : BaseItemsRequest, IReturn<QueryResult<BaseItemDto>>
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
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly IDtoService _dtoService;
        private readonly ILocalizationManager _localizationManager;
        private readonly IJsonSerializer _json;
        private readonly IAuthorizationContext _authContext;

        public TrailersService(
            ILogger<TrailersService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            ILocalizationManager localizationManager,
            IJsonSerializer json,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _localizationManager = localizationManager;
            _json = json;
            _authContext = authContext;
        }

        public object Get(Getrailers request)
        {
            var json = _json.SerializeToString(request);
            var getItems = _json.DeserializeFromString<GetItems>(json);

            getItems.IncludeItemTypes = "Trailer";

            return new ItemsService(
                Logger,
                ServerConfigurationManager,
                ResultFactory,
                _userManager,
                _libraryManager,
                _localizationManager,
                _dtoService,
                _authContext)
            {
                Request = Request,

            }.Get(getItems);
        }
    }
}
