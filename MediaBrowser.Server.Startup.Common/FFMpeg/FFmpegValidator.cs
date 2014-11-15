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

            var fileInfo = new FileInfo(info.EncoderPath);
            var cachePath = Path.Combine(_appPaths.CachePath, fileInfo.Length.ToString(CultureInfo.InvariantCulture).GetMD5().ToString("N"));

            if (!File.Exists(cachePath))
            {
                ValidateCodecs(info.EncoderPath);

                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                File.WriteAllText(cachePath, string.Empty, Encoding.UTF8);
            }
        }

        private void ValidateCodecs(string path)
        {
            var output = GetOutput(path, "-encoders");

            var required = new[]
            {
                "libx264",
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
                    throw new ArgumentException("ffmpeg is missing encoder " + encoder);
                }
            }
        }

        private string GetOutput(string path, string arguments)
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
