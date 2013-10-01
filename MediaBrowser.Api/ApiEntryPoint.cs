using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// Initializes a new instance of the <see cref="ApiEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ApiEntryPoint(ILogger logger)
        {
            Logger = logger;

            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
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

            Parallel.ForEach(_activeTranscodingJobs, KillTranscodingJob);

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
        /// <param name="type">The type.</param>
        /// <param name="process">The process.</param>
        /// <param name="isVideo">if set to <c>true</c> [is video].</param>
        /// <param name="startTimeTicks">The start time ticks.</param>
        /// <param name="sourcePath">The source path.</param>
        public void OnTranscodeBeginning(string path, TranscodingJobType type, Process process, bool isVideo, long? startTimeTicks, string sourcePath)
        {
            lock (_activeTranscodingJobs)
            {
                _activeTranscodingJobs.Add(new TranscodingJob
                {
                    Type = type,
                    Path = path,
                    Process = process,
                    ActiveRequestCount = 1,
                    IsVideo = isVideo,
                    StartTimeTicks = startTimeTicks,
                    SourcePath = sourcePath
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
        public void OnTranscodeFailedToStart(string path, TranscodingJobType type)
        {
            lock (_activeTranscodingJobs)
            {
                var job = _activeTranscodingJobs.First(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                _activeTranscodingJobs.Remove(job);
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
            lock (_activeTranscodingJobs)
            {
                return _activeTranscodingJobs.Any(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Called when [transcode begin request].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        public void OnTranscodeBeginRequest(string path, TranscodingJobType type)
        {
            lock (_activeTranscodingJobs)
            {
                var job = _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (job == null)
                {
                    return;
                }

                job.ActiveRequestCount++;

                if (job.KillTimer != null)
                {
                    job.KillTimer.Dispose();
                    job.KillTimer = null;
                }
            }
        }

        /// <summary>
        /// Called when [transcode end request].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        public void OnTranscodeEndRequest(string path, TranscodingJobType type)
        {
            lock (_activeTranscodingJobs)
            {
                var job = _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (job == null)
                {
                    return;
                }

                job.ActiveRequestCount--;

                if (job.ActiveRequestCount == 0)
                {
                    var timerDuration = type == TranscodingJobType.Progressive ? 1000 : 180000;

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

            KillTranscodingJob(job);
        }

        /// <summary>
        /// Kills the single transcoding job.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        internal void KillSingleTranscodingJob(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentNullException("sourcePath");
            }

            var jobs = new List<TranscodingJob>();

            lock (_activeTranscodingJobs)
            {
                // This is really only needed for HLS. 
                // Progressive streams can stop on their own reliably
                jobs.AddRange(_activeTranscodingJobs.Where(i => string.Equals(sourcePath, i.SourcePath) && i.Type == TranscodingJobType.Hls));
            }

            // This method of killing is a bit of a shortcut, but it saves clients from having to send a request just for that
            // But we can only kill if there's one active job. If there are more we won't know which one to stop
            if (jobs.Count == 1)
            {
                KillTranscodingJob(jobs.First());
            }
        }

        /// <summary>
        /// Kills the transcoding job.
        /// </summary>
        /// <param name="job">The job.</param>
        private async void KillTranscodingJob(TranscodingJob job)
        {
            lock (_activeTranscodingJobs)
            {
                _activeTranscodingJobs.Remove(job);

                if (job.KillTimer != null)
                {
                    job.KillTimer.Dispose();
                    job.KillTimer = null;
                }
            }

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

                    process.Kill();

                    // Need to wait because killing is asynchronous
                    process.WaitForExit(5000);
                }
                catch (Win32Exception ex)
                {
                    Logger.ErrorException("Error killing transcoding job for {0}", ex, job.Path);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.ErrorException("Error killing transcoding job for {0}", ex, job.Path);
                }
                catch (NotSupportedException ex)
                {
                    Logger.ErrorException("Error killing transcoding job for {0}", ex, job.Path);
                }
            }

            // Determine if it exited successfully
            var hasExitedSuccessfully = false;

            try
            {
                hasExitedSuccessfully = process.ExitCode == 0;
            }
            catch (InvalidOperationException)
            {

            }
            catch (NotSupportedException)
            {

            }

            // Dispose the process
            process.Dispose();

            // If it didn't complete successfully cleanup the partial files
            // Also don't cache output from resume points
            // Also don't cache video
            if (!hasExitedSuccessfully || job.StartTimeTicks.HasValue || job.IsVideo)
            {
                Logger.Info("Deleting partial stream file(s) {0}", job.Path);

                await Task.Delay(1000).ConfigureAwait(false);

                try
                {
                    if (job.Type == TranscodingJobType.Progressive)
                    {
                        DeleteProgressivePartialStreamFiles(job.Path);
                    }
                    else
                    {
                        DeleteHlsPartialStreamFiles(job.Path);
                    }
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, job.Path);
                }
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

            foreach (var file in filesToDelete)
            {
                try
                {
                    Logger.Info("Deleting HLS file {0}", file);
                    File.Delete(file);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error deleting HLS file {0}", ex, file);
                }
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

        public bool IsVideo { get; set; }
        public long? StartTimeTicks { get; set; }
        public string SourcePath { get; set; }
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
