using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Special class used for User Roots.  Children contain actual ones defined for this user
    /// PLUS the virtual folders from the physical root (added by plug-ins).
    /// </summary>
    public class UserRootFolder : Folder
    {
        public override async Task<QueryResult<BaseItem>> GetItems(InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                var items = query.User.RootFolder.GetRecursiveChildren(query.User);
                return SortAndFilter(items, query);
            }

            var result = await UserViewManager.GetUserViews(new UserViewQuery
            {
                UserId = query.User.Id.ToString("N")

            }, CancellationToken.None).ConfigureAwait(false);

            return SortAndFilter(result, query);
        }

        public override bool IsPreSorted
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Get the children of this folder from the actual file system
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            return base.GetNonCachedChildren(directoryService).Concat(LibraryManager.RootFolder.VirtualChildren);
        }

        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            if (string.Equals("default", Name, StringComparison.OrdinalIgnoreCase))
            {
                Name = "Media Folders";
                hasChanges = true;
            }

            return hasChanges;
        }

        protected override async Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            await base.ValidateChildrenInternal(progress, cancellationToken, recursive, refreshChildMetadata, refreshOptions, directoryService)
                .ConfigureAwait(false);

            // Not the best way to handle this, but it solves an issue
            // CollectionFolders aren't always getting saved after changes
            // This means that grabbing the item by Id may end up returning the old one
            // Fix is in two places - make sure the folder gets saved
            // And here to remedy it for affected users.
            // In theory this can be removed eventually.
            foreach (var item in Children)
            {
                LibraryManager.RegisterItem(item);
            }
        }

        public override void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, User user)
        {
            // Nothing meaninful here and will only waste resources
        }
    }
}
