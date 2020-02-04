using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Special class used for User Roots.  Children contain actual ones defined for this user
    /// PLUS the virtual folders from the physical root (added by plug-ins).
    /// </summary>
    public class UserRootFolder : Folder
    {
        private List<Guid> _childrenIds = null;
        private readonly object _childIdsLock = new object();
        protected override List<BaseItem> LoadChildren()
        {
            lock (_childIdsLock)
            {
                if (_childrenIds == null)
                {
                    var list = base.LoadChildren();
                    _childrenIds = list.Select(i => i.Id).ToList();
                    return list;
                }

                return _childrenIds.Select(LibraryManager.GetItemById).Where(i => i != null).ToList();
            }
        }

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => false;

        private void ClearCache()
        {
            lock (_childIdsLock)
            {
                _childrenIds = null;
            }
        }

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                return QueryRecursive(query);
            }

            var result = UserViewManager.GetUserViews(new UserViewQuery
            {
                UserId = query.User.Id,
                PresetViews = query.PresetViews
            });

            return UserViewBuilder.SortAndPage(result, null, query, LibraryManager, true);
        }

        public override int GetChildCount(User user)
        {
            return GetChildren(user, true).Count;
        }

        [JsonIgnore]
        protected override bool SupportsShortcutChildren => true;

        [JsonIgnore]
        public override bool IsPreSorted => true;

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            var list = base.GetEligibleChildrenForRecursiveChildren(user).ToList();
            list.AddRange(LibraryManager.RootFolder.VirtualChildren);

            return list;
        }

        public override bool BeforeMetadataRefresh(bool replaceAllMetdata)
        {
            ClearCache();
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetdata);

            if (string.Equals("default", Name, StringComparison.OrdinalIgnoreCase))
            {
                Name = "Media Folders";
                hasChanges = true;
            }

            return hasChanges;
        }

        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            ClearCache();

            return base.GetNonCachedChildren(directoryService);
        }

        protected override async Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            ClearCache();

            await base.ValidateChildrenInternal(progress, cancellationToken, recursive, refreshChildMetadata, refreshOptions, directoryService)
                .ConfigureAwait(false);

            ClearCache();

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
    }
}
