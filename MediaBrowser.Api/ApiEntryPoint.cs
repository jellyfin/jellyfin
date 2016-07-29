using MediaBrowser.Api.Playback;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

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
        private readonly IServerConfigurationManager _config;

        private readonly ISessionManager _sessionManager;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaSourceManager _mediaSourceManager;

        public readonly SemaphoreSlim TranscodingStartLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="mediaSourceManager">The media source manager.</param>
        public ApiEntryPoint(ILogger logger, ISessionManager sessionManager, IServerConfigurationManager config, IFileSystem fileSystem, IMediaSourceManager mediaSourceManager)
        {
            Logger = logger;
            _sessionManager = sessionManager;
            _config = config;
            _fileSystem = fileSystem;
            _mediaSourceManager = mediaSourceManager;

            Instance = this;
            _sessionManager.PlaybackProgress += _sessionManager_PlaybackProgress;
        }

        void _sessionManager_PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.PlaySessionId))
            {
                PingTranscodingJob(e.PlaySessionId, e.IsPaused);
            }
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

        public EncodingOptions GetEncodingOptions()
        {
            return _config.GetConfiguration<EncodingOptions>("encoding");
        }

        /// <summary>
        /// Deletes the encoded media cache.
        /// </summary>
        private void DeleteEncodedMediaCache()
        {
            var path = _config.ApplicationPaths.TranscodingTempPath;

            foreach (var file in _fileSystem.GetFilePaths(path, true)
                .ToList())
            {
                _fileSystem.DeleteFile(file);
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
            var list = _activeTranscodingJobs.ToList();
            var jobCount = list.Count;

            Parallel.ForEach(list, j => KillTranscodingJob(j, false, path => true));

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
        /// <param name="playSessionId">The play session identifier.</param>
        /// <param name="liveStreamId">The live stream identifier.</param>
        /// <param name="transcodingJobId">The transcoding job identifier.</param>
        /// <param name="type">The type.</param>
        /// <param name="process">The process.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="state">The state.</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <returns>TranscodingJob.</returns>
        public TranscodingJob OnTranscodeBeginning(string path,
            string playSessionId,
            string liveStreamId,
            string transcodingJobId,
            TranscodingJobType type,
            Process process,
            string deviceId,
            StreamState state,
            CancellationTokenSource cancellationTokenSource)
        {
            lock (_activeTranscodingJobs)
            {
                var job = new TranscodingJob(Logger)
                {
                    Type = type,
                    Path = path,
                    Process = process,
                    ActiveRequestCount = 1,
                    DeviceId = deviceId,
                    CancellationTokenSource = cancellationTokenSource,
                    Id = transcodingJobId,
                    PlaySessionId = playSessionId,
                    LiveStreamId = liveStreamId
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
                var audioCodec = state.ActualOutputAudioCodec;
                var videoCodec = state.ActualOutputVideoCodec;

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
                    AudioChannels = state.OutputAudioChannels,
                    IsAudioDirect = string.Equals(state.OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase),
                    IsVideoDirect = string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase)
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
                var job = _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));

                if (job != null)
                {
                    _activeTranscodingJobs.Remove(job);
                }
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
                return _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));
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
                var job = _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));

                if (job == null)
                {
                    return null;
                }

                OnTranscodeBeginRequest(job);

                return job;
            }
        }

        public void OnTranscodeBeginRequest(TranscodingJob job)
        {
            job.ActiveRequestCount++;

            if (string.IsNullOrWhiteSpace(job.PlaySessionId) || job.Type == TranscodingJobType.Progressive)
            {
                job.StopKillTimer();
            }
        }

        public void OnTranscodeEndRequest(TranscodingJob job)
        {
            job.ActiveRequestCount--;
            //Logger.Debug("OnTranscodeEndRequest job.ActiveRequestCount={0}", job.ActiveRequestCount);
            if (job.ActiveRequestCount <= 0)
            {
                PingTimer(job, false);
            }
        }
        internal void PingTranscodingJob(string playSessionId, bool? isUserPaused)
        {
            if (string.IsNullOrEmpty(playSessionId))
            {
                throw new ArgumentNullException("playSessionId");
            }

            //Logger.Debug("PingTranscodingJob PlaySessionId={0} isUsedPaused: {1}", playSessionId, isUserPaused);

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
                    //Logger.Debug("Setting job.IsUserPaused to {0}. jobId: {1}", isUserPaused, job.Id);
                    job.IsUserPaused = isUserPaused.Value;
                }
                PingTimer(job, true);
            }
        }

        private async void PingTimer(TranscodingJob job, bool isProgressCheckIn)
        {
            if (job.HasExited)
            {
                job.StopKillTimer();
                return;
            }

            var timerDuration = 1000;

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

            if (!string.IsNullOrWhiteSpace(job.LiveStreamId))
            {
                try
                {
                    await _mediaSourceManager.PingLiveStream(job.LiveStreamId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error closing live stream", ex);
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

            if (!job.HasExited && job.Type != TranscodingJobType.Progressive)
            {
                var timeSinceLastPing = (DateTime.UtcNow - job.LastPingDate).TotalMilliseconds;

                if (timeSinceLastPing < job.PingTimeout)
                {
                    job.StartKillTimer(OnTranscodeKillTimerStopped, job.PingTimeout);
                    return;
                }
            }

            Logger.Debug("Transcoding kill timer stopped for JobId {0} PlaySessionId {1}. Killing transcoding", job.Id, job.PlaySessionId);

            KillTranscodingJob(job, true, path => true);
        }

        /// <summary>
        /// Kills the single transcoding job.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="playSessionId">The play session identifier.</param>
        /// <param name="deleteFiles">The delete files.</param>
        /// <returns>Task.</returns>
        internal void KillTranscodingJobs(string deviceId, string playSessionId, Func<string, bool> deleteFiles)
        {
            KillTranscodingJobs(j =>
            {
                if (!string.IsNullOrWhiteSpace(playSessionId))
                {
                    return string.Equals(playSessionId, j.PlaySessionId, StringComparison.OrdinalIgnoreCase);
                }

                return string.Equals(deviceId, j.DeviceId, StringComparison.OrdinalIgnoreCase);

            }, deleteFiles);
        }

        /// <summary>
        /// Kills the transcoding jobs.
        /// </summary>
        /// <param name="killJob">The kill job.</param>
        /// <param name="deleteFiles">The delete files.</param>
        /// <returns>Task.</returns>
        private void KillTranscodingJobs(Func<TranscodingJob, bool> killJob, Func<string, bool> deleteFiles)
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
                KillTranscodingJob(job, false, deleteFiles);
            }
        }

        /// <summary>
        /// Kills the transcoding job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <param name="closeLiveStream">if set to <c>true</c> [close live stream].</param>
        /// <param name="delete">The delete.</param>
        private async void KillTranscodingJob(TranscodingJob job, bool closeLiveStream, Func<string, bool> delete)
        {
            job.DisposeKillTimer();

            Logger.Debug("KillTranscodingJob - JobId {0} PlaySessionId {1}. Killing transcoding", job.Id, job.PlaySessionId);

            lock (_activeTranscodingJobs)
            {
                _activeTranscodingJobs.Remove(job);

                if (!job.CancellationTokenSource.IsCancellationRequested)
                {
                    job.CancellationTokenSource.Cancel();
                }
            }

            lock (job.ProcessLock)
            {
                if (job.TranscodingThrottler != null)
                {
                    job.TranscodingThrottler.Stop();
                }

                var process = job.Process;

                var hasExited = job.HasExited;

                if (!hasExited)
                {
                    try
                    {
                        Logger.Info("Stopping ffmpeg process with q command for {0}", job.Path);

                        //process.Kill();
                        process.StandardInput.WriteLine("q");

                        // Need to wait because killing is asynchronous
                        if (!process.WaitForExit(5000))
                        {
                            Logger.Info("Killing ffmpeg process for {0}", job.Path);
                            process.Kill();
                        }
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

            if (closeLiveStream && !string.IsNullOrWhiteSpace(job.LiveStreamId))
            {
                try
                {
                    await _mediaSourceManager.CloseLiveStream(job.LiveStreamId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error closing live stream for {0}", ex, job.Path);
                }
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
                //Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, path);

                DeletePartialStreamFiles(path, jobType, retryCount + 1, 500);
            }
            catch (Exception ex)
            {
                //Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, path);
            }
        }

        /// <summary>
        /// Deletes the progressive partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        private void DeleteProgressivePartialStreamFiles(string outputFilePath)
        {
            _fileSystem.DeleteFile(outputFilePath);
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
                .Where(f => f.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                .ToList();

            Exception e = null;

            foreach (var file in filesToDelete)
            {
                try
                {
                    //Logger.Debug("Deleting HLS file {0}", file);
                    _fileSystem.DeleteFile(file);
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
                    //Logger.ErrorException("Error deleting HLS file {0}", ex, file);
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
        /// Gets or sets the play session identifier.
        /// </summary>
        /// <value>The play session identifier.</value>
        public string PlaySessionId { get; set; }
        /// <summary>
        /// Gets or sets the live stream identifier.
        /// </summary>
        /// <value>The live stream identifier.</value>
        public string LiveStreamId { get; set; }

        public bool IsLiveOutput { get; set; }

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
        public ILogger Logger { get; private set; }
        /// <summary>
        /// Gets or sets the active request count.
        /// </summary>
        /// <value>The active request count.</value>
        public int ActiveRequestCount { get; set; }
        /// <summary>
        /// Gets or sets the kill timer.
        /// </summary>
        /// <value>The kill timer.</value>
        private Timer KillTimer { get; set; }

        public string DeviceId { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public object ProcessLock = new object();

        public bool HasExited { get; set; }
        public bool IsUserPaused { get; set; }

        public string Id { get; set; }

        public float? Framerate { get; set; }
        public double? CompletionPercentage { get; set; }

        public long? BytesDownloaded { get; set; }
        public long? BytesTranscoded { get; set; }

        public long? TranscodingPositionTicks { get; set; }
        public long? DownloadPositionTicks { get; set; }

        public TranscodingThrottler TranscodingThrottler { get; set; }

        private readonly object _timerLock = new object();

        public DateTime LastPingDate { get; set; }
        public int PingTimeout { get; set; }

        public TranscodingJob(ILogger logger)
        {
            Logger = logger;
        }

        public void StopKillTimer()
        {
            lock (_timerLock)
            {
                if (KillTimer != null)
                {
                    KillTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        public void DisposeKillTimer()
        {
            lock (_timerLock)
            {
                if (KillTimer != null)
                {
                    KillTimer.Dispose();
                    KillTimer = null;
                }
            }
        }

        public void StartKillTimer(TimerCallback callback)
        {
            StartKillTimer(callback, PingTimeout);
        }

        public void StartKillTimer(TimerCallback callback, int intervalMs)
        {
            if (HasExited)
            {
                return;
            }

            lock (_timerLock)
            {
                if (KillTimer == null)
                {
                    Logger.Debug("Starting kill timer at {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                    KillTimer = new Timer(callback, this, intervalMs, Timeout.Infinite);
                }
                else
                {
                    Logger.Debug("Changing kill timer to {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                    KillTimer.Change(intervalMs, Timeout.Infinite);
                }
            }
        }

        public void ChangeKillTimerIfStarted()
        {
            if (HasExited)
            {
                return;
            }

            lock (_timerLock)
            {
                if (KillTimer != null)
                {
                    var intervalMs = PingTimeout;

                    Logger.Debug("Changing kill timer to {0}ms. JobId {1} PlaySessionId {2}", intervalMs, Id, PlaySessionId);
                    KillTimer.Change(intervalMs, Timeout.Infinite);
                }
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
        Hls,
        /// <summary>
        /// The dash
        /// </summary>
        Dash
    }
}
