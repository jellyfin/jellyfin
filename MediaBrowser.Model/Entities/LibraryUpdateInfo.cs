#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CS1591

using System;

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
        public string[] FoldersAddedTo { get; set; }

        /// <summary>
        /// Gets or sets the folders removed from.
        /// </summary>
        /// <value>The folders removed from.</value>
        public string[] FoldersRemovedFrom { get; set; }

        /// <summary>
        /// Gets or sets the items added.
        /// </summary>
        /// <value>The items added.</value>
        public string[] ItemsAdded { get; set; }

        /// <summary>
        /// Gets or sets the items removed.
        /// </summary>
        /// <value>The items removed.</value>
        public string[] ItemsRemoved { get; set; }

        /// <summary>
        /// Gets or sets the items updated.
        /// </summary>
        /// <value>The items updated.</value>
        public string[] ItemsUpdated { get; set; }

        public string[] CollectionFolders { get; set; }

        public bool IsEmpty => FoldersAddedTo.Length == 0 && FoldersRemovedFrom.Length == 0 && ItemsAdded.Length == 0 && ItemsRemoved.Length == 0 && ItemsUpdated.Length == 0 && CollectionFolders.Length == 0;
    }
}
