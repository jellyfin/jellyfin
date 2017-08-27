
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

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryUpdateInfo"/> class.
        /// </summary>
        public LibraryUpdateInfo()
        {
            FoldersAddedTo = new string[] { };
            FoldersRemovedFrom = new string[] { };
            ItemsAdded = new string[] { };
            ItemsRemoved = new string[] { };
            ItemsUpdated = new string[] { };
        }
    }
}
