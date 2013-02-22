using MediaBrowser.Common.Events;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.UI.Configuration;
using MediaBrowser.UI.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.UI.Playback
{
    /// <summary>
    /// Class BaseMediaPlayer
    /// </summary>
    public abstract class BaseMediaPlayer : IDisposable
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        #region VolumeChanged
        /// <summary>
        /// Occurs when [volume changed].
        /// </summary>
        public event EventHandler VolumeChanged;
        protected void OnVolumeChanged()
        {
            EventHelper.FireEventIfNotNull(VolumeChanged, this, EventArgs.Empty, Logger);
        }
        #endregion

        #region PlayStateChanged
        /// <summary>
        /// Occurs when [play state changed].
        /// </summary>
        public event EventHandler PlayStateChanged;
        protected void OnPlayStateChanged()
        {
            EventHelper.FireEventIfNotNull(PlayStateChanged, this, EventArgs.Empty, Logger);
        }
        #endregion
        
        /// <summary>
        /// The null task result
        /// </summary>
        protected Task<bool> NullTaskResult = Task.FromResult(false);

        /// <summary>
        /// Gets a value indicating whether [supports multi file playback].
        /// </summary>
        /// <value><c>true</c> if [supports multi file playback]; otherwise, <c>false</c>.</value>
        public abstract bool SupportsMultiFilePlayback { get; }

        /// <summary>
        /// The currently playing items
        /// </summary>
        public List<BaseItemDto> Playlist = new List<BaseItemDto>();

        /// <summary>
        /// The _play state
        /// </summary>
        private PlayState _playState;
        /// <summary>
        /// Gets or sets the state of the play.
        /// </summary>
        /// <value>The state of the play.</value>
        public PlayState PlayState
        {
            get
            {
                return _playState;
            }
            set
            {
                _playState = value;

                OnPlayStateChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BaseMediaPlayer" /> is mute.
        /// </summary>
        /// <value><c>true</c> if mute; otherwise, <c>false</c>.</value>
        public bool Mute
        {
            get { return IsMuted; }
            set
            {
                SetMute(value);
                OnVolumeChanged();
            }
        }

        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public int Volume
        {
            get { return GetVolume(); }
            set
            {
                SetVolume(value);
                OnVolumeChanged();
            }
        }

        /// <summary>
        /// Gets the current player configuration.
        /// </summary>
        /// <value>The current player configuration.</value>
        public PlayerConfiguration CurrentPlayerConfiguration { get; private set; }

        /// <summary>
        /// Gets the current play options.
        /// </summary>
        /// <value>The current play options.</value>
        public PlayOptions CurrentPlayOptions { get; private set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Determines whether this instance can play the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if this instance can play the specified item; otherwise, <c>false</c>.</returns>
        public abstract bool CanPlay(BaseItemDto item);

        /// <summary>
        /// Gets a value indicating whether this instance can change volume.
        /// </summary>
        /// <value><c>true</c> if this instance can change volume; otherwise, <c>false</c>.</value>
        public abstract bool CanControlVolume { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can mute.
        /// </summary>
        /// <value><c>true</c> if this instance can mute; otherwise, <c>false</c>.</value>
        public abstract bool CanMute { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can queue.
        /// </summary>
        /// <value><c>true</c> if this instance can queue; otherwise, <c>false</c>.</value>
        public abstract bool CanQueue { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can pause.
        /// </summary>
        /// <value><c>true</c> if this instance can pause; otherwise, <c>false</c>.</value>
        public abstract bool CanPause { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can seek.
        /// </summary>
        /// <value><c>true</c> if this instance can seek; otherwise, <c>false</c>.</value>
        public abstract bool CanSeek { get; }

        /// <summary>
        /// Gets the index of the current playlist.
        /// </summary>
        /// <value>The index of the current playlist.</value>
        public virtual int CurrentPlaylistIndex
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the current media.
        /// </summary>
        /// <value>The current media.</value>
        public BaseItemDto CurrentMedia
        {
            get
            {
                return CurrentPlaylistIndex == -1 ? null : Playlist[CurrentPlaylistIndex];
            }
        }

        /// <summary>
        /// Gets the current position ticks.
        /// </summary>
        /// <value>The current position ticks.</value>
        public virtual long? CurrentPositionTicks
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is muted.
        /// </summary>
        /// <value><c>true</c> if this instance is muted; otherwise, <c>false</c>.</value>
        protected virtual bool IsMuted
        {
            get { return false; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMediaPlayer" /> class.
        /// </summary>
        protected BaseMediaPlayer(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Sets the mute.
        /// </summary>
        /// <param name="mute">if set to <c>true</c> [mute].</param>
        protected virtual void SetMute(bool mute)
        {
        }

        /// <summary>
        /// Sets the volume, on a scale from 0-100
        /// </summary>
        /// <param name="value">The value.</param>
        protected virtual void SetVolume(int value)
        {
        }

        /// <summary>
        /// Gets the volume.
        /// </summary>
        /// <returns>System.Int32.</returns>
        protected virtual int GetVolume()
        {
            return 0;
        }

        /// <summary>
        /// Plays the internal.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        protected abstract void PlayInternal(List<BaseItemDto> items, PlayOptions options, PlayerConfiguration playerConfiguration);

        /// <summary>
        /// Queues the internal.
        /// </summary>
        /// <param name="items">The items.</param>
        protected virtual void QueueInternal(List<BaseItemDto> items)
        {
        }

        /// <summary>
        /// Stops the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected abstract Task StopInternal();

        /// <summary>
        /// The play semaphore
        /// </summary>
        private readonly SemaphoreSlim PlaySemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets or sets the progress update timer.
        /// </summary>
        /// <value>The progress update timer.</value>
        private Timer ProgressUpdateTimer { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance can monitor progress.
        /// </summary>
        /// <value><c>true</c> if this instance can monitor progress; otherwise, <c>false</c>.</value>
        protected virtual bool CanMonitorProgress
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task Stop()
        {
            var playstate = PlayState;

            if (playstate == PlayState.Playing || playstate == PlayState.Paused)
            {
                Logger.Info("Stopping");

                return StopInternal();
            }

            throw new InvalidOperationException(string.Format("{0} is already {1}", Name, playstate));
        }

        /// <summary>
        /// Plays the specified item.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">items</exception>
        internal async Task Play(PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            await PlaySemaphore.WaitAsync();

            PlayState = PlayState.Playing;
            
            lock (Playlist)
            {
                Playlist.Clear();
                Playlist.AddRange(options.Items);
            }

            CurrentPlayerConfiguration = playerConfiguration;
            CurrentPlayOptions = options;

            if (options.Items.Count > 1)
            {
                Logger.Info("Playing {0} items", options.Items.Count);
            }
            else
            {
                Logger.Info("Playing {0}", options.Items[0].Name);
            }

            try
            {
                PlayInternal(options.Items, options, playerConfiguration);
            }
            catch (Exception ex)
            {
                Logger.Info("Error beginning playback", ex);

                CurrentPlayerConfiguration = null;
                CurrentPlayOptions = null;
                Playlist.Clear();

                PlayState = PlayState.Idle;
                PlaySemaphore.Release();

                throw;
            }

            SendPlaybackStartCheckIn(options.Items[0]);

            ReloadProgressUpdateTimer();
        }

        /// <summary>
        /// Restarts the progress update timer.
        /// </summary>
        private void ReloadProgressUpdateTimer()
        {
            ProgressUpdateTimer = new Timer(OnProgressTimerStopped, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Called when [progress timer stopped].
        /// </summary>
        /// <param name="state">The state.</param>
        private void OnProgressTimerStopped(object state)
        {
            var index = CurrentPlaylistIndex;

            if (index != -1)
            {
                SendPlaybackProgressCheckIn(Playlist[index], CurrentPositionTicks);
            }
        }

        /// <summary>
        /// Queues the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <exception cref="System.ArgumentNullException">items</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        internal void Queue(List<BaseItemDto> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            var playstate = PlayState;

            if (playstate != PlayState.Playing && playstate != PlayState.Paused)
            {
                throw new InvalidOperationException(string.Format("{0} cannot queue from playstate: {1}", Name, playstate));
            }

            lock (Playlist)
            {
                Playlist.AddRange(items);
            }

            QueueInternal(items);
        }

        /// <summary>
        /// Called when [player stopped].
        /// </summary>
        /// <param name="lastPlaylistIndex">Last index of the playlist.</param>
        /// <param name="positionTicks">The position ticks.</param>
        protected void OnPlayerStopped(int? lastPlaylistIndex, long? positionTicks)
        {
            Logger.Info("Stopped");

            if (positionTicks.HasValue && positionTicks.Value == 0)
            {
                positionTicks = null;
            }

            var items = Playlist.ToList();

            DisposeProgressUpdateTimer();

            var index = lastPlaylistIndex ?? CurrentPlaylistIndex;

            var lastItem = items[index];
            SendPlaybackStopCheckIn(items[index], positionTicks);

            if (!CanMonitorProgress)
            {
                if (items.Count > 1)
                {
                    MarkWatched(items.Except(new[] { lastItem }));
                }
            }

            OnPlayerStoppedInternal();

            UIKernel.Instance.PlaybackManager.OnPlaybackCompleted(this, Playlist.ToList());

            CurrentPlayerConfiguration = null;
            CurrentPlayOptions = null;
            Logger.Info("Clearing Playlist");
            Playlist.Clear();

            PlayState = PlayState.Idle;
            
            PlaySemaphore.Release();
        }

        /// <summary>
        /// Called when [player stopped internal].
        /// </summary>
        protected virtual void OnPlayerStoppedInternal()
        {

        }

        /// <summary>
        /// Seeks the specified position ticks.
        /// </summary>
        /// <param name="positionTicks">The position ticks.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task Seek(long positionTicks)
        {
            var playState = PlayState;

            if (playState == PlayState.Playing || playState == PlayState.Paused)
            {
                await SeekInternal(positionTicks);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Cannot seek {0} with playstate {1}", Name, PlayState));
            }
        }

        /// <summary>
        /// Seeks the internal.
        /// </summary>
        /// <param name="positionTicks">The position ticks.</param>
        /// <returns>Task.</returns>
        protected virtual Task SeekInternal(long positionTicks)
        {
            return NullTaskResult;
        }

        /// <summary>
        /// The ten seconds
        /// </summary>
        private static readonly long TenSeconds = TimeSpan.FromSeconds(10).Ticks;

        /// <summary>
        /// Goes to next chapter.
        /// </summary>
        /// <returns>Task.</returns>
        public virtual Task GoToNextChapter()
        {
            var current = CurrentPositionTicks;

            var chapter = CurrentMedia.Chapters.FirstOrDefault(c => c.StartPositionTicks > current);

            return chapter != null ? Seek(chapter.StartPositionTicks) : NullTaskResult;
        }

        /// <summary>
        /// Goes to previous chapter.
        /// </summary>
        /// <returns>Task.</returns>
        public virtual Task GoToPreviousChapter()
        {
            var current = CurrentPositionTicks;

            var chapter = CurrentMedia.Chapters.LastOrDefault(c => c.StartPositionTicks < current - TenSeconds);

            return chapter != null ? Seek(chapter.StartPositionTicks) : NullTaskResult;
        }
        
        /// <summary>
        /// Pauses this instance.
        /// </summary>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task Pause()
        {
            if (PlayState == PlayState.Playing)
            {
                await PauseInternal();

                PlayState = PlayState.Paused;
            }
            else
            {
                throw new InvalidOperationException(string.Format("Cannot pause {0} with playstate {1}", Name, PlayState));
            }
        }

        /// <summary>
        /// Pauses the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task PauseInternal()
        {
            return NullTaskResult;
        }

        /// <summary>
        /// Uns the pause.
        /// </summary>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task UnPause()
        {
            if (PlayState == PlayState.Paused)
            {
                await UnPauseInternal();
                PlayState = PlayState.Playing;
            }
            else
            {
                throw new InvalidOperationException(string.Format("Cannot unpause {0} with playstate {1}", Name, PlayState));
            }
        }

        /// <summary>
        /// Uns the pause internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task UnPauseInternal()
        {
            return NullTaskResult;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            Logger.Info("Disposing");

            DisposeProgressUpdateTimer();

            if (PlayState == PlayState.Playing || PlayState == PlayState.Paused)
            {
                var index = CurrentPlaylistIndex;

                if (index != -1)
                {
                    SendPlaybackStopCheckIn(Playlist[index], CurrentPositionTicks);
                }
                Task.Run(() => Stop());
                Thread.Sleep(1000);
            }

            PlaySemaphore.Dispose();
        }

        /// <summary>
        /// Disposes the progress update timer.
        /// </summary>
        private void DisposeProgressUpdateTimer()
        {
            if (ProgressUpdateTimer != null)
            {
                ProgressUpdateTimer.Dispose();
            }
        }

        /// <summary>
        /// Sends the playback start check in.
        /// </summary>
        /// <param name="item">The item.</param>
        protected async void SendPlaybackStartCheckIn(BaseItemDto item)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                return;
            }

            Logger.Info("Sending playback start checkin for {0}", item.Name);

            try
            {
                await UIKernel.Instance.ApiClient.ReportPlaybackStartAsync(item.Id, App.Instance.CurrentUser.Id);
            }
            catch (HttpException ex)
            {
                Logger.ErrorException("Error sending playback start checking for {0}", ex, item.Name);
            }
        }

        /// <summary>
        /// Sends the playback progress check in.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        protected async void SendPlaybackProgressCheckIn(BaseItemDto item, long? positionTicks)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                return;
            }
            var position = positionTicks.HasValue ? TimeSpan.FromTicks(positionTicks.Value).ToString() : "unknown";

            Logger.Info("Sending playback progress checkin for {0} at position {1}", item.Name, position);

            try
            {
                await UIKernel.Instance.ApiClient.ReportPlaybackProgressAsync(item.Id, App.Instance.CurrentUser.Id, positionTicks);
            }
            catch (HttpException ex)
            {
                Logger.ErrorException("Error sending playback progress checking for {0}", ex, item.Name);
            }
        }

        /// <summary>
        /// Sends the playback stop check in.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="positionTicks">The position ticks.</param>
        protected async void SendPlaybackStopCheckIn(BaseItemDto item, long? positionTicks)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                return;
            }
            var position = positionTicks.HasValue ? TimeSpan.FromTicks(positionTicks.Value).ToString() : "unknown";

            Logger.Info("Sending playback stop checkin for {0} at position {1}", item.Name, position);

            try
            {
                await UIKernel.Instance.ApiClient.ReportPlaybackStoppedAsync(item.Id, App.Instance.CurrentUser.Id, positionTicks);
            }
            catch (HttpException ex)
            {
                Logger.ErrorException("Error sending playback stop checking for {0}", ex, item.Name);
            }
        }

        /// <summary>
        /// Marks the watched.
        /// </summary>
        /// <param name="items">The items.</param>
        protected async void MarkWatched(IEnumerable<BaseItemDto> items)
        {
            var idList = items.Where(i => !string.IsNullOrEmpty(i.Id)).Select(i => i.Id);

            try
            {
                await UIKernel.Instance.ApiClient.UpdatePlayedStatusAsync(idList.First(), App.Instance.CurrentUser.Id, true);
            }
            catch (HttpException ex)
            {
                Logger.ErrorException("Error marking items watched", ex);
            }
        }

        /// <summary>
        /// Called when [media changed].
        /// </summary>
        /// <param name="oldPlaylistIndex">Old index of the playlist.</param>
        /// <param name="endingPositionTicks">The ending position ticks.</param>
        /// <param name="newPlaylistIndex">New index of the playlist.</param>
        protected void OnMediaChanged(int oldPlaylistIndex, long? endingPositionTicks, int newPlaylistIndex)
        {
            DisposeProgressUpdateTimer();

            Task.Run(() =>
            {
                if (oldPlaylistIndex != -1)
                {
                    SendPlaybackStopCheckIn(Playlist[oldPlaylistIndex], endingPositionTicks);
                }

                if (newPlaylistIndex != -1)
                {
                    SendPlaybackStartCheckIn(Playlist[newPlaylistIndex]);
                }
            });

            ReloadProgressUpdateTimer();
        }
    }
}
