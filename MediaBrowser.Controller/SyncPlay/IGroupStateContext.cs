#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.Queue;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface IGroupStateContext.
    /// </summary>
    public interface IGroupStateContext
    {
        /// <summary>
        /// Gets the default ping value used for sessions, in milliseconds.
        /// </summary>
        /// <value>The default ping value used for sessions, in milliseconds.</value>
        long DefaultPing { get; }

        /// <summary>
        /// Gets the maximum time offset error accepted for dates reported by clients, in milliseconds.
        /// </summary>
        /// <value>The maximum offset error accepted, in milliseconds.</value>
        long TimeSyncOffset { get; }

        /// <summary>
        /// Gets the maximum offset error accepted for position reported by clients, in milliseconds.
        /// </summary>
        /// <value>The maximum offset error accepted, in milliseconds.</value>
        long MaxPlaybackOffset { get; }

        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        Guid GroupId { get; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        long PositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the last activity.
        /// </summary>
        /// <value>The last activity.</value>
        DateTime LastActivity { get; set; }

        /// <summary>
        /// Gets the play queue.
        /// </summary>
        /// <value>The play queue.</value>
        PlayQueueManager PlayQueue { get; }

        /// <summary>
        /// Sets a new state.
        /// </summary>
        /// <param name="state">The new state.</param>
        void SetState(IGroupState state);

        /// <summary>
        /// Sends a GroupUpdate message to the interested sessions.
        /// </summary>
        /// <typeparam name="T">The type of the data of the message.</typeparam>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task SendGroupUpdate<T>(SessionInfo from, SyncPlayBroadcastType type, GroupUpdate<T> message, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a playback command to the interested sessions.
        /// </summary>
        /// <param name="from">The current session.</param>
        /// <param name="type">The filtering type.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        Task SendCommand(SessionInfo from, SyncPlayBroadcastType type, SendCommand message, CancellationToken cancellationToken);

        /// <summary>
        /// Builds a new playback command with some default values.
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <returns>The command.</returns>
        SendCommand NewSyncPlayCommand(SendCommandType type);

        /// <summary>
        /// Builds a new group update message.
        /// </summary>
        /// <typeparam name="T">The type of the data of the message.</typeparam>
        /// <param name="type">The update type.</param>
        /// <param name="data">The data to send.</param>
        /// <returns>The group update.</returns>
        GroupUpdate<T> NewSyncPlayGroupUpdate<T>(GroupUpdateType type, T data);

        /// <summary>
        /// Sanitizes the PositionTicks, considers the current playing item when available.
        /// </summary>
        /// <param name="positionTicks">The PositionTicks.</param>
        /// <returns>The sanitized position ticks.</returns>
        long SanitizePositionTicks(long? positionTicks);

        /// <summary>
        /// Updates the ping of a session, in milliseconds.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="ping">The ping, in milliseconds.</param>
        void UpdatePing(SessionInfo session, long ping);

        /// <summary>
        /// Gets the highest ping in the group, in milliseconds.
        /// </summary>
        /// <returns>The highest ping in the group.</returns>
        long GetHighestPing();

        /// <summary>
        /// Sets the session's buffering state.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="isBuffering">The state.</param>
        void SetBuffering(SessionInfo session, bool isBuffering);

        /// <summary>
        /// Sets the buffering state of all the sessions.
        /// </summary>
        /// <param name="isBuffering">The state.</param>
        void SetAllBuffering(bool isBuffering);

        /// <summary>
        /// Gets the group buffering state.
        /// </summary>
        /// <returns><c>true</c> if there is a session buffering in the group; <c>false</c> otherwise.</returns>
        bool IsBuffering();

        /// <summary>
        /// Sets the session's group wait state.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="ignoreGroupWait">The state.</param>
        void SetIgnoreGroupWait(SessionInfo session, bool ignoreGroupWait);

        /// <summary>
        /// Sets a new play queue.
        /// </summary>
        /// <param name="playQueue">The new play queue.</param>
        /// <param name="playingItemPosition">The playing item position in the play queue.</param>
        /// <param name="startPositionTicks">The start position ticks.</param>
        /// <returns><c>true</c> if the play queue has been changed; <c>false</c> if something went wrong.</returns>
        bool SetPlayQueue(IReadOnlyList<Guid> playQueue, int playingItemPosition, long startPositionTicks);

        /// <summary>
        /// Sets the playing item.
        /// </summary>
        /// <param name="playlistItemId">The new playing item identifier.</param>
        /// <returns><c>true</c> if the play queue has been changed; <c>false</c> if something went wrong.</returns>
        bool SetPlayingItem(Guid playlistItemId);

        /// <summary>
        /// Clears the play queue.
        /// </summary>
        /// <param name="clearPlayingItem">Whether to remove the playing item as well.</param>
        void ClearPlayQueue(bool clearPlayingItem);

        /// <summary>
        /// Removes items from the play queue.
        /// </summary>
        /// <param name="playlistItemIds">The items to remove.</param>
        /// <returns><c>true</c> if playing item got removed; <c>false</c> otherwise.</returns>
        bool RemoveFromPlayQueue(IReadOnlyList<Guid> playlistItemIds);

        /// <summary>
        /// Moves an item in the play queue.
        /// </summary>
        /// <param name="playlistItemId">The playlist identifier of the item to move.</param>
        /// <param name="newIndex">The new position.</param>
        /// <returns><c>true</c> if item has been moved; <c>false</c> if something went wrong.</returns>
        bool MoveItemInPlayQueue(Guid playlistItemId, int newIndex);

        /// <summary>
        /// Updates the play queue.
        /// </summary>
        /// <param name="newItems">The new items to add to the play queue.</param>
        /// <param name="mode">The mode with which the items will be added.</param>
        /// <returns><c>true</c> if the play queue has been changed; <c>false</c> if something went wrong.</returns>
        bool AddToPlayQueue(IReadOnlyList<Guid> newItems, GroupQueueMode mode);

        /// <summary>
        /// Restarts current item in play queue.
        /// </summary>
        void RestartCurrentItem();

        /// <summary>
        /// Picks next item in play queue.
        /// </summary>
        /// <returns><c>true</c> if the item changed; <c>false</c> otherwise.</returns>
        bool NextItemInQueue();

        /// <summary>
        /// Picks previous item in play queue.
        /// </summary>
        /// <returns><c>true</c> if the item changed; <c>false</c> otherwise.</returns>
        bool PreviousItemInQueue();

        /// <summary>
        /// Sets the repeat mode.
        /// </summary>
        /// <param name="mode">The new mode.</param>
        void SetRepeatMode(GroupRepeatMode mode);

        /// <summary>
        /// Sets the shuffle mode.
        /// </summary>
        /// <param name="mode">The new mode.</param>
        void SetShuffleMode(GroupShuffleMode mode);

        /// <summary>
        /// Creates a play queue update.
        /// </summary>
        /// <param name="reason">The reason for the update.</param>
        /// <returns>The play queue update.</returns>
        PlayQueueUpdate GetPlayQueueUpdate(PlayQueueUpdateReason reason);
    }
}
