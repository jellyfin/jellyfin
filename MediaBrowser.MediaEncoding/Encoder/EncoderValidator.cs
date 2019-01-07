using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Diagnostics;
using Microsoft.Extensions.Logging;

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

        public (IEnumerable<string> decoders, IEnumerable<string> encoders) Validate(string encoderPath)
        {
            _logger.LogInformation("Validating media encoder at {EncoderPath}", encoderPath);

            var decoders = GetCodecs(encoderPath, Codec.Decoder);
            var encoders = GetCodecs(encoderPath, Codec.Encoder);

            _logger.LogInformation("Encoder validation complete");

            return (decoders, encoders);
        }

        public bool ValidateVersion(string encoderAppPath, bool logOutput)
        {
            string output = null;
            try
            {
                output = GetProcessOutput(encoderAppPath, "-version");
            }
            catch (Exception ex)
            {
                if (logOutput)
                {
                    _logger.LogError(ex, "Error validating encoder");
                }
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return false;
            }

            _logger.LogDebug("ffmpeg output: {Output}", output);

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

        private static readonly string[] requiredDecoders = new[]
            {
                "mpeg2video",
                "h264_qsv",
                "hevc_qsv",
                "mpeg2_qsv",
                "vc1_qsv",
                "h264_cuvid",
                "hevc_cuvid",
                "dts",
                "ac3",
                "aac",
                "mp3",
                "h264",
                "hevc"
            };

        private static readonly string[] requiredEncoders = new[]
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

        private enum Codec
        {
            Encoder,
            Decoder
        }

        private IEnumerable<string> GetCodecs(string encoderAppPath, Codec codec)
        {
            string codecstr = codec == Codec.Encoder ? "encoders" : "decoders";
            string output = null;
            try
            {
                output = GetProcessOutput(encoderAppPath, "-" + codecstr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting available {Codec}", codecstr);
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return Enumerable.Empty<string>();
            }

            var required = codec == Codec.Encoder ? requiredEncoders : requiredDecoders;

            var found = Regex
                .Matches(output, @"^\s\S{6}\s(?<codec>[\w|-]+)\s+.+$", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(x => x.Groups["codec"].Value)
                .Where(x => required.Contains(x));

            _logger.LogInformation("Available {Codec}: {Codecs}", codecstr, found);

            return found;
        }

        private string GetProcessOutput(string path, string arguments)
        {
            IProcess process = _processFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = path,
                Arguments = arguments,
                IsHidden = true,
                ErrorDialog = false,
                RedirectStandardOutput = true,
                // ffmpeg uses stderr to log info, don't show this
                RedirectStandardError = true
            });

            _logger.LogDebug("Running {Path} {Arguments}", path, arguments);

            using (process)
            {
                process.Start();

                try
                {
                    return process.StandardOutput.ReadToEnd();
                }
                catch
                {
                    _logger.LogWarning("Killing process {Path} {Arguments}", path, arguments);

                    // Hate having to do this
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error killing process");
                    }

                    throw;
                }
            }
        }
    }
}
