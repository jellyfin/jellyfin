using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class LocalItem
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItemDto Item { get; set; }
        /// <summary>
        /// Gets or sets the local path.
        /// </summary>
        /// <value>The local path.</value>
        public string LocalPath { get; set; }
        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        /// <value>The server identifier.</value>
        public string ServerId { get; set; }
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>The unique identifier.</value>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the file identifier.
        /// </summary>
        /// <value>The file identifier.</value>
        public string FileId { get; set; }
        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public string ItemId { get; set; }
        /// <summary>
        /// Gets or sets the synchronize job item identifier.
        /// </summary>
        /// <value>The synchronize job item identifier.</value>
        public string SyncJobItemId { get; set; }
        /// <summary>
        /// Gets or sets the user ids with access.
        /// </summary>
        /// <value>The user ids with access.</value>
        public List<string> UserIdsWithAccess { get; set; }
        /// <summary>
        /// Gets or sets the additional files.
        /// </summary>
        /// <value>The additional files.</value>
        public List<string> AdditionalFiles { get; set; }

        public LocalItem()
        {
            AdditionalFiles = new List<string>();
            UserIdsWithAccess = new List<string>();
        }
    }
}
