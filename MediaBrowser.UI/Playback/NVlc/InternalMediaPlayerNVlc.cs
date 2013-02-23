using Declarations.Events;
using Declarations.Media;
using Declarations.Players;
using Implementation;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.UI.Configuration;
using MediaBrowser.UI.Playback.InternalPlayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBrowser.UI.Playback.NVlc
{
    /// <summary>
    /// Class InternalMediaPlayer
    /// </summary>
    public class InternalMediaPlayerNVlc : BaseInternalMediaPlayer
    {
        public InternalMediaPlayerNVlc(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets or sets the media player factory.
        /// </summary>
        /// <value>The media player factory.</value>
        private MediaPlayerFactory MediaPlayerFactory { get; set; }

        /// <summary>
        /// Gets or sets the video player.
        /// </summary>
        /// <value>The video player.</value>
        private IMediaListPlayer VideoPlayer { get; set; }

        /// <summary>
        /// Gets or sets the media list.
        /// </summary>
        /// <value>The media list.</value>
        private IMediaList MediaList { get; set; }

        /// <summary>
        /// Gets a value indicating whether [supports multi file playback].
        /// </summary>
        /// <value><c>true</c> if [supports multi file playback]; otherwise, <c>false</c>.</value>
        public override bool SupportsMultiFilePlayback
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can mute.
        /// </summary>
        /// <value><c>true</c> if this instance can mute; otherwise, <c>false</c>.</value>
        public override bool CanMute
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can change volume.
        /// </summary>
        /// <value><c>true</c> if this instance can change volume; otherwise, <c>false</c>.</value>
        public override bool CanControlVolume
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is muted.
        /// </summary>
        /// <value><c>true</c> if this instance is muted; otherwise, <c>false</c>.</value>
        protected override bool IsMuted
        {
            get { return VideoPlayer != null && VideoPlayer.InnerPlayer.Mute; }
        }

        /// <summary>
        /// The _current playlist index
        /// </summary>
        private int _currentPlaylistIndex;

        /// <summary>
        /// Gets the index of the current playlist.
        /// </summary>
        /// <value>The index of the current playlist.</value>
        public override int CurrentPlaylistIndex
        {
            get
            {
                return _currentPlaylistIndex;
            }
        }

        /// <summary>
        /// Gets the current position ticks.
        /// </summary>
        /// <value>The current position ticks.</value>
        public override long? CurrentPositionTicks
        {
            get
            {
                if (VideoPlayer != null)
                {
                    return TimeSpan.FromMilliseconds(VideoPlayer.Time).Ticks;
                }

                return base.CurrentPositionTicks;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can monitor progress.
        /// </summary>
        /// <value><c>true</c> if this instance can monitor progress; otherwise, <c>false</c>.</value>
        protected override bool CanMonitorProgress
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets or sets the windows forms panel.
        /// </summary>
        /// <value>The windows forms panel.</value>
        private Panel WindowsFormsPanel { get; set; }

        /// <summary>
        /// Ensures the player.
        /// </summary>
        protected override void EnsureMediaPlayerCreated()
        {
            if (MediaPlayerFactory != null)
            {
                return;
            }

            WindowsFormsPanel = new Panel();
            WindowsFormsPanel.BackColor = Color.Black;

            App.Instance.HiddenWindow.WindowsFormsHost.Child = WindowsFormsPanel;

            MediaPlayerFactory = new MediaPlayerFactory(new[] 
             {
                "-I", 
                "dummy",  
		        "--ignore-config", 
                "--no-osd",
                "--disable-screensaver",
                //"--ffmpeg-hw",
		        "--plugin-path=./plugins"
             });
        }

        /// <summary>
        /// Events_s the media changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void Events_MediaChanged(object sender, MediaPlayerMediaChanged e)
        {
            //var current = MediaList.FirstOrDefault(i => i.Tag == e.NewMedia.Tag);

            //var newIndex = current != null ? MediaList.IndexOf(current) : -1;

            //var currentIndex = _currentPlaylistIndex;

            //if (newIndex != currentIndex)
            //{
            //    OnMediaChanged(currentIndex, null, newIndex);
            //}

            //_currentPlaylistIndex = newIndex;
        }

        /// <summary>
        /// Determines whether this instance can play the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if this instance can play the specified item; otherwise, <c>false</c>.</returns>
        public override bool CanPlay(BaseItemDto item)
        {
            return item.IsVideo || item.IsAudio;
        }

        /// <summary>
        /// Gets a value indicating whether this instance can queue.
        /// </summary>
        /// <value><c>true</c> if this instance can queue; otherwise, <c>false</c>.</value>
        public override bool CanQueue
        {
            get { return true; }
        }

        /// <summary>
        /// Plays the internal.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        protected override void PlayInternal(List<BaseItemDto> items, PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            EnsureMediaPlayerCreated();

            _currentPlaylistIndex = 0;

            MediaList = MediaPlayerFactory.CreateMediaList<IMediaList>(items.Select(GetPlayablePath));
            VideoPlayer = MediaPlayerFactory.CreateMediaListPlayer<IMediaListPlayer>(MediaList);

            VideoPlayer.InnerPlayer.WindowHandle = WindowsFormsPanel.Handle;

            VideoPlayer.InnerPlayer.Events.PlayerStopped += Events_PlayerStopped;
            VideoPlayer.Play();

            var position = options.StartPositionTicks;

            if (position > 0)
            {
                VideoPlayer.Time = Convert.ToInt64(TimeSpan.FromTicks(position).TotalMilliseconds);
            }

            VideoPlayer.MediaListPlayerEvents.MediaListPlayerNextItemSet += MediaListPlayerEvents_MediaListPlayerNextItemSet;

            base.PlayInternal(items, options, playerConfiguration);
        }

        /// <summary>
        /// Gets the playable path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string GetPlayablePath(BaseItemDto item)
        {
            if (item.VideoType.HasValue && item.VideoType.Value == VideoType.BluRay)
            {
                var file = Directory.EnumerateFiles(item.Path, "*.m2ts", SearchOption.AllDirectories).OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();

                if (!string.IsNullOrEmpty(file))
                {
                    return file;
                }
            }

            return item.Path;
        }

        /// <summary>
        /// Medias the list player events_ media list player next item set.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void MediaListPlayerEvents_MediaListPlayerNextItemSet(object sender, MediaListPlayerNextItemSet e)
        {
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Internal Player"; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can pause.
        /// </summary>
        /// <value><c>true</c> if this instance can pause; otherwise, <c>false</c>.</value>
        public override bool CanPause
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can seek.
        /// </summary>
        /// <value><c>true</c> if this instance can seek; otherwise, <c>false</c>.</value>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Queues the internal.
        /// </summary>
        /// <param name="items">The items.</param>
        protected override void QueueInternal(List<BaseItemDto> items)
        {
        }

        /// <summary>
        /// Seeks the internal.
        /// </summary>
        /// <param name="positionTicks">The position ticks.</param>
        /// <returns>Task.</returns>
        protected override Task SeekInternal(long positionTicks)
        {
            return Task.Run(() => VideoPlayer.Time = Convert.ToInt64(TimeSpan.FromTicks(positionTicks).TotalMilliseconds));
        }

        /// <summary>
        /// Pauses the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task PauseInternal()
        {
            return Task.Run(() => VideoPlayer.Pause());
        }

        /// <summary>
        /// Uns the pause internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task UnPauseInternal()
        {
            return Task.Run(() => VideoPlayer.Pause());
        }

        /// <summary>
        /// Stops the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task StopInternal()
        {
            return Task.Run(() => VideoPlayer.Stop());
        }

        /// <summary>
        /// Handles the PlayerStopped event of the Events control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void Events_PlayerStopped(object sender, EventArgs e)
        {
            OnPlayerStopped(CurrentPlaylistIndex, CurrentPositionTicks);
        }

        /// <summary>
        /// Called when [player stopped].
        /// </summary>
        protected override void OnPlayerStoppedInternal()
        {
            VideoPlayer.MediaListPlayerEvents.MediaListPlayerNextItemSet -= MediaListPlayerEvents_MediaListPlayerNextItemSet;

            MediaList.Dispose();

            VideoPlayer.InnerPlayer.Events.PlayerStopped -= Events_PlayerStopped;
            VideoPlayer.InnerPlayer.Dispose();

            //VideoPlayer.Dispose();
            VideoPlayer = null;

            _currentPlaylistIndex = 0;

            base.OnPlayerStoppedInternal();
        }

        /// <summary>
        /// Gets the volume.
        /// </summary>
        /// <returns>System.Int32.</returns>
        protected override int GetVolume()
        {
            return VideoPlayer.InnerPlayer.Volume;
        }

        /// <summary>
        /// Sets the volume, on a scale from 0-100
        /// </summary>
        /// <param name="value">The value.</param>
        protected override void SetVolume(int value)
        {
            if (value > 0 && VideoPlayer.InnerPlayer.Mute)
            {
                VideoPlayer.InnerPlayer.Mute = false;
            }

            VideoPlayer.InnerPlayer.Volume = value;
        }

        /// <summary>
        /// Sets the mute.
        /// </summary>
        /// <param name="mute">if set to <c>true</c> [mute].</param>
        protected override void SetMute(bool mute)
        {
            VideoPlayer.InnerPlayer.Mute = mute;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            base.Dispose(dispose);

            if (dispose)
            {
                if (MediaList != null)
                {
                    MediaList.Dispose();
                }
                if (VideoPlayer != null)
                {
                    if (VideoPlayer.InnerPlayer != null)
                    {
                        VideoPlayer.InnerPlayer.Dispose();
                    }
                }
                if (MediaPlayerFactory != null)
                {
                    MediaPlayerFactory.Dispose();
                }
            }
        }
    }
}
