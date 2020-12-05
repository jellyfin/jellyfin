using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class QueueItemDto.
    /// </summary>
    public class QueueItemDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueItemDto"/> class.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        public QueueItemDto(Guid itemId)
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
