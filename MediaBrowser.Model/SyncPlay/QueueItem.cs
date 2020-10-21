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
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the playlist identifier of the item.
        /// </summary>
        /// <value>The playlist identifier of the item.</value>
        public string PlaylistItemId { get; set; }
    }
}
