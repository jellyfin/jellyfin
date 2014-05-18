using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class LibraryUpdateInfo
    /// </summary>
    public class LibraryUpdateInfo
    {
        /// <summary>
        /// Gets or sets the folders added to.
        /// </summary>
        /// <value>The folders added to.</value>
        public List<string> FoldersAddedTo { get; set; }
        /// <summary>
        /// Gets or sets the folders removed from.
        /// </summary>
        /// <value>The folders removed from.</value>
        public List<string> FoldersRemovedFrom { get; set; }

        /// <summary>
        /// Gets or sets the items added.
        /// </summary>
        /// <value>The items added.</value>
        public List<string> ItemsAdded { get; set; }

        /// <summary>
        /// Gets or sets the items removed.
        /// </summary>
        /// <value>The items removed.</value>
        public List<string> ItemsRemoved { get; set; }

        /// <summary>
        /// Gets or sets the items updated.
        /// </summary>
        /// <value>The items updated.</value>
        public List<string> ItemsUpdated { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryUpdateInfo"/> class.
        /// </summary>
        public LibraryUpdateInfo()
        {
            FoldersAddedTo = new List<string>();
            FoldersRemovedFrom = new List<string>();
            ItemsAdded = new List<string>();
            ItemsRemoved = new List<string>();
            ItemsUpdated = new List<string>();
        }
    }
}
