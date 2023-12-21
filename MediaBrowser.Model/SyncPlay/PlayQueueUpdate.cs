using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class PlayQueueUpdate.
    /// </summary>
    public class PlayQueueUpdate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayQueueUpdate"/> class.
        /// </summary>
        /// <param name="reason">The reason for the update.</param>
        /// <param name="lastUpdate">The UTC time of the last change to the playing queue.</param>
        /// <param name="playlist">The playlist.</param>
        /// <param name="playingItemIndex">The playing item index in the playlist.</param>
        /// <param name="startPositionTicks">The start position ticks.</param>
        /// <param name="isPlaying">The playing item status.</param>
        /// <param name="shuffleMode">The shuffle mode.</param>
        /// <param name="repeatMode">The repeat mode.</param>
        public PlayQueueUpdate(PlayQueueUpdateReason reason, DateTime lastUpdate, IReadOnlyList<SyncPlayQueueItem> playlist, int playingItemIndex, long startPositionTicks, bool isPlaying, GroupShuffleMode shuffleMode, GroupRepeatMode repeatMode)
        {
            Reason = reason;
            LastUpdate = lastUpdate;
            Playlist = playlist;
            PlayingItemIndex = playingItemIndex;
            StartPositionTicks = startPositionTicks;
            IsPlaying = isPlaying;
            ShuffleMode = shuffleMode;
            RepeatMode = repeatMode;
        }

        /// <summary>
        /// Gets the request type that originated this update.
        /// </summary>
        /// <value>The reason for the update.</value>
        public PlayQueueUpdateReason Reason { get; }

        /// <summary>
        /// Gets the UTC time of the last change to the playing queue.
        /// </summary>
        /// <value>The UTC time of the last change to the playing queue.</value>
        public DateTime LastUpdate { get; }

        /// <summary>
        /// Gets the playlist.
        /// </summary>
        /// <value>The playlist.</value>
        public IReadOnlyList<SyncPlayQueueItem> Playlist { get; }

        /// <summary>
        /// Gets the playing item index in the playlist.
        /// </summary>
        /// <value>The playing item index in the playlist.</value>
        public int PlayingItemIndex { get; }

        /// <summary>
        /// Gets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        public long StartPositionTicks { get; }

        /// <summary>
        /// Gets a value indicating whether the current item is playing.
        /// </summary>
        /// <value>The playing item status.</value>
        public bool IsPlaying { get; }

        /// <summary>
        /// Gets the shuffle mode.
        /// </summary>
        /// <value>The shuffle mode.</value>
        public GroupShuffleMode ShuffleMode { get; }

        /// <summary>
        /// Gets the repeat mode.
        /// </summary>
        /// <value>The repeat mode.</value>
        public GroupRepeatMode RepeatMode { get; }
    }
}
