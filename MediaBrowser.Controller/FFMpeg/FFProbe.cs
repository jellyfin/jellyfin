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
        /// <summary>
        /// Runs FFProbe against an Audio file, caches the result and returns the output
        /// </summary>
        public static FFProbeResult Run(Audio item)
        {
            // Use try catch to avoid having to use File.Exists
            try
            {
                return GetCachedResult(GetFFProbeCachePath(item));
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            FFProbeResult result = Run(item.Path);

            // Fire and forget
            CacheResult(result, GetFFProbeCachePath(item));

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
                    ProtobufSerializer.SerializeToFile<FFProbeResult>(result, outputCachePath);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs FFProbe against a Video file, caches the result and returns the output
        /// </summary>
        public static FFProbeResult Run(Video item)
        {
            // Use try catch to avoid having to use File.Exists
            try
            {
                return GetCachedResult(GetFFProbeCachePath(item));
            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            FFProbeResult result = Run(item.Path);

            // Fire and forget
            CacheResult(result, GetFFProbeCachePath(item));

            return result;
        }

        private static FFProbeResult Run(string input)
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
            finally
            {
                process.Dispose();
            }
        }

        private static string GetFFProbeCachePath(Audio item)
        {
            string outputDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeAudioCacheDirectory, item.Id.ToString().Substring(0, 1));

            return Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".pb");
        }

        private static string GetFFProbeCachePath(Video item)
        {
            string outputDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.FFProbeVideoCacheDirectory, item.Id.ToString().Substring(0, 1));

            return Path.Combine(outputDirectory, item.Id + "-" + item.DateModified.Ticks + ".pb");
        }
    }
}
