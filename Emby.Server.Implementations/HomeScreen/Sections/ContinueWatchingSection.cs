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
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.HomeScreen.Sections
{
    public class ContinueWatchingSection : IHomeScreenSection
    {
        public string Section => "ContinueWatching";

        public string DisplayText { get; set; } = "Continue Watching";

        public int Limit => 1;

        public string Route => null;

        public string AdditionalData { get; set; } = null;

        private readonly IUserViewManager _userViewManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;

        public ContinueWatchingSection(IUserViewManager userViewManager,
            IUserManager userManager,
            IDtoService dtoService,
            ILibraryManager libraryManager,
            ISessionManager sessionManager)
        {
            _userViewManager = userViewManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
        }

        public QueryResult<BaseItemDto> GetResults(HomeScreenSectionPayload payload)
        {
            var user = _userManager.GetUserById(payload.UserId);
            var dtoOptions = new DtoOptions
            {
                Fields = new List<ItemFields>
                {
                    ItemFields.PrimaryImageAspectRatio,
                    ItemFields.BasicSyncInfo
                },
                ImageTypeLimit = 1,
                ImageTypes = new List<ImageType>
                {
                    ImageType.Primary,
                    ImageType.Backdrop,
                    ImageType.Thumb
                }
            };

            var ancestorIds = Array.Empty<Guid>();

            var excludeFolderIds = user.GetPreferenceValues<Guid>(PreferenceKind.LatestItemExcludes);
            if (excludeFolderIds.Length > 0)
            {
                ancestorIds = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                    .Where(i => i is Folder)
                    .Where(i => !excludeFolderIds.Contains(i.Id))
                    .Select(i => i.Id)
                    .ToArray();
            }

            var itemsResult = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                IsResumable = true,
                Limit = 12,
                Recursive = true,
                DtoOptions = dtoOptions,
                MediaTypes = new string[]
                {
                    "Video"
                },
                IsVirtualItem = false,
                CollapseBoxSetItems = false,
                EnableTotalRecordCount = false,
                AncestorIds = ancestorIds
            });

            var returnItems = _dtoService.GetBaseItemDtos(itemsResult.Items, dtoOptions, user);

            return new QueryResult<BaseItemDto>(
                null,
                itemsResult.TotalRecordCount,
                returnItems);
        }
    }
}
