using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.HomeScreen.Sections
{
    /// <summary>
    /// My Media Section.
    /// </summary>
    public class MyMediaSection : IHomeScreenSection
    {
        /// <inheritdoc/>
        public string? Section => "MyMedia";

        /// <inheritdoc/>
        public string? DisplayText { get; set; } = "My Media";

        /// <inheritdoc/>
        public int? Limit => 1;

        /// <inheritdoc/>
        public string? Route => null;

        /// <inheritdoc/>
        public string? AdditionalData { get; set; } = null;

        private readonly IUserViewManager _userViewManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userViewManager">Instance of <see href="IUserViewManager" /> interface.</param>
        /// <param name="userManager">Instance of <see href="IUserManager" /> interface.</param>
        /// <param name="dtoService">Instance of <see href="IDtoService" /> interface.</param>
        public MyMediaSection(IUserViewManager userViewManager,
            IUserManager userManager,
            IDtoService dtoService)
        {
            _userViewManager = userViewManager;
            _userManager = userManager;
            _dtoService = dtoService;
        }

        /// <inheritdoc/>
        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload)
        {
            var query = new UserViewQuery
            {
                UserId = payload.UserId,
                IncludeHidden = false
            };

            var folders = _userViewManager.GetUserViews(query);

            var dtoOptions = new DtoOptions();
            var f = new List<ItemFields>
            {
                ItemFields.PrimaryImageAspectRatio,
                ItemFields.DisplayPreferencesId
            };

            dtoOptions.Fields = f.ToArray();

            var user = _userManager.GetUserById(payload.UserId);

            var dtos = folders.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user))
                .ToArray();

            return new QueryResult<BaseItemDto>(dtos);
        }
    }
}
