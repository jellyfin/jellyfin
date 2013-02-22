using MediaBrowser.Common.IO;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.UI.Configuration;
using MediaBrowser.UI.Playback;
using MediaBrowser.UI.Playback.ExternalPlayer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Tmt5
{
    /// <summary>
    /// Class GenericExternalPlayer
    /// </summary>
    [Export(typeof(BaseMediaPlayer))]
    public class Tmt5MediaPlayer : BaseExternalPlayer
    {
        [ImportingConstructor]
        public Tmt5MediaPlayer([Import("logger")] ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "TMT5"; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can pause.
        /// </summary>
        /// <value><c>true</c> if this instance can pause; otherwise, <c>false</c>.</value>
        public override bool CanPause
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can close automatically.
        /// </summary>
        /// <value><c>true</c> if this instance can close automatically; otherwise, <c>false</c>.</value>
        protected override bool CanCloseAutomatically
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the play state directory.
        /// </summary>
        /// <value>The play state directory.</value>
        private string PlayStateDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ArcSoft");
            }
        }


        /// <summary>
        /// The _current position ticks
        /// </summary>
        private long? _currentPositionTicks;

        /// <summary>
        /// Gets the current position ticks.
        /// </summary>
        /// <value>The current position ticks.</value>
        public override long? CurrentPositionTicks
        {
            get
            {
                return _currentPositionTicks;
            }
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
        /// Gets or sets the status file watcher.
        /// </summary>
        /// <value>The status file watcher.</value>
        private FileSystemWatcher StatusFileWatcher { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has started playing.
        /// </summary>
        /// <value><c>true</c> if this instance has started playing; otherwise, <c>false</c>.</value>
        private bool HasStartedPlaying { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has stopped playing.
        /// </summary>
        /// <value><c>true</c> if this instance has stopped playing; otherwise, <c>false</c>.</value>
        private bool HasStoppedPlaying { get; set; }

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
        /// Called when [player stopped internal].
        /// </summary>
        protected override void OnPlayerStoppedInternal()
        {
            DisposeFileSystemWatcher();
            HasStartedPlaying = false;
            HasStoppedPlaying = false;
            _currentPlaylistIndex = 0;
            _currentPositionTicks = 0;

            base.OnPlayerStoppedInternal();
        }

        /// <summary>
        /// Gets the command arguments.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandArguments(List<BaseItemDto> items, PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            return "\"" + items[0].Path + "\"";
        }

        /// <summary>
        /// Called when [external player launched].
        /// </summary>
        protected override void OnExternalPlayerLaunched()
        {
            base.OnExternalPlayerLaunched();

            // If the playstate directory exists, start watching it
            if (Directory.Exists(PlayStateDirectory))
            {
                ReloadFileSystemWatcher();
            }
        }

        /// <summary>
        /// Pauses the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task PauseInternal()
        {
            return SendCommandToMMC("-pause");
        }

        /// <summary>
        /// Uns the pause internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task UnPauseInternal()
        {
            return SendCommandToMMC("-play");
        }

        /// <summary>
        /// Stops the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task StopInternal()
        {
            return SendCommandToMMC("-stop");
        }

        /// <summary>
        /// Closes the player.
        /// </summary>
        /// <returns>Task.</returns>
        protected Task ClosePlayer()
        {
            return SendCommandToMMC("-close");
        }

        /// <summary>
        /// Seeks the internal.
        /// </summary>
        /// <param name="positionTicks">The position ticks.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">No media to seek to</exception>
        protected override Task SeekInternal(long positionTicks)
        {
            if (CurrentMedia == null)
            {
                throw new InvalidOperationException("No media to seek to");
            }

            if (CurrentMedia.Chapters == null)
            {
                throw new InvalidOperationException("TMT5 cannot seek without chapter information");
            }

            var chapterIndex = 0;

            for (var i = 0; i < CurrentMedia.Chapters.Count; i++)
            {
                if (CurrentMedia.Chapters[i].StartPositionTicks < positionTicks)
                {
                    chapterIndex = i;
                }
            }

            return JumpToChapter(chapterIndex);
        }

        /// <summary>
        /// Jumps to chapter.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <returns>Task.</returns>
        protected Task JumpToChapter(int chapter)
        {
            return SendCommandToMMC(" -chapter " + chapter);
        }

        /// <summary>
        /// Sends an arbitrary command to the TMT MMC console
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>Task.</returns>
        protected Task SendCommandToMMC(string command)
        {
            return Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(CurrentPlayerConfiguration.Command);

                var processInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(directory, "MMCEDT5.exe"),
                    Arguments = command,
                    CreateNoWindow = true
                };

                Logger.Debug("{0} {1}", processInfo.FileName, processInfo.Arguments);

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit(2000);
                }
            });
        }

        /// <summary>
        /// Reloads the file system watcher.
        /// </summary>
        private void ReloadFileSystemWatcher()
        {
            DisposeFileSystemWatcher();

            Logger.Info("Watching TMT folder: " + PlayStateDirectory);

            StatusFileWatcher = new FileSystemWatcher(PlayStateDirectory, "*.set")
            {
                IncludeSubdirectories = true
            };

            // Need to include subdirectories since there are subfolders undearneath this with the TMT version #.
            StatusFileWatcher.Changed += StatusFileWatcher_Changed;
            StatusFileWatcher.EnableRaisingEvents = true;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Handles the Changed event of the StatusFileWatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FileSystemEventArgs" /> instance containing the event data.</param>
        async void StatusFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Logger.Debug("TMT File Watcher reports change type {1} at {0}", e.FullPath, e.ChangeType);

            NameValueCollection values;

            try
            {
                values = FileSystem.ParseIniFile(e.FullPath);
            }
            catch (IOException)
            {
                // This can happen if the file is being written to at the exact moment we're trying to access it
                // Unfortunately we kind of have to just eat it
                return;
            }

            var tmtPlayState = values["State"];

            if (tmtPlayState.Equals("play", StringComparison.OrdinalIgnoreCase))
            {
                PlayState = PlayState.Playing;

                // Playback just started
                HasStartedPlaying = true;

                if (CurrentPlayOptions.StartPositionTicks > 0)
                {
                    SeekInternal(CurrentPlayOptions.StartPositionTicks);
                }
            }
            else if (tmtPlayState.Equals("pause", StringComparison.OrdinalIgnoreCase))
            {
                PlayState = PlayState.Paused;
            }

            // If playback has previously started...
            // First notify the Progress event handler
            // Then check if playback has stopped
            if (HasStartedPlaying)
            {
                TimeSpan currentPosition;

                //TimeSpan.TryParse(values["TotalTime"], out currentDuration);

                if (TimeSpan.TryParse(values["CurTime"], UsCulture, out currentPosition))
                {
                    _currentPositionTicks = currentPosition.Ticks;
                }

                _currentPlaylistIndex = 0;

                // Playback has stopped
                if (tmtPlayState.Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Playstate changed to stopped");

                    if (!HasStoppedPlaying)
                    {
                        HasStoppedPlaying = true;

                        DisposeFileSystemWatcher();

                        await ClosePlayer().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Disposes the file system watcher.
        /// </summary>
        private void DisposeFileSystemWatcher()
        {
            if (StatusFileWatcher != null)
            {
                StatusFileWatcher.EnableRaisingEvents = false;
                StatusFileWatcher.Changed -= StatusFileWatcher_Changed;
                StatusFileWatcher.Dispose();
                StatusFileWatcher = null;
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                DisposeFileSystemWatcher();
            }

            base.Dispose(dispose);
        }
    }
}
