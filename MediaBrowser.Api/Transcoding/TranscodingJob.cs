using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MediaBrowser.Common.Logging;

namespace MediaBrowser.Api.Transcoding
{
    /// <summary>
    /// Represents an active transcoding job
    /// </summary>
    public class TranscodingJob
    {
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public string TranscoderPath { get; set; }
        public string Arguments { get; set; }

        public TranscoderJobStatus Status { get; private set; }

        /// <summary>
        /// Starts the job
        /// </summary>
        public void Start()
        {
            ApiService.AddTranscodingJob(this);
            
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = true;

            startInfo.UseShellExecute = false;

            startInfo.FileName = TranscoderPath;
            startInfo.WorkingDirectory = Path.GetDirectoryName(TranscoderPath);
            startInfo.Arguments = Arguments;

            Logger.LogInfo("TranscodingJob.Start: " + TranscoderPath + " " + Arguments);

            Process process = new Process();

            process.StartInfo = startInfo;

            process.EnableRaisingEvents = true;

            process.Start();

            process.Exited += process_Exited;
        }

        void process_Exited(object sender, EventArgs e)
        {
            ApiService.RemoveTranscodingJob(this);
            
            Process process = sender as Process;

            // If it terminated with an error
            if (process.ExitCode != 0)
            {
                Status = TranscoderJobStatus.Error;

                // Delete this since it won't be valid
                if (File.Exists(OutputFile))
                {
                    File.Delete(OutputFile);
                }
            }
            else
            {
                Status = TranscoderJobStatus.Completed;
            }

            process.Dispose();
        }

        /// <summary>
        /// Provides a helper to wait for the job to exit
        /// </summary>
        public void WaitForExit()
        {
            while (true)
            {
                TranscoderJobStatus status = Status;

                if (status == TranscoderJobStatus.Completed || status == TranscoderJobStatus.Error)
                {
                    break;
                }

                Thread.Sleep(500);
            }
        }
    }

    public enum TranscoderJobStatus
    {
        Queued,
        Started,
        Completed,
        Error
    }
}
