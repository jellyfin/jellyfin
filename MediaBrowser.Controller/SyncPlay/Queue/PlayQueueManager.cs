using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    static class ListShuffleExtension
    {
        private static Random rng = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    /// <summary>
    /// Class PlayQueueManager.
    /// </summary>
    public class PlayQueueManager : IDisposable
    {
        /// <summary>
        /// Gets or sets the playing item index.
        /// </summary>
        /// <value>The playing item index.</value>
        public int PlayingItemIndex { get; private set; }

        /// <summary>
        /// Gets or sets the last time the queue has been changed.
        /// </summary>
        /// <value>The last time the queue has been changed.</value>
        public DateTime LastChange { get; private set; }

        /// <summary>
        /// Gets the sorted playlist.
        /// </summary>
        /// <value>The sorted playlist, or play queue of the group.</value>
        private List<QueueItem> SortedPlaylist { get; set; } = new List<QueueItem>();

        /// <summary>
        /// Gets the shuffled playlist.
        /// </summary>
        /// <value>The shuffled playlist, or play queue of the group.</value>
        private List<QueueItem> ShuffledPlaylist { get; set; } = new List<QueueItem>();

        /// <summary>
        /// Gets or sets the shuffle mode.
        /// </summary>
        /// <value>The shuffle mode.</value>
        public GroupShuffleMode ShuffleMode { get; private set; } = GroupShuffleMode.Sorted;

        /// <summary>
        /// Gets or sets the repeat mode.
        /// </summary>
        /// <value>The repeat mode.</value>
        public GroupRepeatMode RepeatMode { get; private set; } = GroupRepeatMode.RepeatNone;

        /// <summary>
        /// Gets or sets the progressive identifier counter.
        /// </summary>
        /// <value>The progressive identifier.</value>
        private int ProgressiveId { get; set; } = 0;

        /// <summary>
        /// Placeholder index for when no item is playing.
        /// </summary>
        /// <value>The no-playing item index.</value>
        private const int NoPlayingItemIndex = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayQueueManager" /> class.
        /// </summary>
        public PlayQueueManager()
        {
            Reset();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        /// <summary>
        /// Gets the next available identifier.
        /// </summary>
        /// <returns>The next available identifier.</returns>
        private int GetNextProgressiveId() {
            return ProgressiveId++;
        }

        /// <summary>
        /// Creates a list from the array of items. Each item is given an unique playlist identifier.
        /// </summary>
        /// <returns>The list of queue items.</returns>
        private List<QueueItem> CreateQueueItemsFromArray(Guid[] items)
        {
            return items.ToList()
                .Select(item => new QueueItem()
                {
                    ItemId = item,
                    PlaylistItemId = "syncPlayItem" + GetNextProgressiveId()
                })
                .ToList();
        }

        /// <summary>
        /// Gets the current playlist, depending on the shuffle mode.
        /// </summary>
        /// <returns>The playlist.</returns>
        private List<QueueItem> GetPlaylistAsList()
        {
            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                return ShuffledPlaylist;
            }
            else
            {
                return SortedPlaylist;
            }
        }

        /// <summary>
        /// Gets the current playing item, depending on the shuffle mode.
        /// </summary>
        /// <returns>The playing item.</returns>
        private QueueItem GetPlayingItem()
        {
            if (PlayingItemIndex == NoPlayingItemIndex)
            {
                return null;
            }
            else if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                return ShuffledPlaylist[PlayingItemIndex];
            }
            else
            {
                return SortedPlaylist[PlayingItemIndex];
            }
        }

        /// <summary>
        /// Gets the current playlist as an array, depending on the shuffle mode.
        /// </summary>
        /// <returns>The array of items in the playlist.</returns>
        public QueueItem[] GetPlaylist() {
            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                return ShuffledPlaylist.ToArray();
            }
            else
            {
                return SortedPlaylist.ToArray();
            }
        }

        /// <summary>
        /// Sets a new playlist. Playing item is reset.
        /// </summary>
        /// <param name="items">The new items of the playlist.</param>
        public void SetPlaylist(Guid[] items)
        {
            SortedPlaylist.Clear();
            ShuffledPlaylist.Clear();

            SortedPlaylist = CreateQueueItemsFromArray(items);
            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                ShuffledPlaylist = SortedPlaylist.ToList();
                ShuffledPlaylist.Shuffle();
            }

            PlayingItemIndex = NoPlayingItemIndex;
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Appends new items to the playlist. The specified order is mantained.
        /// </summary>
        /// <param name="items">The items to add to the playlist.</param>
        public void Queue(Guid[] items)
        {
            var newItems = CreateQueueItemsFromArray(items);

            SortedPlaylist.AddRange(newItems);
            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                ShuffledPlaylist.AddRange(newItems);
            }

            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Shuffles the playlist. Shuffle mode is changed.
        /// </summary>
        public void ShufflePlaylist()
        {
            if (PlayingItemIndex == NoPlayingItemIndex) {
                ShuffledPlaylist = SortedPlaylist.ToList();
                ShuffledPlaylist.Shuffle();
            }
            else if (ShuffleMode.Equals(GroupShuffleMode.Sorted))
            {
                // First time shuffle.
                var playingItem = SortedPlaylist[PlayingItemIndex];
                ShuffledPlaylist = SortedPlaylist.ToList();
                ShuffledPlaylist.RemoveAt(PlayingItemIndex);
                ShuffledPlaylist.Shuffle();
                ShuffledPlaylist = ShuffledPlaylist.Prepend(playingItem).ToList();
                PlayingItemIndex = 0;
            }
            else
            {
                // Re-shuffle playlist.
                var playingItem = ShuffledPlaylist[PlayingItemIndex];
                ShuffledPlaylist.RemoveAt(PlayingItemIndex);
                ShuffledPlaylist.Shuffle();
                ShuffledPlaylist = ShuffledPlaylist.Prepend(playingItem).ToList();
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
                var playingItem = ShuffledPlaylist[PlayingItemIndex];
                PlayingItemIndex = SortedPlaylist.IndexOf(playingItem);
            }

            ShuffledPlaylist.Clear();

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
            SortedPlaylist.Clear();
            ShuffledPlaylist.Clear();
            LastChange = DateTime.UtcNow;

            if (!clearPlayingItem && playingItem != null)
            {
                SortedPlaylist.Add(playingItem);
                if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
                {
                    ShuffledPlaylist.Add(playingItem);
                }
                PlayingItemIndex = 0;
                }
            else
            {
                PlayingItemIndex = NoPlayingItemIndex;
            }
        }

        /// <summary>
        /// Adds new items to the playlist right after the playing item. The specified order is mantained.
        /// </summary>
        /// <param name="items">The items to add to the playlist.</param>
        public void QueueNext(Guid[] items)
        {
            var newItems = CreateQueueItemsFromArray(items);

            if (ShuffleMode.Equals(GroupShuffleMode.Shuffle))
            {
                var playingItem = GetPlayingItem();
                var sortedPlayingItemIndex = SortedPlaylist.IndexOf(playingItem);
                // Append items to sorted and shuffled playlist as they are.
                SortedPlaylist.InsertRange(sortedPlayingItemIndex + 1, newItems);
                ShuffledPlaylist.InsertRange(PlayingItemIndex + 1, newItems);
            }
            else
            {
                SortedPlaylist.InsertRange(PlayingItemIndex + 1, newItems);
            }

            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets playlist identifier of the playing item, if any.
        /// </summary>
        /// <returns>The playlist identifier of the playing item.</returns>
        public string GetPlayingItemPlaylistId()
        {
            var playingItem = GetPlayingItem();
            if (playingItem != null)
            {
                return playingItem.PlaylistItemId;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the playing item identifier, if any.
        /// </summary>
        /// <returns>The playing item identifier.</returns>
        public Guid GetPlayingItemId()
        {
            var playingItem = GetPlayingItem();
            if (playingItem != null)
            {
                return playingItem.ItemId;
            }
            else
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Sets the playing item using its identifier. If not in the playlist, the playing item is reset.
        /// </summary>
        /// <param name="itemId">The new playing item identifier.</param>
        public void SetPlayingItemById(Guid itemId)
        {
            var itemIds = GetPlaylistAsList().Select(queueItem => queueItem.ItemId).ToList();
            PlayingItemIndex = itemIds.IndexOf(itemId);
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the playing item using its playlist identifier. If not in the playlist, the playing item is reset.
        /// </summary>
        /// <param name="playlistItemId">The new playing item identifier.</param>
        /// <returns><c>true</c> if playing item has been set; <c>false</c> if item is not in the playlist.</returns>
        public bool SetPlayingItemByPlaylistId(string playlistItemId)
        {
            var playlistIds = GetPlaylistAsList().Select(queueItem => queueItem.PlaylistItemId).ToList();
            PlayingItemIndex = playlistIds.IndexOf(playlistItemId);
            LastChange = DateTime.UtcNow;
            return PlayingItemIndex != NoPlayingItemIndex;
        }

        /// <summary>
        /// Sets the playing item using its position. If not in range, the playing item is reset.
        /// </summary>
        /// <param name="playlistIndex">The new playing item index.</param>
        public void SetPlayingItemByIndex(int playlistIndex)
        {
            var list = GetPlaylistAsList();
            if (playlistIndex < 0 || playlistIndex > list.Count())
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
        public bool RemoveFromPlaylist(string[] playlistItemIds)
        {
            var playingItem = GetPlayingItem();
            var playlistItemIdsList = playlistItemIds.ToList();

            SortedPlaylist.RemoveAll(item => playlistItemIdsList.Contains(item.PlaylistItemId));
            ShuffledPlaylist.RemoveAll(item => playlistItemIdsList.Contains(item.PlaylistItemId));

            LastChange = DateTime.UtcNow;

            if (playingItem != null)
            {
                if (playlistItemIds.Contains(playingItem.PlaylistItemId))
                {
                    // Playing item has been removed, picking previous item.
                    PlayingItemIndex--;
                    if (PlayingItemIndex < 0)
                    {
                        // Was first element, picking next if available.
                        // Default to no playing item otherwise.
                        PlayingItemIndex = SortedPlaylist.Count() > 0 ? 0 : NoPlayingItemIndex;
                    }

                    return true;
                }
                else
                {
                    // Restoring playing item.
                    SetPlayingItemByPlaylistId(playingItem.PlaylistItemId);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Moves an item in the playlist to another position.
        /// </summary>
        /// <param name="playlistItemId">The item to move.</param>
        /// <param name="newIndex">The new position.</param>
        /// <returns><c>true</c> if the item has been moved; <c>false</c> otherwise.</returns>
        public bool MovePlaylistItem(string playlistItemId, int newIndex)
        {
            var list = GetPlaylistAsList();
            var playingItem = GetPlayingItem();

            var playlistIds = list.Select(queueItem => queueItem.PlaylistItemId).ToList();
            var oldIndex = playlistIds.IndexOf(playlistItemId);
            if (oldIndex < 0)
            {
                return false;
            }

            var queueItem = list[oldIndex];
            list.RemoveAt(oldIndex);
            newIndex = Math.Min(newIndex, list.Count());
            newIndex = Math.Max(newIndex, 0);
            list.Insert(newIndex, queueItem);

            LastChange = DateTime.UtcNow;
            PlayingItemIndex = list.IndexOf(playingItem);
            return true;
        }

        /// <summary>
        /// Resets the playlist to its initial state.
        /// </summary>
        public void Reset()
        {
            ProgressiveId = 0;
            SortedPlaylist.Clear();
            ShuffledPlaylist.Clear();
            PlayingItemIndex = NoPlayingItemIndex;
            ShuffleMode = GroupShuffleMode.Sorted;
            RepeatMode = GroupRepeatMode.RepeatNone;
            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the repeat mode.
        /// </summary>
        /// <param name="mode">The new mode.</param>
        public void SetRepeatMode(string mode)
        {
            switch (mode)
            {
                case "RepeatOne":
                    RepeatMode = GroupRepeatMode.RepeatOne;
                    break;
                case "RepeatAll":
                    RepeatMode = GroupRepeatMode.RepeatAll;
                    break;
                default:
                    RepeatMode = GroupRepeatMode.RepeatNone;
                    break;
            }

            LastChange = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the shuffle mode.
        /// </summary>
        /// <param name="mode">The new mode.</param>
        public void SetShuffleMode(string mode)
        {
            switch (mode)
            {
                case "Shuffle":
                    ShufflePlaylist();
                    break;
                default:
                    RestoreSortedPlaylist();
                    break;
            }
        }

        /// <summary>
        /// Toggles the shuffle mode between sorted and shuffled.
        /// </summary>
        public void ToggleShuffleMode()
        {
            SetShuffleMode(ShuffleMode.Equals(GroupShuffleMode.Shuffle) ? "Shuffle" : "");
        }

        /// <summary>
        /// Gets the next item in the playlist considering repeat mode and shuffle mode.
        /// </summary>
        /// <returns>The next item in the playlist.</returns>
        public QueueItem GetNextItemPlaylistId()
        {
            int newIndex;
            var playlist = GetPlaylistAsList();

            switch (RepeatMode)
            {
                case GroupRepeatMode.RepeatOne:
                    newIndex = PlayingItemIndex;
                    break;
                case GroupRepeatMode.RepeatAll:
                    newIndex = PlayingItemIndex + 1;
                    if (newIndex >= playlist.Count())
                    {
                        newIndex = 0;
                    }
                    break;
                default:
                    newIndex = PlayingItemIndex + 1;
                    break;
            }

            if (newIndex < 0 || newIndex >= playlist.Count())
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
            if (PlayingItemIndex >= SortedPlaylist.Count())
            {
                if (RepeatMode.Equals(GroupRepeatMode.RepeatAll))
                {
                    PlayingItemIndex = 0;
                }
                else
                {
                    PlayingItemIndex--;
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
                    PlayingItemIndex = SortedPlaylist.Count() - 1;
                }
                else
                {
                    PlayingItemIndex++;
                    return false;
                }
            }

            LastChange = DateTime.UtcNow;
            return true;
        }
    }
}
