#nullable disable

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class PlayQueueUpdate.
    /// </summary>
    public class PlayQueueUpdate
    {
        /// <summary>
        /// Gets or sets the request type that originated this update.
        /// </summary>
        /// <value>The reason for the update.</value>
        public PlayQueueUpdateReason Reason { get; set; }

        /// <summary>
        /// Gets or sets the UTC time of the last change to the playing queue.
        /// </summary>
        /// <value>The UTC time of the last change to the playing queue.</value>
        public string LastUpdate { get; set; }

        /// <summary>
        /// Gets or sets the playlist.
        /// </summary>
        /// <value>The playlist.</value>
        public QueueItem[] Playlist { get; set; }

        /// <summary>
        /// Gets or sets the playing item index in the playlist.
        /// </summary>
        /// <value>The playing item index in the playlist.</value>
        public int PlayingItemIndex { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        public long StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the shuffle mode.
        /// </summary>
        /// <value>The shuffle mode.</value>
        public GroupShuffleMode ShuffleMode { get; set; }

        /// <summary>
        /// Gets or sets the repeat mode.
        /// </summary>
        /// <value>The repeat mode.</value>
        public GroupRepeatMode RepeatMode { get; set; }
    }
}
