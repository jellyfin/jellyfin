using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncoderValidator
    {
        private readonly ILogger _logger;

        public EncoderValidator(ILogger logger)
        {
            _logger = logger;
        }

        public Tuple<List<string>, List<string>> Validate(string encoderPath)
        {
            _logger.Info("Validating media encoder at {0}", encoderPath);

            var decoders = GetDecoders(encoderPath);
            var encoders = GetEncoders(encoderPath);

            _logger.Info("Encoder validation complete");

            return new Tuple<List<string>, List<string>>(decoders, encoders);
        }

        private List<string> GetDecoders(string ffmpegPath)
        {
            string output = string.Empty;
            try
            {
                output = GetFFMpegOutput(ffmpegPath, "-decoders");
            }
            catch
            {
            }
            //_logger.Debug("ffmpeg decoder query result: {0}", output ?? string.Empty);

            var found = new List<string>();
            var required = new[]
            {
                "h264_qsv",
                "mpeg2_qsv",
                "vc1_qsv"
            };

            foreach (var codec in required)
            {
                var srch = " " + codec + "  ";

                if (output.IndexOf(srch, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    _logger.Warn("ffmpeg is missing decoder " + codec);
                }
                else
                {
                    found.Add(codec);
                }
            }

            return found;
        }

        private List<string> GetEncoders(string ffmpegPath)
        {
            string output = null;
            try
            {
                output = GetFFMpegOutput(ffmpegPath, "-encoders");
            }
            catch
            {
            }
            //_logger.Debug("ffmpeg encoder query result: {0}", output ?? string.Empty);

            var found = new List<string>();
            var required = new[]
            {
                "libx264",
                "libx265",
                "mpeg4",
                "msmpeg4",
                //"libvpx",
                //"libvpx-vp9",
                "aac",
                "libmp3lame",
                "libopus",
                //"libvorbis",
                "srt"
            };

            foreach (var codec in required)
            {
                var srch = " " + codec + "  ";

                if (output.IndexOf(srch, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    _logger.Warn("ffmpeg is missing encoder " + codec);
                }
                else
                {
                    found.Add(codec);
                }
            }

            return found;
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

                    return process.StandardOutput.ReadToEnd();
                }
                catch
                {
                    _logger.Info("Killing process {0} {1}", path, arguments);

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
