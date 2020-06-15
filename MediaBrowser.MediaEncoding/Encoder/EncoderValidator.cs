using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncoderValidator
    {
        private const string DefaultEncoderPath = "ffmpeg";

        private static readonly string[] _requiredDecoders = new[]
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

        private static readonly string[] _requiredEncoders = new[]
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

        // These are the library versions that corresponds to our minimum ffmpeg version 4.x according to the version table below
        private static readonly IReadOnlyDictionary<string, Version> _ffmpegMinimumLibraryVersions = new Dictionary<string, Version>
        {
            { "libavutil", new Version(56, 14) },
            { "libavcodec", new Version(58, 18) },
            { "libavformat", new Version(58, 12) },
            { "libavdevice", new Version(58, 3) },
            { "libavfilter", new Version(7, 16) },
            { "libswscale", new Version(5, 1) },
            { "libswresample", new Version(3, 1) },
            { "libpostproc", new Version(55, 1) }
        };

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

            _logger.LogInformation("Found ffmpeg version {Version}", version != null ? version.ToString() : "unknown");

            if (version == null)
            {
                if (MaxVersion != null) // Version is unknown
                {
                    if (MinVersion == MaxVersion)
                    {
                        _logger.LogWarning("FFmpeg validation: We recommend version {MinVersion}", MinVersion);
                    }
                    else
                    {
                        _logger.LogWarning("FFmpeg validation: We recommend a minimum of {MinVersion} and maximum of {MaxVersion}", MinVersion, MaxVersion);
                    }
                }
                else
                {
                    _logger.LogWarning("FFmpeg validation: We recommend minimum version {MinVersion}", MinVersion);
                }

                return false;
            }
            else if (version < MinVersion) // Version is below what we recommend
            {
                _logger.LogWarning("FFmpeg validation: The minimum recommended version is {MinVersion}", MinVersion);
                return false;
            }
            else if (MaxVersion != null && version > MaxVersion) // Version is above what we recommend
            {
                _logger.LogWarning("FFmpeg validation: The maximum recommended version is {MaxVersion}", MaxVersion);
                return false;
            }

            return true;
        }

        public IEnumerable<string> GetDecoders() => GetCodecs(Codec.Decoder);

        public IEnumerable<string> GetEncoders() => GetCodecs(Codec.Encoder);

        /// <summary>
        /// Using the output from "ffmpeg -version" work out the FFmpeg version.
        /// For pre-built binaries the first line should contain a string like "ffmpeg version x.y", which is easy
        /// to parse. If this is not available, then we try to match known library versions to FFmpeg versions.
        /// If that fails then we test the libraries to determine if they're newer than our minimum versions.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        internal Version GetFFmpegVersion(string output)
        {
            // For pre-built binaries the FFmpeg version should be mentioned at the very start of the output
            var match = Regex.Match(output, @"^ffmpeg version n?((?:\d+\.?)+)");

            if (match.Success)
            {
                return new Version(match.Groups[1].Value);
            }
            else
            {
                if (!TryGetFFmpegLibraryVersions(output, out string versionString, out IReadOnlyDictionary<string, Version> versionMap))
                {
                    _logger.LogError("No ffmpeg library versions found");

                    return null;
                }

                // First try to lookup the full version string
                if (_ffmpegVersionMap.TryGetValue(versionString, out Version version))
                {
                    return version;
                }

                // Then try to test for minimum library versions
                return TestMinimumFFmpegLibraryVersions(versionMap);
            }
        }

        private Version TestMinimumFFmpegLibraryVersions(IReadOnlyDictionary<string, Version> versionMap)
        {
            var allVersionsValidated = true;

            foreach (var minimumVersion in _ffmpegMinimumLibraryVersions)
            {
                if (versionMap.TryGetValue(minimumVersion.Key, out var foundVersion))
                {
                    if (foundVersion >= minimumVersion.Value)
                    {
                        _logger.LogInformation("Found {Library} version {FoundVersion} ({MinimumVersion})", minimumVersion.Key, foundVersion, minimumVersion.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Found {Library} version {FoundVersion} lower than recommended version {MinimumVersion}", minimumVersion.Key, foundVersion, minimumVersion.Value);
                        allVersionsValidated = false;
                    }
                }
                else
                {
                    _logger.LogError("{Library} version not found", minimumVersion.Key);
                    allVersionsValidated = false;
                }
            }

            return allVersionsValidated ? MinVersion : null;
        }

        /// <summary>
        /// Grabs the library names and major.minor version numbers from the 'ffmpeg -version' output
        /// </summary>
        /// <param name="output"></param>
        /// <param name="versionString"></param>
        /// <param name="versionMap"></param>
        /// <returns></returns>
        private static bool TryGetFFmpegLibraryVersions(string output, out string versionString, out IReadOnlyDictionary<string, Version> versionMap)
        {
            var sb = new StringBuilder(144);

            var map = new Dictionary<string, Version>();

            foreach (Match match in Regex.Matches(
                output,
                @"((?<name>lib\w+)\s+(?<major>\d+)\.\s*(?<minor>\d+))",
                RegexOptions.Multiline))
            {
                sb.Append(match.Groups["name"])
                    .Append('=')
                    .Append(match.Groups["major"])
                    .Append('.')
                    .Append(match.Groups["minor"])
                    .Append(',');

                var str = $"{match.Groups["major"]}.{match.Groups["minor"]}";

                var version = Version.Parse(str);

                map.Add(match.Groups["name"].Value, version);
            }

            versionString = sb.ToString();
            versionMap = map;

            return sb.Length > 0;
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

            var required = codec == Codec.Encoder ? _requiredEncoders : _requiredDecoders;

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
