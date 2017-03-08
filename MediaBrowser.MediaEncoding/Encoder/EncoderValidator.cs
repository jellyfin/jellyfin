using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using MediaBrowser.Model.Diagnostics;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncoderValidator
    {
        private readonly ILogger _logger;
        private readonly IProcessFactory _processFactory;

        public EncoderValidator(ILogger logger, IProcessFactory processFactory)
        {
            _logger = logger;
            _processFactory = processFactory;
        }

        public Tuple<List<string>, List<string>> Validate(string encoderPath)
        {
            _logger.Info("Validating media encoder at {0}", encoderPath);

            var decoders = GetDecoders(encoderPath);
            var encoders = GetEncoders(encoderPath);

            _logger.Info("Encoder validation complete");

            return new Tuple<List<string>, List<string>>(decoders, encoders);
        }

        public bool ValidateVersion(string encoderAppPath, bool logOutput)
        {
            string output = string.Empty;
            try
            {
                output = GetProcessOutput(encoderAppPath, "-version");
            }
            catch (Exception ex)
            {
                if (logOutput)
                {
                    _logger.ErrorException("Error validating encoder", ex);
                }
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return false;
            }

            if (logOutput)
            {
                _logger.Info("ffmpeg info: {0}", output);
            }

            if (output.IndexOf("Libav developers", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            output = " " + output + " ";

            for (var i = 2013; i <= 2015; i++)
            {
                var yearString = i.ToString(CultureInfo.InvariantCulture);
                if (output.IndexOf(" " + yearString + " ", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return false;
                }
            }

            return true;
        }

        private List<string> GetDecoders(string encoderAppPath)
        {
            string output = string.Empty;
            try
            {
                output = GetProcessOutput(encoderAppPath, "-decoders");
            }
            catch (Exception )
            {
                //_logger.ErrorException("Error detecting available decoders", ex);
            }

            var found = new List<string>();
            var required = new[]
            {
                "h264_qsv",
                "hevc_qsv",
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
                "hevc_nvenc",
                "h264_qsv",
                "hevc_qsv",
                "h264_omx",
                "hevc_omx",
                "h264_vaapi",
                "hevc_vaapi",
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
            var process = _processFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = path,
                Arguments = arguments,
                IsHidden = true,
                ErrorDialog = false,
                RedirectStandardOutput = true
            });

            _logger.Info("Running {0} {1}", path, arguments);

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
