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
        /// Gets or sets the folders.
        /// </summary>
        /// <value>The folders.</value>
        public List<Guid> Folders { get; set; }

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
            Folders = new List<Guid>();
            ItemsAdded = new List<Guid>();
            ItemsRemoved = new List<Guid>();
            ItemsUpdated = new List<Guid>();
        }
    }
}
