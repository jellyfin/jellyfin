using MediaBrowser.Common.Events;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Connectivity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class UserDataManager
    /// </summary>
    public class UserDataManager : BaseManager<Kernel>
    {
        #region Events
        /// <summary>
        /// Occurs when [playback start].
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs> PlaybackStart;
        /// <summary>
        /// Occurs when [playback progress].
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;
        /// <summary>
        /// Occurs when [playback stopped].
        /// </summary>
        public event EventHandler<PlaybackProgressEventArgs> PlaybackStopped;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public UserDataManager(Kernel kernel)
            : base(kernel)
        {

        }

        /// <summary>
        /// Used to report that playback has started for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void OnPlaybackStart(User user, BaseItem item, ClientType clientType, string deviceName)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            Kernel.UserManager.UpdateNowPlayingItemId(user, clientType, deviceName, item);

            // Nothing to save here
            // Fire events to inform plugins
            EventHelper.QueueEventIfNotNull(PlaybackStart, this, new PlaybackProgressEventArgs
            {
                Argument = item,
                User = user
            });
        }

        /// <summary>
        /// Used to report playback progress for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task OnPlaybackProgress(User user, BaseItem item, long? positionTicks, ClientType clientType, string deviceName)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            Kernel.UserManager.UpdateNowPlayingItemId(user, clientType, deviceName, item, positionTicks);

            if (positionTicks.HasValue)
            {
                var data = item.GetUserData(user, true);

                UpdatePlayState(item, data, positionTicks.Value, false);
                await SaveUserDataForItem(user, item, data).ConfigureAwait(false);
            }

            EventHelper.QueueEventIfNotNull(PlaybackProgress, this, new PlaybackProgressEventArgs
            {
                Argument = item,
                User = user,
                PlaybackPositionTicks = positionTicks
            });
        }

        /// <summary>
        /// Used to report that playback has ended for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task OnPlaybackStopped(User user, BaseItem item, long? positionTicks, ClientType clientType, string deviceName)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            Kernel.UserManager.RemoveNowPlayingItemId(user, clientType, deviceName, item);
            
            var data = item.GetUserData(user, true);

            if (positionTicks.HasValue)
            {
                UpdatePlayState(item, data, positionTicks.Value, true);
            }
            else
            {
                // If the client isn't able to report this, then we'll just have to make an assumption
                data.PlayCount++;
                data.Played = true;
            }

            await SaveUserDataForItem(user, item, data).ConfigureAwait(false);

            EventHelper.QueueEventIfNotNull(PlaybackStopped, this, new PlaybackProgressEventArgs
            {
                Argument = item,
                User = user,
                PlaybackPositionTicks = positionTicks
            });
        }

        /// <summary>
        /// Updates playstate position for an item but does not save
        /// </summary>
        /// <param name="item">The item</param>
        /// <param name="data">User data for the item</param>
        /// <param name="positionTicks">The current playback position</param>
        /// <param name="incrementPlayCount">Whether or not to increment playcount</param>
        private void UpdatePlayState(BaseItem item, UserItemData data, long positionTicks, bool incrementPlayCount)
        {
            // If a position has been reported, and if we know the duration
            if (positionTicks > 0 && item.RunTimeTicks.HasValue && item.RunTimeTicks > 0)
            {
                var pctIn = Decimal.Divide(positionTicks, item.RunTimeTicks.Value) * 100;

                // Don't track in very beginning
                if (pctIn < Kernel.Configuration.MinResumePct)
                {
                    positionTicks = 0;
                    incrementPlayCount = false;
                }

                // If we're at the end, assume completed
                else if (pctIn > Kernel.Configuration.MaxResumePct || positionTicks >= item.RunTimeTicks.Value)
                {
                    positionTicks = 0;
                    data.Played = true;
                }

                else
                {
                    // Enforce MinResumeDuration
                    var durationSeconds = TimeSpan.FromTicks(item.RunTimeTicks.Value).TotalSeconds;

                    if (durationSeconds < Kernel.Configuration.MinResumeDurationSeconds)
                    {
                        positionTicks = 0;
                        data.Played = true;
                    }
                }
            }

            data.PlaybackPositionTicks = positionTicks;

            if (incrementPlayCount)
            {
                data.PlayCount++;
                data.LastPlayedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Saves user data for an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="data">The data.</param>
        public Task SaveUserDataForItem(User user, BaseItem item, UserItemData data)
        {
            item.AddOrUpdateUserData(user, data);

            return Kernel.UserDataRepository.SaveUserData(item, CancellationToken.None);
        }
    }
}
