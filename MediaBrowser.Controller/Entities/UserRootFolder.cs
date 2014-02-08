using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Providers;

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
        protected override IEnumerable<BaseItem> GetNonCachedChildren(DirectoryService directoryService)
        {
            return base.GetNonCachedChildren(directoryService).Concat(LibraryManager.RootFolder.VirtualChildren);
        }
    }
}
