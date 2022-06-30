using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    public class HomeScreenSections
    {
        // TODO: Create a proper BaseItem for HomeScreenSections
        public static BaseItemDto MyMedia { get; private set; } = new BaseItemDto
        {
            Name = "MyMedia",
            OriginalTitle = "My Media"
        };

        public static BaseItemDto ContinueWatching { get; private set; } = new BaseItemDto
        {
            Name = "ContinueWatching",
            OriginalTitle = "Continue Watching"
        };

        public static BaseItemDto NextUp { get; private set; } = new BaseItemDto
        {
            Name = "NextUp",
            OriginalTitle = "Next Up",
            SortName = "nextup"
        };

        public static BaseItemDto LatestMovies { get; private set; } = new BaseItemDto
        {
            Name = "LatestMovies",
            OriginalTitle = "Latest Movies",
            Overview = "movies"
        };

        public static BaseItemDto LatestShows { get; private set; } = new BaseItemDto
        {
            Name = "LatestShows",
            OriginalTitle = "Latest Shows",
            Overview = "tvshows"
        };
    }

    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class HomeScreenController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly IUserViewManager _userViewManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly ISessionManager _sessionManager;
        private readonly IHomeScreenManager _homeScreenManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvShowsController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        public HomeScreenController(
            IUserManager userManager,
            IUserViewManager userViewManager,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            ISessionManager sessionManager,
            IHomeScreenManager homeScreenManager)
        {
            _userManager = userManager;
            _userViewManager = userViewManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _sessionManager = sessionManager;
            _homeScreenManager = homeScreenManager;
        }

        [HttpGet("Sections")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<HomeScreenSectionInfo>> GetHomeScreenSections(
            [FromQuery] Guid? userId)
        {
            ModularHomeUserSettings settings = _homeScreenManager.GetUserSettings(userId ?? Guid.Empty);

            List<HomeScreenSectionInfo> sections = _homeScreenManager.GetSectionTypes().Select(x => x.AsInfo()).Where(x => settings.EnabledSections.Contains(x.Section)).ToList();

            return new QueryResult<HomeScreenSectionInfo>(
                0,
                sections.Count,
                sections);
        }

        [HttpGet("Section/{sectionType}")]
        public QueryResult<BaseItemDto> GetSectionContent(
            [FromRoute] string sectionType,
            [FromQuery, Required] Guid userId,
            [FromQuery] string? additionalData)
        {
            HomeScreenSectionPayload payload = new HomeScreenSectionPayload
            {
                UserId = userId,
                AdditionalData = additionalData
            };

            return _homeScreenManager.InvokeResultsDelegate(sectionType, payload);
        }
    }
}
