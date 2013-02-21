using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Controller.Entities;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.FFMpeg
{
    /// <summary>
    /// Runs FFProbe against a media file and returns metadata.
    /// </summary>
    public static class FFProbe
    {
        /// <summary>
        /// Runs FFProbe against an Audio file, caches the result and returns the output
        /// </summary>
        public static FFProbeResult Run(BaseItem item, string cacheDirectory)
        {
            string cachePath = GetFfProbeCachePath(item, cacheDirectory);

            // Use try catch to avoid having to use File.Exists
            try
            {
                return GetCachedResult(cachePath);
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            FFProbeResult result = Run(item.Path);

            if (result != null)
            {
                // Fire and forget
                CacheResult(result, cachePath);
            }

            return result;
        }

        /// <summary>
        /// Gets the cached result of an FFProbe operation
        /// </summary>
        private static FFProbeResult GetCachedResult(string path)
        {
            return ProtobufSerializer.DeserializeFromFile<FFProbeResult>(path);
        }

        /// <summary>
        /// Caches the result of an FFProbe operation
        /// </summary>
        private static async void CacheResult(FFProbeResult result, string outputCachePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    ProtobufSerializer.SerializeToFile(result, outputCachePath);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }).ConfigureAwait(false);
        }

        private static FFProbeResult Run(string input)
        {
            var startInfo = new ProcessStartInfo { };

            startInfo.CreateNoWindow = true;

            startInfo.UseShellExecute = false;

            // Must consume both or ffmpeg may hang due to deadlocks. See comments below.
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            startInfo.FileName = Kernel.Instance.ApplicationPaths.FFProbePath;
            startInfo.WorkingDirectory = Kernel.Instance.ApplicationPaths.FFMpegDirectory;
            startInfo.Arguments = string.Format("\"{0}\" -v quiet -print_format json -show_streams -show_format", input);

            //Logger.LogInfo(startInfo.FileName + " " + startInfo.Arguments);

            var process = new Process { };
            process.StartInfo = startInfo;

            process.EnableRaisingEvents = true;

            process.Exited += ProcessExited;

            try
            {
                process.Start();

                // MUST read both stdout and stderr asynchronously or a deadlock may occurr
                // If we ever decide to disable the ffmpeg log then you must uncomment the below line.
                process.BeginErrorReadLine();

                return JsonSerializer.DeserializeFromStream<FFProbeResult>(process.StandardOutput.BaseStream);
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
        }

        static void ProcessExited(object sender, EventArgs e)
        {
            (sender as Process).Dispose();
        }

        private static string GetFfProbeCachePath(BaseItem item, string cacheDirectory)
        {
            string outputDirectory = Path.Combine(cacheDirectory, item.Id.ToString().Substring(0, 1));

            return Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".pb");
        }
    }
}
