using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.TranscodingDtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api
{
    /// <summary>
    /// The jellyfin api entry point.
    /// </summary>
    public class JellyfinApiEntryPoint : IServerEntryPoint
    {
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly ISessionManager _sessionManager;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly List<TranscodingJob> _activeTranscodingJobs;
        private readonly Dictionary<string, SemaphoreSlim> _transcodingLocks;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinApiEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="mediaSourceManager">The media source manager.</param>
        public JellyfinApiEntryPoint(
            ILogger<JellyfinApiEntryPoint> logger,
            ISessionManager sessionManager,
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IMediaSourceManager mediaSourceManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _serverConfigurationManager = config;
            _fileSystem = fileSystem;
            _mediaSourceManager = mediaSourceManager;

            _activeTranscodingJobs = new List<TranscodingJob>();
            _transcodingLocks = new Dictionary<string, SemaphoreSlim>();

            _sessionManager!.PlaybackProgress += OnPlaybackProgress;
            _sessionManager!.PlaybackStart += OnPlaybackProgress;

            Instance = this;
        }

        /// <summary>
        /// Gets the initialized instance of <see cref="JellyfinApiEntryPoint"/>.
        /// </summary>
        public static JellyfinApiEntryPoint? Instance { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (_disposed)
            {
                return;
            }

            if (dispose)
            {
                // TODO: dispose
            }

            List<TranscodingJob> jobs;
            lock (_activeTranscodingJobs)
            {
                jobs = _activeTranscodingJobs.ToList();
            }

            var jobCount = jobs.Count;

            IEnumerable<Task> GetKillJobs()
            {
                foreach (var job in jobs)
                {
                    yield return KillTranscodingJob(job, false, path => true);
                }
            }

            // Wait for all processes to be killed
            if (jobCount > 0)
            {
                Task.WaitAll(GetKillJobs().ToArray());
            }

            lock (_activeTranscodingJobs)
            {
                _activeTranscodingJobs.Clear();
            }

            lock (_transcodingLocks)
            {
                _transcodingLocks.Clear();
            }

            _sessionManager.PlaybackProgress -= OnPlaybackProgress;
            _sessionManager.PlaybackStart -= OnPlaybackProgress;

            _disposed = true;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            try
            {
                DeleteEncodedMediaCache();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting encoded media cache");
            }

            return Task.CompletedTask;
        }

        private void OnPlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.PlaySessionId))
            {
                PingTranscodingJob(e.PlaySessionId, e.IsPaused);
            }
        }

        /// <summary>
        /// Deletes the encoded media cache.
        /// </summary>
        private void DeleteEncodedMediaCache()
        {
            var path = _serverConfigurationManager.GetTranscodePath();
            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (var file in _fileSystem.GetFilePaths(path, true))
            {
                _fileSystem.DeleteFile(file);
            }
        }

        internal void PingTranscodingJob(string playSessionId, bool? isUserPaused)
        {
            if (string.IsNullOrEmpty(playSessionId))
            {
                throw new ArgumentNullException(nameof(playSessionId));
            }

            _logger.LogDebug("PingTranscodingJob PlaySessionId={0} isUsedPaused: {1}", playSessionId, isUserPaused);

            List<TranscodingJob> jobs;

            lock (_activeTranscodingJobs)
            {
                // This is really only needed for HLS.
                // Progressive streams can stop on their own reliably
                jobs = _activeTranscodingJobs.Where(j => string.Equals(playSessionId, j.PlaySessionId, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var job in jobs)
            {
                if (isUserPaused.HasValue)
                {
                    _logger.LogDebug("Setting job.IsUserPaused to {0}. jobId: {1}", isUserPaused, job.Id);
                    job.IsUserPaused = isUserPaused.Value;
                }

                PingTimer(job, true);
            }
        }

        private void PingTimer(TranscodingJob job, bool isProgressCheckIn)
        {
            if (job.HasExited)
            {
                job.StopKillTimer();
                return;
            }

            var timerDuration = 10000;

            if (job.Type != TranscodingJobType.Progressive)
            {
                timerDuration = 60000;
            }

            job.PingTimeout = timerDuration;
            job.LastPingDate = DateTime.UtcNow;

            // Don't start the timer for playback checkins with progressive streaming
            if (job.Type != TranscodingJobType.Progressive || !isProgressCheckIn)
            {
                job.StartKillTimer(OnTranscodeKillTimerStopped);
            }
            else
            {
                job.ChangeKillTimerIfStarted();
            }
        }

        /// <summary>
        /// Called when [transcode kill timer stopped].
        /// </summary>
        /// <param name="state">The state.</param>
        private async void OnTranscodeKillTimerStopped(object state)
        {
            var job = (TranscodingJob)state;

            if (!job.HasExited && job.Type != TranscodingJobType.Progressive)
            {
                var timeSinceLastPing = (DateTime.UtcNow - job.LastPingDate).TotalMilliseconds;

                if (timeSinceLastPing < job.PingTimeout)
                {
                    job.StartKillTimer(OnTranscodeKillTimerStopped, job.PingTimeout);
                    return;
                }
            }

            _logger.LogInformation("Transcoding kill timer stopped for JobId {0} PlaySessionId {1}. Killing transcoding", job.Id, job.PlaySessionId);

            await KillTranscodingJob(job, true, path => true).ConfigureAwait(false);
        }

        /// <summary>
        /// Kills the transcoding job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="closeLiveStream">if set to <c>true</c> [close live stream].</param>
        /// <param name="delete">The delete.</param>
        private async Task KillTranscodingJob(TranscodingJob job, bool closeLiveStream, Func<string, bool> delete)
        {
            job.DisposeKillTimer();

            _logger.LogDebug("KillTranscodingJob - JobId {0} PlaySessionId {1}. Killing transcoding", job.Id, job.PlaySessionId);

            lock (_activeTranscodingJobs)
            {
                _activeTranscodingJobs.Remove(job);

                if (!job.CancellationTokenSource!.IsCancellationRequested)
                {
                    job.CancellationTokenSource.Cancel();
                }
            }

            lock (_transcodingLocks)
            {
                _transcodingLocks.Remove(job.Path!);
            }

            lock (job)
            {
                job.TranscodingThrottler?.Stop().GetAwaiter().GetResult();

                var process = job.Process;

                var hasExited = job.HasExited;

                if (!hasExited)
                {
                    try
                    {
                        _logger.LogInformation("Stopping ffmpeg process with q command for {Path}", job.Path);

                        process?.StandardInput.WriteLine("q");

                        // Need to wait (an arbitrary amount of time) because killing is asynchronous
                        if (!process!.WaitForExit(5000))
                        {
                            _logger.LogInformation("Killing ffmpeg process for {Path}", job.Path);
                            process.Kill();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }

            if (delete(job.Path!))
            {
                await DeletePartialStreamFiles(job.Path!, job.Type, 0, 1500).ConfigureAwait(false);
            }

            if (closeLiveStream && !string.IsNullOrWhiteSpace(job.LiveStreamId))
            {
                try
                {
                    await _mediaSourceManager.CloseLiveStream(job.LiveStreamId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing live stream for {Path}", job.Path);
                }
            }
        }

        private async Task DeletePartialStreamFiles(string path, TranscodingJobType jobType, int retryCount, int delayMs)
        {
            if (retryCount >= 10)
            {
                return;
            }

            _logger.LogInformation("Deleting partial stream file(s) {Path}", path);

            await Task.Delay(delayMs).ConfigureAwait(false);

            try
            {
                if (jobType == TranscodingJobType.Progressive)
                {
                    DeleteProgressivePartialStreamFiles(path);
                }
                else
                {
                    DeleteHlsPartialStreamFiles(path);
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error deleting partial stream file(s) {Path}", path);

                await DeletePartialStreamFiles(path, jobType, retryCount + 1, 500).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting partial stream file(s) {Path}", path);
            }
        }

        /// <summary>
        /// Deletes the progressive partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        private void DeleteProgressivePartialStreamFiles(string outputFilePath)
        {
            if (File.Exists(outputFilePath))
            {
                _fileSystem.DeleteFile(outputFilePath);
            }
        }

        /// <summary>
        /// Deletes the HLS partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        private void DeleteHlsPartialStreamFiles(string outputFilePath)
        {
            var directory = Path.GetDirectoryName(outputFilePath);
            var name = Path.GetFileNameWithoutExtension(outputFilePath);

            var filesToDelete = _fileSystem.GetFilePaths(directory)
                .Where(f => f.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1);

            List<Exception>? exs = null;
            foreach (var file in filesToDelete)
            {
                try
                {
                    _logger.LogDebug("Deleting HLS file {0}", file);
                    _fileSystem.DeleteFile(file);
                }
                catch (IOException ex)
                {
                    (exs ??= new List<Exception>(4)).Add(ex);
                    _logger.LogError(ex, "Error deleting HLS file {Path}", file);
                }
            }

            if (exs != null)
            {
                throw new AggregateException("Error deleting HLS files", exs);
            }
        }
    }
}
