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
        public static FFProbeResult Run(Audio item)
        {
            // Use try catch to avoid having to use File.Exists
            try
            {
                return GetCachedResult(GetFFProbeAudioCachePath(item));
            }
            catch (FileNotFoundException)
            {
            }

            FFProbeResult result = Run(item.Path);

            // Fire and forget
            CacheResult(result, GetFFProbeAudioCachePath(item));

            return result;
        }

        private static FFProbeResult GetCachedResult(string path)
        {
            return JsvSerializer.DeserializeFromFile<FFProbeResult>(path);
        }

        private static void CacheResult(FFProbeResult result, string outputCachePath)
        {
            Task.Run(() =>
            {
                JsvSerializer.SerializeToFile<FFProbeResult>(result, outputCachePath);
            });
        }

        public static FFProbeResult Run(Video item)
        {
            // Use try catch to avoid having to use File.Exists
            try
            {
                return GetCachedResult(GetFFProbeVideoCachePath(item));
            }
            catch (FileNotFoundException)
            {
            }

            FFProbeResult result = Run(item.Path);

            // Fire and forget
            CacheResult(result, GetFFProbeVideoCachePath(item));

            return result;
        }

        private static FFProbeResult Run(string input)
        {
            MediaBrowser.Common.Logging.Logger.LogInfo(input);

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

            try
            {
                process.Start();

                // MUST read both stdout and stderr asynchronously or a deadlock may occurr
                // If we ever decide to disable the ffmpeg log then you must uncomment the below line.
                process.BeginErrorReadLine();

                FFProbeResult result = JsonSerializer.DeserializeFromStream<FFProbeResult>(process.StandardOutput.BaseStream);

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Logger.LogInfo("FFProbe exited with code {0} for {1}", process.ExitCode, input);
                }

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

        private static string GetFFProbeAudioCachePath(BaseEntity item)
        {
            string outputDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, item.Id.ToString().Substring(0, 1));

            return Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".jsv");
        }

        private static string GetFFProbeVideoCachePath(BaseEntity item)
        {
            string outputDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeVideoCacheDirectory, item.Id.ToString().Substring(0, 1));

            return Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".jsv");
        }
    }
}
