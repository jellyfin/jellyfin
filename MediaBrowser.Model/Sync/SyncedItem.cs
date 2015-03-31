using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncedItem
    {
        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        /// <value>The server identifier.</value>
        public string ServerId { get; set; }
        /// <summary>
        /// Gets or sets the synchronize job identifier.
        /// </summary>
        /// <value>The synchronize job identifier.</value>
        public string SyncJobId { get; set; }
        /// <summary>
        /// Gets or sets the name of the synchronize job.
        /// </summary>
        /// <value>The name of the synchronize job.</value>
        public string SyncJobName { get; set; }
        /// <summary>
        /// Gets or sets the synchronize job date created.
        /// </summary>
        /// <value>The synchronize job date created.</value>
        public DateTime SyncJobDateCreated { get; set; }
        /// <summary>
        /// Gets or sets the synchronize job item identifier.
        /// </summary>
        /// <value>The synchronize job item identifier.</value>
        public string SyncJobItemId { get; set; }
        /// <summary>
        /// Gets or sets the name of the original file.
        /// </summary>
        /// <value>The name of the original file.</value>
        public string OriginalFileName { get; set; }
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItemDto Item { get; set; }
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }
        /// <summary>
        /// Gets or sets the additional files.
        /// </summary>
        /// <value>The additional files.</value>
        public List<ItemFileInfo> AdditionalFiles { get; set; }

        public SyncedItem()
        {
            AdditionalFiles = new List<ItemFileInfo>();
        }
    }
}
