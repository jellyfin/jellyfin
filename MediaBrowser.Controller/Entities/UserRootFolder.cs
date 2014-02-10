using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Special class used for User Roots.  Children contain actual ones defined for this user
    /// PLUS the virtual folders from the physical root (added by plug-ins).
    /// </summary>
    public class UserRootFolder : Folder
    {
        /// <summary>
        /// Get the children of this folder from the actual file system
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            return base.GetNonCachedChildren(directoryService).Concat(LibraryManager.RootFolder.VirtualChildren);
        }

        public override ItemUpdateType BeforeMetadataRefresh()
        {
            var updateType = base.BeforeMetadataRefresh();

            if (string.Equals("default", Name, System.StringComparison.OrdinalIgnoreCase))
            {
                Name = "Default Media Library";
                updateType = updateType | ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }
    }
}
