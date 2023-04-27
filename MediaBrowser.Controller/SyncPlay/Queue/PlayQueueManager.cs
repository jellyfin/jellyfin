#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.Queue
{
    /// <summary>
    /// Class PlayQueueManager.
    /// </summary>
    public class PlayQueueManager
    {
        /// <summary>
        /// Placeholder index for when no item is playing.
        /// </summary>
        /// <value>The no-playing item index.</value>
        private const int NoPlayingItemIndex = -1;

        /// <summary>
        /// The sorted playlist.
        /// </summary>
        /// <value>The sorted playlist, or play queue of the group.</value>
        private List<SyncPlayQueueItem> _sortedPlaylist = new List<SyncPlayQueueItem>();

        /// <summary>
        /// The shuffled playlist.
        /// </summary>
        /// <value>The shuffled playlist, or play queue of the group.</value>
        private List<SyncPlayQueueItem> _shuffledPlaylist = new List<SyncPlayQueueItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayQueueManager" /> class.
        /// </summary>
        public PlayQueueManager()
        {
            Reset();
        }

        /// <summary>
        /// Gets the playing item index.
        /// </summary>
        /// <value>The playing item index.</value>
        public int PlayingItemIndex { get; private set; }

        /// <summary>
        /// Gets the last time the queue has been changed.
        /// </summary>
        /// <value>The last time the queue has been changed.</value>
        public DateTime LastChange { get; private set; }

        /// <summary>
        /// Gets the shuffle mode.
        /// </summary>
        /// <value>The shuffle mode.</value>
        public GroupShuffleMode ShuffleMode { get; private set; } = GroupShuffleMode.Sorted;

        /// <summary>
        /// Gets the repeat mode.
        /// </summary>
        /// <value>The repeat mode.</value>
        public GroupRepeatMode RepeatMode { get; private set; } = GroupRepeatMode.RepeatNone;

        /// <summary>
        /// Checks if an item is playing.
        /// </summary>
        /// <returns><c>true</c> if an item is playing; <c>false</c> otherwise.</returns>
        public bool IsItemPlaying()
        {
            return PlayingItemIndex != NoPlayingItemIndex;
        }

        /// <summary>
        /// Gets the current playlist considering the shuffle mode.
        /// </summary>
        /// <returns>The playlist.</returns>
        public IReadOnlyList<SyncPlayQueueItem> GetPlaylist()
        {
            return GetPlaylistInternal();
        }

        /// <summary>
        /// Sets a new playlist. Playing item is reset.
        /// </summary>
        /// <param name="items">The new items of the playlist.</param>
        public void SetPlaylist(IReadOnlyList<Guid> items)
        {
            _sortedPlaylist.Clear();
            _shuffledPlaylist.Clear();

            _sortedPlaylist = CreateQueueItemsFromArray(items);
            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                _shuffledPlaylist = new List<SyncPlayQueueItem>(_sortedPlaylist);
                _shuffledPlaylist.Shuffle();
            }

            PlayingItemIndex = NoPlayingItemIndex;
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Appends new items to the playlist. The specified order is maintained.
        /// </summary>
        /// <param name="items">The items to add to the playlist.</param>
        public void Queue(IReadOnlyList<Guid> items)
        {
            var newItems = CreateQueueItemsFromArray(items);

            _sortedPlaylist.AddRange(newItems);
            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                _shuffledPlaylist.AddRange(newItems);
            }

            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Shuffles the playlist. Shuffle mode is changed. The playlist gets re-shuffled if already shuffled.
        /// </summary>
        public void ShufflePlaylist()
        {
            if (PlayingItemIndex == NoPlayingItemIndex)
            {
                _shuffledPlaylist = new List<SyncPlayQueueItem>(_sortedPlaylist);
                _shuffledPlaylist.Shuffle();
            }
            else if (ShuffleMode.Equals(GroupShuffleMode.Sorted))
            {
                // First time shuffle.
                var playingItem = _sortedPlaylist[PlayingItemIndex];
                _shuffledPlaylist = new List<SyncPlayQueueItem>(_sortedPlaylist);
                _shuffledPlaylist.RemoveAt(PlayingItemIndex);
                _shuffledPlaylist.Shuffle();
                _shuffledPlaylist.Insert(0, playingItem);
                PlayingItemIndex = 0;
            }
            else
            {
                // Re-shuffle playlist.
                var playingItem = _shuffledPlaylist[PlayingItemIndex];
                _shuffledPlaylist.RemoveAt(PlayingItemIndex);
                _shuffledPlaylist.Shuffle();
                _shuffledPlaylist.Insert(0, playingItem);
                PlayingItemIndex = 0;
            }

            ShuffleMode = GroupShuffleMode.Shuffle;
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Resets the playlist to sorted mode. Shuffle mode is changed.
        /// </summary>
        public void RestoreSortedPlaylist()
        {
            if (PlayingItemIndex != NoPlayingItemIndex)
            {
                var playingItem = _shuffledPlaylist[PlayingItemIndex];
                PlayingItemIndex = _sortedPlaylist.IndexOf(playingItem);
            }

            _shuffledPlaylist.Clear();

            ShuffleMode = GroupShuffleMode.Sorted;
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Clears the playlist. Shuffle mode is preserved.
        /// </summary>
        /// <param name="clearPlayingItem">Whether to remove the playing item as well.</param>
        public void ClearPlaylist(bool clearPlayingItem)
        {
            var playingItem = GetPlayingItem();
            _sortedPlaylist.Clear();
            _shuffledPlaylist.Clear();
            LastChange = DateTime.UtcNow;

            if (!clearPlayingItem && playingItem is not null)
            {
                _sortedPlaylist.Add(playingItem);
                if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
                {
                    _shuffledPlaylist.Add(playingItem);
                }

                PlayingItemIndex = 0;
            }
            else
            {
                PlayingItemIndex = NoPlayingItemIndex;
            }
        }

        /// <summary>
        /// Adds new items to the playlist right after the playing item. The specified order is maintained.
        /// </summary>
        /// <param name="items">The items to add to the playlist.</param>
        public void QueueNext(IReadOnlyList<Guid> items)
        {
            var newItems = CreateQueueItemsFromArray(items);

            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                var playingItem = GetPlayingItem();
                var sortedPlayingItemIndex = _sortedPlaylist.IndexOf(playingItem);
                // Append items to sorted and shuffled playlist as they are.
                _sortedPlaylist.InsertRange(sortedPlayingItemIndex + 1, newItems);
                _shuffledPlaylist.InsertRange(PlayingItemIndex + 1, newItems);
            }
            else
            {
                _sortedPlaylist.InsertRange(PlayingItemIndex + 1, newItems);
            }

            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets playlist identifier of the playing item, if any.
        /// </summary>
        /// <returns>The playlist identifier of the playing item.</returns>
        public Guid GetPlayingItemPlaylistId()
        {
            var playingItem = GetPlayingItem();
            return playingItem?.PlaylistItemId ?? Guid.Empty;
        }

        /// <summary>
        /// Gets the playing item identifier, if any.
        /// </summary>
        /// <returns>The playing item identifier.</returns>
        public Guid GetPlayingItemId()
        {
            var playingItem = GetPlayingItem();
            return playingItem?.ItemId ?? Guid.Empty;
        }

        /// <summary>
        /// Sets the playing item using its identifier. If not in the playlist, the playing item is reset.
        /// </summary>
        /// <param name="itemId">The new playing item identifier.</param>
        public void SetPlayingItemById(Guid itemId)
        {
            var playlist = GetPlaylistInternal();
            PlayingItemIndex = playlist.FindIndex(item => item.ItemId.Equals(itemId));
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the playing item using its playlist identifier. If not in the playlist, the playing item is reset.
        /// </summary>
        /// <param name="playlistItemId">The new playing item identifier.</param>
        /// <returns><c>true</c> if playing item has been set; <c>false</c> if item is not in the playlist.</returns>
        public bool SetPlayingItemByPlaylistId(Guid playlistItemId)
        {
            var playlist = GetPlaylistInternal();
            PlayingItemIndex = playlist.FindIndex(item => item.PlaylistItemId.Equals(playlistItemId));
            LastChange = DateTime.UtcNow;

            return PlayingItemIndex != NoPlayingItemIndex;
        }

        /// <summary>
        /// Sets the playing item using its position. If not in range, the playing item is reset.
        /// </summary>
        /// <param name="playlistIndex">The new playing item index.</param>
        public void SetPlayingItemByIndex(int playlistIndex)
        {
            var playlist = GetPlaylistInternal();
            if (playlistIndex < 0 || playlistIndex > playlist.Count)
            {
                PlayingItemIndex = NoPlayingItemIndex;
            }
            else
            {
                PlayingItemIndex = playlistIndex;
            }

            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes items from the playlist. If not removed, the playing item is preserved.
        /// </summary>
        /// <param name="playlistItemIds">The items to remove.</param>
        /// <returns><c>true</c> if playing item got removed; <c>false</c> otherwise.</returns>
        public bool RemoveFromPlaylist(IReadOnlyList<Guid> playlistItemIds)
        {
            var playingItem = GetPlayingItem();

            _sortedPlaylist.RemoveAll(item => playlistItemIds.Contains(item.PlaylistItemId));
            _shuffledPlaylist.RemoveAll(item => playlistItemIds.Contains(item.PlaylistItemId));

            LastChange = DateTime.UtcNow;

            if (playingItem is not null)
            {
                if (playlistItemIds.Contains(playingItem.PlaylistItemId))
                {
                    // Playing item has been removed, picking previous item.
                    PlayingItemIndex--;
                    if (PlayingItemIndex < 0)
                    {
                        // Was first element, picking next if available.
                        // Default to no playing item otherwise.
                        PlayingItemIndex = _sortedPlaylist.Count > 0 ? 0 : NoPlayingItemIndex;
                    }

                    return true;
                }

                // Restoring playing item.
                SetPlayingItemByPlaylistId(playingItem.PlaylistItemId);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Moves an item in the playlist to another position.
        /// </summary>
        /// <param name="playlistItemId">The item to move.</param>
        /// <param name="newIndex">The new position.</param>
        /// <returns><c>true</c> if the item has been moved; <c>false</c> otherwise.</returns>
        public bool MovePlaylistItem(Guid playlistItemId, int newIndex)
        {
            var playlist = GetPlaylistInternal();
            var playingItem = GetPlayingItem();

            var oldIndex = playlist.FindIndex(item => item.PlaylistItemId.Equals(playlistItemId));
            if (oldIndex < 0)
            {
                return false;
            }

            var queueItem = playlist[oldIndex];
            playlist.RemoveAt(oldIndex);
            newIndex = Math.Clamp(newIndex, 0, playlist.Count);
            playlist.Insert(newIndex, queueItem);

            LastChange = DateTime.UtcNow;
            PlayingItemIndex = playlist.IndexOf(playingItem);
            return true;
        }

        /// <summary>
        /// Resets the playlist to its initial state.
        /// </summary>
        public void Reset()
        {
            _sortedPlaylist.Clear();
            _shuffledPlaylist.Clear();
            PlayingItemIndex = NoPlayingItemIndex;
            ShuffleMode = GroupShuffleMode.Sorted;
            RepeatMode = GroupRepeatMode.RepeatNone;
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the repeat mode.
        /// </summary>
        /// <param name="mode">The new mode.</param>
        public void SetRepeatMode(GroupRepeatMode mode)
        {
            RepeatMode = mode;
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the shuffle mode.
        /// </summary>
        /// <param name="mode">The new mode.</param>
        public void SetShuffleMode(GroupShuffleMode mode)
        {
            if (mode.Equals(GroupShuffleMode.Shuffle))
            {
                ShufflePlaylist();
            }
            else
            {
                RestoreSortedPlaylist();
            }
        }

        /// <summary>
        /// Toggles the shuffle mode between sorted and shuffled.
        /// </summary>
        public void ToggleShuffleMode()
        {
            if (ShuffleMode.Equals(GroupShuffleMode.Sorted))
            {
                ShufflePlaylist();
            }
            else
            {
                RestoreSortedPlaylist();
            }
        }

        /// <summary>
        /// Gets the next item in the playlist considering repeat mode and shuffle mode.
        /// </summary>
        /// <returns>The next item in the playlist.</returns>
        public SyncPlayQueueItem GetNextItemPlaylistId()
        {
            int newIndex;
            var playlist = GetPlaylistInternal();

            switch (RepeatMode)
            {
                case GroupRepeatMode.RepeatOne:
                    newIndex = PlayingItemIndex;
                    break;
                case GroupRepeatMode.RepeatAll:
                    newIndex = PlayingItemIndex + 1;
                    if (newIndex >= playlist.Count)
                    {
                        newIndex = 0;
                    }

                    break;
                default:
                    newIndex = PlayingItemIndex + 1;
                    break;
            }

            if (newIndex < 0 || newIndex >= playlist.Count)
            {
                return null;
            }

            return playlist[newIndex];
        }

        /// <summary>
        /// Sets the next item in the queue as playing item.
        /// </summary>
        /// <returns><c>true</c> if the playing item changed; <c>false</c> otherwise.</returns>
        public bool Next()
        {
            if (RepeatMode.Equals(GroupRepeatMode.RepeatOne))
            {
                LastChange = DateTime.UtcNow;
                return true;
            }

            PlayingItemIndex++;
            if (PlayingItemIndex >= _sortedPlaylist.Count)
            {
                if (RepeatMode.Equals(GroupRepeatMode.RepeatAll))
                {
                    PlayingItemIndex = 0;
                }
                else
                {
                    PlayingItemIndex = _sortedPlaylist.Count - 1;
                    return false;
                }
            }

            LastChange = DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// Sets the previous item in the queue as playing item.
        /// </summary>
        /// <returns><c>true</c> if the playing item changed; <c>false</c> otherwise.</returns>
        public bool Previous()
        {
            if (RepeatMode.Equals(GroupRepeatMode.RepeatOne))
            {
                LastChange = DateTime.UtcNow;
                return true;
            }

            PlayingItemIndex--;
            if (PlayingItemIndex < 0)
            {
                if (RepeatMode.Equals(GroupRepeatMode.RepeatAll))
                {
                    PlayingItemIndex = _sortedPlaylist.Count - 1;
                }
                else
                {
                    PlayingItemIndex = 0;
                    return false;
                }
            }

            LastChange = DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// Creates a list from the array of items. Each item is given an unique playlist identifier.
        /// </summary>
        /// <returns>The list of queue items.</returns>
        private List<SyncPlayQueueItem> CreateQueueItemsFromArray(IReadOnlyList<Guid> items)
        {
            var list = new List<SyncPlayQueueItem>();
            foreach (var item in items)
            {
                var queueItem = new SyncPlayQueueItem(item);
                list.Add(queueItem);
            }

            return list;
        }

        /// <summary>
        /// Gets the current playlist considering the shuffle mode.
        /// </summary>
        /// <returns>The playlist.</returns>
        private List<SyncPlayQueueItem> GetPlaylistInternal()
        {
            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                return _shuffledPlaylist;
            }

            return _sortedPlaylist;
        }

        /// <summary>
        /// Gets the current playing item, depending on the shuffle mode.
        /// </summary>
        /// <returns>The playing item.</returns>
        private SyncPlayQueueItem GetPlayingItem()
        {
            if (PlayingItemIndex == NoPlayingItemIndex)
            {
                return null;
            }

            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                return _shuffledPlaylist[PlayingItemIndex];
            }

            return _sortedPlaylist[PlayingItemIndex];
        }
    }
}
