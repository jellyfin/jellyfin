using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Used to hold information about a user's list of configured virtual folders
    /// </summary>
    public class VirtualFolderInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the locations.
        /// </summary>
        /// <value>The locations.</value>
        public List<string> Locations { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualFolderInfo"/> class.
        /// </summary>
        public VirtualFolderInfo()
        {
            Locations = new List<string>();
        }
    }
}
