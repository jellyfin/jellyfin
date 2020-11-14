using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class QueueItem.
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueItem"/> class.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="playlistItemId">The playlist identifier of the item.</param>
        public QueueItem(Guid itemId, string playlistItemId)
        {
            ItemId = itemId;
            PlaylistItemId = playlistItemId;
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
        public string PlaylistItemId { get; }
    }
}
