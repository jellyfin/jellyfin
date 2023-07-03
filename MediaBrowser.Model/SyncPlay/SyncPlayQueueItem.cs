using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class QueueItem.
    /// </summary>
    public class SyncPlayQueueItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayQueueItem"/> class.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        public SyncPlayQueueItem(Guid itemId)
        {
            ItemId = itemId;
        }

        /// <summary>
        /// Gets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public Guid ItemId { get; }

        /// <summary>
        /// Gets the playlist identifier of the item.
        /// </summary>
        /// <value>The playlist identifier of the item.</value>
        public Guid PlaylistItemId { get; } = Guid.NewGuid();
    }
}
