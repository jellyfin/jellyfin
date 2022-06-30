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
    /// <summary>
    /// API controller for the Modular Home Screen.
    /// </summary>
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
        /// <param name="userViewManager">Instance of the <see cref="IUserViewManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        /// <param name="homeScreenManager">Instance of the <see cref="IHomeScreenManager"/> interface.</param>
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

        /// <summary>
        /// Get what home screen sections the user has enabled.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns></returns>
        [HttpGet("Sections")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<HomeScreenSectionInfo>> GetHomeScreenSections(
            [FromQuery] Guid? userId)
        {
            ModularHomeUserSettings? settings = _homeScreenManager.GetUserSettings(userId ?? Guid.Empty);

            List<HomeScreenSectionInfo> sections = _homeScreenManager.GetSectionTypes().Select(x => x.AsInfo()).Where(x => settings?.EnabledSections.Contains(x.Section ?? "") ?? false).ToList();

            return new QueryResult<HomeScreenSectionInfo>(
                0,
                sections.Count,
                sections);
        }

        /// <summary>
        /// Get the content for the home screen section based on <paramref name="sectionType"/>.
        /// </summary>
        /// <param name="sectionType">The section type.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="additionalData">Any additional data this section is showing.</param>
        /// <returns></returns>
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
