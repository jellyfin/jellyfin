using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncoderValidator
    {
        private const string DefaultEncoderPath = "ffmpeg";

        private static readonly string[] requiredDecoders = new[]
        {
            "mpeg2video",
            "h264_qsv",
            "hevc_qsv",
            "mpeg2_qsv",
            "mpeg2_mmal",
            "mpeg4_mmal",
            "vc1_qsv",
            "vc1_mmal",
            "h264_cuvid",
            "hevc_cuvid",
            "dts",
            "ac3",
            "aac",
            "mp3",
            "h264",
            "h264_mmal",
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
            "libfdk_aac",
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
            "ac3",
            "h264_amf",
            "hevc_amf"
        };

        // Try and use the individual library versions to determine a FFmpeg version
        // This lookup table is to be maintained with the following command line:
        // $ ffmpeg -version | perl -ne ' print "$1=$2.$3," if /^(lib\w+)\s+(\d+)\.\s*(\d+)/'
        private static readonly IReadOnlyDictionary<string, Version> _ffmpegVersionMap = new Dictionary<string, Version>
        {
            { "libavutil=56.31,libavcodec=58.54,libavformat=58.29,libavdevice=58.8,libavfilter=7.57,libswscale=5.5,libswresample=3.5,libpostproc=55.5,", new Version(4, 2) },
            { "libavutil=56.22,libavcodec=58.35,libavformat=58.20,libavdevice=58.5,libavfilter=7.40,libswscale=5.3,libswresample=3.3,libpostproc=55.3,", new Version(4, 1) },
            { "libavutil=56.14,libavcodec=58.18,libavformat=58.12,libavdevice=58.3,libavfilter=7.16,libswscale=5.1,libswresample=3.1,libpostproc=55.1,", new Version(4, 0) },
            { "libavutil=55.78,libavcodec=57.107,libavformat=57.83,libavdevice=57.10,libavfilter=6.107,libswscale=4.8,libswresample=2.9,libpostproc=54.7,", new Version(3, 4) },
            { "libavutil=55.58,libavcodec=57.89,libavformat=57.71,libavdevice=57.6,libavfilter=6.82,libswscale=4.6,libswresample=2.7,libpostproc=54.5,", new Version(3, 3) },
            { "libavutil=55.34,libavcodec=57.64,libavformat=57.56,libavdevice=57.1,libavfilter=6.65,libswscale=4.2,libswresample=2.3,libpostproc=54.1,", new Version(3, 2) },
            { "libavutil=54.31,libavcodec=56.60,libavformat=56.40,libavdevice=56.4,libavfilter=5.40,libswscale=3.1,libswresample=1.2,libpostproc=53.3,", new Version(2, 8) }
        };

        private readonly ILogger _logger;

        private readonly string _encoderPath;

        public EncoderValidator(ILogger logger, string encoderPath = DefaultEncoderPath)
        {
            _logger = logger;
            _encoderPath = encoderPath;
        }

        public static Version MinVersion { get; } = new Version(4, 0);

        public static Version MaxVersion { get; } = null;

        public bool ValidateVersion()
        {
            string output = null;
            try
            {
                output = GetProcessOutput(_encoderPath, "-version");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating encoder");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogError("FFmpeg validation: The process returned no result");
                return false;
            }

            _logger.LogDebug("ffmpeg output: {Output}", output);

            return ValidateVersionInternal(output);
        }

        internal bool ValidateVersionInternal(string versionOutput)
        {
            if (versionOutput.IndexOf("Libav developers", StringComparison.OrdinalIgnoreCase) != -1)
            {
                _logger.LogError("FFmpeg validation: avconv instead of ffmpeg is not supported");
                return false;
            }

            // Work out what the version under test is
            var version = GetFFmpegVersion(versionOutput);

            _logger.LogInformation("Found ffmpeg version {0}", version != null ? version.ToString() : "unknown");

            if (version == null)
            {
                if (MinVersion != null && MaxVersion != null) // Version is unknown
                {
                    if (MinVersion == MaxVersion)
                    {
                        _logger.LogWarning("FFmpeg validation: We recommend ffmpeg version {0}", MinVersion);
                    }
                    else
                    {
                        _logger.LogWarning("FFmpeg validation: We recommend a minimum of {0} and maximum of {1}", MinVersion, MaxVersion);
                    }
                }

                return false;
            }
            else if (MinVersion != null && version < MinVersion) // Version is below what we recommend
            {
                _logger.LogWarning("FFmpeg validation: The minimum recommended ffmpeg version is {0}", MinVersion);
                return false;
            }
            else if (MaxVersion != null && version > MaxVersion) // Version is above what we recommend
            {
                _logger.LogWarning("FFmpeg validation: The maximum recommended ffmpeg version is {0}", MaxVersion);
                return false;
            }

            return true;
        }

        public IEnumerable<string> GetDecoders() => GetCodecs(Codec.Decoder);

        public IEnumerable<string> GetEncoders() => GetCodecs(Codec.Encoder);

        /// <summary>
        /// Using the output from "ffmpeg -version" work out the FFmpeg version.
        /// For pre-built binaries the first line should contain a string like "ffmpeg version x.y", which is easy
        /// to parse.  If this is not available, then we try to match known library versions to FFmpeg versions.
        /// If that fails then we use one of the main libraries to determine if it's new/older than the latest
        /// we have stored.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        internal static Version GetFFmpegVersion(string output)
        {
            // For pre-built binaries the FFmpeg version should be mentioned at the very start of the output
            var match = Regex.Match(output, @"^ffmpeg version n?((?:\d+\.?)+)");

            if (match.Success)
            {
                return new Version(match.Groups[1].Value);
            }
            else
            {
                // Create a reduced version string and lookup key from dictionary
                var reducedVersion = GetLibrariesVersionString(output);

                // Try to lookup the string and return Key, otherwise if not found returns null
                return _ffmpegVersionMap.TryGetValue(reducedVersion, out Version version) ? version : null;
            }
        }

        /// <summary>
        /// Grabs the library names and major.minor version numbers from the 'ffmpeg -version' output
        /// and condenses them on to one line.  Output format is "name1=major.minor,name2=major.minor,etc."
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        private static string GetLibrariesVersionString(string output)
        {
            var rc = new StringBuilder(144);
            foreach (Match m in Regex.Matches(
                output,
                @"((?<name>lib\w+)\s+(?<major>\d+)\.\s*(?<minor>\d+))",
                RegexOptions.Multiline))
            {
                rc.Append(m.Groups["name"])
                    .Append('=')
                    .Append(m.Groups["major"])
                    .Append('.')
                    .Append(m.Groups["minor"])
                    .Append(',');
            }

            return rc.Length == 0 ? null : rc.ToString();
        }

        private enum Codec
        {
            Encoder,
            Decoder
        }

        private IEnumerable<string> GetCodecs(Codec codec)
        {
            string codecstr = codec == Codec.Encoder ? "encoders" : "decoders";
            string output = null;
            try
            {
                output = GetProcessOutput(_encoderPath, "-" + codecstr);
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
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo(path, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    RedirectStandardOutput = true,
                    // ffmpeg uses stderr to log info, don't show this
                    RedirectStandardError = true
                }
            })
            {
                _logger.LogDebug("Running {Path} {Arguments}", path, arguments);

                process.Start();

                return process.StandardOutput.ReadToEnd();
            }
        }
    }
}
