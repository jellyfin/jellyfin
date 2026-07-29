#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public partial class EncoderValidator
    {
        private static readonly string[] _requiredDecoders =
        [
            "h264",
            "hevc",
            "vp8",
            "libvpx",
            "vp9",
            "libvpx-vp9",
            "av1",
            "libdav1d",
            "mpeg2video",
            "mpeg4",
            "msmpeg4",
            "dca",
            "ac3",
            "ac4",
            "aac",
            "mp3",
            "flac",
            "truehd",
            "h264_qsv",
            "hevc_qsv",
            "mpeg2_qsv",
            "vc1_qsv",
            "vp8_qsv",
            "vp9_qsv",
            "av1_qsv",
            "h264_cuvid",
            "hevc_cuvid",
            "mpeg2_cuvid",
            "vc1_cuvid",
            "mpeg4_cuvid",
            "vp8_cuvid",
            "vp9_cuvid",
            "av1_cuvid",
            "h264_rkmpp",
            "hevc_rkmpp",
            "mpeg1_rkmpp",
            "mpeg2_rkmpp",
            "mpeg4_rkmpp",
            "vp8_rkmpp",
            "vp9_rkmpp",
            "av1_rkmpp"
        ];

        private static readonly string[] _requiredEncoders =
        [
            "libx264",
            "libx265",
            "libsvtav1",
            "aac",
            "aac_at",
            "libfdk_aac",
            "ac3",
            "alac",
            "dca",
            "libmp3lame",
            "libopus",
            "libvorbis",
            "flac",
            "truehd",
            "srt",
            "h264_amf",
            "hevc_amf",
            "av1_amf",
            "h264_qsv",
            "hevc_qsv",
            "mjpeg_qsv",
            "av1_qsv",
            "h264_nvenc",
            "hevc_nvenc",
            "av1_nvenc",
            "h264_vaapi",
            "hevc_vaapi",
            "av1_vaapi",
            "mjpeg_vaapi",
            "h264_v4l2m2m",
            "h264_videotoolbox",
            "hevc_videotoolbox",
            "mjpeg_videotoolbox",
            "h264_rkmpp",
            "hevc_rkmpp",
            "mjpeg_rkmpp"
        ];

        private static readonly string[] _requiredFilters =
        [
            // sw
            "alphasrc",
            "zscale",
            "tonemapx",
            // qsv
            "scale_qsv",
            "vpp_qsv",
            "deinterlace_qsv",
            "overlay_qsv",
            // cuda
            "scale_cuda",
            "yadif_cuda",
            "bwdif_cuda",
            "tonemap_cuda",
            "overlay_cuda",
            "transpose_cuda",
            "hwupload_cuda",
            // opencl
            "scale_opencl",
            "tonemap_opencl",
            "overlay_opencl",
            "transpose_opencl",
            "yadif_opencl",
            "bwdif_opencl",
            // vaapi
            "scale_vaapi",
            "deinterlace_vaapi",
            "tonemap_vaapi",
            "procamp_vaapi",
            "overlay_vaapi",
            "transpose_vaapi",
            "hwupload_vaapi",
            // vulkan
            "libplacebo",
            "scale_vulkan",
            "overlay_vulkan",
            "transpose_vulkan",
            "flip_vulkan",
            // videotoolbox
            "yadif_videotoolbox",
            "bwdif_videotoolbox",
            "scale_vt",
            "transpose_vt",
            "overlay_videotoolbox",
            "tonemap_videotoolbox",
            // rkrga
            "scale_rkrga",
            "vpp_rkrga",
            "overlay_rkrga"
        ];

        private static readonly Dictionary<FilterOptionType, (string, string)> _filterOptionsDict = new Dictionary<FilterOptionType, (string, string)>
        {
            { FilterOptionType.ScaleCudaFormat, ("scale_cuda", "format") },
            { FilterOptionType.TonemapCudaName, ("tonemap_cuda", "GPU accelerated HDR to SDR tonemapping") },
            { FilterOptionType.TonemapOpenclBt2390, ("tonemap_opencl", "bt2390") },
            { FilterOptionType.OverlayOpenclFrameSync, ("overlay_opencl", "Action to take when encountering EOF from secondary input") },
            { FilterOptionType.OverlayVaapiFrameSync, ("overlay_vaapi", "Action to take when encountering EOF from secondary input") },
            { FilterOptionType.OverlayVulkanFrameSync, ("overlay_vulkan", "Action to take when encountering EOF from secondary input") },
            { FilterOptionType.TransposeOpenclReversal, ("transpose_opencl", "rotate by half-turn") },
            { FilterOptionType.OverlayOpenclAlphaFormat, ("overlay_opencl", "alpha_format") },
            { FilterOptionType.OverlayCudaAlphaFormat, ("overlay_cuda", "alpha_format") }
        };

        private static readonly Dictionary<BitStreamFilterOptionType, (string, string)> _bsfOptionsDict = new Dictionary<BitStreamFilterOptionType, (string, string)>
        {
            { BitStreamFilterOptionType.HevcMetadataRemoveDovi, ("hevc_metadata", "remove_dovi") },
            { BitStreamFilterOptionType.HevcMetadataRemoveHdr10Plus, ("hevc_metadata", "remove_hdr10plus") },
            { BitStreamFilterOptionType.Av1MetadataRemoveDovi, ("av1_metadata", "remove_dovi") },
            { BitStreamFilterOptionType.Av1MetadataRemoveHdr10Plus, ("av1_metadata", "remove_hdr10plus") },
            { BitStreamFilterOptionType.DoviRpuStrip, ("dovi_rpu", "strip") }
        };

        // These are the library versions that corresponds to our minimum ffmpeg version 4.4 according to the version table below
        // Refers to the versions in https://ffmpeg.org/download.html
        private static readonly Dictionary<string, Version> _ffmpegMinimumLibraryVersions = new Dictionary<string, Version>
        {
            { "libavutil", new Version(56, 70) },
            { "libavcodec", new Version(58, 134) },
            { "libavformat", new Version(58, 76) },
            { "libavdevice", new Version(58, 13) },
            { "libavfilter", new Version(7, 110) },
            { "libswscale", new Version(5, 9) },
            { "libswresample", new Version(3, 9) }
        };

        private readonly ILogger _logger;

        private readonly string _encoderPath;

        private readonly IFFRunner? _ffRunner;

        private readonly Version _minFFmpegMultiThreadedCli = new Version(7, 0);

        public EncoderValidator(ILogger logger, string encoderPath, IFFRunner? ffRunner = null)
        {
            _logger = logger;
            _encoderPath = encoderPath;

            // Null while validating a candidate path: nothing is committed to IFFPaths yet, so the
            // runner cannot resolve a binary. Only ValidateVersion runs in that state.
            _ffRunner = ffRunner;
        }

        private enum Codec
        {
            Encoder,
            Decoder
        }

        // When changing this, also change the minimum library versions in _ffmpegMinimumLibraryVersions
        public static Version MinVersion { get; } = new Version(4, 4);

        public static Version? MaxVersion { get; } = null;

        [GeneratedRegex(@"^ffmpeg version n?((?:[0-9]+\.?)+)")]
        private static partial Regex FfmpegVersionRegex();

        [GeneratedRegex(@"((?<name>lib\w+)\s+(?<major>[0-9]+)\.\s*(?<minor>[0-9]+))", RegexOptions.Multiline)]
        private static partial Regex LibraryRegex();

        public async Task<bool> ValidateVersionAsync()
        {
            var output = string.Empty;
            try
            {
                // Names its own binary: this runs to decide whether that path is usable, so there
                // is nothing committed to IFFPaths for the runner to resolve.
                ArgumentNullException.ThrowIfNull(_ffRunner);
                var result = await _ffRunner.RunAsync(
                    new ValidateBinaryRequest
                    {
                        BinaryPath = _encoderPath,
                        Stdout = async (stdout, ct) =>
                        {
                            using var reader = new StreamReader(stdout);
                            output = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
                        }
                    },
                    CancellationToken.None).ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    _logger.LogError(
                        "FFmpeg validation: {Path} did not report a version ({Reason}). {Stderr}",
                        _encoderPath,
                        result.StopReason,
                        result.Stderr);

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating encoder");
                return false;
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
            if (versionOutput.Contains("Libav developers", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("FFmpeg validation: avconv instead of ffmpeg is not supported");
                return false;
            }

            // Work out what the version under test is
            var version = GetFFmpegVersionInternal(versionOutput);

            _logger.LogInformation("Found ffmpeg version {Version}", version is not null ? version.ToString() : "unknown");

            if (version is null)
            {
                if (MaxVersion is not null) // Version is unknown
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

            if (version < MinVersion) // Version is below what we recommend
            {
                _logger.LogWarning("FFmpeg validation: The minimum recommended version is {MinVersion}", MinVersion);
                return false;
            }

            if (MaxVersion is not null && version > MaxVersion) // Version is above what we recommend
            {
                _logger.LogWarning("FFmpeg validation: The maximum recommended version is {MaxVersion}", MaxVersion);
                return false;
            }

            return true;
        }

        public Task<IEnumerable<string>> GetDecodersAsync() => GetCodecsAsync(Codec.Decoder);

        public Task<IEnumerable<string>> GetEncodersAsync() => GetCodecsAsync(Codec.Encoder);

        public Task<IEnumerable<string>> GetHwaccelsAsync() => GetHwaccelTypesAsync();

        public Task<IEnumerable<string>> GetFiltersAsync() => GetFFmpegFiltersAsync();

        public async Task<IDictionary<FilterOptionType, bool>> GetFiltersWithOptionAsync()
        {
            var results = new Dictionary<FilterOptionType, bool>();
            foreach (var (key, (filter, option)) in _filterOptionsDict)
            {
                results[key] = await CheckFilterWithOptionAsync(filter, option).ConfigureAwait(false);
            }

            return results;
        }

        public async Task<IDictionary<BitStreamFilterOptionType, bool>> GetBitStreamFiltersWithOptionAsync()
        {
            var results = new Dictionary<BitStreamFilterOptionType, bool>();
            foreach (var (key, (filter, option)) in _bsfOptionsDict)
            {
                results[key] = await CheckBitStreamFilterWithOptionAsync(filter, option).ConfigureAwait(false);
            }

            return results;
        }

        public async Task<Version?> GetFFmpegVersionAsync()
        {
            string output;
            try
            {
                output = await InterrogateAsync("-version", false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating encoder");
                return null;
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogError("FFmpeg validation: The process returned no result");
                return null;
            }

            _logger.LogDebug("ffmpeg output: {Output}", output);

            return GetFFmpegVersionInternal(output);
        }

        /// <summary>
        /// Using the output from "ffmpeg -version" work out the FFmpeg version.
        /// For pre-built binaries the first line should contain a string like "ffmpeg version x.y", which is easy
        /// to parse. If this is not available, then we try to match known library versions to FFmpeg versions.
        /// If that fails then we test the libraries to determine if they're newer than our minimum versions.
        /// </summary>
        /// <param name="output">The output from "ffmpeg -version".</param>
        /// <returns>The FFmpeg version.</returns>
        internal Version? GetFFmpegVersionInternal(string output)
        {
            // For pre-built binaries the FFmpeg version should be mentioned at the very start of the output
            var match = FfmpegVersionRegex().Match(output);

            if (match.Success)
            {
                if (Version.TryParse(match.Groups[1].ValueSpan, out var result))
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
        private static Dictionary<string, Version> GetFFmpegLibraryVersions(string output)
        {
            var map = new Dictionary<string, Version>();

            foreach (Match match in LibraryRegex().Matches(output))
            {
                var version = new Version(
                    int.Parse(match.Groups["major"].ValueSpan, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups["minor"].ValueSpan, CultureInfo.InvariantCulture));

                map.Add(match.Groups["name"].Value, version);
            }

            return map;
        }

        public async Task<bool> CheckVaapiDeviceByDriverNameAsync(string driverName, string renderNodePath)
        {
            if (!OperatingSystem.IsLinux())
            {
                return false;
            }

            if (string.IsNullOrEmpty(driverName) || string.IsNullOrEmpty(renderNodePath))
            {
                return false;
            }

            try
            {
                // Two things here are deliberate and easy to undo by accident.
                //
                // The explicit -v verbose overrides the level the runner would otherwise derive from
                // the server's log level. The driver name is only printed while the device is being
                // initialised, and at the default level of warning that line is never emitted, so the
                // match below would fail on hardware that is in fact present.
                //
                // The answer is the log, not the exit code. There is no input or output file, so
                // FFmpeg initialises the device and then exits non-zero with rc=234 about on a working
                // node and on a missing one alike. Only the content distinguishes them, which is why
                // this reads the output rather than calling InterrogationSucceeds.
                var output = await InterrogateAsync("-v verbose -init_hw_device vaapi=va:" + renderNodePath, true).ConfigureAwait(false);
                return output.Contains(driverName, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting the given vaapi render node path");
                return false;
            }
        }

        public async Task<bool> CheckVulkanDrmDeviceByExtensionNameAsync(string renderNodePath, string[] vulkanExtensions)
        {
            if (!OperatingSystem.IsLinux())
            {
                return false;
            }

            if (string.IsNullOrEmpty(renderNodePath))
            {
                return false;
            }

            try
            {
                // Same two properties as the vaapi check above: -v verbose is required for the device
                // log to be emitted at all, and the verdict comes from that log rather than from the
                // exit code, which is non-zero either way for want of an output file.
                //
                // The devices are chained. "drm=dr:<node>" opens the render node under the alias dr,
                // then "vulkan=vk@dr" derives a Vulkan device *from* that one — the @ is what makes
                // this test the Vulkan driver backing this specific node instead of whichever device
                // the loader would have picked on its own.
                var command = "-v verbose -init_hw_device drm=dr:" + renderNodePath + " -init_hw_device vulkan=vk@dr";
                var output = await InterrogateAsync(command, true).ConfigureAwait(false);

                // Every extension must be present; the caller asks about a set it needs in full.
                foreach (string ext in vulkanExtensions)
                {
                    if (!output.Contains(ext, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting the given drm render node path");
                return false;
            }
        }

        [SupportedOSPlatform("macos")]
        public bool CheckIsVideoToolboxAv1DecodeAvailable()
        {
            return ApplePlatformHelper.HasAv1HardwareAccel(_logger);
        }

        private async Task<IEnumerable<string>> GetHwaccelTypesAsync()
        {
            string? output = null;
            try
            {
                output = await InterrogateAsync("-hwaccels", false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting available hwaccel types");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return [];
            }

            var found = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Skip(1).Distinct().ToList();
            _logger.LogInformation("Available hwaccel types: {Types}", found);

            return found;
        }

        public async Task<bool> CheckFilterWithOptionAsync(string filter, string option)
        {
            if (string.IsNullOrEmpty(filter) || string.IsNullOrEmpty(option))
            {
                return false;
            }

            string output;
            try
            {
                output = await InterrogateAsync("-h filter=" + filter, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting the given filter");
                return false;
            }

            // Two questions, in order. "-h filter=x" prints help for a filter it knows and a bare
            // "Unknown filter" line for one it does not, so the header confirms the filter exists
            // before the option is looked for in its parameter list. Without that first check a
            // missing filter and a present filter lacking the option are the same answer, and the
            // warning below would name the wrong problem.
            if (output.Contains("Filter " + filter, StringComparison.Ordinal))
            {
                return output.Contains(option, StringComparison.Ordinal);
            }

            _logger.LogWarning("Filter: {Name} with option {Option} is not available", filter, option);

            return false;
        }

        public async Task<bool> CheckBitStreamFilterWithOptionAsync(string filter, string option)
        {
            if (string.IsNullOrEmpty(filter) || string.IsNullOrEmpty(option))
            {
                return false;
            }

            string output;
            try
            {
                output = await InterrogateAsync("-h bsf=" + filter, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting the given bit stream filter");
                return false;
            }

            // Same two-stage shape as CheckFilterWithOptionAsync, against "-h bsf=" and the header
            // FFmpeg prints for a bit stream filter.
            if (output.Contains("Bit stream filter " + filter, StringComparison.Ordinal))
            {
                return output.Contains(option, StringComparison.Ordinal);
            }

            _logger.LogWarning("Bit stream filter: {Name} with option {Option} is not available", filter, option);

            return false;
        }

        public async Task<bool> CheckSupportedRuntimeKeyAsync(string keyDesc, Version? ffmpegVersion)
        {
            if (string.IsNullOrEmpty(keyDesc))
            {
                return false;
            }

            string output;
            try
            {
                // Asks FFmpeg which keys it will respond to while running, by giving it something to
                // do and then interrupting it: the runner writes RuntimeKeyProbeRequest.QueryKey ("?")
                // to stdin, and FFmpeg answers with its key list on stderr.
                //
                // The work is a 1x1 null source encoded to nothing, so the run costs nothing while it
                // lasts. The duration only has to outlive the write — the process is torn down as soon
                // as the answer is in hand, never actually running for the hours nominated here.
                var duration = ffmpegVersion >= _minFFmpegMultiThreadedCli ? 10000 : 1000;
                var runtime = await _ffRunner!.RunAsync(
                    new RuntimeKeyProbeRequest
                    {
                        Arguments = $"-f lavfi -i nullsrc=s=1x1:d={duration} -f null -",

                        // Complete, not a trailing window: the key list is printed when the query is
                        // answered and is then followed by ordinary encoding progress, so a window
                        // sized for diagnosing a failure would scroll the answer away.
                        Stderr = FFOutputSink.Complete()
                    },
                    CancellationToken.None).ConfigureAwait(false);
                output = runtime.Stderr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking supported runtime key");
                return false;
            }

            return output.Contains(keyDesc, StringComparison.Ordinal);
        }

        /// <summary>
        /// Tests whether the encoder accepts a given <c>-hwaccel_flags</c> value, by asking it to do
        /// trivial work with that flag set and seeing whether it agrees to start.
        /// </summary>
        /// <remarks>
        /// Unlike the device checks above, the verdict here is the exit code and the output is
        /// discarded: FFmpeg rejects an unknown flag while parsing options, before it does anything.
        /// Measured against jellyfin-ffmpeg 7.1.4, a known flag exits 0 and an unknown one exits 234.
        /// </remarks>
        /// <param name="flag">The <c>-hwaccel_flags</c> value to test, without its leading <c>+</c>.</param>
        /// <returns><c>true</c> if the encoder accepts the flag; otherwise, <c>false</c>.</returns>
        public async Task<bool> CheckSupportedHwaccelFlagAsync(string flag)
        {
            return !string.IsNullOrEmpty(flag)
                && await InterrogationSucceedsAsync($"-hwaccel_flags +{flag} -f lavfi -i nullsrc=s=1x1:d=100 -f null -").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests whether ffprobe — not ffmpeg, hence <c>probeOnly</c> — recognises an option, so that
        /// options absent from some builds are only ever passed to a prober that accepts them.
        /// </summary>
        /// <remarks>
        /// Also decided by exit code. The option is named with no value against a 1x1 null source: a
        /// recognised option exits 0 and an unrecognised one exits 1, since ffprobe fails on the
        /// option itself long before the source matters.
        /// </remarks>
        /// <param name="option">The option name to test, without its leading <c>-</c>.</param>
        /// <returns><c>true</c> if the prober recognises the option; otherwise, <c>false</c>.</returns>
        public async Task<bool> CheckSupportedProberOptionAsync(string option)
        {
            return !string.IsNullOrEmpty(option)
                && await InterrogationSucceedsAsync($"-f lavfi -i nullsrc=s=1x1:d=1 -{option}", probeOnly: true).ConfigureAwait(false);
        }

        private async Task<IEnumerable<string>> GetCodecsAsync(Codec codec)
        {
            string codecstr = codec == Codec.Encoder ? "encoders" : "decoders";
            string output;
            try
            {
                output = await InterrogateAsync("-" + codecstr, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting available {Codec}", codecstr);
                return [];
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return [];
            }

            var required = codec == Codec.Encoder ? _requiredEncoders : _requiredDecoders;

            var found = CodecRegex()
                .Matches(output)
                .Select(x => x.Groups["codec"].Value)
                .Where(x => required.Contains(x));

            _logger.LogInformation("Available {Codec}: {Codecs}", codecstr, found);

            return found;
        }

        private async Task<IEnumerable<string>> GetFFmpegFiltersAsync()
        {
            string output;
            try
            {
                output = await InterrogateAsync("-filters", false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting available filters");
                return [];
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return [];
            }

            var found = FilterRegex()
                .Matches(output)
                .Select(x => x.Groups["filter"].Value)
                .Where(x => _requiredFilters.Contains(x));

            _logger.LogInformation("Available filters: {Filters}", found);

            return found;
        }

        /// <summary>
        /// Runs an interrogation through the shared runner and returns the stream the caller reads.
        /// Retains all stderr: these probes answer questions by scraping their own output, so a
        /// truncated middle would silently change the answer.
        /// </summary>
        private async Task<string> InterrogateAsync(string arguments, bool readStdErr, bool probeOnly = false)
        {
            var request = new CapabilitiesRequest
            {
                Arguments = arguments,
                ProbeOnly = probeOnly,
                Stderr = FFOutputSink.Complete()
            };

            if (readStdErr)
            {
                var stderrResult = await _ffRunner!.RunAsync(request, CancellationToken.None).ConfigureAwait(false);
                return stderrResult.Stderr;
            }

            var output = string.Empty;
            await _ffRunner!.RunAsync(
                request with
                {
                    Stdout = async (stdout, ct) =>
                    {
                        using var reader = new StreamReader(stdout);
                        output = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
                    }
                },
                CancellationToken.None).ConfigureAwait(false);

            return output;
        }

        /// <summary>Runs an interrogation whose answer is only whether FFmpeg accepted it.</summary>
        private async Task<bool> InterrogationSucceedsAsync(string arguments, bool probeOnly = false)
        {
            var result = await _ffRunner!.RunAsync(
                new CapabilitiesRequest { Arguments = arguments, ProbeOnly = probeOnly },
                CancellationToken.None).ConfigureAwait(false);

            return result.Succeeded;
        }

        [GeneratedRegex("^\\s\\S{6}\\s(?<codec>[\\w|-]+)\\s+.+$", RegexOptions.Multiline)]
        private static partial Regex CodecRegex();

        [GeneratedRegex("^\\s\\S{2,3}\\s(?<filter>[\\w|-]+)\\s+.+$", RegexOptions.Multiline)]
        private static partial Regex FilterRegex();
    }
}
