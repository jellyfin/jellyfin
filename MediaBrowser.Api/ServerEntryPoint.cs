using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// The instance
        /// </summary>
        public static ServerEntryPoint Instance;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ServerEntryPoint(ILogger logger)
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
            var jobCount = ActiveTranscodingJobs.Count;

            Parallel.ForEach(ActiveTranscodingJobs, OnTranscodeKillTimerStopped);

            // Try to allow for some time to kill the ffmpeg processes and delete the partial stream files
            if (jobCount > 0)
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// The active transcoding jobs
        /// </summary>
        private readonly List<TranscodingJob> ActiveTranscodingJobs = new List<TranscodingJob>();

        /// <summary>
        /// Called when [transcode beginning].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        /// <param name="process">The process.</param>
        public void OnTranscodeBeginning(string path, TranscodingJobType type, Process process)
        {
            lock (ActiveTranscodingJobs)
            {
                ActiveTranscodingJobs.Add(new TranscodingJob
                {
                    Type = type,
                    Path = path,
                    Process = process,
                    ActiveRequestCount = 1
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
            lock (ActiveTranscodingJobs)
            {
                var job = ActiveTranscodingJobs.First(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                ActiveTranscodingJobs.Remove(job);
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
            lock (ActiveTranscodingJobs)
            {
                return ActiveTranscodingJobs.Any(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Called when [transcode begin request].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        public void OnTranscodeBeginRequest(string path, TranscodingJobType type)
        {
            lock (ActiveTranscodingJobs)
            {
                var job = ActiveTranscodingJobs.FirstOrDefault(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

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
            lock (ActiveTranscodingJobs)
            {
                var job = ActiveTranscodingJobs.FirstOrDefault(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (job == null)
                {
                    return;
                }

                job.ActiveRequestCount--;

                if (job.ActiveRequestCount == 0)
                {
                    var timerDuration = type == TranscodingJobType.Progressive ? 1000 : 30000;

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
        /// Called when [transcoding finished].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        public void OnTranscodingFinished(string path, TranscodingJobType type)
        {
            lock (ActiveTranscodingJobs)
            {
                var job = ActiveTranscodingJobs.FirstOrDefault(j => j.Type == type && j.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (job == null)
                {
                    return;
                }

                ActiveTranscodingJobs.Remove(job);

                if (job.KillTimer != null)
                {
                    job.KillTimer.Dispose();
                    job.KillTimer = null;
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

            lock (ActiveTranscodingJobs)
            {
                ActiveTranscodingJobs.Remove(job);

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
            catch (Win32Exception ex)
            {
                Logger.ErrorException("Error determining if ffmpeg process has exited for {0}", ex, job.Path);
            }
            catch (InvalidOperationException ex)
            {
                Logger.ErrorException("Error determining if ffmpeg process has exited for {0}", ex, job.Path);
            }
            catch (NotSupportedException ex)
            {
                Logger.ErrorException("Error determining if ffmpeg process has exited for {0}", ex, job.Path);
            }

            if (hasExited)
            {
                return;
            }

            try
            {
                Logger.Info("Killing ffmpeg process for {0}", job.Path);

                process.Kill();
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
        /// <summary>
        /// Enum TranscodingJobType
        /// </summary>
        /// <summary>
        /// Gets or sets the kill timer.
        /// </summary>
        /// <value>The kill timer.</value>
        public Timer KillTimer { get; set; }
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
