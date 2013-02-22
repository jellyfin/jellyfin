using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.UI.Configuration;
using MediaBrowser.UI.UserInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBrowser.UI.Playback.ExternalPlayer
{
    /// <summary>
    /// Class BaseExternalPlayer
    /// </summary>
    public abstract class BaseExternalPlayer : BaseMediaPlayer
    {
        protected BaseExternalPlayer(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this instance can mute.
        /// </summary>
        /// <value><c>true</c> if this instance can mute; otherwise, <c>false</c>.</value>
        public override bool CanMute
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can change volume.
        /// </summary>
        /// <value><c>true</c> if this instance can change volume; otherwise, <c>false</c>.</value>
        public override bool CanControlVolume
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can close automatically.
        /// </summary>
        /// <value><c>true</c> if this instance can close automatically; otherwise, <c>false</c>.</value>
        protected virtual bool CanCloseAutomatically
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [supports multi file playback].
        /// </summary>
        /// <value><c>true</c> if [supports multi file playback]; otherwise, <c>false</c>.</value>
        public override bool SupportsMultiFilePlayback
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the current process.
        /// </summary>
        /// <value>The current process.</value>
        protected Process CurrentProcess { get; private set; }

        /// <summary>
        /// Gets the process start info.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        /// <returns>ProcessStartInfo.</returns>
        protected virtual ProcessStartInfo GetProcessStartInfo(List<BaseItemDto> items, PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            return new ProcessStartInfo
            {
                FileName = playerConfiguration.Command,
                Arguments = GetCommandArguments(items, options, playerConfiguration)
            };
        }

        /// <summary>
        /// Gets the command arguments.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetCommandArguments(List<BaseItemDto> items, PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            var args = playerConfiguration.Args;

            if (string.IsNullOrEmpty(args))
            {
                return string.Empty;
            }

            return GetCommandArguments(items, args);
        }

        /// <summary>
        /// Gets the command arguments.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="formatString">The format string.</param>
        /// <returns>System.String.</returns>
        protected string GetCommandArguments(List<BaseItemDto> items, string formatString)
        {
            var paths = items.Select(i => "\"" + GetPathForCommandLine(i) + "\"");

            return string.Format(formatString, string.Join(" ", paths.ToArray()));
        }

        /// <summary>
        /// Gets the path for command line.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetPathForCommandLine(BaseItemDto item)
        {
            return item.Path;
        }

        /// <summary>
        /// Gets a value indicating whether this instance can queue.
        /// </summary>
        /// <value><c>true</c> if this instance can queue; otherwise, <c>false</c>.</value>
        public override bool CanQueue
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can pause.
        /// </summary>
        /// <value><c>true</c> if this instance can pause; otherwise, <c>false</c>.</value>
        public override bool CanPause
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can seek.
        /// </summary>
        /// <value><c>true</c> if this instance can seek; otherwise, <c>false</c>.</value>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Plays the internal.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        protected override void PlayInternal(List<BaseItemDto> items, PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            CurrentProcess = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = GetProcessStartInfo(items, options, playerConfiguration)
            };

            Logger.Info("{0} {1}", CurrentProcess.StartInfo.FileName, CurrentProcess.StartInfo.Arguments);

            CurrentProcess.Start();

            OnExternalPlayerLaunched();

            if (!CanCloseAutomatically)
            {
                KeyboardListener.KeyDown += KeyboardListener_KeyDown;
            }

            CurrentProcess.Exited += CurrentProcess_Exited;
        }

        /// <summary>
        /// Handles the KeyDown event of the KeyboardListener control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
        void KeyboardListener_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.MediaStop)
            {
                var playstate = PlayState;

                if (playstate == PlayState.Paused || playstate == PlayState.Playing)
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// Handles the Exited event of the CurrentProcess control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void CurrentProcess_Exited(object sender, EventArgs e)
        {
            var process = (Process)sender;

            process.Dispose();

            OnPlayerStopped(CurrentPlaylistIndex, CurrentPositionTicks);
        }

        /// <summary>
        /// Stops the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task StopInternal()
        {
            return Task.Run(() => CurrentProcess.Kill());
        }

        /// <summary>
        /// Called when [player stopped internal].
        /// </summary>
        protected override void OnPlayerStoppedInternal()
        {
            KeyboardListener.KeyDown -= KeyboardListener_KeyDown;

            base.OnPlayerStoppedInternal();
        }

        /// <summary>
        /// Called when [external player launched].
        /// </summary>
        protected virtual void OnExternalPlayerLaunched()
        {
            
        }
    }
}
