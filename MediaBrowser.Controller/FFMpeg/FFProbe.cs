using System;
using System.Diagnostics;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Serialization;

namespace MediaBrowser.Controller.FFMpeg
{
    public static class FFProbe
    {
        public static FFProbeResult Run(string path)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = true;

            startInfo.UseShellExecute = false;

            // Must consume both or ffmpeg may hang due to deadlocks. See comments below.
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.FileName = Kernel.Instance.ApplicationPaths.FFProbePath;
            startInfo.WorkingDirectory = Kernel.Instance.ApplicationPaths.FFMpegDirectory;
            startInfo.Arguments = string.Format("\"{0}\" -v quiet -print_format json -show_streams -show_format", path);

            Logger.LogInfo(startInfo.FileName + " " + startInfo.Arguments);

            Process process = new Process();
            process.StartInfo = startInfo;

            try
            {
                process.Start();

                // MUST read both stdout and stderr asynchronously or a deadlock may occurr
                // If we ever decide to disable the ffmpeg log then you must uncomment the below line.
                process.BeginErrorReadLine();

                FFProbeResult result = JsonSerializer.DeserializeFromStream<FFProbeResult>(process.StandardOutput.BaseStream);

                process.WaitForExit();

                Logger.LogInfo("FFMpeg exited with code " + process.ExitCode);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);

                // Hate having to do this
                try
                {
                    process.Kill();
                }
                catch
                {
                }

                return null;
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
