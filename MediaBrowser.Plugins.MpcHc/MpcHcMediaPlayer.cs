using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.UI.Configuration;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Playback;
using MediaBrowser.UI.Playback.ExternalPlayer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.MpcHc
{
    /// <summary>
    /// Class GenericExternalPlayer
    /// </summary>
    [Export(typeof(BaseMediaPlayer))]
    public class MpcHcMediaPlayer : BaseExternalPlayer
    {
        /// <summary>
        /// The state sync lock
        /// </summary>
        private object stateSyncLock = new object();

        /// <summary>
        /// The MPC HTTP interface resource pool
        /// </summary>
        private SemaphoreSlim MpcHttpInterfaceResourcePool = new SemaphoreSlim(1, 1);

        [ImportingConstructor]
        public MpcHcMediaPlayer([Import("logger")] ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets or sets the HTTP interface cancellation token.
        /// </summary>
        /// <value>The HTTP interface cancellation token.</value>
        private CancellationTokenSource HttpInterfaceCancellationTokenSource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has started playing.
        /// </summary>
        /// <value><c>true</c> if this instance has started playing; otherwise, <c>false</c>.</value>
        private bool HasStartedPlaying { get; set; }

        /// <summary>
        /// Gets or sets the status update timer.
        /// </summary>
        /// <value>The status update timer.</value>
        private Timer StatusUpdateTimer { get; set; }

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
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "MpcHc"; }
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
        /// Determines whether this instance can play the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if this instance can play the specified item; otherwise, <c>false</c>.</returns>
        public override bool CanPlay(BaseItemDto item)
        {
            return item.IsVideo || item.IsAudio;
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
            var formatString = "{0} /play /fullscreen /close";

            var firstItem = items[0];

            var startTicks = Math.Max(options.StartPositionTicks, 0);

            if (startTicks > 0 && firstItem.IsVideo && firstItem.VideoType.HasValue && firstItem.VideoType.Value == VideoType.Dvd)
            {
                formatString += " /dvdpos 1#" + TimeSpan.FromTicks(startTicks).ToString("hh\\:mm\\:ss");
            }
            else
            {
                formatString += " /start " + TimeSpan.FromTicks(startTicks).TotalMilliseconds;
            }


            return GetCommandArguments(items, formatString);
        }

        /// <summary>
        /// Gets the path for command line.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected override string GetPathForCommandLine(BaseItemDto item)
        {
            var path = base.GetPathForCommandLine(item);

            if (item.IsVideo && item.VideoType.HasValue)
            {
                if (item.VideoType.Value == VideoType.Dvd)
                {
                    // Point directly to the video_ts path
                    // Otherwise mpc will play any other media files that might exist in the dvd top folder (e.g. video backdrops).
                    var videoTsPath = Path.Combine(path, "video_ts");

                    if (Directory.Exists(videoTsPath))
                    {
                        path = videoTsPath;
                    }
                }
                if (item.VideoType.Value == VideoType.BluRay)
                {
                    // Point directly to the bdmv path
                    var bdmvPath = Path.Combine(path, "bdmv");

                    if (Directory.Exists(bdmvPath))
                    {
                        path = bdmvPath;
                    }
                }
            }

            return FormatPath(path);
        }

        /// <summary>
        /// Formats the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        private string FormatPath(string path)
        {
            if (path.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                path = path.TrimEnd('\\');
            }

            return path;
        }

        /// <summary>
        /// Called when [external player launched].
        /// </summary>
        protected override void OnExternalPlayerLaunched()
        {
            base.OnExternalPlayerLaunched();

            ReloadStatusUpdateTimer();
        }

        /// <summary>
        /// Reloads the status update timer.
        /// </summary>
        private void ReloadStatusUpdateTimer()
        {
            DisposeStatusTimer();

            HttpInterfaceCancellationTokenSource = new CancellationTokenSource();

            StatusUpdateTimer = new Timer(OnStatusUpdateTimerStopped, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Called when [status update timer stopped].
        /// </summary>
        /// <param name="state">The state.</param>
        private async void OnStatusUpdateTimerStopped(object state)
        {
            try
            {
                var token = HttpInterfaceCancellationTokenSource.Token;

                using (var stream = await UIKernel.Instance.HttpManager.Get(StatusUrl, MpcHttpInterfaceResourcePool, token).ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();

                    using (var reader = new StreamReader(stream))
                    {
                        token.ThrowIfCancellationRequested();

                        var result = await reader.ReadToEndAsync().ConfigureAwait(false);

                        token.ThrowIfCancellationRequested();

                        ProcessStatusResult(result);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.ErrorException("Error connecting to MpcHc status interface", ex);
            }
            catch (OperationCanceledException)
            {
                // Manually cancelled by us
                Logger.Info("Status request cancelled");
            }
        }

        /// <summary>
        /// Processes the status result.
        /// </summary>
        /// <param name="result">The result.</param>
        private async void ProcessStatusResult(string result)
        {
            // Sample result
            // OnStatus('test.avi', 'Playing', 5292, '00:00:05', 1203090, '00:20:03', 0, 100, 'C:\test.avi')
            // 5292 = position in ms
            // 00:00:05 = position
            // 1203090 = duration in ms
            // 00:20:03 = duration

            var quoteChar = result.IndexOf(", \"", StringComparison.OrdinalIgnoreCase) == -1 ? '\'' : '\"';

            // Strip off the leading "OnStatus(" and the trailing ")"
            result = result.Substring(result.IndexOf(quoteChar));
            result = result.Substring(0, result.LastIndexOf(quoteChar));

            // Strip off the filename at the beginning
            result = result.Substring(result.IndexOf(string.Format("{0}, {0}", quoteChar), StringComparison.OrdinalIgnoreCase) + 3);

            // Find the last index of ", '" so that we can extract and then strip off the file path at the end.
            var lastIndexOfSeparator = result.LastIndexOf(", " + quoteChar, StringComparison.OrdinalIgnoreCase);

            // Get the current playing file path
            var currentPlayingFile = result.Substring(lastIndexOfSeparator + 2).Trim(quoteChar);

            // Strip off the current playing file path
            result = result.Substring(0, lastIndexOfSeparator);

            var values = result.Split(',').Select(v => v.Trim().Trim(quoteChar)).ToList();

            var currentPositionTicks = TimeSpan.FromMilliseconds(double.Parse(values[1])).Ticks;
            //var currentDurationTicks = TimeSpan.FromMilliseconds(double.Parse(values[3])).Ticks;

            var playstate = values[0];

            var playlistIndex = GetPlaylistIndex(currentPlayingFile);

            if (playstate.Equals("stopped", StringComparison.OrdinalIgnoreCase))
            {
                if (HasStartedPlaying)
                {
                    await ClosePlayer().ConfigureAwait(false);
                }
            }
            else
            {
                lock (stateSyncLock)
                {
                    if (_currentPlaylistIndex != playlistIndex)
                    {
                        OnMediaChanged(_currentPlaylistIndex, _currentPositionTicks, playlistIndex);
                    }

                    _currentPositionTicks = currentPositionTicks;
                    _currentPlaylistIndex = playlistIndex;
                }

                if (playstate.Equals("playing", StringComparison.OrdinalIgnoreCase))
                {
                    HasStartedPlaying = true;
                    PlayState = PlayState.Playing;
                }
                else if (playstate.Equals("paused", StringComparison.OrdinalIgnoreCase))
                {
                    HasStartedPlaying = true;
                    PlayState = PlayState.Paused;
                }
            }
        }

        /// <summary>
        /// Gets the index of the playlist.
        /// </summary>
        /// <param name="nowPlayingPath">The now playing path.</param>
        /// <returns>System.Int32.</returns>
        private int GetPlaylistIndex(string nowPlayingPath)
        {
            for (var i = 0; i < Playlist.Count; i++)
            {
                var item = Playlist[i];

                var pathArg = GetPathForCommandLine(item);

                if (pathArg.Equals(nowPlayingPath, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }

                if (item.VideoType.HasValue)
                {
                    if (item.VideoType.Value == VideoType.BluRay || item.VideoType.Value == VideoType.Dvd || item.VideoType.Value == VideoType.HdDvd)
                    {
                        if (nowPlayingPath.StartsWith(pathArg, StringComparison.OrdinalIgnoreCase))
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Called when [player stopped internal].
        /// </summary>
        protected override void OnPlayerStoppedInternal()
        {
            HttpInterfaceCancellationTokenSource.Cancel();

            DisposeStatusTimer();
            _currentPositionTicks = null;
            _currentPlaylistIndex = 0;
            HasStartedPlaying = false;
            HttpInterfaceCancellationTokenSource = null;

            base.OnPlayerStoppedInternal();
        }

        /// <summary>
        /// Disposes the status timer.
        /// </summary>
        private void DisposeStatusTimer()
        {
            if (StatusUpdateTimer != null)
            {
                StatusUpdateTimer.Dispose();
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
                DisposeStatusTimer();

                MpcHttpInterfaceResourcePool.Dispose();
            }

            base.Dispose(dispose);
        }

        /// <summary>
        /// Seeks the internal.
        /// </summary>
        /// <param name="positionTicks">The position ticks.</param>
        /// <returns>Task.</returns>
        protected override Task SeekInternal(long positionTicks)
        {
            var additionalParams = new Dictionary<string, string>();

            var time = TimeSpan.FromTicks(positionTicks);

            var timeString = time.Hours + ":" + time.Minutes + ":" + time.Seconds;

            additionalParams.Add("position", timeString);

            return SendCommandToPlayer("-1", additionalParams);
        }

        /// <summary>
        /// Pauses the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task PauseInternal()
        {
            return SendCommandToPlayer("888", new Dictionary<string, string>());
        }

        /// <summary>
        /// Uns the pause internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task UnPauseInternal()
        {
            return SendCommandToPlayer("887", new Dictionary<string, string>());
        }

        /// <summary>
        /// Stops the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task StopInternal()
        {
            return SendCommandToPlayer("890", new Dictionary<string, string>());
        }

        /// <summary>
        /// Closes the player.
        /// </summary>
        /// <returns>Task.</returns>
        protected Task ClosePlayer()
        {
            return SendCommandToPlayer("816", new Dictionary<string, string>());
        }

        /// <summary>
        /// Sends a command to MPC using the HTTP interface
        /// http://www.autohotkey.net/~specter333/MPC/HTTP%20Commands.txt
        /// </summary>
        /// <param name="commandNumber">The command number.</param>
        /// <param name="additionalParams">The additional params.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">commandNumber</exception>
        private async Task SendCommandToPlayer(string commandNumber, Dictionary<string, string> additionalParams)
        {
            if (string.IsNullOrEmpty(commandNumber))
            {
                throw new ArgumentNullException("commandNumber");
            }

            if (additionalParams == null)
            {
                throw new ArgumentNullException("additionalParams");
            }

            var url = CommandUrl + "?wm_command=" + commandNumber;

            url = additionalParams.Keys.Aggregate(url, (current, name) => current + ("&" + name + "=" + additionalParams[name]));

            Logger.Info("Sending command to MPC: " + url);

            try
            {
                using (var stream = await UIKernel.Instance.HttpManager.Get(url, MpcHttpInterfaceResourcePool, HttpInterfaceCancellationTokenSource.Token).ConfigureAwait(false))
                {
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.ErrorException("Error connecting to MpcHc command interface", ex);
            }
            catch (OperationCanceledException)
            {
                // Manually cancelled by us
                Logger.Info("Command request cancelled");
            }
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
        /// Gets the server name that the http interface will be running on
        /// </summary>
        /// <value>The HTTP server.</value>
        private string HttpServer
        {
            get
            {
                return "localhost";
            }
        }

        /// <summary>
        /// Gets the port that the web interface will be running on
        /// </summary>
        /// <value>The HTTP port.</value>
        private string HttpPort
        {
            get
            {
                return "13579";
            }
        }

        /// <summary>
        /// Gets the url of that will be called to for status
        /// </summary>
        /// <value>The status URL.</value>
        private string StatusUrl
        {
            get
            {
                return "http://" + HttpServer + ":" + HttpPort + "/status.html";
            }
        }

        /// <summary>
        /// Gets the url of that will be called to send commands
        /// </summary>
        /// <value>The command URL.</value>
        private string CommandUrl
        {
            get
            {
                return "http://" + HttpServer + ":" + HttpPort + "/command.html";
            }
        }
    }
}
