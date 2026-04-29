using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Represents the result of a Phase 1 tree scan.
    /// </summary>
    public class Phase1TreeScanResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the folder was scanned by the tree scanner.
        /// If false, the caller should fall back to normal discovery.
        /// </summary>
        public bool Scanned { get; set; }

        /// <summary>
        /// Gets the list of newly discovered items.
        /// </summary>
        public List<BaseItem> NewItems { get; } = new List<BaseItem>();

        /// <summary>
        /// Gets the list of existing items that were updated.
        /// </summary>
        public List<BaseItem> UpdatedItems { get; } = new List<BaseItem>();

        /// <summary>
        /// Gets the list of items that no longer exist in the filesystem.
        /// </summary>
        public List<BaseItem> RemovedItems { get; } = new List<BaseItem>();

        /// <summary>
        /// Gets the complete list of valid items (new + updated) after the scan.
        /// </summary>
        public List<BaseItem> AllItems { get; } = new List<BaseItem>();
    }
}
