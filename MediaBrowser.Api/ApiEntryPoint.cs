using MediaBrowser.Api.Playback;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ApiEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// The instance
        /// </summary>
        public static ApiEntryPoint Instance;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// The application paths
        /// </summary>
        private readonly IServerApplicationPaths _appPaths;

        private readonly ISessionManager _sessionManager;

        public readonly SemaphoreSlim TranscodingStartLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="sessionManager">The session manager.</param>
        public ApiEntryPoint(ILogger logger, IServerApplicationPaths appPaths, ISessionManager sessionManager)
        {
            Logger = logger;
            _appPaths = appPaths;
            _sessionManager = sessionManager;

            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            try
            {
                DeleteEncodedMediaCache();
            }
            catch (DirectoryNotFoundException)
            {
                // Don't clutter the log
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error deleting encoded media cache", ex);
            }
        }

        /// <summary>
        /// Deletes the encoded media cache.
        /// </summary>
        private void DeleteEncodedMediaCache()
        {
            foreach (var file in Directory.EnumerateFiles(_appPaths.TranscodingTempPath, "*", SearchOption.AllDirectories)
                .ToList())
            {
                File.Delete(file);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
            var jobCount = _activeTranscodingJobs.Count;

            Parallel.ForEach(_activeTranscodingJobs.ToList(), j => KillTranscodingJob(j, path => true));

            // Try to allow for some time to kill the ffmpeg processes and delete the partial stream files
            if (jobCount > 0)
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// The active transcoding jobs
        /// </summary>
        private readonly List<TranscodingJob> _activeTranscodingJobs = new List<TranscodingJob>();

        /// <summary>
        /// Called when [transcode beginning].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="transcodingJobId">The transcoding job identifier.</param>
        /// <param name="type">The type.</param>
        /// <param name="process">The process.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="state">The state.</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <returns>TranscodingJob.</returns>
        public TranscodingJob OnTranscodeBeginning(string path,
            string transcodingJobId,
            TranscodingJobType type,
            Process process,
            string deviceId,
            StreamState state,
            CancellationTokenSource cancellationTokenSource)
        {
            lock (_activeTranscodingJobs)
            {
                var job = new TranscodingJob
                {
                    Type = type,
                    Path = path,
                    Process = process,
                    ActiveRequestCount = 1,
                    DeviceId = deviceId,
                    CancellationTokenSource = cancellationTokenSource,
                    Id = transcodingJobId
                };

                _activeTranscodingJobs.Add(job);

                ReportTranscodingProgress(job, state, null, null, null, null);

                return job;
            }
        }

        public void ReportTranscodingProgress(TranscodingJob job, StreamState state, TimeSpan? transcodingPosition, float? framerate, double? percentComplete, long? bytesTranscoded)
        {
            var ticks = transcodingPosition.HasValue ? transcodingPosition.Value.Ticks : (long?)null;

            if (job != null)
            {
                job.Framerate = framerate;
                job.CompletionPercentage = percentComplete;
                job.TranscodingPositionTicks = ticks;
                job.BytesTranscoded = bytesTranscoded;
            }

            var deviceId = state.Request.DeviceId;

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                var audioCodec = state.Request.AudioCodec;
                var videoCodec = state.VideoRequest == null ? null : state.VideoRequest.VideoCodec;

                if (string.Equals(state.OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrEmpty(audioCodec))
                {
                    audioCodec = state.OutputAudioCodec;
                }
                if (string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrEmpty(videoCodec))
                {
                    videoCodec = state.OutputVideoCodec;
                }

                _sessionManager.ReportTranscodingInfo(deviceId, new TranscodingInfo
                {
                    Bitrate = state.TotalOutputBitrate,
                    AudioCodec = audioCodec,
                    VideoCodec = videoCodec,
                    Container = state.OutputContainer,
                    Framerate = framerate,
                    CompletionPercentage = percentComplete,
                    Width = state.OutputWidth,
                    Height = state.OutputHeight,
                    AudioChannels = state.OutputAudioChannels
                });
            }
        }

        /// <summary>
        /// <summary>
        /// The progressive
        /// </summary>
        /// Called when [transcode failed to start].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        /// <param name="state">The state.</param>
        public void OnTranscodeFailedToStart(string path, TranscodingJobType type, StreamState state)
        {
            lock (_activeTranscodingJobs)
            {
                var job = _activeTranscodingJobs.First(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                _activeTranscodingJobs.Remove(job);
            }

            if (!string.IsNullOrWhiteSpace(state.Request.DeviceId))
            {
                _sessionManager.ClearTranscodingInfo(state.Request.DeviceId);
            }
        }

        /// <summary>
        /// Determines whether [has active transcoding job] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [has active transcoding job] [the specified path]; otherwise, <c>false</c>.</returns>
        public bool HasActiveTranscodingJob(string path, TranscodingJobType type)
        {
            return GetTranscodingJob(path, type) != null;
        }

        public TranscodingJob GetTranscodingJob(string path, TranscodingJobType type)
        {
            lock (_activeTranscodingJobs)
            {
                return _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
        }

        public TranscodingJob GetTranscodingJob(string id)
        {
            lock (_activeTranscodingJobs)
            {
                return _activeTranscodingJobs.FirstOrDefault(j => j.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Called when [transcode begin request].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        public TranscodingJob OnTranscodeBeginRequest(string path, TranscodingJobType type)
        {
            lock (_activeTranscodingJobs)
            {
                var job = _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (job == null)
                {
                    return null;
                }

                job.ActiveRequestCount++;

                job.DisposeKillTimer();

                return job;
            }
        }

        public void OnTranscodeEndRequest(TranscodingJob job)
        {
            job.ActiveRequestCount--;

            if (job.ActiveRequestCount == 0)
            {
                if (job.Type == TranscodingJobType.Progressive)
                {
                    const int timerDuration = 1000;

                    if (job.KillTimer == null)
                    {
                        job.KillTimer = new Timer(OnTranscodeKillTimerStopped, job, timerDuration, Timeout.Infinite);
                    }
                    else
                    {
                        job.KillTimer.Change(timerDuration, Timeout.Infinite);
                    }
                }
            }
        }

        /// <summary>
        /// Called when [transcode kill timer stopped].
        /// </summary>
        /// <param name="state">The state.</param>
        private void OnTranscodeKillTimerStopped(object state)
        {
            var job = (TranscodingJob)state;

            KillTranscodingJob(job, path => true);
        }

        /// <summary>
        /// Kills the single transcoding job.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deleteFiles">The delete files.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">deviceId</exception>
        internal void KillTranscodingJobs(string deviceId, Func<string, bool> deleteFiles)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException("deviceId");
            }

            KillTranscodingJobs(j => string.Equals(deviceId, j.DeviceId, StringComparison.OrdinalIgnoreCase), deleteFiles);
        }

        /// <summary>
        /// Kills the transcoding jobs.
        /// </summary>
        /// <param name="killJob">The kill job.</param>
        /// <param name="deleteFiles">The delete files.</param>
        /// <returns>Task.</returns>
        internal void KillTranscodingJobs(Func<TranscodingJob, bool> killJob, Func<string, bool> deleteFiles)
        {
            var jobs = new List<TranscodingJob>();

            lock (_activeTranscodingJobs)
            {
                // This is really only needed for HLS. 
                // Progressive streams can stop on their own reliably
                jobs.AddRange(_activeTranscodingJobs.Where(killJob));
            }

            if (jobs.Count == 0)
            {
                return;
            }

            foreach (var job in jobs)
            {
                KillTranscodingJob(job, deleteFiles);
            }
        }

        /// <summary>
        /// Kills the transcoding job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="delete">The delete.</param>
        private void KillTranscodingJob(TranscodingJob job, Func<string, bool> delete)
        {
            lock (_activeTranscodingJobs)
            {
                _activeTranscodingJobs.Remove(job);

                if (!job.CancellationTokenSource.IsCancellationRequested)
                {
                    job.CancellationTokenSource.Cancel();
                }

                job.DisposeKillTimer();
            }

            lock (job.ProcessLock)
            {
                var process = job.Process;

                var hasExited = true;

                try
                {
                    hasExited = process.HasExited;
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error determining if ffmpeg process has exited for {0}", ex, job.Path);
                }

                if (!hasExited)
                {
                    try
                    {
                        Logger.Info("Killing ffmpeg process for {0}", job.Path);

                        //process.Kill();
                        process.StandardInput.WriteLine("q");

                        // Need to wait because killing is asynchronous
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error killing transcoding job for {0}", ex, job.Path);
                    }
                }
            }

            if (delete(job.Path))
            {
                DeletePartialStreamFiles(job.Path, job.Type, 0, 1500);
            }
        }

        private async void DeletePartialStreamFiles(string path, TranscodingJobType jobType, int retryCount, int delayMs)
        {
            if (retryCount >= 10)
            {
                return;
            }

            Logger.Info("Deleting partial stream file(s) {0}", path);

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
            catch (DirectoryNotFoundException)
            {

            }
            catch (FileNotFoundException)
            {

            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, path);

                DeletePartialStreamFiles(path, jobType, retryCount + 1, 500);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, path);
            }
        }

        /// <summary>
        /// Deletes the progressive partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        private void DeleteProgressivePartialStreamFiles(string outputFilePath)
        {
            File.Delete(outputFilePath);
        }

        /// <summary>
        /// Deletes the HLS partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        private void DeleteHlsPartialStreamFiles(string outputFilePath)
        {
            var directory = Path.GetDirectoryName(outputFilePath);
            var name = Path.GetFileNameWithoutExtension(outputFilePath);

            var filesToDelete = Directory.EnumerateFiles(directory)
                .Where(f => f.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                .ToList();

            Exception e = null;

            foreach (var file in filesToDelete)
            {
                try
                {
                    Logger.Info("Deleting HLS file {0}", file);
                    File.Delete(file);
                }
                catch (DirectoryNotFoundException)
                {

                }
                catch (FileNotFoundException)
                {

                }
                catch (IOException ex)
                {
                    e = ex;
                    Logger.ErrorException("Error deleting HLS file {0}", ex, file);
                }
            }

            if (e != null)
            {
                throw e;
            }
        }
    }

    /// <summary>
    /// Class TranscodingJob
    /// </summary>
    public class TranscodingJob
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public TranscodingJobType Type { get; set; }
        /// <summary>
        /// Gets or sets the process.
        /// </summary>
        /// <value>The process.</value>
        public Process Process { get; set; }
        /// <summary>
        /// Gets or sets the active request count.
        /// </summary>
        /// <value>The active request count.</value>
        public int ActiveRequestCount { get; set; }
        /// <summary>
        /// Gets or sets the kill timer.
        /// </summary>
        /// <value>The kill timer.</value>
        public Timer KillTimer { get; set; }

        public string DeviceId { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public object ProcessLock = new object();

        public bool HasExited { get; set; }

        public string Id { get; set; }

        public float? Framerate { get; set; }
        public double? CompletionPercentage { get; set; }

        public long? BytesDownloaded { get; set; }
        public long? BytesTranscoded { get; set; }

        public long? TranscodingPositionTicks { get; set; }
        public long? DownloadPositionTicks { get; set; }

        public void DisposeKillTimer()
        {
            if (KillTimer != null)
            {
                KillTimer.Dispose();
                KillTimer = null;
            }
        }
    }

    /// <summary>
    /// Enum TranscodingJobType
    /// </summary>
    public enum TranscodingJobType
    {
        /// <summary>
        /// The progressive
        /// </summary>
        Progressive,
        /// <summary>
        /// The HLS
        /// </summary>
        Hls
    }
}
