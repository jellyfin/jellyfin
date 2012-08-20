using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.FFMpeg
{
    /// <summary>
    /// Runs FFProbe against a media file and returns metadata.
    /// </summary>
    public static class FFProbe
    {
        public async static Task<FFProbeResult> Run(Audio item, string outputCachePath)
        {
            // Use try catch to avoid having to use File.Exists
            try
            {
                using (FileStream stream = File.OpenRead(outputCachePath))
                {
                    return JsonSerializer.DeserializeFromStream<FFProbeResult>(stream);
                }
            }
            catch (FileNotFoundException)
            {
            }

            await Run(item.Path, outputCachePath);

            using (FileStream stream = File.OpenRead(outputCachePath))
            {
                return JsonSerializer.DeserializeFromStream<FFProbeResult>(stream);
            }
        }

        public async static Task<FFProbeResult> Run(Video item, string outputCachePath)
        {
            // Use try catch to avoid having to use File.Exists
            try
            {
                using (FileStream stream = File.OpenRead(outputCachePath))
                {
                    return JsonSerializer.DeserializeFromStream<FFProbeResult>(stream);
                }
            }
            catch (FileNotFoundException)
            {
            }

            await Run(item.Path, outputCachePath);

            using (FileStream stream = File.OpenRead(outputCachePath))
            {
                return JsonSerializer.DeserializeFromStream<FFProbeResult>(stream);
            }
        }

        private async static Task Run(string input, string output)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.CreateNoWindow = true;

            startInfo.UseShellExecute = false;

            // Must consume both or ffmpeg may hang due to deadlocks. See comments below.
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.FileName = Kernel.Instance.ApplicationPaths.FFProbePath;
            startInfo.WorkingDirectory = Kernel.Instance.ApplicationPaths.FFMpegDirectory;
            startInfo.Arguments = string.Format("\"{0}\" -v quiet -print_format json -show_streams -show_format", input);

            //Logger.LogInfo(startInfo.FileName + " " + startInfo.Arguments);

            Process process = new Process();
            process.StartInfo = startInfo;

            FileStream stream = new FileStream(output, FileMode.Create);

            try
            {
                process.Start();

                // MUST read both stdout and stderr asynchronously or a deadlock may occurr
                // If we ever decide to disable the ffmpeg log then you must uncomment the below line.
                process.BeginErrorReadLine();

                await process.StandardOutput.BaseStream.CopyToAsync(stream);

                process.WaitForExit();

                stream.Dispose();

                if (process.ExitCode != 0)
                {
                    Logger.LogInfo("FFProbe exited with code {0} for {1}", process.ExitCode, input);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);

                stream.Dispose();

                // Hate having to do this
                try
                {
                    process.Kill();
                }
                catch
                {
                }
                File.Delete(output);
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
