using System;
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
        public List<Guid> FoldersAddedTo { get; set; }
        /// <summary>
        /// Gets or sets the folders removed from.
        /// </summary>
        /// <value>The folders removed from.</value>
        public List<Guid> FoldersRemovedFrom { get; set; }

        /// <summary>
        /// Gets or sets the items added.
        /// </summary>
        /// <value>The items added.</value>
        public List<Guid> ItemsAdded { get; set; }

        /// <summary>
        /// Gets or sets the items removed.
        /// </summary>
        /// <value>The items removed.</value>
        public List<Guid> ItemsRemoved { get; set; }

        /// <summary>
        /// Gets or sets the items updated.
        /// </summary>
        /// <value>The items updated.</value>
        public List<Guid> ItemsUpdated { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryUpdateInfo"/> class.
        /// </summary>
        public LibraryUpdateInfo()
        {
            FoldersAddedTo = new List<Guid>();
            FoldersRemovedFrom = new List<Guid>();
            ItemsAdded = new List<Guid>();
            ItemsRemoved = new List<Guid>();
            ItemsUpdated = new List<Guid>();
        }
    }
}
