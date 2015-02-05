using System.Collections.Generic;

namespace MediaBrowser.Controller.Sync
{
    public class SyncAccount
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public List<string> UserIds { get; set; }

        public SyncAccount()
        {
            UserIds = new List<string>();
        }
    }
}
