#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class LibraryUpdateInfo.
    /// </summary>
    public class LibraryUpdateInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryUpdateInfo"/> class.
        /// </summary>
        public LibraryUpdateInfo()
        {
            FoldersAddedTo = [];
            FoldersRemovedFrom = [];
            ItemsAdded = [];
            ItemsRemoved = [];
            ItemsUpdated = [];
            CollectionFolders = [];
        }

        /// <summary>
        /// Gets or sets the folders added to.
        /// </summary>
        /// <value>The folders added to.</value>
        public IReadOnlyList<string> FoldersAddedTo { get; set; }

        /// <summary>
        /// Gets or sets the folders removed from.
        /// </summary>
        /// <value>The folders removed from.</value>
        public IReadOnlyList<string> FoldersRemovedFrom { get; set; }

        /// <summary>
        /// Gets or sets the items added.
        /// </summary>
        /// <value>The items added.</value>
        public IReadOnlyList<string> ItemsAdded { get; set; }

        /// <summary>
        /// Gets or sets the items removed.
        /// </summary>
        /// <value>The items removed.</value>
        public IReadOnlyList<string> ItemsRemoved { get; set; }

        /// <summary>
        /// Gets or sets the items updated.
        /// </summary>
        /// <value>The items updated.</value>
        public IReadOnlyList<string> ItemsUpdated { get; set; }

        public IReadOnlyList<string> CollectionFolders { get; set; }

        public bool IsEmpty => FoldersAddedTo.Count == 0 && FoldersRemovedFrom.Count == 0 && ItemsAdded.Count == 0 && ItemsRemoved.Count == 0 && ItemsUpdated.Count == 0 && CollectionFolders.Count == 0;
    }
}
