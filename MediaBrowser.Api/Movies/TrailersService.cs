using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;

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
        private readonly ICollectionManager _collectionManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IJsonSerializer _json;
        private readonly IAuthorizationContext _authContext;

        public TrailersService(IUserManager userManager, IUserDataManager userDataRepository, ILibraryManager libraryManager, IDtoService dtoService, ICollectionManager collectionManager, ILocalizationManager localizationManager, IJsonSerializer json, IAuthorizationContext authContext)
        {
            _userManager = userManager;
            _userDataRepository = userDataRepository;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _collectionManager = collectionManager;
            _localizationManager = localizationManager;
            _json = json;
            _authContext = authContext;
        }

        public object Get(Getrailers request)
        {
            var json = _json.SerializeToString(request);
            var getItems = _json.DeserializeFromString<GetItems>(json);

            getItems.IncludeItemTypes = "Trailer";

            return new ItemsService(_userManager, _libraryManager, _localizationManager, _dtoService, _authContext)
            {
                Request = Request,

            }.Get(getItems);
        }
    }
}
