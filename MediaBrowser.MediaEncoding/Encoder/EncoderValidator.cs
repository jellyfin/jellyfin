using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public (IEnumerable<string> decoders, IEnumerable<string> encoders) GetAvailableCoders(string encoderPath)
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
                if (logOutput)
                {
                    _logger.LogError("FFmpeg validation: The process returned no result");
                }
                return false;
            }

            _logger.LogDebug("ffmpeg output: {Output}", output);

            if (output.IndexOf("Libav developers", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (logOutput)
                {
                    _logger.LogError("FFmpeg validation: avconv instead of ffmpeg is not supported");
                }
                return false;
            }

            // The min and max FFmpeg versions required to run jellyfin successfully
            var minRequired = new Version(4, 0);
            var maxRequired = new Version(4, 0);

            // Work out what the version under test is
            var underTest = GetFFmpegVersion(output);

            if (logOutput)
            {
                _logger.LogInformation("FFmpeg validation: Found ffmpeg version {0}", underTest != null ? underTest.ToString() : "unknown");

                if (underTest == null) // Version is unknown
                {
                    if (minRequired.Equals(maxRequired))
                    {
                        _logger.LogWarning("FFmpeg validation: We recommend ffmpeg version {0}", minRequired.ToString());
                    }
                    else
                    {
                        _logger.LogWarning("FFmpeg validation: We recommend a minimum of {0} and maximum of {1}", minRequired.ToString(), maxRequired.ToString());
                    }
                }
                else if (underTest.CompareTo(minRequired) < 0) // Version is below what we recommend
                {
                    _logger.LogWarning("FFmpeg validation: The minimum recommended ffmpeg version is {0}", minRequired.ToString());
                }
                else if (underTest.CompareTo(maxRequired) > 0) // Version is above what we recommend
                {
                    _logger.LogWarning("FFmpeg validation: The maximum recommended ffmpeg version is {0}", maxRequired.ToString());
                }
                else  // Version is ok
                {
                    _logger.LogInformation("FFmpeg validation: Found suitable ffmpeg version");
                }
            }

            // underTest shall be null if versions is unknown
            return (underTest == null) ? false : (underTest.CompareTo(minRequired) >= 0 && underTest.CompareTo(maxRequired) <= 0);
        }

        /// <summary>
        /// Using the output from "ffmpeg -version" work out the FFmpeg version.
        /// For pre-built binaries the first line should contain a string like "ffmpeg version x.y", which is easy
        /// to parse.  If this is not available, then we try to match known library versions to FFmpeg versions.
        /// If that fails then we use one of the main libraries to determine if it's new/older than the latest
        /// we have stored.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        static private Version GetFFmpegVersion(string output)
        {
            // For pre-built binaries the FFmpeg version should be mentioned at the very start of the output
            var match = Regex.Match(output, @"ffmpeg version (\d+\.\d+)");

            if (match.Success)
            {
                return new Version(match.Groups[1].Value);
            }
            else
            {
                // Try and use the individual library versions to determine a FFmpeg version
                // This lookup table is to be maintained with the following command line:
                // $ ./ffmpeg.exe -version | perl -ne ' print "$1=$2.$3," if /^(lib\w+)\s+(\d+)\.\s*(\d+)/'
                var lut = new ReadOnlyDictionary<Version, string>
                    (new Dictionary<Version, string>
                    {
                        { new Version("4.1"), "libavutil=56.22,libavcodec=58.35,libavformat=58.20,libavdevice=58.5,libavfilter=7.40,libswscale=5.3,libswresample=3.3,libpostproc=55.3," },
                        { new Version("4.0"), "libavutil=56.14,libavcodec=58.18,libavformat=58.12,libavdevice=58.3,libavfilter=7.16,libswscale=5.1,libswresample=3.1,libpostproc=55.1," },
                        { new Version("3.4"), "libavutil=55.78,libavcodec=57.107,libavformat=57.83,libavdevice=57.10,libavfilter=6.107,libswscale=4.8,libswresample=2.9,libpostproc=54.7," },
                        { new Version("3.3"), "libavutil=55.58,libavcodec=57.89,libavformat=57.71,libavdevice=57.6,libavfilter=6.82,libswscale=4.6,libswresample=2.7,libpostproc=54.5," },
                        { new Version("3.2"), "libavutil=55.34,libavcodec=57.64,libavformat=57.56,libavdevice=57.1,libavfilter=6.65,libswscale=4.2,libswresample=2.3,libpostproc=54.1," },
                        { new Version("2.8"), "libavutil=54.31,libavcodec=56.60,libavformat=56.40,libavdevice=56.4,libavfilter=5.40,libswscale=3.1,libswresample=1.2,libpostproc=53.3," }
                    });

                // Create a reduced version string and lookup key from dictionary
                var reducedVersion = GetVersionString(output);

                // Try to lookup the string and return Key, otherwise if not found returns null
                return lut.FirstOrDefault(x => x.Value == reducedVersion).Key;
            }
        }

        /// <summary>
        /// Grabs the library names and major.minor version numbers from the 'ffmpeg -version' output
        /// and condenses them on to one line.  Output format is "name1=major.minor,name2=major.minor,etc."
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        static private string GetVersionString(string output)
        {
            string pattern = @"((?<name>lib\w+)\s+(?<major>\d+)\.\s*(?<minor>\d+))";
            RegexOptions options = RegexOptions.Multiline;

            string rc = null;

            foreach (Match m in Regex.Matches(output, pattern, options))
            {
                rc += string.Concat(m.Groups["name"], '=', m.Groups["major"], '.', m.Groups["minor"], ',');
            }

            return rc;
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
                "h264_v4l2m2m",
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
