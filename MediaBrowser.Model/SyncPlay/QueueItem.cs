#nullable disable

using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class QueueItem.
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the playlist id of the item.
        /// </summary>
        /// <value>The playlist id of the item.</value>
        public string PlaylistItemId { get; set; }
    }
}
