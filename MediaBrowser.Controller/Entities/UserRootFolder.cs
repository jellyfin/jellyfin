#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
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
        private readonly Lock _childIdsLock = new();
        private List<Guid> _childrenIds = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRootFolder"/> class.
        /// </summary>
        public UserRootFolder()
        {
            IsRoot = true;
        }

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => false;

        [JsonIgnore]
        protected override bool SupportsShortcutChildren => true;

        [JsonIgnore]
        public override bool IsPreSorted => true;

        private void ClearCache()
        {
            lock (_childIdsLock)
            {
                _childrenIds = null;
            }
        }

        protected override IReadOnlyList<BaseItem> LoadChildren()
        {
            lock (_childIdsLock)
            {
                if (_childrenIds is null)
                {
                    var list = base.LoadChildren();
                    _childrenIds = list.Select(i => i.Id).ToList();
                    return list;
                }

                return _childrenIds.Select(LibraryManager.GetItemById).Where(i => i is not null).ToList();
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
                User = query.User,
                PresetViews = query.PresetViews
            });

            return UserViewBuilder.SortAndPage(result, null, query, LibraryManager, true);
        }

        public override int GetChildCount(User user)
        {
            return GetChildren(user, true).Count;
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            var list = base.GetEligibleChildrenForRecursiveChildren(user).ToList();
            list.AddRange(LibraryManager.RootFolder.VirtualChildren);

            return list;
        }

        public override bool BeforeMetadataRefresh(bool replaceAllMetadata)
        {
            ClearCache();
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetadata);

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

        protected override async Task ValidateChildrenInternal(IProgress<double> progress, bool recursive, bool refreshChildMetadata, bool allowRemoveRoot, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            ClearCache();

            await base.ValidateChildrenInternal(progress, recursive, refreshChildMetadata, allowRemoveRoot, refreshOptions, directoryService, cancellationToken)
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
