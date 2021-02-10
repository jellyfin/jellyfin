#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncoderValidator
    {
        private const string DefaultEncoderPath = "ffmpeg";

        private static readonly string[] _requiredDecoders = new[]
        {
            "h264",
            "hevc",
            "mpeg2video",
            "mpeg4",
            "msmpeg4",
            "dts",
            "ac3",
            "aac",
            "mp3",
            "flac",
            "h264_qsv",
            "hevc_qsv",
            "mpeg2_qsv",
            "vc1_qsv",
            "vp8_qsv",
            "vp9_qsv",
            "h264_cuvid",
            "hevc_cuvid",
            "mpeg2_cuvid",
            "vc1_cuvid",
            "mpeg4_cuvid",
            "vp8_cuvid",
            "vp9_cuvid",
            "h264_mmal",
            "mpeg2_mmal",
            "mpeg4_mmal",
            "vc1_mmal",
            "h264_mediacodec",
            "hevc_mediacodec",
            "mpeg2_mediacodec",
            "mpeg4_mediacodec",
            "vp8_mediacodec",
            "vp9_mediacodec",
            "h264_opencl",
            "hevc_opencl",
            "mpeg2_opencl",
            "mpeg4_opencl",
            "vp8_opencl",
            "vp9_opencl",
            "vc1_opencl"
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
            "ac3",
            "libmp3lame",
            "libopus",
            "libvorbis",
            "flac",
            "srt",
            "h264_amf",
            "hevc_amf",
            "h264_qsv",
            "hevc_qsv",
            "h264_nvenc",
            "hevc_nvenc",
            "h264_vaapi",
            "hevc_vaapi",
            "h264_omx",
            "hevc_omx",
            "h264_v4l2m2m",
            "h264_videotoolbox",
            "hevc_videotoolbox"
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

        private readonly ILogger _logger;

        private readonly string _encoderPath;

        public EncoderValidator(ILogger logger, string encoderPath = DefaultEncoderPath)
        {
            _logger = logger;
            _encoderPath = encoderPath;
        }

        private enum Codec
        {
            Encoder,
            Decoder
        }

        // When changing this, also change the minimum library versions in _ffmpegMinimumLibraryVersions
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

        public IEnumerable<string> GetHwaccels() => GetHwaccelTypes();

        /// <summary>
        /// Using the output from "ffmpeg -version" work out the FFmpeg version.
        /// For pre-built binaries the first line should contain a string like "ffmpeg version x.y", which is easy
        /// to parse. If this is not available, then we try to match known library versions to FFmpeg versions.
        /// If that fails then we test the libraries to determine if they're newer than our minimum versions.
        /// </summary>
        /// <param name="output">The output from "ffmpeg -version".</param>
        /// <returns>The FFmpeg version.</returns>
        internal Version GetFFmpegVersion(string output)
        {
            // For pre-built binaries the FFmpeg version should be mentioned at the very start of the output
            var match = Regex.Match(output, @"^ffmpeg version n?((?:[0-9]+\.?)+)");

            if (match.Success)
            {
                if (Version.TryParse(match.Groups[1].Value, out var result))
                {
                    return result;
                }
            }

            var versionMap = GetFFmpegLibraryVersions(output);

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
        /// and condenses them on to one line.  Output format is "name1=major.minor,name2=major.minor,etc.".
        /// </summary>
        /// <param name="output">The 'ffmpeg -version' output.</param>
        /// <returns>The library names and major.minor version numbers.</returns>
        private static IReadOnlyDictionary<string, Version> GetFFmpegLibraryVersions(string output)
        {
            var map = new Dictionary<string, Version>();

            foreach (Match match in Regex.Matches(
                output,
                @"((?<name>lib\w+)\s+(?<major>[0-9]+)\.\s*(?<minor>[0-9]+))",
                RegexOptions.Multiline))
            {
                var version = new Version(
                    int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture));

                map.Add(match.Groups["name"].Value, version);
            }

            return map;
        }

        private IEnumerable<string> GetHwaccelTypes()
        {
            string output = null;
            try
            {
                output = GetProcessOutput(_encoderPath, "-hwaccels");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting available hwaccel types");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return Enumerable.Empty<string>();
            }

            var found = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Distinct().ToList();
            _logger.LogInformation("Available hwaccel types: {Types}", found);

            return found;
        }

        public bool CheckFilter(string filter, string option)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return false;
            }

            string output = null;
            try
            {
                output = GetProcessOutput(_encoderPath, "-h filter=" + filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting the given filter");
            }

            if (output.Contains("Filter " + filter, StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(option))
                {
                    return true;
                }

                return output.Contains(option, StringComparison.Ordinal);
            }

            _logger.LogWarning("Filter: {Name} with option {Option} is not available", filter, option);

            return false;
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
