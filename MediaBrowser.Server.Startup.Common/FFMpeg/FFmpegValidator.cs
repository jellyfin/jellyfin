using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace MediaBrowser.Server.Startup.Common.FFMpeg
{
    public class FFmpegValidator
    {
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;

        public FFmpegValidator(ILogger logger, IApplicationPaths appPaths)
        {
            _logger = logger;
            _appPaths = appPaths;
        }

        public void Validate(FFMpegInfo info)
        {
            _logger.Info("FFMpeg: {0}", info.EncoderPath);
            _logger.Info("FFProbe: {0}", info.ProbePath);

            string cacheKey;

            try
            {
                cacheKey = new FileInfo(info.EncoderPath).Length.ToString(CultureInfo.InvariantCulture).GetMD5().ToString("N");
            }
            catch (IOException)
            {
                // This could happen if ffmpeg is coming from a Path variable and we don't have the full path
                cacheKey = Guid.NewGuid().ToString("N");
            }
            catch
            {
                cacheKey = Guid.NewGuid().ToString("N");
            }

            var cachePath = Path.Combine(_appPaths.CachePath, "1" + cacheKey);

            ValidateCodecs(info.EncoderPath, cachePath);
        }

        private void ValidateCodecs(string ffmpegPath, string cachePath)
        {
            string output = null;
            try
            {
                output = File.ReadAllText(cachePath, Encoding.UTF8);
            }
            catch
            {

            }

            if (string.IsNullOrWhiteSpace(output))
            {
                try
                {
                    output = GetFFMpegOutput(ffmpegPath, "-encoders");
                }
                catch
                {
                    return;
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                    File.WriteAllText(cachePath, output, Encoding.UTF8);
                }
                catch
                {

                }
            }

            ValidateCodecsFromOutput(output);
        }

        private void ValidateCodecsFromOutput(string output)
        {
            var required = new[]
            {
                "libx264",
                "libx265",
                "mpeg4",
                "msmpeg4",
                "libvpx",
                //"libvpx-vp9",
                "aac",
                "ac3",
                "libmp3lame",
                "libvorbis",
                "srt"
            };

            foreach (var encoder in required)
            {
                var srch = " " + encoder + "  ";

                if (output.IndexOf(srch, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    _logger.Error("ffmpeg is missing encoder " + encoder);
                    //throw new ArgumentException("ffmpeg is missing encoder " + encoder);
                }
            }
        }

        private string GetFFMpegOutput(string path, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = path,
                    Arguments = arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            using (process)
            {
                process.Start();

                try
                {
                    process.BeginErrorReadLine();

                    using (var reader = new StreamReader(process.StandardOutput.BaseStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch
                {
                    // Hate having to do this
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex1)
                    {
                        _logger.ErrorException("Error killing ffmpeg", ex1);
                    }

                    throw;
                }
            }
        }
    }
}
