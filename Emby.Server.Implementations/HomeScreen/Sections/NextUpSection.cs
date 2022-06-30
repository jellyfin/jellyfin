using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.HomeScreen.Sections
{
    /// <summary>
    /// Next Up Section.
    /// </summary>
    public class NextUpSection : IHomeScreenSection
    {
        /// <inheritdoc/>
        public string? Section => "NextUp";

        /// <inheritdoc/>
        public string? DisplayText { get; set; } = "Next Up";

        /// <inheritdoc/>
        public int? Limit => 1;

        /// <inheritdoc/>
        public string? Route => "nextup";

        /// <inheritdoc/>
        public string? AdditionalData { get; set; } = null;

        private readonly IUserViewManager _userViewManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;
        private readonly ITVSeriesManager _tvSeriesManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userViewManager">Instance of <see href="IUserViewManager" /> interface.</param>
        /// <param name="userManager">Instance of <see href="IUserManager" /> interface.</param>
        /// <param name="dtoService">Instance of <see href="IDtoService" /> interface.</param>
        /// <param name="libraryManager">Instance of <see href="ILibraryManager" /> interface.</param>
        /// <param name="sessionManager">Instance of <see href="ISessionManager" /> interface.</param>
        /// <param name="tvSeriesManager">Instance of <see href="ITVSeriesManager" /> interface.</param>
        public NextUpSection(IUserViewManager userViewManager,
            IUserManager userManager,
            IDtoService dtoService,
            ILibraryManager libraryManager,
            ISessionManager sessionManager,
            ITVSeriesManager tvSeriesManager)
        {
            _userViewManager = userViewManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _tvSeriesManager = tvSeriesManager;
        }

        /// <inheritdoc/>
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload)
        {
            List<ItemFields> fields = new List<ItemFields>
            {
                ItemFields.PrimaryImageAspectRatio,
                ItemFields.DateCreated,
                ItemFields.BasicSyncInfo,
                ItemFields.Path,
                ItemFields.MediaSourceCount
            };

            var options = new DtoOptions { Fields = fields };
            options.ImageTypeLimit = 1;
            options.ImageTypes = new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Banner,
                ImageType.Thumb
            };

            var result = _tvSeriesManager.GetNextUp(
                new NextUpQuery
                {
                    Limit = 24,
                    SeriesId = null,
                    StartIndex = null,
                    UserId = payload.UserId,
                    EnableTotalRecordCount = false,
                    DisableFirstEpisode = true,
                    NextUpDateCutoff = DateTime.MinValue,
                    EnableRewatching = true
                },
                options);

            var user = _userManager.GetUserById(payload.UserId);

            var returnItems = _dtoService.GetBaseItemDtos(result.Items, options, user);

            return new QueryResult<BaseItemDto>(
                null,
                result.TotalRecordCount,
                returnItems);
        }
    }
}
