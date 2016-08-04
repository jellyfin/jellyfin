using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private List<string> GetDecoders(string encoderAppPath)
        {
            string output = string.Empty;
            try
            {
                output = GetProcessOutput(encoderAppPath, "-decoders");
            }
            catch
            {
            }

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

                if (output.IndexOf(srch, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    _logger.Info("Decoder available: " + codec);
                    found.Add(codec);
                }
            }

            return found;
        }

        private List<string> GetEncoders(string encoderAppPath)
        {
            string output = null;
            try
            {
                output = GetProcessOutput(encoderAppPath, "-encoders");
            }
            catch
            {
            }

            var found = new List<string>();
            var required = new[]
            {
                "libx264",
                "libx265",
                "mpeg4",
                "msmpeg4",
                "libvpx",
                "libvpx-vp9",
                "aac",
                "libmp3lame",
                "libopus",
                "libvorbis",
                "srt",
                "h264_nvenc",
                "h264_qsv",
                "ac3"
            };

            output = output ?? string.Empty;

            var index = 0;

            foreach (var codec in required)
            {
                var srch = " " + codec + "  ";

                if (output.IndexOf(srch, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    if (index < required.Length - 1)
                    {
                        _logger.Info("Encoder available: " + codec);
                    }

                    found.Add(codec);
                }
                index++;
            }

            return found;
        }

        private string GetProcessOutput(string path, string arguments)
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
                    RedirectStandardOutput = true
                }
            };

            using (process)
            {
                process.Start();

                try
                {
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
                        _logger.ErrorException("Error killing process", ex1);
                    }

                    throw;
                }
            }
        }
    }
}
