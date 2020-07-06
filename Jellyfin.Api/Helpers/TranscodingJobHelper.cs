using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.PlaybackDtos;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Transcoding job helpers.
    /// </summary>
    public class TranscodingJobHelper
    {
        /// <summary>
        /// The active transcoding jobs.
        /// </summary>
        private static readonly List<TranscodingJobDto> _activeTranscodingJobs = new List<TranscodingJobDto>();

        /// <summary>
        /// The transcoding locks.
        /// </summary>
        private static readonly Dictionary<string, SemaphoreSlim> _transcodingLocks = new Dictionary<string, SemaphoreSlim>();

        private readonly ILogger<TranscodingJobHelper> _logger;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodingJobHelper"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TranscodingJobHelpers}"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        public TranscodingJobHelper(
            ILogger<TranscodingJobHelper> logger,
            IMediaSourceManager mediaSourceManager,
            IFileSystem fileSystem)
        {
            _logger = logger;
            _mediaSourceManager = mediaSourceManager;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Get transcoding job.
        /// </summary>
        /// <param name="playSessionId">Playback session id.</param>
        /// <returns>The transcoding job.</returns>
        public TranscodingJobDto GetTranscodingJob(string playSessionId)
        {
            lock (_activeTranscodingJobs)
            {
                return _activeTranscodingJobs.FirstOrDefault(j => string.Equals(j.PlaySessionId, playSessionId, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Ping transcoding job.
        /// </summary>
        /// <param name="playSessionId">Play session id.</param>
        /// <param name="isUserPaused">Is user paused.</param>
        /// <exception cref="ArgumentNullException">Play session id is null.</exception>
        public void PingTranscodingJob(string playSessionId, bool? isUserPaused)
        {
            if (string.IsNullOrEmpty(playSessionId))
            {
                throw new ArgumentNullException(nameof(playSessionId));
            }

            _logger.LogDebug("PingTranscodingJob PlaySessionId={0} isUsedPaused: {1}", playSessionId, isUserPaused);

            List<TranscodingJobDto> jobs;

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

        private void PingTimer(TranscodingJobDto job, bool isProgressCheckIn)
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
            var job = (TranscodingJobDto)state;

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
        /// Kills the single transcoding job.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="playSessionId">The play session identifier.</param>
        /// <param name="deleteFiles">The delete files.</param>
        /// <returns>Task.</returns>
        public Task KillTranscodingJobs(string deviceId, string playSessionId, Func<string, bool> deleteFiles)
        {
            return KillTranscodingJobs(
                j => string.IsNullOrWhiteSpace(playSessionId)
                    ? string.Equals(deviceId, j.DeviceId, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(playSessionId, j.PlaySessionId, StringComparison.OrdinalIgnoreCase), deleteFiles);
        }

        /// <summary>
        /// Kills the transcoding jobs.
        /// </summary>
        /// <param name="killJob">The kill job.</param>
        /// <param name="deleteFiles">The delete files.</param>
        /// <returns>Task.</returns>
        private Task KillTranscodingJobs(Func<TranscodingJobDto, bool> killJob, Func<string, bool> deleteFiles)
        {
            var jobs = new List<TranscodingJobDto>();

            lock (_activeTranscodingJobs)
            {
                // This is really only needed for HLS.
                // Progressive streams can stop on their own reliably
                jobs.AddRange(_activeTranscodingJobs.Where(killJob));
            }

            if (jobs.Count == 0)
            {
                return Task.CompletedTask;
            }

            IEnumerable<Task> GetKillJobs()
            {
                foreach (var job in jobs)
                {
                    yield return KillTranscodingJob(job, false, deleteFiles);
                }
            }

            return Task.WhenAll(GetKillJobs());
        }

        /// <summary>
        /// Kills the transcoding job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="closeLiveStream">if set to <c>true</c> [close live stream].</param>
        /// <param name="delete">The delete.</param>
        private async Task KillTranscodingJob(TranscodingJobDto job, bool closeLiveStream, Func<string, bool> delete)
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

            lock (job.ProcessLock!)
            {
                job.TranscodingThrottler?.Stop().GetAwaiter().GetResult();

                var process = job.Process;

                var hasExited = job.HasExited;

                if (!hasExited)
                {
                    try
                    {
                        _logger.LogInformation("Stopping ffmpeg process with q command for {Path}", job.Path);

                        process!.StandardInput.WriteLine("q");

                        // Need to wait because killing is asynchronous
                        if (!process.WaitForExit(5000))
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
