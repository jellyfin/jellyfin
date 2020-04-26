using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Library Controller.
    /// </summary>
    public class LibraryController : BaseJellyfinApiController
    {
        private readonly IProviderManager _providerManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localization;
        private readonly ILibraryMonitor _libraryMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryController"/> class.
        /// </summary>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="activityManager">Instance of the <see cref="IActivityManager"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="libraryMonitor">Instance of the <see cref="ILibraryMonitor"/> interface.</param>
        public LibraryController(
            IProviderManager providerManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDtoService dtoService,
            IAuthorizationContext authContext,
            IActivityManager activityManager,
            ILocalizationManager localization,
            ILibraryMonitor libraryMonitor)
        {
            _providerManager = providerManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _authContext = authContext;
            _activityManager = activityManager;
            _localization = localization;
            _libraryMonitor = libraryMonitor;
        }
    }
}
