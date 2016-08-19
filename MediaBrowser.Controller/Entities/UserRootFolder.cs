using System.Runtime.Serialization;
using MediaBrowser.Controller.Providers;
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
        private List<Guid> _childrenIds = null;
        private readonly object _childIdsLock = new object();
        protected override IEnumerable<BaseItem> LoadChildren()
        {
            lock (_childIdsLock)
            {
                if (_childrenIds == null)
                {
                    var list = base.LoadChildren().ToList();
                    _childrenIds = list.Select(i => i.Id).ToList();
                    return list;
                }

                return _childrenIds.Select(LibraryManager.GetItemById).Where(i => i != null).ToList();
            }
        }

        private void ClearCache()
        {
            lock (_childIdsLock)
            {
                _childrenIds = null;
            }
        }

        protected override async Task<QueryResult<BaseItem>> GetItemsInternal(InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                return QueryRecursive(query);
            }

            var result = await UserViewManager.GetUserViews(new UserViewQuery
            {
                UserId = query.User.Id.ToString("N"),
                PresetViews = query.PresetViews

            }, CancellationToken.None).ConfigureAwait(false);

            var user = query.User;
            Func<BaseItem, bool> filter = i => UserViewBuilder.Filter(i, user, query, UserDataManager, LibraryManager);
            
            return PostFilterAndSort(result.Where(filter), query, true, true);
        }

        public override int GetChildCount(User user)
        {
            return GetChildren(user, true).Count();
        }

        [IgnoreDataMember]
        protected override bool SupportsShortcutChildren
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool IsPreSorted
        {
            get
            {
                return true;
            }
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            var list = base.GetEligibleChildrenForRecursiveChildren(user).ToList();
            list.AddRange(LibraryManager.RootFolder.VirtualChildren);

            return list;
        }

        public override bool BeforeMetadataRefresh()
        {
            ClearCache();

            var hasChanges = base.BeforeMetadataRefresh();

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
