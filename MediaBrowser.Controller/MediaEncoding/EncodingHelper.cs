#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;

namespace MediaBrowser.Controller.MediaEncoding
{
    public partial class EncodingHelper
    {
        private const string QsvAlias = "qs";
        private const string VaapiAlias = "va";
        private const string D3d11vaAlias = "dx11";
        private const string VideotoolboxAlias = "vt";
        private const string OpenclAlias = "ocl";
        private const string CudaAlias = "cu";
        private const string DrmAlias = "dr";
        private const string VulkanAlias = "vk";
        private readonly IApplicationPaths _appPaths;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly IConfiguration _config;
        private readonly IConfigurationManager _configurationManager;

        // i915 hang was fixed by linux 6.2 (3f882f2)
        private readonly Version _minKerneli915Hang = new Version(5, 18);
        private readonly Version _maxKerneli915Hang = new Version(6, 1, 3);
        private readonly Version _minFixedKernel60i915Hang = new Version(6, 0, 18);

        private readonly Version _minFFmpegImplictHwaccel = new Version(6, 0);
        private readonly Version _minFFmpegHwaUnsafeOutput = new Version(6, 0);
        private readonly Version _minFFmpegOclCuTonemapMode = new Version(5, 1, 3);
        private readonly Version _minFFmpegSvtAv1Params = new Version(5, 1);
        private readonly Version _minFFmpegVaapiH26xEncA53CcSei = new Version(6, 0);

        private static readonly string[] _videoProfilesH264 = new[]
        {
            "ConstrainedBaseline",
            "Baseline",
            "Extended",
            "Main",
            "High",
            "ProgressiveHigh",
            "ConstrainedHigh",
            "High10"
        };

        private static readonly string[] _videoProfilesH265 = new[]
        {
            "Main",
            "Main10"
        };

        private static readonly string[] _videoProfilesAv1 = new[]
        {
            "Main",
            "High",
            "Professional",
        };

        private static readonly HashSet<string> _mp4ContainerNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "mp4",
            "m4a",
            "m4p",
            "m4b",
            "m4r",
            "m4v",
        };

        // Set max transcoding channels for encoders that can't handle more than a set amount of channels
        // AAC, FLAC, ALAC, libopus, libvorbis encoders all support at least 8 channels
        private static readonly Dictionary<string, int> _audioTranscodeChannelLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            { "wmav2", 2 },
            { "libmp3lame", 2 },
            { "libfdk_aac", 6 },
            { "aac_at", 6 },
            { "ac3", 6 },
            { "eac3", 6 },
            { "dca", 6 },
            { "mlp", 6 },
            { "truehd", 6 },
        };

        public static readonly string[] LosslessAudioCodecs = new string[]
        {
            "alac",
            "ape",
            "flac",
            "mlp",
            "truehd",
            "wavpack"
        };

        public EncodingHelper(
            IApplicationPaths appPaths,
            IMediaEncoder mediaEncoder,
            ISubtitleEncoder subtitleEncoder,
            IConfiguration config,
            IConfigurationManager configurationManager)
        {
            _appPaths = appPaths;
            _mediaEncoder = mediaEncoder;
            _subtitleEncoder = subtitleEncoder;
            _config = config;
            _configurationManager = configurationManager;
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhiteSpaceRegex();

        public string GetH264Encoder(EncodingJobInfo state, EncodingOptions encodingOptions)
            => GetH26xOrAv1Encoder("libx264", "h264", state, encodingOptions);

        public string GetH265Encoder(EncodingJobInfo state, EncodingOptions encodingOptions)
            => GetH26xOrAv1Encoder("libx265", "hevc", state, encodingOptions);

        public string GetAv1Encoder(EncodingJobInfo state, EncodingOptions encodingOptions)
            => GetH26xOrAv1Encoder("libsvtav1", "av1", state, encodingOptions);

        private string GetH26xOrAv1Encoder(string defaultEncoder, string hwEncoder, EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is experimental anyway, it's not worth adding additional variables such as this.
            if (state.VideoType == VideoType.VideoFile)
            {
                var hwType = encodingOptions.HardwareAccelerationType;

                var codecMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "amf",                  hwEncoder + "_amf" },
                    { "nvenc",                hwEncoder + "_nvenc" },
                    { "qsv",                  hwEncoder + "_qsv" },
                    { "vaapi",                hwEncoder + "_vaapi" },
                    { "videotoolbox",         hwEncoder + "_videotoolbox" },
                    { "v4l2m2m",              hwEncoder + "_v4l2m2m" },
                };

                if (!string.IsNullOrEmpty(hwType)
                    && encodingOptions.EnableHardwareEncoding
                    && codecMap.TryGetValue(hwType, out var preferredEncoder)
                    && _mediaEncoder.SupportsEncoder(preferredEncoder))
                {
                    return preferredEncoder;
                }
            }

            return defaultEncoder;
        }

        private bool IsVaapiSupported(EncodingJobInfo state)
        {
            // vaapi will throw an error with this input
            // [vaapi @ 0x7faed8000960] No VAAPI support for codec mpeg4 profile -99.
            if (string.Equals(state.VideoStream?.Codec, "mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _mediaEncoder.SupportsHwaccel("vaapi");
        }

        private bool IsVaapiFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("drm")
                   && _mediaEncoder.SupportsHwaccel("vaapi")
                   && _mediaEncoder.SupportsFilter("scale_vaapi")
                   && _mediaEncoder.SupportsFilter("deinterlace_vaapi")
                   && _mediaEncoder.SupportsFilter("tonemap_vaapi")
                   && _mediaEncoder.SupportsFilter("procamp_vaapi")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.OverlayVaapiFrameSync)
                   && _mediaEncoder.SupportsFilter("hwupload_vaapi");
        }

        private bool IsOpenclFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("opencl")
                   && _mediaEncoder.SupportsFilter("scale_opencl")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.TonemapOpenclBt2390)
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.OverlayOpenclFrameSync);
        }

        private bool IsCudaFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("cuda")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.ScaleCudaFormat)
                   && _mediaEncoder.SupportsFilter("yadif_cuda")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.TonemapCudaName)
                   && _mediaEncoder.SupportsFilter("overlay_cuda")
                   && _mediaEncoder.SupportsFilter("hwupload_cuda");
        }

        private bool IsVulkanFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("vulkan")
                   && _mediaEncoder.SupportsFilter("libplacebo")
                   && _mediaEncoder.SupportsFilter("scale_vulkan")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.OverlayVulkanFrameSync);
        }

        private bool IsHwTonemapAvailable(EncodingJobInfo state, EncodingOptions options)
        {
            if (state.VideoStream is null
                || !options.EnableTonemapping
                || GetVideoColorBitDepth(state) != 10)
            {
                return false;
            }

            if (string.Equals(state.VideoStream.Codec, "hevc", StringComparison.OrdinalIgnoreCase)
                && state.VideoStream.VideoRange == VideoRange.HDR
                && state.VideoStream.VideoRangeType == VideoRangeType.DOVI)
            {
                // Only native SW decoder and HW accelerator can parse dovi rpu.
                var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
                var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
                var isNvdecDecoder = vidDecoder.Contains("cuda", StringComparison.OrdinalIgnoreCase);
                var isVaapiDecoder = vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
                var isD3d11vaDecoder = vidDecoder.Contains("d3d11va", StringComparison.OrdinalIgnoreCase);
                return isSwDecoder || isNvdecDecoder || isVaapiDecoder || isD3d11vaDecoder;
            }

            return state.VideoStream.VideoRange == VideoRange.HDR
                   && (state.VideoStream.VideoRangeType == VideoRangeType.HDR10
                       || state.VideoStream.VideoRangeType == VideoRangeType.HLG);
        }

        private bool IsVulkanHwTonemapAvailable(EncodingJobInfo state, EncodingOptions options)
        {
            if (state.VideoStream is null)
            {
                return false;
            }

            // libplacebo has partial Dolby Vision to SDR tonemapping support.
            return options.EnableTonemapping
                   && state.VideoStream.VideoRange == VideoRange.HDR
                   && GetVideoColorBitDepth(state) == 10;
        }

        private bool IsVaapiVppTonemapAvailable(EncodingJobInfo state, EncodingOptions options)
        {
            if (state.VideoStream is null
                || !options.EnableVppTonemapping
                || GetVideoColorBitDepth(state) != 10)
            {
                return false;
            }

            // Native VPP tonemapping may come to QSV in the future.

            return state.VideoStream.VideoRange == VideoRange.HDR
                   && state.VideoStream.VideoRangeType == VideoRangeType.HDR10;
        }

        /// <summary>
        /// Gets the name of the output video codec.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="encodingOptions">Encoding options.</param>
        /// <returns>Encoder string.</returns>
        public string GetVideoEncoder(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var codec = state.OutputVideoCodec;

            if (!string.IsNullOrEmpty(codec))
            {
                if (string.Equals(codec, "av1", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAv1Encoder(state, encodingOptions);
                }

                if (string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase))
                {
                    return GetH265Encoder(state, encodingOptions);
                }

                if (string.Equals(codec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    return GetH264Encoder(state, encodingOptions);
                }

                if (string.Equals(codec, "vp8", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "vpx", StringComparison.OrdinalIgnoreCase))
                {
                    return "libvpx";
                }

                if (string.Equals(codec, "vp9", StringComparison.OrdinalIgnoreCase))
                {
                    return "libvpx-vp9";
                }

                if (string.Equals(codec, "wmv", StringComparison.OrdinalIgnoreCase))
                {
                    return "wmv2";
                }

                if (string.Equals(codec, "theora", StringComparison.OrdinalIgnoreCase))
                {
                    return "libtheora";
                }

                return codec.ToLowerInvariant();
            }

            return "copy";
        }

        /// <summary>
        /// Gets the user agent param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        public string GetUserAgentParam(EncodingJobInfo state)
        {
            if (state.RemoteHttpHeaders.TryGetValue("User-Agent", out string useragent))
            {
                return "-user_agent \"" + useragent + "\"";
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the referer param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        public string GetRefererParam(EncodingJobInfo state)
        {
            if (state.RemoteHttpHeaders.TryGetValue("Referer", out string referer))
            {
                return "-referer \"" + referer + "\"";
            }

            return string.Empty;
        }

        public static string GetInputFormat(string container)
        {
            if (string.IsNullOrEmpty(container))
            {
                return null;
            }

            container = container.Replace("mkv", "matroska", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(container, "ts", StringComparison.OrdinalIgnoreCase))
            {
                return "mpegts";
            }

            // For these need to find out the ffmpeg names
            if (string.Equals(container, "m2ts", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "wmv", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "mts", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "vob", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "mpg", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "mpeg", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "rec", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "dvr-ms", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "ogm", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "divx", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "tp", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "rmvb", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(container, "rtp", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Seeing reported failures here, not sure yet if this is related to specifying input format
            if (string.Equals(container, "m4v", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // obviously don't do this for strm files
            if (string.Equals(container, "strm", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // ISO files don't have an ffmpeg format
            if (string.Equals(container, "iso", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return container;
        }

        /// <summary>
        /// Gets decoder from a codec.
        /// </summary>
        /// <param name="codec">Codec to use.</param>
        /// <returns>Decoder string.</returns>
        public string GetDecoderFromCodec(string codec)
        {
            // For these need to find out the ffmpeg names
            if (string.Equals(codec, "mp2", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(codec, "aac_latm", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(codec, "eac3", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (_mediaEncoder.SupportsDecoder(codec))
            {
                return codec;
            }

            return null;
        }

        /// <summary>
        /// Infers the audio codec based on the url.
        /// </summary>
        /// <param name="container">Container to use.</param>
        /// <returns>Codec string.</returns>
        public string InferAudioCodec(string container)
        {
            var ext = "." + (container ?? string.Empty);

            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "mp3";
            }

            if (string.Equals(ext, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                return "aac";
            }

            if (string.Equals(ext, ".wma", StringComparison.OrdinalIgnoreCase))
            {
                return "wma";
            }

            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            if (string.Equals(ext, ".oga", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            if (string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            if (string.Equals(ext, ".webma", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            return "copy";
        }

        /// <summary>
        /// Infers the video codec.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Nullable{VideoCodecs}.</returns>
        public string InferVideoCodec(string url)
        {
            var ext = Path.GetExtension(url.AsSpan());

            if (ext.Equals(".asf", StringComparison.OrdinalIgnoreCase))
            {
                return "wmv";
            }

            if (ext.Equals(".webm", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: this may not always mean VP8, as the codec ages
                return "vp8";
            }

            if (ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "theora";
            }

            if (ext.Equals(".m3u8", StringComparison.OrdinalIgnoreCase) || ext.Equals(".ts", StringComparison.OrdinalIgnoreCase))
            {
                return "h264";
            }

            return "copy";
        }

        public int GetVideoProfileScore(string videoCodec, string videoProfile)
        {
            // strip spaces because they may be stripped out on the query string
            string profile = videoProfile.Replace(" ", string.Empty, StringComparison.Ordinal);
            if (string.Equals("h264", videoCodec, StringComparison.OrdinalIgnoreCase))
            {
                return Array.FindIndex(_videoProfilesH264, x => string.Equals(x, profile, StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals("hevc", videoCodec, StringComparison.OrdinalIgnoreCase))
            {
                return Array.FindIndex(_videoProfilesH265, x => string.Equals(x, profile, StringComparison.OrdinalIgnoreCase));
            }

            if (string.Equals("av1", videoCodec, StringComparison.OrdinalIgnoreCase))
            {
                return Array.FindIndex(_videoProfilesAv1, x => string.Equals(x, profile, StringComparison.OrdinalIgnoreCase));
            }

            return -1;
        }

        public string GetInputPathArgument(EncodingJobInfo state)
        {
            return state.MediaSource.VideoType switch
            {
                VideoType.Dvd => _mediaEncoder.GetInputArgument(_mediaEncoder.GetPrimaryPlaylistVobFiles(state.MediaPath, null).ToList(), state.MediaSource),
                VideoType.BluRay => _mediaEncoder.GetInputArgument(_mediaEncoder.GetPrimaryPlaylistM2tsFiles(state.MediaPath).ToList(), state.MediaSource),
                _ => _mediaEncoder.GetInputArgument(state.MediaPath, state.MediaSource)
            };
        }

        /// <summary>
        /// Gets the audio encoder.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        public string GetAudioEncoder(EncodingJobInfo state)
        {
            var codec = state.OutputAudioCodec;

            if (string.Equals(codec, "aac", StringComparison.OrdinalIgnoreCase))
            {
                // Use Apple's aac encoder if available as it provides best audio quality
                if (_mediaEncoder.SupportsEncoder("aac_at"))
                {
                    return "aac_at";
                }

                // Use libfdk_aac for better audio quality if using custom build of FFmpeg which has fdk_aac support
                if (_mediaEncoder.SupportsEncoder("libfdk_aac"))
                {
                    return "libfdk_aac";
                }

                return "aac";
            }

            if (string.Equals(codec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "libmp3lame";
            }

            if (string.Equals(codec, "vorbis", StringComparison.OrdinalIgnoreCase))
            {
                return "libvorbis";
            }

            if (string.Equals(codec, "wma", StringComparison.OrdinalIgnoreCase))
            {
                return "wmav2";
            }

            if (string.Equals(codec, "opus", StringComparison.OrdinalIgnoreCase))
            {
                return "libopus";
            }

            if (string.Equals(codec, "flac", StringComparison.OrdinalIgnoreCase))
            {
                return "flac";
            }

            if (string.Equals(codec, "dts", StringComparison.OrdinalIgnoreCase))
            {
                return "dca";
            }

            return codec.ToLowerInvariant();
        }

        private string GetVideoToolboxDeviceArgs(string alias)
        {
            alias ??= VideotoolboxAlias;

            // device selection in vt is not supported.
            return " -init_hw_device videotoolbox=" + alias;
        }

        private string GetCudaDeviceArgs(int deviceIndex, string alias)
        {
            alias ??= CudaAlias;
            deviceIndex = deviceIndex >= 0
                ? deviceIndex
                : 0;

            return string.Format(
                CultureInfo.InvariantCulture,
                " -init_hw_device cuda={0}:{1}",
                alias,
                deviceIndex);
        }

        private string GetVulkanDeviceArgs(int deviceIndex, string deviceName, string srcDeviceAlias, string alias)
        {
            alias ??= VulkanAlias;
            deviceIndex = deviceIndex >= 0
                ? deviceIndex
                : 0;
            var vendorOpts = string.IsNullOrEmpty(deviceName)
                ? ":" + deviceIndex
                : ":" + "\"" + deviceName + "\"";
            var options = string.IsNullOrEmpty(srcDeviceAlias)
                ? vendorOpts
                : "@" + srcDeviceAlias;

            return string.Format(
                CultureInfo.InvariantCulture,
                " -init_hw_device vulkan={0}{1}",
                alias,
                options);
        }

        private string GetOpenclDeviceArgs(int deviceIndex, string deviceVendorName, string srcDeviceAlias, string alias)
        {
            alias ??= OpenclAlias;
            deviceIndex = deviceIndex >= 0
                ? deviceIndex
                : 0;
            var vendorOpts = string.IsNullOrEmpty(deviceVendorName)
                ? ":0.0"
                : ":." + deviceIndex + ",device_vendor=\"" + deviceVendorName + "\"";
            var options = string.IsNullOrEmpty(srcDeviceAlias)
                ? vendorOpts
                : "@" + srcDeviceAlias;

            return string.Format(
                CultureInfo.InvariantCulture,
                " -init_hw_device opencl={0}{1}",
                alias,
                options);
        }

        private string GetD3d11vaDeviceArgs(int deviceIndex, string deviceVendorId, string alias)
        {
            alias ??= D3d11vaAlias;
            deviceIndex = deviceIndex >= 0 ? deviceIndex : 0;
            var options = string.IsNullOrEmpty(deviceVendorId)
                ? deviceIndex.ToString(CultureInfo.InvariantCulture)
                : ",vendor=" + deviceVendorId;

            return string.Format(
                CultureInfo.InvariantCulture,
                " -init_hw_device d3d11va={0}:{1}",
                alias,
                options);
        }

        private string GetVaapiDeviceArgs(string renderNodePath, string driver, string kernelDriver, string srcDeviceAlias, string alias)
        {
            alias ??= VaapiAlias;
            renderNodePath = renderNodePath ?? "/dev/dri/renderD128";
            var driverOpts = string.IsNullOrEmpty(driver)
                ? ":" + renderNodePath
                : ":,driver=" + driver + (string.IsNullOrEmpty(kernelDriver) ? string.Empty : ",kernel_driver=" + kernelDriver);
            var options = string.IsNullOrEmpty(srcDeviceAlias)
                ? driverOpts
                : "@" + srcDeviceAlias;

            return string.Format(
                CultureInfo.InvariantCulture,
                " -init_hw_device vaapi={0}{1}",
                alias,
                options);
        }

        private string GetDrmDeviceArgs(string renderNodePath, string alias)
        {
            alias ??= DrmAlias;
            renderNodePath = renderNodePath ?? "/dev/dri/renderD128";

            return string.Format(
                CultureInfo.InvariantCulture,
                " -init_hw_device drm={0}:{1}",
                alias,
                renderNodePath);
        }

        private string GetQsvDeviceArgs(string alias)
        {
            var arg = " -init_hw_device qsv=" + (alias ?? QsvAlias);
            if (OperatingSystem.IsLinux())
            {
                // derive qsv from vaapi device
                return GetVaapiDeviceArgs(null, "iHD", "i915", null, VaapiAlias) + arg + "@" + VaapiAlias;
            }

            if (OperatingSystem.IsWindows())
            {
                // derive qsv from d3d11va device
                return GetD3d11vaDeviceArgs(0, "0x8086", D3d11vaAlias) + arg + "@" + D3d11vaAlias;
            }

            return null;
        }

        private string GetFilterHwDeviceArgs(string alias)
        {
            return string.IsNullOrEmpty(alias)
                ? string.Empty
                : " -filter_hw_device " + alias;
        }

        public string GetGraphicalSubCanvasSize(EncodingJobInfo state)
        {
            // DVBSUB and DVDSUB use the fixed canvas size 720x576
            if (state.SubtitleStream is not null
                && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode
                && !state.SubtitleStream.IsTextSubtitleStream
                && !string.Equals(state.SubtitleStream.Codec, "DVBSUB", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(state.SubtitleStream.Codec, "DVDSUB", StringComparison.OrdinalIgnoreCase))
            {
                var inW = state.VideoStream?.Width;
                var inH = state.VideoStream?.Height;
                var reqW = state.BaseRequest.Width;
                var reqH = state.BaseRequest.Height;
                var reqMaxW = state.BaseRequest.MaxWidth;
                var reqMaxH = state.BaseRequest.MaxHeight;

                // setup a relative small canvas_size for overlay_qsv/vaapi to reduce transfer overhead
                var (overlayW, overlayH) = GetFixedOutputSize(inW, inH, reqW, reqH, reqMaxW, 1080);

                if (overlayW.HasValue && overlayH.HasValue)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        " -canvas_size {0}x{1}",
                        overlayW.Value,
                        overlayH.Value);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the input video hwaccel argument.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <returns>Input video hwaccel arguments.</returns>
        public string GetInputVideoHwaccelArgs(EncodingJobInfo state, EncodingOptions options)
        {
            if (!state.IsVideoRequest)
            {
                return string.Empty;
            }

            var vidEncoder = GetVideoEncoder(state, options) ?? string.Empty;
            if (IsCopyCodec(vidEncoder))
            {
                return string.Empty;
            }

            var args = new StringBuilder();
            var isWindows = OperatingSystem.IsWindows();
            var isLinux = OperatingSystem.IsLinux();
            var isMacOS = OperatingSystem.IsMacOS();
            var optHwaccelType = options.HardwareAccelerationType;
            var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
            var isHwTonemapAvailable = IsHwTonemapAvailable(state, options);

            if (string.Equals(optHwaccelType, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                if (!isLinux || !_mediaEncoder.SupportsHwaccel("vaapi"))
                {
                    return string.Empty;
                }

                var isVaapiDecoder = vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
                var isVaapiEncoder = vidEncoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
                if (!isVaapiDecoder && !isVaapiEncoder)
                {
                    return string.Empty;
                }

                if (_mediaEncoder.IsVaapiDeviceInteliHD)
                {
                    args.Append(GetVaapiDeviceArgs(null, "iHD", null, null, VaapiAlias));
                }
                else if (_mediaEncoder.IsVaapiDeviceInteli965)
                {
                    // Only override i965 since it has lower priority than iHD in libva lookup.
                    Environment.SetEnvironmentVariable("LIBVA_DRIVER_NAME", "i965");
                    Environment.SetEnvironmentVariable("LIBVA_DRIVER_NAME_JELLYFIN", "i965");
                    args.Append(GetVaapiDeviceArgs(null, "i965", null, null, VaapiAlias));
                }

                var filterDevArgs = string.Empty;
                var doOclTonemap = isHwTonemapAvailable && IsOpenclFullSupported();

                if (_mediaEncoder.IsVaapiDeviceInteliHD || _mediaEncoder.IsVaapiDeviceInteli965)
                {
                    if (doOclTonemap && !isVaapiDecoder)
                    {
                        args.Append(GetOpenclDeviceArgs(0, null, VaapiAlias, OpenclAlias));
                        filterDevArgs = GetFilterHwDeviceArgs(OpenclAlias);
                    }
                }
                else if (_mediaEncoder.IsVaapiDeviceAmd)
                {
                    // Disable AMD EFC feature since it's still unstable in upstream Mesa.
                    Environment.SetEnvironmentVariable("AMD_DEBUG", "noefc");

                    if (IsVulkanFullSupported()
                        && _mediaEncoder.IsVaapiDeviceSupportVulkanDrmInterop)
                    {
                        args.Append(GetDrmDeviceArgs(options.VaapiDevice, DrmAlias));
                        args.Append(GetVaapiDeviceArgs(null, null, null, DrmAlias, VaapiAlias));
                        args.Append(GetVulkanDeviceArgs(0, null, DrmAlias, VulkanAlias));

                        // libplacebo wants an explicitly set vulkan filter device.
                        filterDevArgs = GetFilterHwDeviceArgs(VulkanAlias);
                    }
                    else
                    {
                        args.Append(GetVaapiDeviceArgs(options.VaapiDevice, null, null, null, VaapiAlias));
                        filterDevArgs = GetFilterHwDeviceArgs(VaapiAlias);

                        if (doOclTonemap)
                        {
                            // ROCm/ROCr OpenCL runtime
                            args.Append(GetOpenclDeviceArgs(0, "Advanced Micro Devices", null, OpenclAlias));
                            filterDevArgs = GetFilterHwDeviceArgs(OpenclAlias);
                        }
                    }
                }
                else if (doOclTonemap)
                {
                    args.Append(GetOpenclDeviceArgs(0, null, null, OpenclAlias));
                    filterDevArgs = GetFilterHwDeviceArgs(OpenclAlias);
                }

                args.Append(filterDevArgs);
            }
            else if (string.Equals(optHwaccelType, "qsv", StringComparison.OrdinalIgnoreCase))
            {
                if ((!isLinux && !isWindows) || !_mediaEncoder.SupportsHwaccel("qsv"))
                {
                    return string.Empty;
                }

                var isD3d11vaDecoder = vidDecoder.Contains("d3d11va", StringComparison.OrdinalIgnoreCase);
                var isVaapiDecoder = vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
                var isQsvDecoder = vidDecoder.Contains("qsv", StringComparison.OrdinalIgnoreCase);
                var isQsvEncoder = vidEncoder.Contains("qsv", StringComparison.OrdinalIgnoreCase);
                var isHwDecoder = isQsvDecoder || isVaapiDecoder || isD3d11vaDecoder;
                if (!isHwDecoder && !isQsvEncoder)
                {
                    return string.Empty;
                }

                args.Append(GetQsvDeviceArgs(QsvAlias));
                var filterDevArgs = GetFilterHwDeviceArgs(QsvAlias);
                // child device used by qsv.
                if (_mediaEncoder.SupportsHwaccel("vaapi") || _mediaEncoder.SupportsHwaccel("d3d11va"))
                {
                    if (isHwTonemapAvailable && IsOpenclFullSupported())
                    {
                        var srcAlias = isLinux ? VaapiAlias : D3d11vaAlias;
                        args.Append(GetOpenclDeviceArgs(0, null, srcAlias, OpenclAlias));
                        if (!isHwDecoder)
                        {
                            filterDevArgs = GetFilterHwDeviceArgs(OpenclAlias);
                        }
                    }
                }

                args.Append(filterDevArgs);
            }
            else if (string.Equals(optHwaccelType, "nvenc", StringComparison.OrdinalIgnoreCase))
            {
                if ((!isLinux && !isWindows) || !IsCudaFullSupported())
                {
                    return string.Empty;
                }

                var isCuvidDecoder = vidDecoder.Contains("cuvid", StringComparison.OrdinalIgnoreCase);
                var isNvdecDecoder = vidDecoder.Contains("cuda", StringComparison.OrdinalIgnoreCase);
                var isNvencEncoder = vidEncoder.Contains("nvenc", StringComparison.OrdinalIgnoreCase);
                var isHwDecoder = isNvdecDecoder || isCuvidDecoder;
                if (!isHwDecoder && !isNvencEncoder)
                {
                    return string.Empty;
                }

                args.Append(GetCudaDeviceArgs(0, CudaAlias))
                     .Append(GetFilterHwDeviceArgs(CudaAlias));
            }
            else if (string.Equals(optHwaccelType, "amf", StringComparison.OrdinalIgnoreCase))
            {
                if (!isWindows || !_mediaEncoder.SupportsHwaccel("d3d11va"))
                {
                    return string.Empty;
                }

                var isD3d11vaDecoder = vidDecoder.Contains("d3d11va", StringComparison.OrdinalIgnoreCase);
                var isAmfEncoder = vidEncoder.Contains("amf", StringComparison.OrdinalIgnoreCase);
                if (!isD3d11vaDecoder && !isAmfEncoder)
                {
                    return string.Empty;
                }

                // no dxva video processor hw filter.
                args.Append(GetD3d11vaDeviceArgs(0, "0x1002", D3d11vaAlias));
                var filterDevArgs = string.Empty;
                if (IsOpenclFullSupported())
                {
                    args.Append(GetOpenclDeviceArgs(0, null, D3d11vaAlias, OpenclAlias));
                    filterDevArgs = GetFilterHwDeviceArgs(OpenclAlias);
                }

                args.Append(filterDevArgs);
            }
            else if (string.Equals(optHwaccelType, "videotoolbox", StringComparison.OrdinalIgnoreCase))
            {
                if (!isMacOS || !_mediaEncoder.SupportsHwaccel("videotoolbox"))
                {
                    return string.Empty;
                }

                var isVideotoolboxDecoder = vidDecoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase);
                var isVideotoolboxEncoder = vidEncoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase);
                if (!isVideotoolboxDecoder && !isVideotoolboxEncoder)
                {
                    return string.Empty;
                }

                // no videotoolbox hw filter.
                args.Append(GetVideoToolboxDeviceArgs(VideotoolboxAlias));
            }

            if (!string.IsNullOrEmpty(vidDecoder))
            {
                args.Append(vidDecoder);
            }

            // hw transpose filters should be added manually.
            args.Append(" -autorotate 0");

            return args.ToString().Trim();
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="segmentContainer">Segment Container.</param>
        /// <returns>Input arguments.</returns>
        public string GetInputArgument(EncodingJobInfo state, EncodingOptions options, string segmentContainer)
        {
            var arg = new StringBuilder();
            var inputVidHwaccelArgs = GetInputVideoHwaccelArgs(state, options);

            if (!string.IsNullOrEmpty(inputVidHwaccelArgs))
            {
                arg.Append(inputVidHwaccelArgs);
            }

            var canvasArgs = GetGraphicalSubCanvasSize(state);
            if (!string.IsNullOrEmpty(canvasArgs))
            {
                arg.Append(canvasArgs);
            }

            if (state.MediaSource.VideoType == VideoType.Dvd || state.MediaSource.VideoType == VideoType.BluRay)
            {
                var tmpConcatPath = Path.Join(_configurationManager.GetTranscodePath(), state.MediaSource.Id + ".concat");
                _mediaEncoder.GenerateConcatConfig(state.MediaSource, tmpConcatPath);
                arg.Append(" -f concat -safe 0 -i ")
                    .Append(tmpConcatPath);
            }
            else
            {
                arg.Append(" -i ")
                    .Append(GetInputPathArgument(state));
            }

            // sub2video for external graphical subtitles
            if (state.SubtitleStream is not null
                && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode
                && !state.SubtitleStream.IsTextSubtitleStream
                && state.SubtitleStream.IsExternal)
            {
                var subtitlePath = state.SubtitleStream.Path;
                var subtitleExtension = Path.GetExtension(subtitlePath.AsSpan());

                if (subtitleExtension.Equals(".sub", StringComparison.OrdinalIgnoreCase)
                    || subtitleExtension.Equals(".sup", StringComparison.OrdinalIgnoreCase))
                {
                    var idxFile = Path.ChangeExtension(subtitlePath, ".idx");
                    if (File.Exists(idxFile))
                    {
                        subtitlePath = idxFile;
                    }
                }

                // Also seek the external subtitles stream.
                var seekSubParam = GetFastSeekCommandLineParameter(state, options, segmentContainer);
                if (!string.IsNullOrEmpty(seekSubParam))
                {
                    arg.Append(' ').Append(seekSubParam);
                }

                if (!string.IsNullOrEmpty(canvasArgs))
                {
                    arg.Append(canvasArgs);
                }

                arg.Append(" -i file:\"").Append(subtitlePath).Append('\"');
            }

            if (state.AudioStream is not null && state.AudioStream.IsExternal)
            {
                // Also seek the external audio stream.
                var seekAudioParam = GetFastSeekCommandLineParameter(state, options, segmentContainer);
                if (!string.IsNullOrEmpty(seekAudioParam))
                {
                    arg.Append(' ').Append(seekAudioParam);
                }

                arg.Append(" -i \"").Append(state.AudioStream.Path).Append('"');
            }

            // Disable auto inserted SW scaler for HW decoders in case of changed resolution.
            var isSwDecoder = string.IsNullOrEmpty(GetHardwareVideoDecoder(state, options));
            if (!isSwDecoder && _mediaEncoder.EncoderVersion >= new Version(4, 4))
            {
                arg.Append(" -autoscale 0");
            }

            return arg.ToString();
        }

        /// <summary>
        /// Determines whether the specified stream is H264.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns><c>true</c> if the specified stream is H264; otherwise, <c>false</c>.</returns>
        public static bool IsH264(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1
                    || codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static bool IsH265(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("265", StringComparison.OrdinalIgnoreCase) != -1
                || codec.IndexOf("hevc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static bool IsAAC(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("aac", StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static string GetBitStreamArgs(MediaStream stream)
        {
            // TODO This is auto inserted into the mpegts mux so it might not be needed.
            // https://www.ffmpeg.org/ffmpeg-bitstream-filters.html#h264_005fmp4toannexb
            if (IsH264(stream))
            {
                return "-bsf:v h264_mp4toannexb";
            }

            if (IsH265(stream))
            {
                return "-bsf:v hevc_mp4toannexb";
            }

            if (IsAAC(stream))
            {
                // Convert adts header(mpegts) to asc header(mp4).
                return "-bsf:a aac_adtstoasc";
            }

            return null;
        }

        public static string GetAudioBitStreamArguments(EncodingJobInfo state, string segmentContainer, string mediaSourceContainer)
        {
            var bitStreamArgs = string.Empty;
            var segmentFormat = GetSegmentFileExtension(segmentContainer).TrimStart('.');

            // Apply aac_adtstoasc bitstream filter when media source is in mpegts.
            if (string.Equals(segmentFormat, "mp4", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(mediaSourceContainer, "mpegts", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(mediaSourceContainer, "aac", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(mediaSourceContainer, "hls", StringComparison.OrdinalIgnoreCase)))
            {
                bitStreamArgs = GetBitStreamArgs(state.AudioStream);
                bitStreamArgs = string.IsNullOrEmpty(bitStreamArgs) ? string.Empty : " " + bitStreamArgs;
            }

            return bitStreamArgs;
        }

        public static string GetSegmentFileExtension(string segmentContainer)
        {
            if (!string.IsNullOrWhiteSpace(segmentContainer))
            {
                return "." + segmentContainer;
            }

            return ".ts";
        }

        public string GetVideoBitrateParam(EncodingJobInfo state, string videoCodec)
        {
            if (state.OutputVideoBitrate is null)
            {
                return string.Empty;
            }

            int bitrate = state.OutputVideoBitrate.Value;

            // Bit rate under 1000k is not allowed in h264_qsv
            if (string.Equals(videoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase))
            {
                bitrate = Math.Max(bitrate, 1000);
            }

            // Currently use the same buffer size for all encoders
            int bufsize = bitrate * 2;

            if (string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "libvpx-vp9", StringComparison.OrdinalIgnoreCase))
            {
                // When crf is used with vpx, b:v becomes a max rate
                // https://trac.ffmpeg.org/wiki/Encode/VP8
                // https://trac.ffmpeg.org/wiki/Encode/VP9
                return FormattableString.Invariant($" -maxrate:v {bitrate} -bufsize:v {bufsize} -b:v {bitrate}");
            }

            if (string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
            {
                return FormattableString.Invariant($" -b:v {bitrate}");
            }

            if (string.Equals(videoCodec, "libsvtav1", StringComparison.OrdinalIgnoreCase))
            {
                return FormattableString.Invariant($" -b:v {bitrate} -bufsize {bufsize}");
            }

            if (string.Equals(videoCodec, "libx264", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "libx265", StringComparison.OrdinalIgnoreCase))
            {
                return FormattableString.Invariant($" -maxrate {bitrate} -bufsize {bufsize}");
            }

            if (string.Equals(videoCodec, "h264_amf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "hevc_amf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "av1_amf", StringComparison.OrdinalIgnoreCase))
            {
                // Override the too high default qmin 18 in transcoding preset
                return FormattableString.Invariant($" -rc cbr -qmin 0 -qmax 32 -b:v {bitrate} -maxrate {bitrate} -bufsize {bufsize}");
            }

            if (string.Equals(videoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "hevc_vaapi", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "av1_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                // VBR in i965 driver may result in pixelated output.
                if (_mediaEncoder.IsVaapiDeviceInteli965)
                {
                    return FormattableString.Invariant($" -rc_mode CBR -b:v {bitrate} -maxrate {bitrate} -bufsize {bufsize}");
                }

                return FormattableString.Invariant($" -rc_mode VBR -b:v {bitrate} -maxrate {bitrate} -bufsize {bufsize}");
            }

            return FormattableString.Invariant($" -b:v {bitrate} -maxrate {bitrate} -bufsize {bufsize}");
        }

        public static string NormalizeTranscodingLevel(EncodingJobInfo state, string level)
        {
            if (double.TryParse(level, CultureInfo.InvariantCulture, out double requestLevel))
            {
                if (string.Equals(state.ActualOutputVideoCodec, "av1", StringComparison.OrdinalIgnoreCase))
                {
                    // Transcode to level 5.3 (15) and lower for maximum compatibility.
                    // https://en.wikipedia.org/wiki/AV1#Levels
                    if (requestLevel < 0 || requestLevel >= 15)
                    {
                        return "15";
                    }
                }
                else if (string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(state.ActualOutputVideoCodec, "h265", StringComparison.OrdinalIgnoreCase))
                {
                    // Transcode to level 5.0 and lower for maximum compatibility.
                    // Level 5.0 is suitable for up to 4k 30fps hevc encoding, otherwise let the encoder to handle it.
                    // https://en.wikipedia.org/wiki/High_Efficiency_Video_Coding_tiers_and_levels
                    // MaxLumaSampleRate = 3840*2160*30 = 248832000 < 267386880.
                    if (requestLevel < 0 || requestLevel >= 150)
                    {
                        return "150";
                    }
                }
                else if (string.Equals(state.ActualOutputVideoCodec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    // Transcode to level 5.1 and lower for maximum compatibility.
                    // h264 4k 30fps requires at least level 5.1 otherwise it will break on safari fmp4.
                    // https://en.wikipedia.org/wiki/Advanced_Video_Coding#Levels
                    if (requestLevel < 0 || requestLevel >= 51)
                    {
                        return "51";
                    }
                }
            }

            return level;
        }

        /// <summary>
        /// Gets the text subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="enableAlpha">Enable alpha processing.</param>
        /// <param name="enableSub2video">Enable sub2video mode.</param>
        /// <returns>System.String.</returns>
        public string GetTextSubtitlesFilter(EncodingJobInfo state, bool enableAlpha, bool enableSub2video)
        {
            var seconds = Math.Round(TimeSpan.FromTicks(state.StartTimeTicks ?? 0).TotalSeconds);

            // hls always copies timestamps
            var setPtsParam = state.CopyTimestamps || state.TranscodingType != TranscodingJobType.Progressive
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, ",setpts=PTS -{0}/TB", seconds);

            var alphaParam = enableAlpha ? ":alpha=1" : string.Empty;
            var sub2videoParam = enableSub2video ? ":sub2video=1" : string.Empty;

            var fontPath = Path.Combine(_appPaths.CachePath, "attachments", state.MediaSource.Id);
            var fontParam = string.Format(
                CultureInfo.InvariantCulture,
                ":fontsdir='{0}'",
                _mediaEncoder.EscapeSubtitleFilterPath(fontPath));

            if (state.SubtitleStream.IsExternal)
            {
                var charsetParam = string.Empty;

                if (!string.IsNullOrEmpty(state.SubtitleStream.Language))
                {
                    var charenc = _subtitleEncoder.GetSubtitleFileCharacterSet(
                            state.SubtitleStream,
                            state.SubtitleStream.Language,
                            state.MediaSource,
                            CancellationToken.None).GetAwaiter().GetResult();

                    if (!string.IsNullOrEmpty(charenc))
                    {
                        charsetParam = ":charenc=" + charenc;
                    }
                }

                // TODO: Perhaps also use original_size=1920x800 ??
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "subtitles=f='{0}'{1}{2}{3}{4}{5}",
                    _mediaEncoder.EscapeSubtitleFilterPath(state.SubtitleStream.Path),
                    charsetParam,
                    alphaParam,
                    sub2videoParam,
                    fontParam,
                    setPtsParam);
            }

            var mediaPath = state.MediaPath ?? string.Empty;

            return string.Format(
                CultureInfo.InvariantCulture,
                "subtitles=f='{0}':si={1}{2}{3}{4}{5}",
                _mediaEncoder.EscapeSubtitleFilterPath(mediaPath),
                state.InternalSubtitleStreamOffset.ToString(CultureInfo.InvariantCulture),
                alphaParam,
                sub2videoParam,
                fontParam,
                // fallbackFontParam,
                setPtsParam);
        }

        public double? GetFramerateParam(EncodingJobInfo state)
        {
            var request = state.BaseRequest;

            if (request.Framerate.HasValue)
            {
                return request.Framerate.Value;
            }

            var maxrate = request.MaxFramerate;

            if (maxrate.HasValue && state.VideoStream is not null)
            {
                var contentRate = state.VideoStream.AverageFrameRate ?? state.VideoStream.RealFrameRate;

                if (contentRate.HasValue && contentRate.Value > maxrate.Value)
                {
                    return maxrate;
                }
            }

            return null;
        }

        public string GetHlsVideoKeyFrameArguments(
            EncodingJobInfo state,
            string codec,
            int segmentLength,
            bool isEventPlaylist,
            int? startNumber)
        {
            var args = string.Empty;
            var gopArg = string.Empty;

            var keyFrameArg = string.Format(
                CultureInfo.InvariantCulture,
                " -force_key_frames:0 \"expr:gte(t,n_forced*{0})\"",
                segmentLength);

            var framerate = state.VideoStream?.RealFrameRate;
            if (framerate.HasValue)
            {
                // This is to make sure keyframe interval is limited to our segment,
                // as forcing keyframes is not enough.
                // Example: we encoded half of desired length, then codec detected
                // scene cut and inserted a keyframe; next forced keyframe would
                // be created outside of segment, which breaks seeking.
                gopArg = string.Format(
                    CultureInfo.InvariantCulture,
                    " -g:v:0 {0} -keyint_min:v:0 {0}",
                    Math.Ceiling(segmentLength * framerate.Value));
            }

            // Unable to force key frames using these encoders, set key frames by GOP.
            if (string.Equals(codec, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "h264_amf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc_qsv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc_nvenc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "av1_qsv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "av1_nvenc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "av1_amf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "libsvtav1", StringComparison.OrdinalIgnoreCase))
            {
                args += gopArg;
            }
            else if (string.Equals(codec, "libx264", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(codec, "libx265", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(codec, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(codec, "hevc_vaapi", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(codec, "av1_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                args += keyFrameArg;

                // prevent the libx264 from post processing to break the set keyframe.
                if (string.Equals(codec, "libx264", StringComparison.OrdinalIgnoreCase))
                {
                    args += " -sc_threshold:v:0 0";
                }
            }
            else
            {
                args += keyFrameArg + gopArg;
            }

            // global_header produced by AMD HEVC VA-API encoder causes non-playable fMP4 on iOS
            if (string.Equals(codec, "hevc_vaapi", StringComparison.OrdinalIgnoreCase)
                && _mediaEncoder.IsVaapiDeviceAmd)
            {
                args += " -flags:v -global_header";
            }

            return args;
        }

        /// <summary>
        /// Gets the video bitrate to specify on the command line.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="videoEncoder">Video encoder to use.</param>
        /// <param name="encodingOptions">Encoding options.</param>
        /// <param name="defaultPreset">Default present to use for encoding.</param>
        /// <returns>Video bitrate.</returns>
        public string GetVideoQualityParam(EncodingJobInfo state, string videoEncoder, EncodingOptions encodingOptions, string defaultPreset)
        {
            var param = string.Empty;

            // Tutorials: Enable Intel GuC / HuC firmware loading for Low Power Encoding.
            // https://01.org/group/43/downloads/firmware
            // https://wiki.archlinux.org/title/intel_graphics#Enable_GuC_/_HuC_firmware_loading
            // Intel Low Power Encoding can save unnecessary CPU-GPU synchronization,
            // which will reduce overhead in performance intensive tasks such as 4k transcoding and tonemapping.
            var intelLowPowerHwEncoding = false;

            // Workaround for linux 5.18 to 6.1.3 i915 hang at cost of performance.
            // https://github.com/intel/media-driver/issues/1456
            var enableWaFori915Hang = false;

            if (string.Equals(encodingOptions.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                var isIntelVaapiDriver = _mediaEncoder.IsVaapiDeviceInteliHD || _mediaEncoder.IsVaapiDeviceInteli965;

                if (string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    intelLowPowerHwEncoding = encodingOptions.EnableIntelLowPowerH264HwEncoder && isIntelVaapiDriver;
                }
                else if (string.Equals(videoEncoder, "hevc_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    intelLowPowerHwEncoding = encodingOptions.EnableIntelLowPowerHevcHwEncoder && isIntelVaapiDriver;
                }
            }
            else if (string.Equals(encodingOptions.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
            {
                if (OperatingSystem.IsLinux())
                {
                    var ver = Environment.OSVersion.Version;
                    var isFixedKernel60 = ver.Major == 6 && ver.Minor == 0 && ver >= _minFixedKernel60i915Hang;
                    var isUnaffectedKernel = ver < _minKerneli915Hang || ver > _maxKerneli915Hang;

                    if (!(isUnaffectedKernel || isFixedKernel60))
                    {
                        var vidDecoder = GetHardwareVideoDecoder(state, encodingOptions) ?? string.Empty;
                        var isIntelDecoder = vidDecoder.Contains("qsv", StringComparison.OrdinalIgnoreCase)
                                             || vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
                        var doOclTonemap = _mediaEncoder.SupportsHwaccel("qsv")
                            && IsVaapiSupported(state)
                            && IsOpenclFullSupported()
                            && !IsVaapiVppTonemapAvailable(state, encodingOptions)
                            && IsHwTonemapAvailable(state, encodingOptions);

                        enableWaFori915Hang = isIntelDecoder && doOclTonemap;
                    }
                }

                if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    intelLowPowerHwEncoding = encodingOptions.EnableIntelLowPowerH264HwEncoder;
                }
                else if (string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    intelLowPowerHwEncoding = encodingOptions.EnableIntelLowPowerHevcHwEncoder;
                }
                else
                {
                    enableWaFori915Hang = false;
                }
            }

            if (intelLowPowerHwEncoding)
            {
                param += " -low_power 1";
            }

            if (enableWaFori915Hang)
            {
                param += " -async_depth 1";
            }

            var isVc1 = string.Equals(state.VideoStream?.Codec, "vc1", StringComparison.OrdinalIgnoreCase);
            var isLibX265 = string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase) || isLibX265)
            {
                if (!string.IsNullOrEmpty(encodingOptions.EncoderPreset))
                {
                    param += " -preset " + encodingOptions.EncoderPreset;
                }
                else
                {
                    param += " -preset " + defaultPreset;
                }

                int encodeCrf = encodingOptions.H264Crf;
                if (isLibX265)
                {
                    encodeCrf = encodingOptions.H265Crf;
                }

                if (encodeCrf >= 0 && encodeCrf <= 51)
                {
                    param += " -crf " + encodeCrf.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    string defaultCrf = "23";
                    if (isLibX265)
                    {
                        defaultCrf = "28";
                    }

                    param += " -crf " + defaultCrf;
                }
            }
            else if (string.Equals(videoEncoder, "libsvtav1", StringComparison.OrdinalIgnoreCase))
            {
                // Default to use the recommended preset 10.
                // Omit presets < 5, which are too slow for on the fly encoding.
                // https://gitlab.com/AOMediaCodec/SVT-AV1/-/blob/master/Docs/Ffmpeg.md
                param += encodingOptions.EncoderPreset switch
                {
                    "veryslow" => " -preset 5",
                    "slower" => " -preset 6",
                    "slow" => " -preset 7",
                    "medium" => " -preset 8",
                    "fast" => " -preset 9",
                    "faster" => " -preset 10",
                    "veryfast" => " -preset 11",
                    "superfast" => " -preset 12",
                    "ultrafast" => " -preset 13",
                    _ => " -preset 10"
                };
            }
            else if (string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(videoEncoder, "hevc_vaapi", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(videoEncoder, "av1_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                // -compression_level is not reliable on AMD.
                if (_mediaEncoder.IsVaapiDeviceInteliHD)
                {
                    param += encodingOptions.EncoderPreset switch
                    {
                        "veryslow" => " -compression_level 1",
                        "slower" => " -compression_level 2",
                        "slow" => " -compression_level 3",
                        "medium" => " -compression_level 4",
                        "fast" => " -compression_level 5",
                        "faster" => " -compression_level 6",
                        "veryfast" => " -compression_level 7",
                        "superfast" => " -compression_level 7",
                        "ultrafast" => " -compression_level 7",
                        _ => string.Empty
                    };
                }
            }
            else if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) // h264 (h264_qsv)
                     || string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase) // hevc (hevc_qsv)
                     || string.Equals(videoEncoder, "av1_qsv", StringComparison.OrdinalIgnoreCase)) // av1 (av1_qsv)
            {
                string[] valid_presets = { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast" };

                if (valid_presets.Contains(encodingOptions.EncoderPreset, StringComparison.OrdinalIgnoreCase))
                {
                    param += " -preset " + encodingOptions.EncoderPreset;
                }
                else
                {
                    param += " -preset veryfast";
                }

                // Only h264_qsv has look_ahead option
                if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -look_ahead 0";
                }
            }
            else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase) // h264 (h264_nvenc)
                     || string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase) // hevc (hevc_nvenc)
                     || string.Equals(videoEncoder, "av1_nvenc", StringComparison.OrdinalIgnoreCase)) // av1 (av1_nvenc)
            {
                switch (encodingOptions.EncoderPreset)
                {
                    case "veryslow":
                        param += " -preset p7";
                        break;

                    case "slower":
                        param += " -preset p6";
                        break;

                    case "slow":
                        param += " -preset p5";
                        break;

                    case "medium":
                        param += " -preset p4";
                        break;

                    case "fast":
                        param += " -preset p3";
                        break;

                    case "faster":
                        param += " -preset p2";
                        break;

                    case "veryfast":
                    case "superfast":
                    case "ultrafast":
                        param += " -preset p1";
                        break;

                    default:
                        param += " -preset p1";
                        break;
                }
            }
            else if (string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase) // h264 (h264_amf)
                     || string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase) // hevc (hevc_amf)
                     || string.Equals(videoEncoder, "av1_amf", StringComparison.OrdinalIgnoreCase)) // av1 (av1_amf)
            {
                switch (encodingOptions.EncoderPreset)
                {
                    case "veryslow":
                    case "slower":
                    case "slow":
                        param += " -quality quality";
                        break;

                    case "medium":
                        param += " -quality balanced";
                        break;

                    case "fast":
                    case "faster":
                    case "veryfast":
                    case "superfast":
                    case "ultrafast":
                        param += " -quality speed";
                        break;

                    default:
                        param += " -quality speed";
                        break;
                }

                if (string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoEncoder, "av1_amf", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -header_insertion_mode gop";
                }

                if (string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -gops_per_idr 1";
                }
            }
            else if (string.Equals(videoEncoder, "libvpx", StringComparison.OrdinalIgnoreCase)) // vp8
            {
                // Values 0-3, 0 being highest quality but slower
                var profileScore = 0;

                string crf;
                var qmin = "0";
                var qmax = "50";

                crf = "10";

                if (isVc1)
                {
                    profileScore++;
                }

                // Max of 2
                profileScore = Math.Min(profileScore, 2);

                // http://www.webmproject.org/docs/encoder-parameters/
                param += string.Format(
                    CultureInfo.InvariantCulture,
                    " -speed 16 -quality good -profile:v {0} -slices 8 -crf {1} -qmin {2} -qmax {3}",
                    profileScore.ToString(CultureInfo.InvariantCulture),
                    crf,
                    qmin,
                    qmax);
            }
            else if (string.Equals(videoEncoder, "libvpx-vp9", StringComparison.OrdinalIgnoreCase)) // vp9
            {
                // When `-deadline` is set to `good` or `best`, `-cpu-used` ranges from 0-5.
                // When `-deadline` is set to `realtime`, `-cpu-used` ranges from 0-15.
                // Resources:
                //   * https://trac.ffmpeg.org/wiki/Encode/VP9
                //   * https://superuser.com/questions/1586934
                //   * https://developers.google.com/media/vp9
                param += encodingOptions.EncoderPreset switch
                {
                    "veryslow" => " -deadline best -cpu-used 0",
                    "slower" => " -deadline best -cpu-used 2",
                    "slow" => " -deadline best -cpu-used 3",
                    "medium" => " -deadline good -cpu-used 0",
                    "fast" => " -deadline good -cpu-used 1",
                    "faster" => " -deadline good -cpu-used 2",
                    "veryfast" => " -deadline good -cpu-used 3",
                    "superfast" => " -deadline good -cpu-used 4",
                    "ultrafast" => " -deadline good -cpu-used 5",
                    _ => " -deadline good -cpu-used 1"
                };

                // TODO: until VP9 gets its own CRF setting, base CRF on H.265.
                int h265Crf = encodingOptions.H265Crf;
                int defaultVp9Crf = 31;
                if (h265Crf >= 0 && h265Crf <= 51)
                {
                    // This conversion factor is chosen to match the default CRF for H.265 to the
                    // recommended 1080p CRF from Google. The factor also maps the logarithmic CRF
                    // scale of x265 [0, 51] to that of VP9 [0, 63] relatively well.

                    // Resources:
                    //   * https://developers.google.com/media/vp9/settings/vod
                    const float H265ToVp9CrfConversionFactor = 1.12F;

                    var vp9Crf = Convert.ToInt32(h265Crf * H265ToVp9CrfConversionFactor);

                    // Encoder allows for CRF values in the range [0, 63].
                    vp9Crf = Math.Clamp(vp9Crf, 0, 63);

                    param += FormattableString.Invariant($" -crf {vp9Crf}");
                }
                else
                {
                    param += FormattableString.Invariant($" -crf {defaultVp9Crf}");
                }

                param += " -row-mt 1 -profile 1";
            }
            else if (string.Equals(videoEncoder, "mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                param += " -mbd rd -flags +mv4+aic -trellis 2 -cmp 2 -subcmp 2 -bf 2";
            }
            else if (string.Equals(videoEncoder, "wmv2", StringComparison.OrdinalIgnoreCase)) // asf/wmv
            {
                param += " -qmin 2";
            }
            else if (string.Equals(videoEncoder, "msmpeg4", StringComparison.OrdinalIgnoreCase))
            {
                param += " -mbd 2";
            }

            param += GetVideoBitrateParam(state, videoEncoder);

            var framerate = GetFramerateParam(state);
            if (framerate.HasValue)
            {
                param += string.Format(CultureInfo.InvariantCulture, " -r {0}", framerate.Value.ToString(CultureInfo.InvariantCulture));
            }

            var targetVideoCodec = state.ActualOutputVideoCodec;
            if (string.Equals(targetVideoCodec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase))
            {
                targetVideoCodec = "hevc";
            }

            var profile = state.GetRequestedProfiles(targetVideoCodec).FirstOrDefault() ?? string.Empty;
            profile = WhiteSpaceRegex().Replace(profile, string.Empty);

            // We only transcode to HEVC 8-bit for now, force Main Profile.
            if (profile.Contains("main10", StringComparison.OrdinalIgnoreCase)
                || profile.Contains("mainstill", StringComparison.OrdinalIgnoreCase))
            {
                profile = "main";
            }

            // Extended Profile is not supported by any known h264 encoders, force Main Profile.
            if (profile.Contains("extended", StringComparison.OrdinalIgnoreCase))
            {
                profile = "main";
            }

            // Only libx264 support encoding H264 High 10 Profile, otherwise force High Profile.
            if (!string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase)
                && profile.Contains("high10", StringComparison.OrdinalIgnoreCase))
            {
                profile = "high";
            }

            // We only need Main profile of AV1 encoders.
            if (videoEncoder.Contains("av1", StringComparison.OrdinalIgnoreCase)
                && (profile.Contains("high", StringComparison.OrdinalIgnoreCase)
                    || profile.Contains("professional", StringComparison.OrdinalIgnoreCase)))
            {
                profile = "main";
            }

            // h264_vaapi does not support Baseline profile, force Constrained Baseline in this case,
            // which is compatible (and ugly).
            if (string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                && profile.Contains("baseline", StringComparison.OrdinalIgnoreCase))
            {
                profile = "constrained_baseline";
            }

            // libx264, h264_qsv and h264_nvenc does not support Constrained Baseline profile, force Baseline in this case.
            if ((string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase))
                && profile.Contains("baseline", StringComparison.OrdinalIgnoreCase))
            {
                profile = "baseline";
            }

            // libx264, h264_qsv, h264_nvenc and h264_vaapi does not support Constrained High profile, force High in this case.
            if ((string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                && profile.Contains("high", StringComparison.OrdinalIgnoreCase))
            {
                profile = "high";
            }

            if (string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase)
                && profile.Contains("baseline", StringComparison.OrdinalIgnoreCase))
            {
                profile = "constrained_baseline";
            }

            if (string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase)
                && profile.Contains("constrainedhigh", StringComparison.OrdinalIgnoreCase))
            {
                profile = "constrained_high";
            }

            if (!string.IsNullOrEmpty(profile))
            {
                // Currently there's no profile option in av1_nvenc encoder
                if (!(string.Equals(videoEncoder, "av1_nvenc", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase)))
                {
                    param += " -profile:v:0 " + profile;
                }
            }

            var level = state.GetRequestedLevel(targetVideoCodec);

            if (!string.IsNullOrEmpty(level))
            {
                level = NormalizeTranscodingLevel(state, level);

                // libx264, QSV, AMF can adjust the given level to match the output.
                if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
                else if (string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    // hevc_qsv use -level 51 instead of -level 153.
                    if (double.TryParse(level, CultureInfo.InvariantCulture, out double hevcLevel))
                    {
                        param += " -level " + (hevcLevel / 3);
                    }
                }
                else if (string.Equals(videoEncoder, "av1_qsv", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "libsvtav1", StringComparison.OrdinalIgnoreCase))
                {
                    // libsvtav1 and av1_qsv use -level 60 instead of -level 16
                    // https://aomedia.org/av1/specification/annex-a/
                    if (int.TryParse(level, NumberStyles.Any, CultureInfo.InvariantCulture, out int av1Level))
                    {
                        var x = 2 + (av1Level >> 2);
                        var y = av1Level & 3;
                        var res = (x * 10) + y;
                        param += " -level " + res;
                    }
                }
                else if (string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "av1_amf", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
                else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "av1_nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    // level option may cause NVENC to fail.
                    // NVENC cannot adjust the given level, just throw an error.
                }
                else if (string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "hevc_vaapi", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "av1_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    // level option may cause corrupted frames on AMD VAAPI.
                    if (_mediaEncoder.IsVaapiDeviceInteliHD || _mediaEncoder.IsVaapiDeviceInteli965)
                    {
                        param += " -level " + level;
                    }
                }
                else if (!string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
            }

            if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                param += " -x264opts:0 subme=0:me_range=4:rc_lookahead=10:me=dia:no_chroma_me:8x8dct=0:partitions=none";
            }

            if (string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
            {
                // libx265 only accept level option in -x265-params.
                // level option may cause libx265 to fail.
                // libx265 cannot adjust the given level, just throw an error.
                // TODO: set fine tuned params.
                param += " -x265-params:0 no-info=1";
            }

            if (string.Equals(videoEncoder, "libsvtav1", StringComparison.OrdinalIgnoreCase)
                && _mediaEncoder.EncoderVersion >= _minFFmpegSvtAv1Params)
            {
                param += " -svtav1-params:0 rc=1:tune=0:film-grain=0:enable-overlays=1:enable-tf=0";
            }

            /* Access unit too large: 8192 < 20880 error */
            if ((string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(videoEncoder, "hevc_vaapi", StringComparison.OrdinalIgnoreCase)) &&
                 _mediaEncoder.EncoderVersion >= _minFFmpegVaapiH26xEncA53CcSei)
            {
                param += " -sei -a53_cc";
            }

            return param;
        }

        public bool CanStreamCopyVideo(EncodingJobInfo state, MediaStream videoStream)
        {
            var request = state.BaseRequest;

            if (!request.AllowVideoStreamCopy)
            {
                return false;
            }

            if (videoStream.IsInterlaced
                && state.DeInterlace(videoStream.Codec, false))
            {
                return false;
            }

            if (videoStream.IsAnamorphic ?? false)
            {
                if (request.RequireNonAnamorphic)
                {
                    return false;
                }
            }

            // Can't stream copy if we're burning in subtitles
            if (request.SubtitleStreamIndex.HasValue
                && request.SubtitleStreamIndex.Value >= 0
                && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
            {
                return false;
            }

            if (string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                && videoStream.IsAVC.HasValue
                && !videoStream.IsAVC.Value
                && request.RequireAvc)
            {
                return false;
            }

            // Source and target codecs must match
            if (string.IsNullOrEmpty(videoStream.Codec)
                || (state.SupportedVideoCodecs.Length != 0
                    && !state.SupportedVideoCodecs.Contains(videoStream.Codec, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var requestedProfiles = state.GetRequestedProfiles(videoStream.Codec);

            // If client is requesting a specific video profile, it must match the source
            if (requestedProfiles.Length > 0)
            {
                if (string.IsNullOrEmpty(videoStream.Profile))
                {
                    // return false;
                }

                var requestedProfile = requestedProfiles[0];
                // strip spaces because they may be stripped out on the query string as well
                if (!string.IsNullOrEmpty(videoStream.Profile)
                    && !requestedProfiles.Contains(videoStream.Profile.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase))
                {
                    var currentScore = GetVideoProfileScore(videoStream.Codec, videoStream.Profile);
                    var requestedScore = GetVideoProfileScore(videoStream.Codec, requestedProfile);

                    if (currentScore == -1 || currentScore > requestedScore)
                    {
                        return false;
                    }
                }
            }

            var requestedRangeTypes = state.GetRequestedRangeTypes(videoStream.Codec);
            if (requestedRangeTypes.Length > 0)
            {
                if (videoStream.VideoRangeType == VideoRangeType.Unknown)
                {
                    return false;
                }

                if (!requestedRangeTypes.Contains(videoStream.VideoRangeType.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Video width must fall within requested value
            if (request.MaxWidth.HasValue
                && (!videoStream.Width.HasValue || videoStream.Width.Value > request.MaxWidth.Value))
            {
                return false;
            }

            // Video height must fall within requested value
            if (request.MaxHeight.HasValue
                && (!videoStream.Height.HasValue || videoStream.Height.Value > request.MaxHeight.Value))
            {
                return false;
            }

            // Video framerate must fall within requested value
            var requestedFramerate = request.MaxFramerate ?? request.Framerate;
            if (requestedFramerate.HasValue)
            {
                var videoFrameRate = videoStream.AverageFrameRate ?? videoStream.RealFrameRate;

                if (!videoFrameRate.HasValue || videoFrameRate.Value > requestedFramerate.Value)
                {
                    return false;
                }
            }

            // Video bitrate must fall within requested value
            if (request.VideoBitRate.HasValue
                && (!videoStream.BitRate.HasValue || videoStream.BitRate.Value > request.VideoBitRate.Value))
            {
                return false;
            }

            var maxBitDepth = state.GetRequestedVideoBitDepth(videoStream.Codec);
            if (maxBitDepth.HasValue)
            {
                if (videoStream.BitDepth.HasValue && videoStream.BitDepth.Value > maxBitDepth.Value)
                {
                    return false;
                }
            }

            var maxRefFrames = state.GetRequestedMaxRefFrames(videoStream.Codec);
            if (maxRefFrames.HasValue
                && videoStream.RefFrames.HasValue && videoStream.RefFrames.Value > maxRefFrames.Value)
            {
                return false;
            }

            // If a specific level was requested, the source must match or be less than
            var level = state.GetRequestedLevel(videoStream.Codec);
            if (double.TryParse(level, CultureInfo.InvariantCulture, out var requestLevel))
            {
                if (!videoStream.Level.HasValue)
                {
                    // return false;
                }

                if (videoStream.Level.HasValue && videoStream.Level.Value > requestLevel)
                {
                    return false;
                }
            }

            if (string.Equals(state.InputContainer, "avi", StringComparison.OrdinalIgnoreCase)
                && string.Equals(videoStream.Codec, "h264", StringComparison.OrdinalIgnoreCase)
                && !(videoStream.IsAVC ?? false))
            {
                // see Coach S01E01 - Kelly and the Professor(0).avi
                return false;
            }

            return true;
        }

        public bool CanStreamCopyAudio(EncodingJobInfo state, MediaStream audioStream, IEnumerable<string> supportedAudioCodecs)
        {
            var request = state.BaseRequest;

            if (!request.AllowAudioStreamCopy)
            {
                return false;
            }

            var maxBitDepth = state.GetRequestedAudioBitDepth(audioStream.Codec);
            if (maxBitDepth.HasValue
                && audioStream.BitDepth.HasValue
                && audioStream.BitDepth.Value > maxBitDepth.Value)
            {
                return false;
            }

            // Source and target codecs must match
            if (string.IsNullOrEmpty(audioStream.Codec)
                || !supportedAudioCodecs.Contains(audioStream.Codec, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Channels must fall within requested value
            var channels = state.GetRequestedAudioChannels(audioStream.Codec);
            if (channels.HasValue)
            {
                if (!audioStream.Channels.HasValue || audioStream.Channels.Value <= 0)
                {
                    return false;
                }

                if (audioStream.Channels.Value > channels.Value)
                {
                    return false;
                }
            }

            // Sample rate must fall within requested value
            if (request.AudioSampleRate.HasValue)
            {
                if (!audioStream.SampleRate.HasValue || audioStream.SampleRate.Value <= 0)
                {
                    return false;
                }

                if (audioStream.SampleRate.Value > request.AudioSampleRate.Value)
                {
                    return false;
                }
            }

            // Audio bitrate must fall within requested value
            if (request.AudioBitRate.HasValue
                && audioStream.BitRate.HasValue
                && audioStream.BitRate.Value > request.AudioBitRate.Value)
            {
                return false;
            }

            return request.EnableAutoStreamCopy;
        }

        public int GetVideoBitrateParamValue(BaseEncodingJobOptions request, MediaStream videoStream, string outputVideoCodec)
        {
            var bitrate = request.VideoBitRate;

            if (videoStream is not null)
            {
                var isUpscaling = request.Height.HasValue
                    && videoStream.Height.HasValue
                    && request.Height.Value > videoStream.Height.Value
                    && request.Width.HasValue
                    && videoStream.Width.HasValue
                    && request.Width.Value > videoStream.Width.Value;

                // Don't allow bitrate increases unless upscaling
                if (!isUpscaling && bitrate.HasValue && videoStream.BitRate.HasValue)
                {
                    bitrate = GetMinBitrate(videoStream.BitRate.Value, bitrate.Value);
                }

                if (bitrate.HasValue)
                {
                    var inputVideoCodec = videoStream.Codec;
                    bitrate = ScaleBitrate(bitrate.Value, inputVideoCodec, outputVideoCodec);

                    // If a max bitrate was requested, don't let the scaled bitrate exceed it
                    if (request.VideoBitRate.HasValue)
                    {
                        bitrate = Math.Min(bitrate.Value, request.VideoBitRate.Value);
                    }
                }
            }

            // Cap the max target bitrate to intMax/2 to satisfy the bufsize=bitrate*2.
            return Math.Min(bitrate ?? 0, int.MaxValue / 2);
        }

        private int GetMinBitrate(int sourceBitrate, int requestedBitrate)
        {
            // these values were chosen from testing to improve low bitrate streams
            if (sourceBitrate <= 2000000)
            {
                sourceBitrate = Convert.ToInt32(sourceBitrate * 2.5);
            }
            else if (sourceBitrate <= 3000000)
            {
                sourceBitrate *= 2;
            }

            var bitrate = Math.Min(sourceBitrate, requestedBitrate);

            return bitrate;
        }

        private static double GetVideoBitrateScaleFactor(string codec)
        {
            // hevc & vp9 - 40% more efficient than h.264
            if (string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "vp9", StringComparison.OrdinalIgnoreCase))
            {
                return .6;
            }

            // av1 - 50% more efficient than h.264
            if (string.Equals(codec, "av1", StringComparison.OrdinalIgnoreCase))
            {
                return .5;
            }

            return 1;
        }

        private static int ScaleBitrate(int bitrate, string inputVideoCodec, string outputVideoCodec)
        {
            var inputScaleFactor = GetVideoBitrateScaleFactor(inputVideoCodec);
            var outputScaleFactor = GetVideoBitrateScaleFactor(outputVideoCodec);

            // Don't scale the real bitrate lower than the requested bitrate
            var scaleFactor = Math.Max(outputScaleFactor / inputScaleFactor, 1);

            if (bitrate <= 500000)
            {
                scaleFactor = Math.Max(scaleFactor, 4);
            }
            else if (bitrate <= 1000000)
            {
                scaleFactor = Math.Max(scaleFactor, 3);
            }
            else if (bitrate <= 2000000)
            {
                scaleFactor = Math.Max(scaleFactor, 2.5);
            }
            else if (bitrate <= 3000000)
            {
                scaleFactor = Math.Max(scaleFactor, 2);
            }

            return Convert.ToInt32(scaleFactor * bitrate);
        }

        public int? GetAudioBitrateParam(BaseEncodingJobOptions request, MediaStream audioStream, int? outputAudioChannels)
        {
            return GetAudioBitrateParam(request.AudioBitRate, request.AudioCodec, audioStream, outputAudioChannels);
        }

        public int? GetAudioBitrateParam(int? audioBitRate, string audioCodec, MediaStream audioStream, int? outputAudioChannels)
        {
            if (audioStream is null)
            {
                return null;
            }

            var inputChannels = audioStream.Channels ?? 0;
            var outputChannels = outputAudioChannels ?? 0;
            var bitrate = audioBitRate ?? int.MaxValue;

            if (string.IsNullOrEmpty(audioCodec)
                || string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase)
                || string.Equals(audioCodec, "mp3", StringComparison.OrdinalIgnoreCase)
                || string.Equals(audioCodec, "opus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(audioCodec, "vorbis", StringComparison.OrdinalIgnoreCase)
                || string.Equals(audioCodec, "ac3", StringComparison.OrdinalIgnoreCase)
                || string.Equals(audioCodec, "eac3", StringComparison.OrdinalIgnoreCase))
            {
                return (inputChannels, outputChannels) switch
                {
                    (>= 6, >= 6 or 0) => Math.Min(640000, bitrate),
                    (> 0, > 0) => Math.Min(outputChannels * 128000, bitrate),
                    (> 0, _) => Math.Min(inputChannels * 128000, bitrate),
                    (_, _) => Math.Min(384000, bitrate)
                };
            }

            if (string.Equals(audioCodec, "dts", StringComparison.OrdinalIgnoreCase)
                || string.Equals(audioCodec, "dca", StringComparison.OrdinalIgnoreCase))
            {
                return (inputChannels, outputChannels) switch
                {
                    (>= 6, >= 6 or 0) => Math.Min(768000, bitrate),
                    (> 0, > 0) => Math.Min(outputChannels * 136000, bitrate),
                    (> 0, _) => Math.Min(inputChannels * 136000, bitrate),
                    (_, _) => Math.Min(672000, bitrate)
                };
            }

            // Empty bitrate area is not allow on iOS
            // Default audio bitrate to 128K per channel if we don't have codec specific defaults
            // https://ffmpeg.org/ffmpeg-codecs.html#toc-Codec-Options
            return 128000 * (outputAudioChannels ?? audioStream.Channels ?? 2);
        }

        public string GetAudioVbrModeParam(string encoder, int bitratePerChannel)
        {
            if (string.Equals(encoder, "libfdk_aac", StringComparison.OrdinalIgnoreCase))
            {
                return " -vbr:a " + bitratePerChannel switch
                {
                    < 32000 => "1",
                    < 48000 => "2",
                    < 64000 => "3",
                    < 96000 => "4",
                    _ => "5"
                };
            }

            if (string.Equals(encoder, "libmp3lame", StringComparison.OrdinalIgnoreCase))
            {
                return " -qscale:a " + bitratePerChannel switch
                {
                    < 48000 => "8",
                    < 64000 => "6",
                    < 88000 => "4",
                    < 112000 => "2",
                    _ => "0"
                };
            }

            if (string.Equals(encoder, "libvorbis", StringComparison.OrdinalIgnoreCase))
            {
                return " -qscale:a " + bitratePerChannel switch
                {
                    < 40000 => "0",
                    < 56000 => "2",
                    < 80000 => "4",
                    < 112000 => "6",
                    _ => "8"
                };
            }

            return null;
        }

        public string GetAudioFilterParam(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var channels = state.OutputAudioChannels;

            var filters = new List<string>();

            if (channels.HasValue
                && channels.Value == 2
                && state.AudioStream is not null
                && state.AudioStream.Channels.HasValue
                && state.AudioStream.Channels.Value > 5)
            {
                switch (encodingOptions.DownMixStereoAlgorithm)
                {
                    case DownMixStereoAlgorithms.Dave750:
                        filters.Add("volume=4.25");
                        filters.Add("pan=stereo|c0=0.5*c2+0.707*c0+0.707*c4+0.5*c3|c1=0.5*c2+0.707*c1+0.707*c5+0.5*c3");
                        break;
                    case DownMixStereoAlgorithms.NightmodeDialogue:
                        filters.Add("pan=stereo|c0=c2+0.30*c0+0.30*c4|c1=c2+0.30*c1+0.30*c5");
                        break;
                    case DownMixStereoAlgorithms.None:
                    default:
                        if (!encodingOptions.DownMixAudioBoost.Equals(1))
                        {
                            filters.Add("volume=" + encodingOptions.DownMixAudioBoost.ToString(CultureInfo.InvariantCulture));
                        }

                        break;
                }
            }

            var isCopyingTimestamps = state.CopyTimestamps || state.TranscodingType != TranscodingJobType.Progressive;
            if (state.SubtitleStream is not null && state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode && !isCopyingTimestamps)
            {
                var seconds = TimeSpan.FromTicks(state.StartTimeTicks ?? 0).TotalSeconds;

                filters.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "asetpts=PTS-{0}/TB",
                        Math.Round(seconds)));
            }

            if (filters.Count > 0)
            {
                return " -af \"" + string.Join(',', filters) + "\"";
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="outputAudioCodec">The output audio codec.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        public int? GetNumAudioChannelsParam(EncodingJobInfo state, MediaStream audioStream, string outputAudioCodec)
        {
            if (audioStream is null)
            {
                return null;
            }

            var request = state.BaseRequest;

            var codec = outputAudioCodec ?? string.Empty;

            int? resultChannels = state.GetRequestedAudioChannels(codec);

            var inputChannels = audioStream.Channels;

            if (inputChannels > 0)
            {
                resultChannels = inputChannels < resultChannels ? inputChannels : resultChannels ?? inputChannels;
            }

            var isTranscodingAudio = !IsCopyCodec(codec);

            if (isTranscodingAudio)
            {
                var audioEncoder = GetAudioEncoder(state);
                if (!_audioTranscodeChannelLookup.TryGetValue(audioEncoder, out var transcoderChannelLimit))
                {
                    // Set default max transcoding channels to 8 to prevent encoding errors due to asking for too many channels.
                    transcoderChannelLimit = 8;
                }

                // Set resultChannels to minimum between resultChannels, TranscodingMaxAudioChannels, transcoderChannelLimit
                resultChannels = transcoderChannelLimit < resultChannels ? transcoderChannelLimit : resultChannels ?? transcoderChannelLimit;

                if (request.TranscodingMaxAudioChannels < resultChannels)
                {
                    resultChannels = request.TranscodingMaxAudioChannels;
                }

                // Avoid transcoding to audio channels other than 1ch, 2ch, 6ch (5.1 layout) and 8ch (7.1 layout).
                // https://developer.apple.com/documentation/http_live_streaming/hls_authoring_specification_for_apple_devices
                if (state.TranscodingType != TranscodingJobType.Progressive
                    && ((resultChannels > 2 && resultChannels < 6) || resultChannels == 7))
                {
                    resultChannels = 2;
                }
            }

            return resultChannels;
        }

        /// <summary>
        /// Enforces the resolution limit.
        /// </summary>
        /// <param name="state">The state.</param>
        public void EnforceResolutionLimit(EncodingJobInfo state)
        {
            var videoRequest = state.BaseRequest;

            // Switch the incoming params to be ceilings rather than fixed values
            videoRequest.MaxWidth = videoRequest.MaxWidth ?? videoRequest.Width;
            videoRequest.MaxHeight = videoRequest.MaxHeight ?? videoRequest.Height;

            videoRequest.Width = null;
            videoRequest.Height = null;
        }

        /// <summary>
        /// Gets the fast seek command line parameter.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="options">The options.</param>
        /// <param name="segmentContainer">Segment Container.</param>
        /// <returns>System.String.</returns>
        /// <value>The fast seek command line parameter.</value>
        public string GetFastSeekCommandLineParameter(EncodingJobInfo state, EncodingOptions options, string segmentContainer)
        {
            var time = state.BaseRequest.StartTimeTicks ?? 0;
            var seekParam = string.Empty;

            if (time > 0)
            {
                seekParam += string.Format(CultureInfo.InvariantCulture, "-ss {0}", _mediaEncoder.GetTimeParameter(time));

                if (state.IsVideoRequest)
                {
                    var outputVideoCodec = GetVideoEncoder(state, options);
                    var segmentFormat = GetSegmentFileExtension(segmentContainer).TrimStart('.');

                    // Important: If this is ever re-enabled, make sure not to use it with wtv because it breaks seeking
                    // Disable -noaccurate_seek on mpegts container due to the timestamps issue on some clients,
                    // but it's still required for fMP4 container otherwise the audio can't be synced to the video.
                    if (!string.Equals(state.InputContainer, "wtv", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(segmentFormat, "ts", StringComparison.OrdinalIgnoreCase)
                        && state.TranscodingType != TranscodingJobType.Progressive
                        && !state.EnableBreakOnNonKeyFrames(outputVideoCodec)
                        && (state.BaseRequest.StartTimeTicks ?? 0) > 0)
                    {
                        seekParam += " -noaccurate_seek";
                    }
                }
            }

            return seekParam;
        }

        /// <summary>
        /// Gets the map args.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        public string GetMapArgs(EncodingJobInfo state)
        {
            // If we don't have known media info
            // If input is video, use -sn to drop subtitles
            // Otherwise just return empty
            if (state.VideoStream is null && state.AudioStream is null)
            {
                return state.IsInputVideo ? "-sn" : string.Empty;
            }

            // We have media info, but we don't know the stream index
            if (state.VideoStream is not null && state.VideoStream.Index == -1)
            {
                return "-sn";
            }

            // We have media info, but we don't know the stream index
            if (state.AudioStream is not null && state.AudioStream.Index == -1)
            {
                return state.IsInputVideo ? "-sn" : string.Empty;
            }

            var args = string.Empty;

            if (state.VideoStream is not null)
            {
                int videoStreamIndex = FindIndex(state.MediaSource.MediaStreams, state.VideoStream);

                args += string.Format(
                    CultureInfo.InvariantCulture,
                    "-map 0:{0}",
                    videoStreamIndex);
            }
            else
            {
                // No known video stream
                args += "-vn";
            }

            if (state.AudioStream is not null)
            {
                int audioStreamIndex = FindIndex(state.MediaSource.MediaStreams, state.AudioStream);
                if (state.AudioStream.IsExternal)
                {
                    bool hasExternalGraphicsSubs = state.SubtitleStream is not null
                        && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode
                        && state.SubtitleStream.IsExternal
                        && !state.SubtitleStream.IsTextSubtitleStream;
                    int externalAudioMapIndex = hasExternalGraphicsSubs ? 2 : 1;

                    args += string.Format(
                        CultureInfo.InvariantCulture,
                        " -map {0}:{1}",
                        externalAudioMapIndex,
                        audioStreamIndex);
                }
                else
                {
                    args += string.Format(
                        CultureInfo.InvariantCulture,
                        " -map 0:{0}",
                        audioStreamIndex);
                }
            }
            else
            {
                args += " -map -0:a";
            }

            var subtitleMethod = state.SubtitleDeliveryMethod;
            if (state.SubtitleStream is null || subtitleMethod == SubtitleDeliveryMethod.Hls)
            {
                args += " -map -0:s";
            }
            else if (subtitleMethod == SubtitleDeliveryMethod.Embed)
            {
                int subtitleStreamIndex = FindIndex(state.MediaSource.MediaStreams, state.SubtitleStream);

                args += string.Format(
                    CultureInfo.InvariantCulture,
                    " -map 0:{0}",
                    subtitleStreamIndex);
            }
            else if (state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream)
            {
                int externalSubtitleStreamIndex = FindIndex(state.MediaSource.MediaStreams, state.SubtitleStream);

                args += string.Format(
                    CultureInfo.InvariantCulture,
                    " -map 1:{0} -sn",
                    externalSubtitleStreamIndex);
            }

            return args;
        }

        /// <summary>
        /// Gets the negative map args by filters.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoProcessFilters">The videoProcessFilters.</param>
        /// <returns>System.String.</returns>
        public string GetNegativeMapArgsByFilters(EncodingJobInfo state, string videoProcessFilters)
        {
            string args = string.Empty;

            // http://ffmpeg.org/ffmpeg-all.html#toc-Complex-filtergraphs-1
            if (state.VideoStream is not null && videoProcessFilters.Contains("-filter_complex", StringComparison.Ordinal))
            {
                int videoStreamIndex = FindIndex(state.MediaSource.MediaStreams, state.VideoStream);

                args += string.Format(
                    CultureInfo.InvariantCulture,
                    "-map -0:{0} ",
                    videoStreamIndex);
            }

            return args;
        }

        /// <summary>
        /// Determines which stream will be used for playback.
        /// </summary>
        /// <param name="allStream">All stream.</param>
        /// <param name="desiredIndex">Index of the desired.</param>
        /// <param name="type">The type.</param>
        /// <param name="returnFirstIfNoIndex">if set to <c>true</c> [return first if no index].</param>
        /// <returns>MediaStream.</returns>
        public MediaStream GetMediaStream(IEnumerable<MediaStream> allStream, int? desiredIndex, MediaStreamType type, bool returnFirstIfNoIndex = true)
        {
            var streams = allStream.Where(s => s.Type == type).OrderBy(i => i.Index).ToList();

            if (desiredIndex.HasValue)
            {
                var stream = streams.FirstOrDefault(s => s.Index == desiredIndex.Value);

                if (stream is not null)
                {
                    return stream;
                }
            }

            if (returnFirstIfNoIndex && type == MediaStreamType.Audio)
            {
                return streams.FirstOrDefault(i => i.Channels.HasValue && i.Channels.Value > 0) ??
                       streams.FirstOrDefault();
            }

            // Just return the first one
            return returnFirstIfNoIndex ? streams.FirstOrDefault() : null;
        }

        public static (int? Width, int? Height) GetFixedOutputSize(
            int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight)
        {
            if (!videoWidth.HasValue && !requestedWidth.HasValue)
            {
                return (null, null);
            }

            if (!videoHeight.HasValue && !requestedHeight.HasValue)
            {
                return (null, null);
            }

            int inputWidth = Convert.ToInt32(videoWidth ?? requestedWidth, CultureInfo.InvariantCulture);
            int inputHeight = Convert.ToInt32(videoHeight ?? requestedHeight, CultureInfo.InvariantCulture);
            int outputWidth = requestedWidth ?? inputWidth;
            int outputHeight = requestedHeight ?? inputHeight;

            // Don't transcode video to bigger than 4k when using HW.
            int maximumWidth = Math.Min(requestedMaxWidth ?? outputWidth, 4096);
            int maximumHeight = Math.Min(requestedMaxHeight ?? outputHeight, 4096);

            if (outputWidth > maximumWidth || outputHeight > maximumHeight)
            {
                var scaleW = (double)maximumWidth / outputWidth;
                var scaleH = (double)maximumHeight / outputHeight;
                var scale = Math.Min(scaleW, scaleH);
                outputWidth = Math.Min(maximumWidth, (int)(outputWidth * scale));
                outputHeight = Math.Min(maximumHeight, (int)(outputHeight * scale));
            }

            outputWidth = 2 * (outputWidth / 2);
            outputHeight = 2 * (outputHeight / 2);

            return (outputWidth, outputHeight);
        }

        public static string GetHwScaleFilter(
            string hwScaleSuffix,
            string videoFormat,
            int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight)
        {
            var (outWidth, outHeight) = GetFixedOutputSize(
                videoWidth,
                videoHeight,
                requestedWidth,
                requestedHeight,
                requestedMaxWidth,
                requestedMaxHeight);

            var isFormatFixed = !string.IsNullOrEmpty(videoFormat);
            var isSizeFixed = !videoWidth.HasValue
                || outWidth.Value != videoWidth.Value
                || !videoHeight.HasValue
                || outHeight.Value != videoHeight.Value;

            var arg1 = isSizeFixed ? ("=w=" + outWidth.Value + ":h=" + outHeight.Value) : string.Empty;
            var arg2 = isFormatFixed ? ("format=" + videoFormat) : string.Empty;
            if (isFormatFixed)
            {
                arg2 = (isSizeFixed ? ':' : '=') + arg2;
            }

            if (!string.IsNullOrEmpty(hwScaleSuffix) && (isSizeFixed || isFormatFixed))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "scale_{0}{1}{2}",
                    hwScaleSuffix,
                    arg1,
                    arg2);
            }

            return string.Empty;
        }

        public static string GetCustomSwScaleFilter(
            int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight)
        {
            var (outWidth, outHeight) = GetFixedOutputSize(
                videoWidth,
                videoHeight,
                requestedWidth,
                requestedHeight,
                requestedMaxWidth,
                requestedMaxHeight);

            if (outWidth.HasValue && outHeight.HasValue)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "scale=s={0}x{1}:flags=fast_bilinear",
                    outWidth.Value,
                    outHeight.Value);
            }

            return string.Empty;
        }

        public static string GetAlphaSrcFilter(
            EncodingJobInfo state,
            int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight,
            int? framerate)
        {
            var reqTicks = state.BaseRequest.StartTimeTicks ?? 0;
            var startTime = TimeSpan.FromTicks(reqTicks).ToString(@"hh\\\:mm\\\:ss\\\.fff", CultureInfo.InvariantCulture);
            var (outWidth, outHeight) = GetFixedOutputSize(
                videoWidth,
                videoHeight,
                requestedWidth,
                requestedHeight,
                requestedMaxWidth,
                requestedMaxHeight);

            if (outWidth.HasValue && outHeight.HasValue)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "alphasrc=s={0}x{1}:r={2}:start='{3}'",
                    outWidth.Value,
                    outHeight.Value,
                    framerate ?? 10,
                    reqTicks > 0 ? startTime : 0);
            }

            return string.Empty;
        }

        public static string GetSwScaleFilter(
            EncodingJobInfo state,
            EncodingOptions options,
            string videoEncoder,
            int? videoWidth,
            int? videoHeight,
            Video3DFormat? threedFormat,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight)
        {
            var isV4l2 = string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase);
            var scaleVal = isV4l2 ? 64 : 2;

            // If fixed dimensions were supplied
            if (requestedWidth.HasValue && requestedHeight.HasValue)
            {
                if (isV4l2)
                {
                    var widthParam = requestedWidth.Value.ToString(CultureInfo.InvariantCulture);
                    var heightParam = requestedHeight.Value.ToString(CultureInfo.InvariantCulture);

                    return string.Format(
                            CultureInfo.InvariantCulture,
                            "scale=trunc({0}/64)*64:trunc({1}/2)*2",
                            widthParam,
                            heightParam);
                }

                return GetFixedSwScaleFilter(threedFormat, requestedWidth.Value, requestedHeight.Value);
            }

            // If Max dimensions were supplied, for width selects lowest even number between input width and width req size and selects lowest even number from in width*display aspect and requested size

            if (requestedMaxWidth.HasValue && requestedMaxHeight.HasValue)
            {
                var maxWidthParam = requestedMaxWidth.Value.ToString(CultureInfo.InvariantCulture);
                var maxHeightParam = requestedMaxHeight.Value.ToString(CultureInfo.InvariantCulture);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "scale=trunc(min(max(iw\\,ih*a)\\,min({0}\\,{1}*a))/{2})*{2}:trunc(min(max(iw/a\\,ih)\\,min({0}/a\\,{1}))/2)*2",
                    maxWidthParam,
                    maxHeightParam,
                    scaleVal);
            }

            // If a fixed width was requested
            if (requestedWidth.HasValue)
            {
                if (threedFormat.HasValue)
                {
                    // This method can handle 0 being passed in for the requested height
                    return GetFixedSwScaleFilter(threedFormat, requestedWidth.Value, 0);
                }

                var widthParam = requestedWidth.Value.ToString(CultureInfo.InvariantCulture);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "scale={0}:trunc(ow/a/2)*2",
                    widthParam);
            }

            // If a fixed height was requested
            if (requestedHeight.HasValue)
            {
                var heightParam = requestedHeight.Value.ToString(CultureInfo.InvariantCulture);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "scale=trunc(oh*a/{1})*{1}:{0}",
                    heightParam,
                    scaleVal);
            }

            // If a max width was requested
            if (requestedMaxWidth.HasValue)
            {
                var maxWidthParam = requestedMaxWidth.Value.ToString(CultureInfo.InvariantCulture);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "scale=trunc(min(max(iw\\,ih*a)\\,{0})/{1})*{1}:trunc(ow/a/2)*2",
                    maxWidthParam,
                    scaleVal);
            }

            // If a max height was requested
            if (requestedMaxHeight.HasValue)
            {
                var maxHeightParam = requestedMaxHeight.Value.ToString(CultureInfo.InvariantCulture);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "scale=trunc(oh*a/{1})*{1}:min(max(iw/a\\,ih)\\,{0})",
                    maxHeightParam,
                    scaleVal);
            }

            return string.Empty;
        }

        private static string GetFixedSwScaleFilter(Video3DFormat? threedFormat, int requestedWidth, int requestedHeight)
        {
            var widthParam = requestedWidth.ToString(CultureInfo.InvariantCulture);
            var heightParam = requestedHeight.ToString(CultureInfo.InvariantCulture);

            string filter = null;

            if (threedFormat.HasValue)
            {
                switch (threedFormat.Value)
                {
                    case Video3DFormat.HalfSideBySide:
                        filter = "crop=iw/2:ih:0:0,scale=(iw*2):ih,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
                        // hsbs crop width in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to requestedWidth. Work out the correct height based on the display aspect it will maintain the aspect where -1 in this case (3d) may not.
                        break;
                    case Video3DFormat.FullSideBySide:
                        filter = "crop=iw/2:ih:0:0,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
                        // fsbs crop width in half,set the display aspect,crop out any black bars we may have made the scale width to requestedWidth.
                        break;
                    case Video3DFormat.HalfTopAndBottom:
                        filter = "crop=iw:ih/2:0:0,scale=(iw*2):ih),setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
                        // htab crop height in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to requestedWidth
                        break;
                    case Video3DFormat.FullTopAndBottom:
                        filter = "crop=iw:ih/2:0:0,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
                        // ftab crop height in half, set the display aspect,crop out any black bars we may have made the scale width to requestedWidth
                        break;
                    default:
                        break;
                }
            }

            // default
            if (filter is null)
            {
                if (requestedHeight > 0)
                {
                    filter = "scale=trunc({0}/2)*2:trunc({1}/2)*2";
                }
                else
                {
                    filter = "scale={0}:trunc({0}/a/2)*2";
                }
            }

            return string.Format(CultureInfo.InvariantCulture, filter, widthParam, heightParam);
        }

        public static string GetSwDeinterlaceFilter(EncodingJobInfo state, EncodingOptions options)
        {
            var doubleRateDeint = options.DeinterlaceDoubleRate && state.VideoStream?.AverageFrameRate <= 30;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}:-1:0",
                string.Equals(options.DeinterlaceMethod, "bwdif", StringComparison.OrdinalIgnoreCase) ? "bwdif" : "yadif",
                doubleRateDeint ? "1" : "0");
        }

        public static string GetHwDeinterlaceFilter(EncodingJobInfo state, EncodingOptions options, string hwDeintSuffix)
        {
            var doubleRateDeint = options.DeinterlaceDoubleRate && (state.VideoStream?.AverageFrameRate ?? 60) <= 30;
            if (hwDeintSuffix.Contains("cuda", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "yadif_cuda={0}:-1:0",
                    doubleRateDeint ? "1" : "0");
            }

            if (hwDeintSuffix.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "deinterlace_vaapi=rate={0}",
                    doubleRateDeint ? "field" : "frame");
            }

            if (hwDeintSuffix.Contains("qsv", StringComparison.OrdinalIgnoreCase))
            {
                return "deinterlace_qsv=mode=2";
            }

            if (hwDeintSuffix.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "yadif_videotoolbox={0}:-1:0",
                    doubleRateDeint ? "1" : "0");
            }

            return string.Empty;
        }

        public string GetHwTonemapFilter(EncodingOptions options, string hwTonemapSuffix, string videoFormat)
        {
            if (string.IsNullOrEmpty(hwTonemapSuffix))
            {
                return string.Empty;
            }

            var args = string.Empty;
            var algorithm = options.TonemappingAlgorithm;

            if (string.Equals(hwTonemapSuffix, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                args = "procamp_vaapi=b={1}:c={2},tonemap_vaapi=format={0}:p=bt709:t=bt709:m=bt709:extra_hw_frames=32";

                return string.Format(
                        CultureInfo.InvariantCulture,
                        args,
                        videoFormat ?? "nv12",
                        options.VppTonemappingBrightness,
                        options.VppTonemappingContrast);
            }
            else
            {
                args = "tonemap_{0}=format={1}:p=bt709:t=bt709:m=bt709:tonemap={2}:peak={3}:desat={4}";

                if (string.Equals(options.TonemappingMode, "max", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(options.TonemappingMode, "rgb", StringComparison.OrdinalIgnoreCase))
                {
                    if (_mediaEncoder.EncoderVersion >= _minFFmpegOclCuTonemapMode)
                    {
                        args += ":tonemap_mode={5}";
                    }
                }

                if (options.TonemappingParam != 0)
                {
                    args += ":param={6}";
                }

                if (string.Equals(options.TonemappingRange, "tv", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(options.TonemappingRange, "pc", StringComparison.OrdinalIgnoreCase))
                {
                    args += ":range={7}";
                }
            }

            return string.Format(
                    CultureInfo.InvariantCulture,
                    args,
                    hwTonemapSuffix,
                    videoFormat ?? "nv12",
                    algorithm,
                    options.TonemappingPeak,
                    options.TonemappingDesat,
                    options.TonemappingMode,
                    options.TonemappingParam,
                    options.TonemappingRange);
        }

        public string GetLibplaceboFilter(
            EncodingOptions options,
            string videoFormat,
            bool doTonemap,
            int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight)
        {
            var (outWidth, outHeight) = GetFixedOutputSize(
                videoWidth,
                videoHeight,
                requestedWidth,
                requestedHeight,
                requestedMaxWidth,
                requestedMaxHeight);

            var isFormatFixed = !string.IsNullOrEmpty(videoFormat);
            var isSizeFixed = !videoWidth.HasValue
                || outWidth.Value != videoWidth.Value
                || !videoHeight.HasValue
                || outHeight.Value != videoHeight.Value;

            var sizeArg = isSizeFixed ? (":w=" + outWidth.Value + ":h=" + outHeight.Value) : string.Empty;
            var formatArg = isFormatFixed ? (":format=" + videoFormat) : string.Empty;
            var tonemapArg = string.Empty;

            if (doTonemap)
            {
                var algorithm = options.TonemappingAlgorithm;
                var mode = options.TonemappingMode;
                var range = options.TonemappingRange;

                if (string.Equals(algorithm, "bt2390", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm = "bt.2390";
                }
                else if (string.Equals(algorithm, "none", StringComparison.OrdinalIgnoreCase))
                {
                    algorithm = "clip";
                }

                tonemapArg = ":tonemapping=" + algorithm;

                if (string.Equals(mode, "max", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(mode, "rgb", StringComparison.OrdinalIgnoreCase))
                {
                    tonemapArg += ":tonemapping_mode=" + mode;
                }

                tonemapArg += ":peak_detect=0:color_primaries=bt709:color_trc=bt709:colorspace=bt709";

                if (string.Equals(range, "tv", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(range, "pc", StringComparison.OrdinalIgnoreCase))
                {
                    tonemapArg += ":range=" + range;
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "libplacebo=upscaler=none:downscaler=none{0}{1}{2}",
                sizeArg,
                formatArg,
                tonemapArg);
        }

        /// <summary>
        /// Gets the parameter of software filter chain.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="vidEncoder">Video encoder to use.</param>
        /// <returns>The tuple contains three lists: main, sub and overlay filters.</returns>
        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetSwVidFilterChain(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidEncoder)
        {
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isVaapiEncoder = vidEncoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isV4l2Encoder = vidEncoder.Contains("h264_v4l2m2m", StringComparison.OrdinalIgnoreCase);

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, false));

            // INPUT sw surface(memory/copy-back from vram)
            // sw deint
            if (doDeintH2645)
            {
                var deintFilter = GetSwDeinterlaceFilter(state, options);
                mainFilters.Add(deintFilter);
            }

            var outFormat = isSwDecoder ? "yuv420p" : "nv12";
            var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
            if (isVaapiEncoder)
            {
                outFormat = "nv12";
            }
            else if (isV4l2Encoder)
            {
                outFormat = "yuv420p";
            }

            // sw scale
            mainFilters.Add(swScaleFilter);
            mainFilters.Add("format=" + outFormat);

            // sw tonemap <= TODO: finsh the fast tonemap filter

            // OUTPUT yuv420p/nv12 surface(memory)

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (hasTextSubs)
            {
                // subtitles=f='*.ass':alpha=0
                var textSubtitlesFilter = GetTextSubtitlesFilter(state, false, false);
                mainFilters.Add(textSubtitlesFilter);
            }
            else if (hasGraphicalSubs)
            {
                // [0:s]scale=s=1280x720
                var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                subFilters.Add(subSwScaleFilter);
                overlayFilters.Add("overlay=eof_action=endall:shortest=1:repeatlast=0");
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        /// <summary>
        /// Gets the parameter of Nvidia NVENC filter chain.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="vidEncoder">Video encoder to use.</param>
        /// <returns>The tuple contains three lists: main, sub and overlay filters.</returns>
        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetNvidiaVidFilterChain(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidEncoder)
        {
            if (!string.Equals(options.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null, null);
            }

            var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !vidEncoder.Contains("nvenc", StringComparison.OrdinalIgnoreCase);

            // legacy cuvid pipeline(copy-back)
            if ((isSwDecoder && isSwEncoder)
                || !IsCudaFullSupported()
                || !_mediaEncoder.SupportsFilter("alphasrc"))
            {
                return GetSwVidFilterChain(state, options, vidEncoder);
            }

            // prefered nvdec/cuvid + cuda filters + nvenc pipeline
            return GetNvidiaVidFiltersPrefered(state, options, vidDecoder, vidEncoder);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetNvidiaVidFiltersPrefered(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidDecoder,
            string vidEncoder)
        {
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var isNvDecoder = vidDecoder.Contains("cuda", StringComparison.OrdinalIgnoreCase);
            var isNvencEncoder = vidEncoder.Contains("nvenc", StringComparison.OrdinalIgnoreCase);
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !isNvencEncoder;
            var isCuInCuOut = isNvDecoder && isNvencEncoder;

            var doubleRateDeint = options.DeinterlaceDoubleRate && (state.VideoStream?.AverageFrameRate ?? 60) <= 30;
            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doCuTonemap = IsHwTonemapAvailable(state, options);

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, doCuTonemap));

            if (isSwDecoder)
            {
                // INPUT sw surface(memory)
                // sw deint
                if (doDeintH2645)
                {
                    var swDeintFilter = GetSwDeinterlaceFilter(state, options);
                    mainFilters.Add(swDeintFilter);
                }

                var outFormat = doCuTonemap ? "yuv420p10le" : "yuv420p";
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add("format=" + outFormat);

                // sw => hw
                if (doCuTonemap)
                {
                    mainFilters.Add("hwupload=derive_device=cuda");
                }
            }

            if (isNvDecoder)
            {
                // INPUT cuda surface(vram)
                // hw deint
                if (doDeintH2645)
                {
                    var deintFilter = GetHwDeinterlaceFilter(state, options, "cuda");
                    mainFilters.Add(deintFilter);
                }

                var outFormat = doCuTonemap ? string.Empty : "yuv420p";
                var hwScaleFilter = GetHwScaleFilter("cuda", outFormat, inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                // hw scale
                mainFilters.Add(hwScaleFilter);
            }

            // hw tonemap
            if (doCuTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "cuda", "yuv420p");
                mainFilters.Add(tonemapFilter);
            }

            var memoryOutput = false;
            var isUploadForCuTonemap = isSwDecoder && doCuTonemap;
            if ((isNvDecoder && isSwEncoder) || (isUploadForCuTonemap && hasSubs))
            {
                memoryOutput = true;

                // OUTPUT yuv420p surface(memory)
                mainFilters.Add("hwdownload");
                mainFilters.Add("format=yuv420p");
            }

            // OUTPUT yuv420p surface(memory)
            if (isSwDecoder && isNvencEncoder && !isUploadForCuTonemap)
            {
                memoryOutput = true;
            }

            if (memoryOutput)
            {
                // text subtitles
                if (hasTextSubs)
                {
                    var textSubtitlesFilter = GetTextSubtitlesFilter(state, false, false);
                    mainFilters.Add(textSubtitlesFilter);
                }
            }

            // OUTPUT cuda(yuv420p) surface(vram)

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (isCuInCuOut)
            {
                if (hasSubs)
                {
                    if (hasGraphicalSubs)
                    {
                        // scale=s=1280x720,format=yuva420p,hwupload
                        var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                        subFilters.Add(subSwScaleFilter);
                        subFilters.Add("format=yuva420p");
                    }
                    else if (hasTextSubs)
                    {
                        // alphasrc=s=1280x720:r=10:start=0,format=yuva420p,subtitles,hwupload
                        var alphaSrcFilter = GetAlphaSrcFilter(state, inW, inH, reqW, reqH, reqMaxW, reqMaxH, hasAssSubs ? 10 : 5);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=yuva420p");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    subFilters.Add("hwupload=derive_device=cuda");
                    overlayFilters.Add("overlay_cuda=eof_action=endall:shortest=1:repeatlast=0");
                }
            }
            else
            {
                if (hasGraphicalSubs)
                {
                    var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subSwScaleFilter);
                    overlayFilters.Add("overlay=eof_action=endall:shortest=1:repeatlast=0");
                }
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        /// <summary>
        /// Gets the parameter of AMD AMF filter chain.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="vidEncoder">Video encoder to use.</param>
        /// <returns>The tuple contains three lists: main, sub and overlay filters.</returns>
        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetAmdVidFilterChain(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidEncoder)
        {
            if (!string.Equals(options.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null, null);
            }

            var isWindows = OperatingSystem.IsWindows();
            var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !vidEncoder.Contains("amf", StringComparison.OrdinalIgnoreCase);
            var isAmfDx11OclSupported = isWindows && _mediaEncoder.SupportsHwaccel("d3d11va") && IsOpenclFullSupported();

            // legacy d3d11va pipeline(copy-back)
            if ((isSwDecoder && isSwEncoder)
                || !isAmfDx11OclSupported
                || !_mediaEncoder.SupportsFilter("alphasrc"))
            {
                return GetSwVidFilterChain(state, options, vidEncoder);
            }

            // prefered d3d11va + opencl filters + amf pipeline
            return GetAmdDx11VidFiltersPrefered(state, options, vidDecoder, vidEncoder);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetAmdDx11VidFiltersPrefered(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidDecoder,
            string vidEncoder)
        {
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var isD3d11vaDecoder = vidDecoder.Contains("d3d11va", StringComparison.OrdinalIgnoreCase);
            var isAmfEncoder = vidEncoder.Contains("amf", StringComparison.OrdinalIgnoreCase);
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !isAmfEncoder;
            var isDxInDxOut = isD3d11vaDecoder && isAmfEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doOclTonemap = IsHwTonemapAvailable(state, options);

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, doOclTonemap));

            if (isSwDecoder)
            {
                // INPUT sw surface(memory)
                // sw deint
                if (doDeintH2645)
                {
                    var swDeintFilter = GetSwDeinterlaceFilter(state, options);
                    mainFilters.Add(swDeintFilter);
                }

                var outFormat = doOclTonemap ? "yuv420p10le" : "yuv420p";
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add("format=" + outFormat);

                // keep video at memory except ocl tonemap,
                // since the overhead caused by hwupload >>> using sw filter.
                // sw => hw
                if (doOclTonemap)
                {
                    mainFilters.Add("hwupload=derive_device=d3d11va:extra_hw_frames=16");
                    mainFilters.Add("format=d3d11");
                    mainFilters.Add("hwmap=derive_device=opencl");
                }
            }

            if (isD3d11vaDecoder)
            {
                // INPUT d3d11 surface(vram)
                // map from d3d11va to opencl via d3d11-opencl interop.
                mainFilters.Add("hwmap=derive_device=opencl");

                // hw deint <= TODO: finsh the 'yadif_opencl' filter

                var outFormat = doOclTonemap ? string.Empty : "nv12";
                var hwScaleFilter = GetHwScaleFilter("opencl", outFormat, inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                // hw scale
                mainFilters.Add(hwScaleFilter);
            }

            // hw tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12");
                mainFilters.Add(tonemapFilter);
            }

            var memoryOutput = false;
            var isUploadForOclTonemap = isSwDecoder && doOclTonemap;
            if (isD3d11vaDecoder && isSwEncoder)
            {
                memoryOutput = true;

                // OUTPUT nv12 surface(memory)
                // prefer hwmap to hwdownload on opencl.
                var hwTransferFilter = hasGraphicalSubs ? "hwdownload" : "hwmap=mode=read";
                mainFilters.Add(hwTransferFilter);
                mainFilters.Add("format=nv12");
            }

            // OUTPUT yuv420p surface
            if (isSwDecoder && isAmfEncoder && !isUploadForOclTonemap)
            {
                memoryOutput = true;
            }

            if (memoryOutput)
            {
                // text subtitles
                if (hasTextSubs)
                {
                    var textSubtitlesFilter = GetTextSubtitlesFilter(state, false, false);
                    mainFilters.Add(textSubtitlesFilter);
                }
            }

            if ((isDxInDxOut || isUploadForOclTonemap) && !hasSubs)
            {
                // OUTPUT d3d11(nv12) surface(vram)
                // reverse-mapping via d3d11-opencl interop.
                mainFilters.Add("hwmap=derive_device=d3d11va:reverse=1");
                mainFilters.Add("format=d3d11");
            }

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (isDxInDxOut || isUploadForOclTonemap)
            {
                if (hasSubs)
                {
                    if (hasGraphicalSubs)
                    {
                        // scale=s=1280x720,format=yuva420p,hwupload
                        var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                        subFilters.Add(subSwScaleFilter);
                        subFilters.Add("format=yuva420p");
                    }
                    else if (hasTextSubs)
                    {
                        // alphasrc=s=1280x720:r=10:start=0,format=yuva420p,subtitles,hwupload
                        var alphaSrcFilter = GetAlphaSrcFilter(state, inW, inH, reqW, reqH, reqMaxW, reqMaxH, hasAssSubs ? 10 : 5);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=yuva420p");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    subFilters.Add("hwupload=derive_device=opencl");
                    overlayFilters.Add("overlay_opencl=eof_action=endall:shortest=1:repeatlast=0");
                    overlayFilters.Add("hwmap=derive_device=d3d11va:reverse=1");
                    overlayFilters.Add("format=d3d11");
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subSwScaleFilter);
                    overlayFilters.Add("overlay=eof_action=endall:shortest=1:repeatlast=0");
                }
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        /// <summary>
        /// Gets the parameter of Intel QSV filter chain.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="vidEncoder">Video encoder to use.</param>
        /// <returns>The tuple contains three lists: main, sub and overlay filters.</returns>
        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetIntelVidFilterChain(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidEncoder)
        {
            if (!string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null, null);
            }

            var isWindows = OperatingSystem.IsWindows();
            var isLinux = OperatingSystem.IsLinux();
            var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !vidEncoder.Contains("qsv", StringComparison.OrdinalIgnoreCase);
            var isQsvOclSupported = _mediaEncoder.SupportsHwaccel("qsv") && IsOpenclFullSupported();
            var isIntelDx11OclSupported = isWindows
                && _mediaEncoder.SupportsHwaccel("d3d11va")
                && isQsvOclSupported;
            var isIntelVaapiOclSupported = isLinux
                && IsVaapiSupported(state)
                && isQsvOclSupported;

            // legacy qsv pipeline(copy-back)
            if ((isSwDecoder && isSwEncoder)
                || (!isIntelVaapiOclSupported && !isIntelDx11OclSupported)
                || !_mediaEncoder.SupportsFilter("alphasrc"))
            {
                return GetSwVidFilterChain(state, options, vidEncoder);
            }

            // prefered qsv(vaapi) + opencl filters pipeline
            if (isIntelVaapiOclSupported)
            {
                return GetIntelQsvVaapiVidFiltersPrefered(state, options, vidDecoder, vidEncoder);
            }

            // prefered qsv(d3d11) + opencl filters pipeline
            if (isIntelDx11OclSupported)
            {
                return GetIntelQsvDx11VidFiltersPrefered(state, options, vidDecoder, vidEncoder);
            }

            return (null, null, null);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetIntelQsvDx11VidFiltersPrefered(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidDecoder,
            string vidEncoder)
        {
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var isD3d11vaDecoder = vidDecoder.Contains("d3d11va", StringComparison.OrdinalIgnoreCase);
            var isQsvDecoder = vidDecoder.Contains("qsv", StringComparison.OrdinalIgnoreCase);
            var isQsvEncoder = vidEncoder.Contains("qsv", StringComparison.OrdinalIgnoreCase);
            var isHwDecoder = isD3d11vaDecoder || isQsvDecoder;
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !isQsvEncoder;
            var isQsvInQsvOut = isHwDecoder && isQsvEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doOclTonemap = IsHwTonemapAvailable(state, options);

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, doOclTonemap));

            if (isSwDecoder)
            {
                // INPUT sw surface(memory)
                // sw deint
                if (doDeintH2645)
                {
                    var swDeintFilter = GetSwDeinterlaceFilter(state, options);
                    mainFilters.Add(swDeintFilter);
                }

                var outFormat = doOclTonemap ? "yuv420p10le" : (hasGraphicalSubs ? "yuv420p" : "nv12");
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add("format=" + outFormat);

                // keep video at memory except ocl tonemap,
                // since the overhead caused by hwupload >>> using sw filter.
                // sw => hw
                if (doOclTonemap)
                {
                    mainFilters.Add("hwupload=derive_device=opencl");
                }
            }
            else if (isD3d11vaDecoder || isQsvDecoder)
            {
                var outFormat = doOclTonemap ? string.Empty : "nv12";
                var hwScaleFilter = GetHwScaleFilter("qsv", outFormat, inW, inH, reqW, reqH, reqMaxW, reqMaxH);

                if (isD3d11vaDecoder)
                {
                    if (!string.IsNullOrEmpty(hwScaleFilter) || doDeintH2645)
                    {
                        // INPUT d3d11 surface(vram)
                        // map from d3d11va to qsv.
                        mainFilters.Add("hwmap=derive_device=qsv");
                    }
                }

                // hw deint
                if (doDeintH2645)
                {
                    var deintFilter = GetHwDeinterlaceFilter(state, options, "qsv");
                    mainFilters.Add(deintFilter);
                }

                // hw scale
                mainFilters.Add(hwScaleFilter);
            }

            if (doOclTonemap && isHwDecoder)
            {
                // map from qsv to opencl via qsv(d3d11)-opencl interop.
                mainFilters.Add("hwmap=derive_device=opencl");
            }

            // hw tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12");
                mainFilters.Add(tonemapFilter);
            }

            var memoryOutput = false;
            var isUploadForOclTonemap = isSwDecoder && doOclTonemap;
            var isHwmapUsable = isSwEncoder && doOclTonemap;
            if ((isHwDecoder && isSwEncoder) || isUploadForOclTonemap)
            {
                memoryOutput = true;

                // OUTPUT nv12 surface(memory)
                // prefer hwmap to hwdownload on opencl.
                // qsv hwmap is not fully implemented for the time being.
                mainFilters.Add(isHwmapUsable ? "hwmap=mode=read" : "hwdownload");
                mainFilters.Add("format=nv12");
            }

            // OUTPUT nv12 surface(memory)
            if (isSwDecoder && isQsvEncoder)
            {
                memoryOutput = true;
            }

            if (memoryOutput)
            {
                // text subtitles
                if (hasTextSubs)
                {
                    var textSubtitlesFilter = GetTextSubtitlesFilter(state, false, false);
                    mainFilters.Add(textSubtitlesFilter);
                }
            }

            if (isQsvInQsvOut && doOclTonemap)
            {
                // OUTPUT qsv(nv12) surface(vram)
                // reverse-mapping via qsv(d3d11)-opencl interop.
                mainFilters.Add("hwmap=derive_device=qsv:reverse=1");
                mainFilters.Add("format=qsv");
            }

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (isQsvInQsvOut)
            {
                if (hasSubs)
                {
                    if (hasGraphicalSubs)
                    {
                        // scale,format=bgra,hwupload
                        // overlay_qsv can handle overlay scaling,
                        // add a dummy scale filter to pair with -canvas_size.
                        subFilters.Add("scale=flags=fast_bilinear");
                        subFilters.Add("format=bgra");
                    }
                    else if (hasTextSubs)
                    {
                        // alphasrc=s=1280x720:r=10:start=0,format=bgra,subtitles,hwupload
                        var alphaSrcFilter = GetAlphaSrcFilter(state, inW, inH, reqW, reqH, reqMaxW, 1080, hasAssSubs ? 10 : 5);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=bgra");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    // qsv requires a fixed pool size.
                    // default to 64 otherwise it will fail on certain iGPU.
                    subFilters.Add("hwupload=derive_device=qsv:extra_hw_frames=64");

                    var (overlayW, overlayH) = GetFixedOutputSize(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    var overlaySize = (overlayW.HasValue && overlayH.HasValue)
                        ? (":w=" + overlayW.Value + ":h=" + overlayH.Value)
                        : string.Empty;
                    var overlayQsvFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        "overlay_qsv=eof_action=endall:shortest=1:repeatlast=0{0}",
                        overlaySize);
                    overlayFilters.Add(overlayQsvFilter);
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subSwScaleFilter);
                    overlayFilters.Add("overlay=eof_action=endall:shortest=1:repeatlast=0");
                }
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetIntelQsvVaapiVidFiltersPrefered(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidDecoder,
            string vidEncoder)
        {
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var isVaapiDecoder = vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isQsvDecoder = vidDecoder.Contains("qsv", StringComparison.OrdinalIgnoreCase);
            var isQsvEncoder = vidEncoder.Contains("qsv", StringComparison.OrdinalIgnoreCase);
            var isHwDecoder = isVaapiDecoder || isQsvDecoder;
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !isQsvEncoder;
            var isQsvInQsvOut = isHwDecoder && isQsvEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doVaVppTonemap = IsVaapiVppTonemapAvailable(state, options);
            var doOclTonemap = !doVaVppTonemap && IsHwTonemapAvailable(state, options);
            var doTonemap = doVaVppTonemap || doOclTonemap;
            var doDeintH2645 = doDeintH264 || doDeintHevc;

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, doTonemap));

            if (isSwDecoder)
            {
                // INPUT sw surface(memory)
                // sw deint
                if (doDeintH2645)
                {
                    var swDeintFilter = GetSwDeinterlaceFilter(state, options);
                    mainFilters.Add(swDeintFilter);
                }

                var outFormat = doOclTonemap ? "yuv420p10le" : (hasGraphicalSubs ? "yuv420p" : "nv12");
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add("format=" + outFormat);

                // keep video at memory except ocl tonemap,
                // since the overhead caused by hwupload >>> using sw filter.
                // sw => hw
                if (doOclTonemap)
                {
                    mainFilters.Add("hwupload=derive_device=opencl");
                }
            }
            else if (isVaapiDecoder || isQsvDecoder)
            {
                // INPUT vaapi/qsv surface(vram)
                // hw deint
                if (doDeintH2645)
                {
                    var deintFilter = GetHwDeinterlaceFilter(state, options, isVaapiDecoder ? "vaapi" : "qsv");
                    mainFilters.Add(deintFilter);
                }

                var outFormat = doTonemap ? string.Empty : "nv12";
                var hwScaleFilter = GetHwScaleFilter(isVaapiDecoder ? "vaapi" : "qsv", outFormat, inW, inH, reqW, reqH, reqMaxW, reqMaxH);

                // allocate extra pool sizes for vaapi vpp
                if (!string.IsNullOrEmpty(hwScaleFilter) && isVaapiDecoder)
                {
                    hwScaleFilter += ":extra_hw_frames=24";
                }

                // hw scale
                mainFilters.Add(hwScaleFilter);
            }

            // vaapi vpp tonemap
            if (doVaVppTonemap && isHwDecoder)
            {
                if (isQsvDecoder)
                {
                    // map from qsv to vaapi.
                    mainFilters.Add("hwmap=derive_device=vaapi");
                }

                var tonemapFilter = GetHwTonemapFilter(options, "vaapi", "nv12");
                mainFilters.Add(tonemapFilter);

                if (isQsvDecoder)
                {
                    // map from vaapi to qsv.
                    mainFilters.Add("hwmap=derive_device=qsv");
                }
            }

            if (doOclTonemap && isHwDecoder)
            {
                // map from qsv to opencl via qsv(vaapi)-opencl interop.
                mainFilters.Add("hwmap=derive_device=opencl");
            }

            // ocl tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12");
                mainFilters.Add(tonemapFilter);
            }

            var memoryOutput = false;
            var isUploadForOclTonemap = isSwDecoder && doOclTonemap;
            var isHwmapUsable = isSwEncoder && (doOclTonemap || isVaapiDecoder);
            if ((isHwDecoder && isSwEncoder) || isUploadForOclTonemap)
            {
                memoryOutput = true;

                // OUTPUT nv12 surface(memory)
                // prefer hwmap to hwdownload on opencl/vaapi.
                // qsv hwmap is not fully implemented for the time being.
                mainFilters.Add(isHwmapUsable ? "hwmap=mode=read" : "hwdownload");
                mainFilters.Add("format=nv12");
            }

            // OUTPUT nv12 surface(memory)
            if (isSwDecoder && isQsvEncoder)
            {
                memoryOutput = true;
            }

            if (memoryOutput)
            {
                // text subtitles
                if (hasTextSubs)
                {
                    var textSubtitlesFilter = GetTextSubtitlesFilter(state, false, false);
                    mainFilters.Add(textSubtitlesFilter);
                }
            }

            if (isQsvInQsvOut)
            {
                if (doOclTonemap)
                {
                    // OUTPUT qsv(nv12) surface(vram)
                    // reverse-mapping via qsv(vaapi)-opencl interop.
                    // add extra pool size to avoid the 'cannot allocate memory' error on hevc_qsv.
                    mainFilters.Add("hwmap=derive_device=qsv:reverse=1:extra_hw_frames=16");
                    mainFilters.Add("format=qsv");
                }
                else if (isVaapiDecoder)
                {
                    mainFilters.Add("hwmap=derive_device=qsv");
                    mainFilters.Add("format=qsv");
                }
            }

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (isQsvInQsvOut)
            {
                if (hasSubs)
                {
                    if (hasGraphicalSubs)
                    {
                        subFilters.Add("scale=flags=fast_bilinear");
                        subFilters.Add("format=bgra");
                    }
                    else if (hasTextSubs)
                    {
                        var alphaSrcFilter = GetAlphaSrcFilter(state, inW, inH, reqW, reqH, reqMaxW, 1080, hasAssSubs ? 10 : 5);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=bgra");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    // qsv requires a fixed pool size.
                    // default to 64 otherwise it will fail on certain iGPU.
                    subFilters.Add("hwupload=derive_device=qsv:extra_hw_frames=64");

                    var (overlayW, overlayH) = GetFixedOutputSize(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    var overlaySize = (overlayW.HasValue && overlayH.HasValue)
                        ? (":w=" + overlayW.Value + ":h=" + overlayH.Value)
                        : string.Empty;
                    var overlayQsvFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        "overlay_qsv=eof_action=endall:shortest=1:repeatlast=0{0}",
                        overlaySize);
                    overlayFilters.Add(overlayQsvFilter);
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subSwScaleFilter);
                    overlayFilters.Add("overlay=eof_action=pass:shortest=1:repeatlast=0");
                }
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        /// <summary>
        /// Gets the parameter of Intel/AMD VAAPI filter chain.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="vidEncoder">Video encoder to use.</param>
        /// <returns>The tuple contains three lists: main, sub and overlay filters.</returns>
        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetVaapiVidFilterChain(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidEncoder)
        {
            if (!string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null, null);
            }

            var isLinux = OperatingSystem.IsLinux();
            var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !vidEncoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isVaapiFullSupported = isLinux && IsVaapiSupported(state) && IsVaapiFullSupported();
            var isVaapiOclSupported = isVaapiFullSupported && IsOpenclFullSupported();
            var isVaapiVkSupported = isVaapiFullSupported && IsVulkanFullSupported();

            // legacy vaapi pipeline(copy-back)
            if ((isSwDecoder && isSwEncoder)
                || !isVaapiOclSupported
                || !_mediaEncoder.SupportsFilter("alphasrc"))
            {
                var swFilterChain = GetSwVidFilterChain(state, options, vidEncoder);

                if (!isSwEncoder)
                {
                    var newfilters = new List<string>();
                    var noOverlay = swFilterChain.OverlayFilters.Count == 0;
                    newfilters.AddRange(noOverlay ? swFilterChain.MainFilters : swFilterChain.OverlayFilters);
                    newfilters.Add("hwupload=derive_device=vaapi");

                    var mainFilters = noOverlay ? newfilters : swFilterChain.MainFilters;
                    var overlayFilters = noOverlay ? swFilterChain.OverlayFilters : newfilters;
                    return (mainFilters, swFilterChain.SubFilters, overlayFilters);
                }

                return swFilterChain;
            }

            // prefered vaapi + opencl filters pipeline
            if (_mediaEncoder.IsVaapiDeviceInteliHD)
            {
                // Intel iHD path, with extra vpp tonemap and overlay support.
                return GetIntelVaapiFullVidFiltersPrefered(state, options, vidDecoder, vidEncoder);
            }

            // prefered vaapi + vulkan filters pipeline
            if (_mediaEncoder.IsVaapiDeviceAmd
                && isVaapiVkSupported
                && _mediaEncoder.IsVaapiDeviceSupportVulkanDrmInterop)
            {
                // AMD radeonsi path(targeting Polaris/gfx8+), with extra vulkan tonemap and overlay support.
                return GetAmdVaapiFullVidFiltersPrefered(state, options, vidDecoder, vidEncoder);
            }

            // Intel i965 and Amd legacy driver path, only featuring scale and deinterlace support.
            return GetVaapiLimitedVidFiltersPrefered(state, options, vidDecoder, vidEncoder);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetIntelVaapiFullVidFiltersPrefered(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidDecoder,
            string vidEncoder)
        {
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var isVaapiDecoder = vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isVaapiEncoder = vidEncoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !isVaapiEncoder;
            var isVaInVaOut = isVaapiDecoder && isVaapiEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doVaVppTonemap = isVaapiDecoder && IsVaapiVppTonemapAvailable(state, options);
            var doOclTonemap = !doVaVppTonemap && IsHwTonemapAvailable(state, options);
            var doTonemap = doVaVppTonemap || doOclTonemap;
            var doDeintH2645 = doDeintH264 || doDeintHevc;

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, doTonemap));

            if (isSwDecoder)
            {
                // INPUT sw surface(memory)
                // sw deint
                if (doDeintH2645)
                {
                    var swDeintFilter = GetSwDeinterlaceFilter(state, options);
                    mainFilters.Add(swDeintFilter);
                }

                var outFormat = doOclTonemap ? "yuv420p10le" : "nv12";
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add("format=" + outFormat);

                // keep video at memory except ocl tonemap,
                // since the overhead caused by hwupload >>> using sw filter.
                // sw => hw
                if (doOclTonemap)
                {
                    mainFilters.Add("hwupload=derive_device=opencl");
                }
            }
            else if (isVaapiDecoder)
            {
                // INPUT vaapi surface(vram)
                // hw deint
                if (doDeintH2645)
                {
                    var deintFilter = GetHwDeinterlaceFilter(state, options, "vaapi");
                    mainFilters.Add(deintFilter);
                }

                var outFormat = doTonemap ? string.Empty : "nv12";
                var hwScaleFilter = GetHwScaleFilter("vaapi", outFormat, inW, inH, reqW, reqH, reqMaxW, reqMaxH);

                // allocate extra pool sizes for vaapi vpp
                if (!string.IsNullOrEmpty(hwScaleFilter))
                {
                    hwScaleFilter += ":extra_hw_frames=24";
                }

                // hw scale
                mainFilters.Add(hwScaleFilter);
            }

            // vaapi vpp tonemap
            if (doVaVppTonemap && isVaapiDecoder)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "vaapi", "nv12");
                mainFilters.Add(tonemapFilter);
            }

            if (doOclTonemap && isVaapiDecoder)
            {
                // map from vaapi to opencl via vaapi-opencl interop(Intel only).
                mainFilters.Add("hwmap=derive_device=opencl");
            }

            // ocl tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12");
                mainFilters.Add(tonemapFilter);
            }

            if (doOclTonemap && isVaInVaOut)
            {
                // OUTPUT vaapi(nv12) surface(vram)
                // reverse-mapping via vaapi-opencl interop.
                mainFilters.Add("hwmap=derive_device=vaapi:reverse=1");
                mainFilters.Add("format=vaapi");
            }

            var memoryOutput = false;
            var isUploadForOclTonemap = isSwDecoder && doOclTonemap;
            var isHwmapNotUsable = isUploadForOclTonemap && isVaapiEncoder;
            if ((isVaapiDecoder && isSwEncoder) || isUploadForOclTonemap)
            {
                memoryOutput = true;

                // OUTPUT nv12 surface(memory)
                // prefer hwmap to hwdownload on opencl/vaapi.
                mainFilters.Add(isHwmapNotUsable ? "hwdownload" : "hwmap=mode=read");
                mainFilters.Add("format=nv12");
            }

            // OUTPUT nv12 surface(memory)
            if (isSwDecoder && isVaapiEncoder)
            {
                memoryOutput = true;
            }

            if (memoryOutput)
            {
                // text subtitles
                if (hasTextSubs)
                {
                    var textSubtitlesFilter = GetTextSubtitlesFilter(state, false, false);
                    mainFilters.Add(textSubtitlesFilter);
                }
            }

            if (memoryOutput && isVaapiEncoder)
            {
                if (!hasGraphicalSubs)
                {
                    mainFilters.Add("hwupload_vaapi");
                }
            }

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (isVaInVaOut)
            {
                if (hasSubs)
                {
                    if (hasGraphicalSubs)
                    {
                        subFilters.Add("scale=flags=fast_bilinear");
                        subFilters.Add("format=bgra");
                    }
                    else if (hasTextSubs)
                    {
                        var alphaSrcFilter = GetAlphaSrcFilter(state, inW, inH, reqW, reqH, reqMaxW, 1080, hasAssSubs ? 10 : 5);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=bgra");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    subFilters.Add("hwupload=derive_device=vaapi");

                    var (overlayW, overlayH) = GetFixedOutputSize(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    var overlaySize = (overlayW.HasValue && overlayH.HasValue)
                        ? (":w=" + overlayW.Value + ":h=" + overlayH.Value)
                        : string.Empty;
                    var overlayVaapiFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        "overlay_vaapi=eof_action=endall:shortest=1:repeatlast=0{0}",
                        overlaySize);
                    overlayFilters.Add(overlayVaapiFilter);
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subSwScaleFilter);
                    overlayFilters.Add("overlay=eof_action=pass:shortest=1:repeatlast=0");

                    if (isVaapiEncoder)
                    {
                        overlayFilters.Add("hwupload_vaapi");
                    }
                }
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetAmdVaapiFullVidFiltersPrefered(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidDecoder,
            string vidEncoder)
        {
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var isVaapiDecoder = vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isVaapiEncoder = vidEncoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !isVaapiEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doVkTonemap = IsVulkanHwTonemapAvailable(state, options);
            var doDeintH2645 = doDeintH264 || doDeintHevc;

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, doVkTonemap));

            if (isSwDecoder)
            {
                // INPUT sw surface(memory)
                // sw deint
                if (doDeintH2645)
                {
                    var swDeintFilter = GetSwDeinterlaceFilter(state, options);
                    mainFilters.Add(swDeintFilter);
                }

                if (doVkTonemap || hasSubs)
                {
                    // sw => hw
                    mainFilters.Add("hwupload=derive_device=vulkan");
                    mainFilters.Add("format=vulkan");
                }
                else
                {
                    // sw scale
                    var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                    mainFilters.Add(swScaleFilter);
                    mainFilters.Add("format=nv12");
                }
            }
            else if (isVaapiDecoder)
            {
                // INPUT vaapi surface(vram)
                if (doVkTonemap || hasSubs)
                {
                    // map from vaapi to vulkan/drm via interop (Polaris/gfx8+).
                    mainFilters.Add("hwmap=derive_device=vulkan");
                    mainFilters.Add("format=vulkan");
                }
                else
                {
                    // hw deint
                    if (doDeintH2645)
                    {
                        var deintFilter = GetHwDeinterlaceFilter(state, options, "vaapi");
                        mainFilters.Add(deintFilter);
                    }

                    // hw scale
                    var hwScaleFilter = GetHwScaleFilter("vaapi", "nv12", inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    mainFilters.Add(hwScaleFilter);
                }
            }

            // vk libplacebo
            if (doVkTonemap || hasSubs)
            {
                var libplaceboFilter = GetLibplaceboFilter(options, "bgra", doVkTonemap, inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                mainFilters.Add(libplaceboFilter);
            }

            if (doVkTonemap && !hasSubs)
            {
                // OUTPUT vaapi(nv12) surface(vram)
                // map from vulkan/drm to vaapi via interop (Polaris/gfx8+).
                mainFilters.Add("hwmap=derive_device=vaapi");
                mainFilters.Add("format=vaapi");

                // clear the surf->meta_offset and output nv12
                mainFilters.Add("scale_vaapi=format=nv12");

                // hw deint
                if (doDeintH2645)
                {
                    var deintFilter = GetHwDeinterlaceFilter(state, options, "vaapi");
                    mainFilters.Add(deintFilter);
                }
            }

            if (!hasSubs)
            {
                // OUTPUT nv12 surface(memory)
                if (isSwEncoder && (doVkTonemap || isVaapiDecoder))
                {
                    mainFilters.Add("hwdownload");
                    mainFilters.Add("format=nv12");
                }

                if (isSwDecoder && isVaapiEncoder && !doVkTonemap)
                {
                    mainFilters.Add("hwupload_vaapi");
                }
            }

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (hasSubs)
            {
                if (hasGraphicalSubs)
                {
                    // scale=s=1280x720,format=bgra,hwupload
                    var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subSwScaleFilter);
                    subFilters.Add("format=bgra");
                }
                else if (hasTextSubs)
                {
                    var alphaSrcFilter = GetAlphaSrcFilter(state, inW, inH, reqW, reqH, reqMaxW, reqMaxH, hasAssSubs ? 10 : 5);
                    var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                    subFilters.Add(alphaSrcFilter);
                    subFilters.Add("format=bgra");
                    subFilters.Add(subTextSubtitlesFilter);
                }

                subFilters.Add("hwupload=derive_device=vulkan");
                subFilters.Add("format=vulkan");

                overlayFilters.Add("overlay_vulkan=eof_action=endall:shortest=1:repeatlast=0");

                if (isSwEncoder)
                {
                    // OUTPUT nv12 surface(memory)
                    overlayFilters.Add("scale_vulkan=format=nv12");
                    overlayFilters.Add("hwdownload");
                    overlayFilters.Add("format=nv12");
                }
                else if (isVaapiEncoder)
                {
                    // OUTPUT vaapi(nv12) surface(vram)
                    // map from vulkan/drm to vaapi via interop (Polaris/gfx8+).
                    overlayFilters.Add("hwmap=derive_device=vaapi");
                    overlayFilters.Add("format=vaapi");

                    // clear the surf->meta_offset and output nv12
                    overlayFilters.Add("scale_vaapi=format=nv12");

                    // hw deint
                    if (doDeintH2645)
                    {
                        var deintFilter = GetHwDeinterlaceFilter(state, options, "vaapi");
                        overlayFilters.Add(deintFilter);
                    }
                }
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetVaapiLimitedVidFiltersPrefered(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidDecoder,
            string vidEncoder)
        {
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var isVaapiDecoder = vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isVaapiEncoder = vidEncoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !isVaapiEncoder;
            var isVaInVaOut = isVaapiDecoder && isVaapiEncoder;
            var isi965Driver = _mediaEncoder.IsVaapiDeviceInteli965;
            var isAmdDriver = _mediaEncoder.IsVaapiDeviceAmd;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doOclTonemap = IsHwTonemapAvailable(state, options);

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, doOclTonemap));

            var outFormat = string.Empty;
            if (isSwDecoder)
            {
                // INPUT sw surface(memory)
                // sw deint
                if (doDeintH2645)
                {
                    var swDeintFilter = GetSwDeinterlaceFilter(state, options);
                    mainFilters.Add(swDeintFilter);
                }

                outFormat = doOclTonemap ? "yuv420p10le" : "nv12";
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add("format=" + outFormat);

                // keep video at memory except ocl tonemap,
                // since the overhead caused by hwupload >>> using sw filter.
                // sw => hw
                if (doOclTonemap)
                {
                    mainFilters.Add("hwupload=derive_device=opencl");
                }
            }
            else if (isVaapiDecoder)
            {
                // INPUT vaapi surface(vram)
                // hw deint
                if (doDeintH2645)
                {
                    var deintFilter = GetHwDeinterlaceFilter(state, options, "vaapi");
                    mainFilters.Add(deintFilter);
                }

                outFormat = doOclTonemap ? string.Empty : "nv12";
                var hwScaleFilter = GetHwScaleFilter("vaapi", outFormat, inW, inH, reqW, reqH, reqMaxW, reqMaxH);

                // allocate extra pool sizes for vaapi vpp
                if (!string.IsNullOrEmpty(hwScaleFilter))
                {
                    hwScaleFilter += ":extra_hw_frames=24";
                }

                // hw scale
                mainFilters.Add(hwScaleFilter);
            }

            if (doOclTonemap && isVaapiDecoder)
            {
                if (isi965Driver)
                {
                    // map from vaapi to opencl via vaapi-opencl interop(Intel only).
                    mainFilters.Add("hwmap=derive_device=opencl");
                }
                else
                {
                    mainFilters.Add("hwdownload");
                    mainFilters.Add("format=p010le");
                    mainFilters.Add("hwupload=derive_device=opencl");
                }
            }

            // ocl tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12");
                mainFilters.Add(tonemapFilter);
            }

            if (doOclTonemap && isVaInVaOut)
            {
                if (isi965Driver)
                {
                    // OUTPUT vaapi(nv12) surface(vram)
                    // reverse-mapping via vaapi-opencl interop.
                    mainFilters.Add("hwmap=derive_device=vaapi:reverse=1");
                    mainFilters.Add("format=vaapi");
                }
            }

            var memoryOutput = false;
            var isUploadForOclTonemap = doOclTonemap && (isSwDecoder || (isVaapiDecoder && !isi965Driver));
            var isHwmapNotUsable = hasGraphicalSubs || isUploadForOclTonemap;
            var isHwmapForSubs = hasSubs && isVaapiDecoder;
            var isHwUnmapForTextSubs = hasTextSubs && isVaInVaOut && !isUploadForOclTonemap;
            if ((isVaapiDecoder && isSwEncoder) || isUploadForOclTonemap || isHwmapForSubs)
            {
                memoryOutput = true;

                // OUTPUT nv12 surface(memory)
                // prefer hwmap to hwdownload on opencl/vaapi.
                mainFilters.Add(isHwmapNotUsable ? "hwdownload" : "hwmap");
                mainFilters.Add("format=nv12");
            }

            // OUTPUT nv12 surface(memory)
            if (isSwDecoder && isVaapiEncoder)
            {
                memoryOutput = true;
            }

            if (memoryOutput)
            {
                // text subtitles
                if (hasTextSubs)
                {
                    var textSubtitlesFilter = GetTextSubtitlesFilter(state, false, false);
                    mainFilters.Add(textSubtitlesFilter);
                }
            }

            if (isHwUnmapForTextSubs)
            {
                mainFilters.Add("hwmap");
                mainFilters.Add("format=vaapi");
            }
            else if (memoryOutput && isVaapiEncoder)
            {
                if (!hasGraphicalSubs)
                {
                    mainFilters.Add("hwupload_vaapi");
                }
            }

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subSwScaleFilter = GetCustomSwScaleFilter(inW, inH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subSwScaleFilter);
                    overlayFilters.Add("overlay=eof_action=pass:shortest=1:repeatlast=0");

                    if (isVaapiEncoder)
                    {
                        overlayFilters.Add("hwupload_vaapi");
                    }
                }
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        /// <summary>
        /// Gets the parameter of Apple VideoToolBox filter chain.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="vidEncoder">Video encoder to use.</param>
        /// <returns>The tuple contains three lists: main, sub and overlay filters.</returns>
        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetAppleVidFilterChain(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidEncoder)
        {
            if (!string.Equals(options.HardwareAccelerationType, "videotoolbox", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null, null);
            }

            var swFilterChain = GetSwVidFilterChain(state, options, vidEncoder);

            if (!options.EnableHardwareEncoding)
            {
                return swFilterChain;
            }

            if (_mediaEncoder.EncoderVersion.CompareTo(new Version("5.0.0")) < 0)
            {
                // All features used here requires ffmpeg 5.0 or later, fallback to software filters if using an old ffmpeg
                return swFilterChain;
            }

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;
            var newfilters = new List<string>();
            var noOverlay = swFilterChain.OverlayFilters.Count == 0;
            var supportsHwDeint = _mediaEncoder.SupportsFilter("yadif_videotoolbox");
            // fallback to software filters if we are using filters not supported by hardware yet.
            var useHardwareFilters = noOverlay && (!doDeintH2645 || supportsHwDeint);

            if (!useHardwareFilters)
            {
                return swFilterChain;
            }

            // ffmpeg cannot use videotoolbox to scale
            var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, inW, inH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
            newfilters.Add(swScaleFilter);

            // hwupload on videotoolbox encoders can automatically convert AVFrame into its CVPixelBuffer equivalent
            // videotoolbox will automatically convert the CVPixelBuffer to a pixel format the encoder supports, so we don't have to set a pixel format explicitly here
            // This will reduce CPU usage significantly on UHD videos with 10 bit colors because we bypassed the ffmpeg pixel format conversion
            newfilters.Add("hwupload");

            if (doDeintH2645)
            {
                var deintFilter = GetHwDeinterlaceFilter(state, options, "videotoolbox");
                newfilters.Add(deintFilter);
            }

            return (newfilters, swFilterChain.SubFilters, swFilterChain.OverlayFilters);
        }

        /// <summary>
        /// Gets the parameter of video processing filters.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="outputVideoCodec">Video codec to use.</param>
        /// <returns>The video processing filters parameter.</returns>
        public string GetVideoProcessingFilterParam(
            EncodingJobInfo state,
            EncodingOptions options,
            string outputVideoCodec)
        {
            var videoStream = state.VideoStream;
            if (videoStream is null)
            {
                return string.Empty;
            }

            var hasSubs = state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;

            List<string> mainFilters;
            List<string> subFilters;
            List<string> overlayFilters;

            if (string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                (mainFilters, subFilters, overlayFilters) = GetVaapiVidFilterChain(state, options, outputVideoCodec);
            }
            else if (string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
            {
                (mainFilters, subFilters, overlayFilters) = GetIntelVidFilterChain(state, options, outputVideoCodec);
            }
            else if (string.Equals(options.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
            {
                (mainFilters, subFilters, overlayFilters) = GetNvidiaVidFilterChain(state, options, outputVideoCodec);
            }
            else if (string.Equals(options.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
            {
                (mainFilters, subFilters, overlayFilters) = GetAmdVidFilterChain(state, options, outputVideoCodec);
            }
            else if (string.Equals(options.HardwareAccelerationType, "videotoolbox", StringComparison.OrdinalIgnoreCase))
            {
                (mainFilters, subFilters, overlayFilters) = GetAppleVidFilterChain(state, options, outputVideoCodec);
            }
            else
            {
                (mainFilters, subFilters, overlayFilters) = GetSwVidFilterChain(state, options, outputVideoCodec);
            }

            mainFilters?.RemoveAll(filter => string.IsNullOrEmpty(filter));
            subFilters?.RemoveAll(filter => string.IsNullOrEmpty(filter));
            overlayFilters?.RemoveAll(filter => string.IsNullOrEmpty(filter));

            var mainStr = string.Empty;
            if (mainFilters?.Count > 0)
            {
                mainStr = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}",
                    string.Join(',', mainFilters));
            }

            if (overlayFilters?.Count == 0)
            {
                // -vf "scale..."
                return string.IsNullOrEmpty(mainStr) ? string.Empty : " -vf \"" + mainStr + "\"";
            }

            if (overlayFilters?.Count > 0
                && subFilters?.Count > 0
                && state.SubtitleStream is not null)
            {
                // overlay graphical/text subtitles
                var subStr = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}",
                        string.Join(',', subFilters));

                var overlayStr = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}",
                        string.Join(',', overlayFilters));

                var mapPrefix = Convert.ToInt32(state.SubtitleStream.IsExternal);
                var subtitleStreamIndex = FindIndex(state.MediaSource.MediaStreams, state.SubtitleStream);
                var videoStreamIndex = FindIndex(state.MediaSource.MediaStreams, state.VideoStream);

                if (hasSubs)
                {
                    // -filter_complex "[0:s]scale=s[sub]..."
                    var filterStr = string.IsNullOrEmpty(mainStr)
                        ? " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}][sub]{5}\""
                        : " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3}[main];[main][sub]{5}\"";

                    if (hasTextSubs)
                    {
                        filterStr = string.IsNullOrEmpty(mainStr)
                            ? " -filter_complex \"{4}[sub];[0:{2}][sub]{5}\""
                            : " -filter_complex \"{4}[sub];[0:{2}]{3}[main];[main][sub]{5}\"";
                    }

                    return string.Format(
                        CultureInfo.InvariantCulture,
                        filterStr,
                        mapPrefix,
                        subtitleStreamIndex,
                        videoStreamIndex,
                        mainStr,
                        subStr,
                        overlayStr);
                }
            }

            return string.Empty;
        }

        public string GetOverwriteColorPropertiesParam(EncodingJobInfo state, bool isTonemapAvailable)
        {
            if (isTonemapAvailable)
            {
                return GetInputHdrParam(state.VideoStream?.ColorTransfer);
            }

            return GetOutputSdrParam(null);
        }

        public string GetInputHdrParam(string colorTransfer)
        {
            if (string.Equals(colorTransfer, "arib-std-b67", StringComparison.OrdinalIgnoreCase))
            {
                // HLG
                return "setparams=color_primaries=bt2020:color_trc=arib-std-b67:colorspace=bt2020nc";
            }

            // HDR10
            return "setparams=color_primaries=bt2020:color_trc=smpte2084:colorspace=bt2020nc";
        }

        public string GetOutputSdrParam(string tonemappingRange)
        {
            // SDR
            if (string.Equals(tonemappingRange, "tv", StringComparison.OrdinalIgnoreCase))
            {
                return "setparams=color_primaries=bt709:color_trc=bt709:colorspace=bt709:range=tv";
            }

            if (string.Equals(tonemappingRange, "pc", StringComparison.OrdinalIgnoreCase))
            {
                return "setparams=color_primaries=bt709:color_trc=bt709:colorspace=bt709:range=pc";
            }

            return "setparams=color_primaries=bt709:color_trc=bt709:colorspace=bt709";
        }

        public static int GetVideoColorBitDepth(EncodingJobInfo state)
        {
            var videoStream = state.VideoStream;
            if (videoStream is not null)
            {
                if (videoStream.BitDepth.HasValue)
                {
                    return videoStream.BitDepth.Value;
                }

                if (string.Equals(videoStream.PixelFormat, "yuv420p", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.PixelFormat, "yuvj420p", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.PixelFormat, "yuv444p", StringComparison.OrdinalIgnoreCase))
                {
                    return 8;
                }

                if (string.Equals(videoStream.PixelFormat, "yuv420p10le", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.PixelFormat, "yuv444p10le", StringComparison.OrdinalIgnoreCase))
                {
                    return 10;
                }

                if (string.Equals(videoStream.PixelFormat, "yuv420p12le", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.PixelFormat, "yuv444p12le", StringComparison.OrdinalIgnoreCase))
                {
                    return 12;
                }

                return 8;
            }

            return 0;
        }

        /// <summary>
        /// Gets the ffmpeg option string for the hardware accelerated video decoder.
        /// </summary>
        /// <param name="state">The encoding job info.</param>
        /// <param name="options">The encoding options.</param>
        /// <returns>The option string or null if none available.</returns>
        protected string GetHardwareVideoDecoder(EncodingJobInfo state, EncodingOptions options)
        {
            var videoStream = state.VideoStream;
            var mediaSource = state.MediaSource;
            if (videoStream is null || mediaSource is null)
            {
                return null;
            }

            // HWA decoders can handle both video files and video folders.
            var videoType = state.VideoType;
            if (videoType != VideoType.VideoFile
                && videoType != VideoType.Iso
                && videoType != VideoType.Dvd
                && videoType != VideoType.BluRay)
            {
                return null;
            }

            if (IsCopyCodec(state.OutputVideoCodec))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(videoStream.Codec) && !string.IsNullOrEmpty(options.HardwareAccelerationType))
            {
                var bitDepth = GetVideoColorBitDepth(state);

                // Only HEVC, VP9 and AV1 formats have 10-bit hardware decoder support now.
                if (bitDepth == 10
                    && !(string.Equals(videoStream.Codec, "hevc", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoStream.Codec, "h265", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoStream.Codec, "vp9", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoStream.Codec, "av1", StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                if (string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
                {
                    return GetQsvHwVidDecoder(state, options, videoStream, bitDepth);
                }

                if (string.Equals(options.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    return GetNvdecVidDecoder(state, options, videoStream, bitDepth);
                }

                if (string.Equals(options.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAmfVidDecoder(state, options, videoStream, bitDepth);
                }

                if (string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    return GetVaapiVidDecoder(state, options, videoStream, bitDepth);
                }

                if (string.Equals(options.HardwareAccelerationType, "videotoolbox", StringComparison.OrdinalIgnoreCase))
                {
                    return GetVideotoolboxVidDecoder(state, options, videoStream, bitDepth);
                }
            }

            var whichCodec = videoStream.Codec;
            if (string.Equals(whichCodec, "avc", StringComparison.OrdinalIgnoreCase))
            {
                whichCodec = "h264";
            }
            else if (string.Equals(whichCodec, "h265", StringComparison.OrdinalIgnoreCase))
            {
                whichCodec = "hevc";
            }

            // Avoid a second attempt if no hardware acceleration is being used
            options.HardwareDecodingCodecs = Array.FindAll(options.HardwareDecodingCodecs, val => !string.Equals(val, whichCodec, StringComparison.OrdinalIgnoreCase));

            // leave blank so ffmpeg will decide
            return null;
        }

        /// <summary>
        /// Gets a hw decoder name.
        /// </summary>
        /// <param name="options">Encoding options.</param>
        /// <param name="decoderPrefix">Decoder prefix.</param>
        /// <param name="decoderSuffix">Decoder suffix.</param>
        /// <param name="videoCodec">Video codec to use.</param>
        /// <param name="bitDepth">Video color bit depth.</param>
        /// <returns>Hardware decoder name.</returns>
        public string GetHwDecoderName(EncodingOptions options, string decoderPrefix, string decoderSuffix, string videoCodec, int bitDepth)
        {
            if (string.IsNullOrEmpty(decoderPrefix) || string.IsNullOrEmpty(decoderSuffix))
            {
                return null;
            }

            var decoderName = decoderPrefix + '_' + decoderSuffix;

            var isCodecAvailable = _mediaEncoder.SupportsDecoder(decoderName) && options.HardwareDecodingCodecs.Contains(videoCodec, StringComparison.OrdinalIgnoreCase);
            if (bitDepth == 10 && isCodecAvailable)
            {
                if (string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                    && options.HardwareDecodingCodecs.Contains("hevc", StringComparison.OrdinalIgnoreCase)
                    && !options.EnableDecodingColorDepth10Hevc)
                {
                    return null;
                }

                if (string.Equals(videoCodec, "vp9", StringComparison.OrdinalIgnoreCase)
                    && options.HardwareDecodingCodecs.Contains("vp9", StringComparison.OrdinalIgnoreCase)
                    && !options.EnableDecodingColorDepth10Vp9)
                {
                    return null;
                }
            }

            if (string.Equals(decoderSuffix, "cuvid", StringComparison.OrdinalIgnoreCase) && options.EnableEnhancedNvdecDecoder)
            {
                return null;
            }

            if (string.Equals(decoderSuffix, "qsv", StringComparison.OrdinalIgnoreCase) && options.PreferSystemNativeHwDecoder)
            {
                return null;
            }

            return isCodecAvailable ? (" -c:v " + decoderName) : null;
        }

        /// <summary>
        /// Gets a hwaccel type to use as a hardware decoder depending on the system.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="videoCodec">Video codec to use.</param>
        /// <param name="bitDepth">Video color bit depth.</param>
        /// <param name="outputHwSurface">Specifies if output hw surface.</param>
        /// <returns>Hardware accelerator type.</returns>
        public string GetHwaccelType(EncodingJobInfo state, EncodingOptions options, string videoCodec, int bitDepth, bool outputHwSurface)
        {
            var isWindows = OperatingSystem.IsWindows();
            var isLinux = OperatingSystem.IsLinux();
            var isMacOS = OperatingSystem.IsMacOS();
            var isD3d11Supported = isWindows && _mediaEncoder.SupportsHwaccel("d3d11va");
            var isVaapiSupported = isLinux && IsVaapiSupported(state);
            var isCudaSupported = (isLinux || isWindows) && IsCudaFullSupported();
            var isQsvSupported = (isLinux || isWindows) && _mediaEncoder.SupportsHwaccel("qsv");
            var isVideotoolboxSupported = isMacOS && _mediaEncoder.SupportsHwaccel("videotoolbox");
            var isCodecAvailable = options.HardwareDecodingCodecs.Contains(videoCodec, StringComparison.OrdinalIgnoreCase);

            var ffmpegVersion = _mediaEncoder.EncoderVersion;

            // Set the av1 codec explicitly to trigger hw accelerator, otherwise libdav1d will be used.
            var isAv1 = ffmpegVersion < _minFFmpegImplictHwaccel
                && string.Equals(videoCodec, "av1", StringComparison.OrdinalIgnoreCase);

            // Allow profile mismatch if decoding H.264 baseline with d3d11va and vaapi hwaccels.
            var profileMismatch = string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase)
                && string.Equals(state.VideoStream?.Profile, "baseline", StringComparison.OrdinalIgnoreCase);

            // Disable the extra internal copy in nvdec. We already handle it in filter chain.
            var nvdecNoInternalCopy = ffmpegVersion >= _minFFmpegHwaUnsafeOutput;

            if (bitDepth == 10 && isCodecAvailable)
            {
                if (string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                    && options.HardwareDecodingCodecs.Contains("hevc", StringComparison.OrdinalIgnoreCase)
                    && !options.EnableDecodingColorDepth10Hevc)
                {
                    return null;
                }

                if (string.Equals(videoCodec, "vp9", StringComparison.OrdinalIgnoreCase)
                    && options.HardwareDecodingCodecs.Contains("vp9", StringComparison.OrdinalIgnoreCase)
                    && !options.EnableDecodingColorDepth10Vp9)
                {
                    return null;
                }
            }

            // Intel qsv/d3d11va/vaapi
            if (string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
            {
                if (options.PreferSystemNativeHwDecoder)
                {
                    if (isVaapiSupported && isCodecAvailable)
                    {
                        return " -hwaccel vaapi" + (outputHwSurface ? " -hwaccel_output_format vaapi" : string.Empty)
                            + (profileMismatch ? " -hwaccel_flags +allow_profile_mismatch" : string.Empty) + (isAv1 ? " -c:v av1" : string.Empty);
                    }

                    if (isD3d11Supported && isCodecAvailable)
                    {
                        return " -hwaccel d3d11va" + (outputHwSurface ? " -hwaccel_output_format d3d11" : string.Empty)
                            + (profileMismatch ? " -hwaccel_flags +allow_profile_mismatch" : string.Empty) + " -threads 2" + (isAv1 ? " -c:v av1" : string.Empty);
                    }
                }
                else
                {
                    if (isQsvSupported && isCodecAvailable)
                    {
                        return " -hwaccel qsv" + (outputHwSurface ? " -hwaccel_output_format qsv" : string.Empty);
                    }
                }
            }

            // Nvidia cuda
            if (string.Equals(options.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
            {
                if (isCudaSupported && isCodecAvailable)
                {
                    if (options.EnableEnhancedNvdecDecoder)
                    {
                        // set -threads 1 to nvdec decoder explicitly since it doesn't implement threading support.
                        return " -hwaccel cuda" + (outputHwSurface ? " -hwaccel_output_format cuda" : string.Empty)
                            + (nvdecNoInternalCopy ? " -hwaccel_flags +unsafe_output" : string.Empty) + " -threads 1" + (isAv1 ? " -c:v av1" : string.Empty);
                    }

                    // cuvid decoder doesn't have threading issue.
                    return " -hwaccel cuda" + (outputHwSurface ? " -hwaccel_output_format cuda" : string.Empty);
                }
            }

            // Amd d3d11va
            if (string.Equals(options.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
            {
                if (isD3d11Supported && isCodecAvailable)
                {
                    return " -hwaccel d3d11va" + (outputHwSurface ? " -hwaccel_output_format d3d11" : string.Empty)
                        + (profileMismatch ? " -hwaccel_flags +allow_profile_mismatch" : string.Empty) + (isAv1 ? " -c:v av1" : string.Empty);
                }
            }

            // Vaapi
            if (string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase)
                && isVaapiSupported
                && isCodecAvailable)
            {
                return " -hwaccel vaapi" + (outputHwSurface ? " -hwaccel_output_format vaapi" : string.Empty)
                    + (profileMismatch ? " -hwaccel_flags +allow_profile_mismatch" : string.Empty) + (isAv1 ? " -c:v av1" : string.Empty);
            }

            // Apple videotoolbox
            if (string.Equals(options.HardwareAccelerationType, "videotoolbox", StringComparison.OrdinalIgnoreCase)
                && isVideotoolboxSupported
                && isCodecAvailable)
            {
                return " -hwaccel videotoolbox" + (outputHwSurface ? " -hwaccel_output_format videotoolbox_vld" : string.Empty);
            }

            return null;
        }

        public string GetQsvHwVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            var isWindows = OperatingSystem.IsWindows();
            var isLinux = OperatingSystem.IsLinux();

            if ((!isWindows && !isLinux)
                || !string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var isQsvOclSupported = _mediaEncoder.SupportsHwaccel("qsv") && IsOpenclFullSupported();
            var isIntelDx11OclSupported = isWindows
                && _mediaEncoder.SupportsHwaccel("d3d11va")
                && isQsvOclSupported;
            var isIntelVaapiOclSupported = isLinux
                && IsVaapiSupported(state)
                && isQsvOclSupported;
            var hwSurface = (isIntelDx11OclSupported || isIntelVaapiOclSupported)
                && _mediaEncoder.SupportsFilter("alphasrc");

            var is8bitSwFormatsQsv = string.Equals("yuv420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                                     || string.Equals("yuvj420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10bitSwFormatsQsv = is8bitSwFormatsQsv || string.Equals("yuv420p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            // TODO: add more 8/10bit and 4:4:4 formats for Qsv after finishing the ffcheck tool

            if (is8bitSwFormatsQsv)
            {
                if (string.Equals(videoStream.Codec, "avc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.Codec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "h264", bitDepth, hwSurface) + GetHwDecoderName(options, "h264", "qsv", "h264", bitDepth);
                }

                if (string.Equals(videoStream.Codec, "vc1", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vc1", bitDepth, hwSurface) + GetHwDecoderName(options, "vc1", "qsv", "vc1", bitDepth);
                }

                if (string.Equals(videoStream.Codec, "vp8", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp8", bitDepth, hwSurface) + GetHwDecoderName(options, "vp8", "qsv", "vp8", bitDepth);
                }

                if (string.Equals(videoStream.Codec, "mpeg2video", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg2video", bitDepth, hwSurface) + GetHwDecoderName(options, "mpeg2", "qsv", "mpeg2video", bitDepth);
                }
            }

            if (is8_10bitSwFormatsQsv)
            {
                if (string.Equals(videoStream.Codec, "hevc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.Codec, "h265", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, hwSurface) + GetHwDecoderName(options, "hevc", "qsv", "hevc", bitDepth);
                }

                if (string.Equals(videoStream.Codec, "vp9", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, hwSurface) + GetHwDecoderName(options, "vp9", "qsv", "vp9", bitDepth);
                }

                if (string.Equals(videoStream.Codec, "av1", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "av1", bitDepth, hwSurface) + GetHwDecoderName(options, "av1", "qsv", "av1", bitDepth);
                }
            }

            return null;
        }

        public string GetNvdecVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            if ((!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
                || !string.Equals(options.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var hwSurface = IsCudaFullSupported() && _mediaEncoder.SupportsFilter("alphasrc");
            var is8bitSwFormatsNvdec = string.Equals("yuv420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                                       || string.Equals("yuvj420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10bitSwFormatsNvdec = is8bitSwFormatsNvdec || string.Equals("yuv420p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            // TODO: add more 8/10/12bit and 4:4:4 formats for Nvdec after finishing the ffcheck tool

            if (is8bitSwFormatsNvdec)
            {
                if (string.Equals("avc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "h264", bitDepth, hwSurface) + GetHwDecoderName(options, "h264", "cuvid", "h264", bitDepth);
                }

                if (string.Equals("mpeg2video", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg2video", bitDepth, hwSurface) + GetHwDecoderName(options, "mpeg2", "cuvid", "mpeg2video", bitDepth);
                }

                if (string.Equals("vc1", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vc1", bitDepth, hwSurface) + GetHwDecoderName(options, "vc1", "cuvid", "vc1", bitDepth);
                }

                if (string.Equals("mpeg4", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg4", bitDepth, hwSurface) + GetHwDecoderName(options, "mpeg4", "cuvid", "mpeg4", bitDepth);
                }

                if (string.Equals("vp8", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp8", bitDepth, hwSurface) + GetHwDecoderName(options, "vp8", "cuvid", "vp8", bitDepth);
                }
            }

            if (is8_10bitSwFormatsNvdec)
            {
                if (string.Equals("hevc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h265", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, hwSurface) + GetHwDecoderName(options, "hevc", "cuvid", "hevc", bitDepth);
                }

                if (string.Equals("vp9", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, hwSurface) + GetHwDecoderName(options, "vp9", "cuvid", "vp9", bitDepth);
                }

                if (string.Equals("av1", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "av1", bitDepth, hwSurface) + GetHwDecoderName(options, "av1", "cuvid", "av1", bitDepth);
                }
            }

            return null;
        }

        public string GetAmfVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            if (!OperatingSystem.IsWindows()
                || !string.Equals(options.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var hwSurface = _mediaEncoder.SupportsHwaccel("d3d11va")
                && IsOpenclFullSupported()
                && _mediaEncoder.SupportsFilter("alphasrc");
            var is8bitSwFormatsAmf = string.Equals("yuv420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                                     || string.Equals("yuvj420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10bitSwFormatsAmf = is8bitSwFormatsAmf || string.Equals("yuv420p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);

            if (is8bitSwFormatsAmf)
            {
                if (string.Equals("avc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "h264", bitDepth, hwSurface);
                }

                if (string.Equals("mpeg2video", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg2video", bitDepth, hwSurface);
                }

                if (string.Equals("vc1", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vc1", bitDepth, hwSurface);
                }
            }

            if (is8_10bitSwFormatsAmf)
            {
                if (string.Equals("hevc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h265", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, hwSurface);
                }

                if (string.Equals("vp9", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, hwSurface);
                }

                if (string.Equals("av1", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "av1", bitDepth, hwSurface);
                }
            }

            return null;
        }

        public string GetVaapiVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            if (!OperatingSystem.IsLinux()
                || !string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var hwSurface = IsVaapiSupported(state)
                && IsVaapiFullSupported()
                && IsOpenclFullSupported()
                && _mediaEncoder.SupportsFilter("alphasrc");
            var is8bitSwFormatsVaapi = string.Equals("yuv420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                                       || string.Equals("yuvj420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10bitSwFormatsVaapi = is8bitSwFormatsVaapi || string.Equals("yuv420p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);

            if (is8bitSwFormatsVaapi)
            {
                if (string.Equals("avc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "h264", bitDepth, hwSurface);
                }

                if (string.Equals("mpeg2video", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg2video", bitDepth, hwSurface);
                }

                if (string.Equals("vc1", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vc1", bitDepth, hwSurface);
                }

                if (string.Equals("vp8", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp8", bitDepth, hwSurface);
                }
            }

            if (is8_10bitSwFormatsVaapi)
            {
                if (string.Equals("hevc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h265", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, hwSurface);
                }

                if (string.Equals("vp9", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, hwSurface);
                }

                if (string.Equals("av1", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "av1", bitDepth, hwSurface);
                }
            }

            return null;
        }

        public string GetVideotoolboxVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            if (!OperatingSystem.IsMacOS()
                || !string.Equals(options.HardwareAccelerationType, "videotoolbox", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var is8bitSwFormatsVt = string.Equals("yuv420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals("yuvj420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10bitSwFormatsVt = is8bitSwFormatsVt || string.Equals("yuv420p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);

            if (is8bitSwFormatsVt)
            {
                if (string.Equals("avc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "h264", bitDepth, false);
                }

                if (string.Equals("mpeg2video", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg2video", bitDepth, false);
                }

                if (string.Equals("mpeg4", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg4", bitDepth, false);
                }
            }

            if (is8_10bitSwFormatsVt)
            {
                if (string.Equals("hevc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h265", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, false);
                }

                if (string.Equals("vp9", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, false);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the number of threads.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="encodingOptions">Encoding options.</param>
        /// <param name="outputVideoCodec">Video codec to use.</param>
        /// <returns>Number of threads.</returns>
#nullable enable
        public static int GetNumberOfThreads(EncodingJobInfo? state, EncodingOptions encodingOptions, string? outputVideoCodec)
        {
            // VP8 and VP9 encoders must have their thread counts set.
            bool mustSetThreadCount = string.Equals(outputVideoCodec, "libvpx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(outputVideoCodec, "libvpx-vp9", StringComparison.OrdinalIgnoreCase);

            var threads = state?.BaseRequest.CpuCoreLimit ?? encodingOptions.EncodingThreadCount;

            if (threads <= 0)
            {
                // Automatically set thread count
                return mustSetThreadCount ? Math.Max(Environment.ProcessorCount - 1, 1) : 0;
            }

            if (threads >= Environment.ProcessorCount)
            {
                return Environment.ProcessorCount;
            }

            return threads;
        }

#nullable disable
        public void TryStreamCopy(EncodingJobInfo state)
        {
            if (state.VideoStream is not null && CanStreamCopyVideo(state, state.VideoStream))
            {
                state.OutputVideoCodec = "copy";
            }
            else
            {
                var user = state.User;

                // If the user doesn't have access to transcoding, then force stream copy, regardless of whether it will be compatible or not
                if (user is not null && !user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding))
                {
                    state.OutputVideoCodec = "copy";
                }
            }

            if (state.AudioStream is not null
                && CanStreamCopyAudio(state, state.AudioStream, state.SupportedAudioCodecs))
            {
                state.OutputAudioCodec = "copy";
            }
            else
            {
                var user = state.User;

                // If the user doesn't have access to transcoding, then force stream copy, regardless of whether it will be compatible or not
                if (user is not null && !user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding))
                {
                    state.OutputAudioCodec = "copy";
                }
            }
        }

        public string GetInputModifier(EncodingJobInfo state, EncodingOptions encodingOptions, string segmentContainer)
        {
            var inputModifier = string.Empty;
            var analyzeDurationArgument = string.Empty;

            // Apply -analyzeduration as per the environment variable,
            // otherwise ffmpeg will break on certain files due to default value is 0.
            var ffmpegAnalyzeDuration = _config.GetFFmpegAnalyzeDuration() ?? string.Empty;

            if (state.MediaSource.AnalyzeDurationMs > 0)
            {
                analyzeDurationArgument = "-analyzeduration " + (state.MediaSource.AnalyzeDurationMs.Value * 1000).ToString(CultureInfo.InvariantCulture);
            }
            else if (!string.IsNullOrEmpty(ffmpegAnalyzeDuration))
            {
                analyzeDurationArgument = "-analyzeduration " + ffmpegAnalyzeDuration;
            }

            if (!string.IsNullOrEmpty(analyzeDurationArgument))
            {
                inputModifier += " " + analyzeDurationArgument;
            }

            inputModifier = inputModifier.Trim();

            // Apply -probesize if configured
            var ffmpegProbeSize = _config.GetFFmpegProbeSize();

            if (!string.IsNullOrEmpty(ffmpegProbeSize))
            {
                inputModifier += $" -probesize {ffmpegProbeSize}";
            }

            var userAgentParam = GetUserAgentParam(state);

            if (!string.IsNullOrEmpty(userAgentParam))
            {
                inputModifier += " " + userAgentParam;
            }

            inputModifier = inputModifier.Trim();

            var refererParam = GetRefererParam(state);

            if (!string.IsNullOrEmpty(refererParam))
            {
                inputModifier += " " + refererParam;
            }

            inputModifier = inputModifier.Trim();

            inputModifier += " " + GetFastSeekCommandLineParameter(state, encodingOptions, segmentContainer);
            inputModifier = inputModifier.Trim();

            if (state.InputProtocol == MediaProtocol.Rtsp)
            {
                inputModifier += " -rtsp_transport tcp+udp -rtsp_flags prefer_tcp";
            }

            if (!string.IsNullOrEmpty(state.InputAudioSync))
            {
                inputModifier += " -async " + state.InputAudioSync;
            }

            if (!string.IsNullOrEmpty(state.InputVideoSync))
            {
                inputModifier += " -vsync " + state.InputVideoSync;
            }

            if (state.ReadInputAtNativeFramerate && state.InputProtocol != MediaProtocol.Rtsp)
            {
                inputModifier += " -re";
            }

            var flags = new List<string>();
            if (state.IgnoreInputDts)
            {
                flags.Add("+igndts");
            }

            if (state.IgnoreInputIndex)
            {
                flags.Add("+ignidx");
            }

            if (state.GenPtsInput || IsCopyCodec(state.OutputVideoCodec))
            {
                flags.Add("+genpts");
            }

            if (state.DiscardCorruptFramesInput)
            {
                flags.Add("+discardcorrupt");
            }

            if (state.EnableFastSeekInput)
            {
                flags.Add("+fastseek");
            }

            if (flags.Count > 0)
            {
                inputModifier += " -fflags " + string.Join(string.Empty, flags);
            }

            if (state.IsVideoRequest)
            {
                if (!string.IsNullOrEmpty(state.InputContainer) && state.VideoType == VideoType.VideoFile && string.IsNullOrEmpty(encodingOptions.HardwareAccelerationType))
                {
                    var inputFormat = GetInputFormat(state.InputContainer);
                    if (!string.IsNullOrEmpty(inputFormat))
                    {
                        inputModifier += " -f " + inputFormat;
                    }
                }
            }

            if (state.MediaSource.RequiresLooping)
            {
                inputModifier += " -stream_loop -1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 2";
            }

            return inputModifier;
        }

        public void AttachMediaSourceInfo(
            EncodingJobInfo state,
            EncodingOptions encodingOptions,
            MediaSourceInfo mediaSource,
            string requestedUrl)
        {
            ArgumentNullException.ThrowIfNull(state);

            ArgumentNullException.ThrowIfNull(mediaSource);

            var path = mediaSource.Path;
            var protocol = mediaSource.Protocol;

            if (!string.IsNullOrEmpty(mediaSource.EncoderPath) && mediaSource.EncoderProtocol.HasValue)
            {
                path = mediaSource.EncoderPath;
                protocol = mediaSource.EncoderProtocol.Value;
            }

            state.MediaPath = path;
            state.InputProtocol = protocol;
            state.InputContainer = mediaSource.Container;
            state.RunTimeTicks = mediaSource.RunTimeTicks;
            state.RemoteHttpHeaders = mediaSource.RequiredHttpHeaders;

            state.IsoType = mediaSource.IsoType;

            if (mediaSource.Timestamp.HasValue)
            {
                state.InputTimestamp = mediaSource.Timestamp.Value;
            }

            state.RunTimeTicks = mediaSource.RunTimeTicks;
            state.RemoteHttpHeaders = mediaSource.RequiredHttpHeaders;
            state.ReadInputAtNativeFramerate = mediaSource.ReadAtNativeFramerate;

            if (state.ReadInputAtNativeFramerate
                || (mediaSource.Protocol == MediaProtocol.File
                && string.Equals(mediaSource.Container, "wtv", StringComparison.OrdinalIgnoreCase)))
            {
                state.InputVideoSync = "-1";
                state.InputAudioSync = "1";
            }

            if (string.Equals(mediaSource.Container, "wma", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mediaSource.Container, "asf", StringComparison.OrdinalIgnoreCase))
            {
                // Seeing some stuttering when transcoding wma to audio-only HLS
                state.InputAudioSync = "1";
            }

            var mediaStreams = mediaSource.MediaStreams;

            if (state.IsVideoRequest)
            {
                var videoRequest = state.BaseRequest;

                if (string.IsNullOrEmpty(videoRequest.VideoCodec))
                {
                    if (string.IsNullOrEmpty(requestedUrl))
                    {
                        requestedUrl = "test." + videoRequest.Container;
                    }

                    videoRequest.VideoCodec = InferVideoCodec(requestedUrl);
                }

                state.VideoStream = GetMediaStream(mediaStreams, videoRequest.VideoStreamIndex, MediaStreamType.Video);
                state.SubtitleStream = GetMediaStream(mediaStreams, videoRequest.SubtitleStreamIndex, MediaStreamType.Subtitle, false);
                state.SubtitleDeliveryMethod = videoRequest.SubtitleMethod;
                state.AudioStream = GetMediaStream(mediaStreams, videoRequest.AudioStreamIndex, MediaStreamType.Audio);

                if (state.SubtitleStream is not null && !state.SubtitleStream.IsExternal)
                {
                    state.InternalSubtitleStreamOffset = mediaStreams.Where(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal).ToList().IndexOf(state.SubtitleStream);
                }

                EnforceResolutionLimit(state);

                NormalizeSubtitleEmbed(state);
            }
            else
            {
                state.AudioStream = GetMediaStream(mediaStreams, null, MediaStreamType.Audio, true);
            }

            state.MediaSource = mediaSource;

            var request = state.BaseRequest;
            var supportedAudioCodecs = state.SupportedAudioCodecs;
            if (request is not null && supportedAudioCodecs is not null && supportedAudioCodecs.Length > 0)
            {
                var supportedAudioCodecsList = supportedAudioCodecs.ToList();

                ShiftAudioCodecsIfNeeded(supportedAudioCodecsList, state.AudioStream);

                state.SupportedAudioCodecs = supportedAudioCodecsList.ToArray();

                request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(i => _mediaEncoder.CanEncodeToAudioCodec(i))
                    ?? state.SupportedAudioCodecs.FirstOrDefault();
            }

            var supportedVideoCodecs = state.SupportedVideoCodecs;
            if (request is not null && supportedVideoCodecs is not null && supportedVideoCodecs.Length > 0)
            {
                var supportedVideoCodecsList = supportedVideoCodecs.ToList();

                ShiftVideoCodecsIfNeeded(supportedVideoCodecsList, encodingOptions);

                state.SupportedVideoCodecs = supportedVideoCodecsList.ToArray();

                request.VideoCodec = state.SupportedVideoCodecs.FirstOrDefault();
            }
        }

        private void ShiftAudioCodecsIfNeeded(List<string> audioCodecs, MediaStream audioStream)
        {
            // No need to shift if there is only one supported audio codec.
            if (audioCodecs.Count < 2)
            {
                return;
            }

            var inputChannels = audioStream is null ? 6 : audioStream.Channels ?? 6;
            var shiftAudioCodecs = new List<string>();
            if (inputChannels >= 6)
            {
                // DTS and TrueHD are not supported by HLS
                // Keep them in the supported codecs list, but shift them to the end of the list so that if transcoding happens, another codec is used
                shiftAudioCodecs.Add("dca");
                shiftAudioCodecs.Add("truehd");
            }
            else
            {
                // Transcoding to 2ch ac3 or eac3 almost always causes a playback failure
                // Keep them in the supported codecs list, but shift them to the end of the list so that if transcoding happens, another codec is used
                shiftAudioCodecs.Add("ac3");
                shiftAudioCodecs.Add("eac3");
            }

            if (audioCodecs.All(i => shiftAudioCodecs.Contains(i, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            while (shiftAudioCodecs.Contains(audioCodecs[0], StringComparison.OrdinalIgnoreCase))
            {
                var removed = shiftAudioCodecs[0];
                audioCodecs.RemoveAt(0);
                audioCodecs.Add(removed);
            }
        }

        private void ShiftVideoCodecsIfNeeded(List<string> videoCodecs, EncodingOptions encodingOptions)
        {
            // No need to shift if there is only one supported video codec.
            if (videoCodecs.Count < 2)
            {
                return;
            }

            // Shift codecs to the end of list if it's not allowed.
            var shiftVideoCodecs = new List<string>();
            if (!encodingOptions.AllowHevcEncoding)
            {
                shiftVideoCodecs.Add("hevc");
                shiftVideoCodecs.Add("h265");
            }

            if (!encodingOptions.AllowAv1Encoding)
            {
                shiftVideoCodecs.Add("av1");
            }

            if (videoCodecs.All(i => shiftVideoCodecs.Contains(i, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            while (shiftVideoCodecs.Contains(videoCodecs[0], StringComparison.OrdinalIgnoreCase))
            {
                var removed = shiftVideoCodecs[0];
                videoCodecs.RemoveAt(0);
                videoCodecs.Add(removed);
            }
        }

        private void NormalizeSubtitleEmbed(EncodingJobInfo state)
        {
            if (state.SubtitleStream is null || state.SubtitleDeliveryMethod != SubtitleDeliveryMethod.Embed)
            {
                return;
            }

            // This is tricky to remux in, after converting to dvdsub it's not positioned correctly
            // Therefore, let's just burn it in
            if (string.Equals(state.SubtitleStream.Codec, "DVBSUB", StringComparison.OrdinalIgnoreCase))
            {
                state.SubtitleDeliveryMethod = SubtitleDeliveryMethod.Encode;
            }
        }

        public string GetSubtitleEmbedArguments(EncodingJobInfo state)
        {
            if (state.SubtitleStream is null || state.SubtitleDeliveryMethod != SubtitleDeliveryMethod.Embed)
            {
                return string.Empty;
            }

            var format = state.SupportedSubtitleCodecs.FirstOrDefault();
            string codec;

            if (string.IsNullOrEmpty(format) || string.Equals(format, state.SubtitleStream.Codec, StringComparison.OrdinalIgnoreCase))
            {
                codec = "copy";
            }
            else
            {
                codec = format;
            }

            return " -codec:s:0 " + codec + " -disposition:s:0 default";
        }

        public string GetProgressiveVideoFullCommandLine(EncodingJobInfo state, EncodingOptions encodingOptions, string outputPath, string defaultPreset)
        {
            // Get the output codec name
            var videoCodec = GetVideoEncoder(state, encodingOptions);

            var format = string.Empty;
            var keyFrame = string.Empty;

            if (Path.GetExtension(outputPath.AsSpan()).Equals(".mp4", StringComparison.OrdinalIgnoreCase)
                && state.BaseRequest.Context == EncodingContext.Streaming)
            {
                // Comparison: https://github.com/jansmolders86/mediacenterjs/blob/master/lib/transcoding/desktop.js
                format = " -f mp4 -movflags frag_keyframe+empty_moov+delay_moov";
            }

            var threads = GetNumberOfThreads(state, encodingOptions, videoCodec);

            var inputModifier = GetInputModifier(state, encodingOptions, null);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}{2} {3} {4} -map_metadata -1 -map_chapters -1 -threads {5} {6}{7}{8} -y \"{9}\"",
                inputModifier,
                GetInputArgument(state, encodingOptions, null),
                keyFrame,
                GetMapArgs(state),
                GetProgressiveVideoArguments(state, encodingOptions, videoCodec, defaultPreset),
                threads,
                GetProgressiveVideoAudioArguments(state, encodingOptions),
                GetSubtitleEmbedArguments(state),
                format,
                outputPath).Trim();
        }

        public string GetOutputFFlags(EncodingJobInfo state)
        {
            var flags = new List<string>();
            if (state.GenPtsOutput)
            {
                flags.Add("+genpts");
            }

            if (flags.Count > 0)
            {
                return " -fflags " + string.Join(string.Empty, flags);
            }

            return string.Empty;
        }

        public string GetProgressiveVideoArguments(EncodingJobInfo state, EncodingOptions encodingOptions, string videoCodec, string defaultPreset)
        {
            var args = "-codec:v:0 " + videoCodec;

            if (state.BaseRequest.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            if (IsCopyCodec(videoCodec))
            {
                if (state.VideoStream is not null
                    && string.Equals(state.OutputContainer, "ts", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
                {
                    string bitStreamArgs = GetBitStreamArgs(state.VideoStream);
                    if (!string.IsNullOrEmpty(bitStreamArgs))
                    {
                        args += " " + bitStreamArgs;
                    }
                }

                if (state.RunTimeTicks.HasValue && state.BaseRequest.CopyTimestamps)
                {
                    args += " -copyts -avoid_negative_ts disabled -start_at_zero";
                }

                if (!state.RunTimeTicks.HasValue)
                {
                    args += " -fflags +genpts";
                }
            }
            else
            {
                var keyFrameArg = string.Format(
                    CultureInfo.InvariantCulture,
                    " -force_key_frames \"expr:gte(t,n_forced*{0})\"",
                    5);

                args += keyFrameArg;

                var hasGraphicalSubs = state.SubtitleStream is not null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

                var hasCopyTs = false;

                // video processing filters.
                var videoProcessParam = GetVideoProcessingFilterParam(state, encodingOptions, videoCodec);

                var negativeMapArgs = GetNegativeMapArgsByFilters(state, videoProcessParam);

                args = negativeMapArgs + args + videoProcessParam;

                hasCopyTs = videoProcessParam.Contains("copyts", StringComparison.OrdinalIgnoreCase);

                if (state.RunTimeTicks.HasValue && state.BaseRequest.CopyTimestamps)
                {
                    if (!hasCopyTs)
                    {
                        args += " -copyts";
                    }

                    args += " -avoid_negative_ts disabled";

                    if (!(state.SubtitleStream is not null && state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream))
                    {
                        args += " -start_at_zero";
                    }
                }

                var qualityParam = GetVideoQualityParam(state, videoCodec, encodingOptions, defaultPreset);

                if (!string.IsNullOrEmpty(qualityParam))
                {
                    args += " " + qualityParam.Trim();
                }
            }

            if (!string.IsNullOrEmpty(state.OutputVideoSync))
            {
                args += " -vsync " + state.OutputVideoSync;
            }

            args += GetOutputFFlags(state);

            return args;
        }

        public string GetProgressiveVideoAudioArguments(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            // If the video doesn't have an audio stream, return a default.
            if (state.AudioStream is null && state.VideoStream is not null)
            {
                return string.Empty;
            }

            // Get the output codec name
            var codec = GetAudioEncoder(state);

            var args = "-codec:a:0 " + codec;

            if (IsCopyCodec(codec))
            {
                return args;
            }

            var channels = state.OutputAudioChannels;

            if (channels.HasValue && ((channels.Value != 2 && state.AudioStream.Channels <= 5) || encodingOptions.DownMixStereoAlgorithm == DownMixStereoAlgorithms.None))
            {
                args += " -ac " + channels.Value;
            }

            var bitrate = state.OutputAudioBitrate;
            if (bitrate.HasValue && !LosslessAudioCodecs.Contains(codec, StringComparison.OrdinalIgnoreCase))
            {
                var vbrParam = GetAudioVbrModeParam(codec, bitrate.Value / (channels ?? 2));
                if (encodingOptions.EnableAudioVbr && vbrParam is not null)
                {
                    args += vbrParam;
                }
                else
                {
                    args += " -ab " + bitrate.Value.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                args += " -ar " + state.OutputAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture);
            }

            args += GetAudioFilterParam(state, encodingOptions);

            return args;
        }

        public string GetProgressiveAudioFullCommandLine(EncodingJobInfo state, EncodingOptions encodingOptions, string outputPath)
        {
            var audioTranscodeParams = new List<string>();

            var bitrate = state.OutputAudioBitrate;
            var channels = state.OutputAudioChannels;
            var outputCodec = state.OutputAudioCodec;

            if (bitrate.HasValue && !LosslessAudioCodecs.Contains(outputCodec, StringComparison.OrdinalIgnoreCase))
            {
                var vbrParam = GetAudioVbrModeParam(GetAudioEncoder(state), bitrate.Value / (channels ?? 2));
                if (encodingOptions.EnableAudioVbr && vbrParam is not null)
                {
                    audioTranscodeParams.Add(vbrParam);
                }
                else
                {
                    audioTranscodeParams.Add("-ab " + bitrate.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (channels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + state.OutputAudioChannels.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(outputCodec))
            {
                audioTranscodeParams.Add("-acodec " + GetAudioEncoder(state));
            }

            if (GetAudioEncoder(state).StartsWith("pcm_", StringComparison.Ordinal))
            {
                audioTranscodeParams.Add(string.Concat("-f ", GetAudioEncoder(state).AsSpan(4)));
                audioTranscodeParams.Add("-ar " + state.BaseRequest.AudioBitRate);
            }

            if (!string.Equals(outputCodec, "opus", StringComparison.OrdinalIgnoreCase))
            {
                // opus only supports specific sampling rates
                var sampleRate = state.OutputAudioSampleRate;
                if (sampleRate.HasValue)
                {
                    var sampleRateValue = sampleRate.Value switch
                    {
                        <= 8000 => 8000,
                        <= 12000 => 12000,
                        <= 16000 => 16000,
                        <= 24000 => 24000,
                        _ => 48000
                    };

                    audioTranscodeParams.Add("-ar " + sampleRateValue.ToString(CultureInfo.InvariantCulture));
                }
            }

            // Copy the movflags from GetProgressiveVideoFullCommandLine
            // See #9248 and the associated PR for why this is needed
            if (_mp4ContainerNames.Contains(state.OutputContainer))
            {
                audioTranscodeParams.Add("-movflags empty_moov+delay_moov");
            }

            var threads = GetNumberOfThreads(state, encodingOptions, null);

            var inputModifier = GetInputModifier(state, encodingOptions, null);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}{7}{8} -threads {2}{3} {4} -id3v2_version 3 -write_id3v1 1{6} -y \"{5}\"",
                inputModifier,
                GetInputArgument(state, encodingOptions, null),
                threads,
                " -vn",
                string.Join(' ', audioTranscodeParams),
                outputPath,
                string.Empty,
                string.Empty,
                string.Empty).Trim();
        }

        public static int FindIndex(IReadOnlyList<MediaStream> mediaStreams, MediaStream streamToFind)
        {
            var index = 0;
            var length = mediaStreams.Count;

            for (var i = 0; i < length; i++)
            {
                var currentMediaStream = mediaStreams[i];
                if (currentMediaStream == streamToFind)
                {
                    return index;
                }

                if (string.Equals(currentMediaStream.Path, streamToFind.Path, StringComparison.Ordinal))
                {
                    index++;
                }
            }

            return -1;
        }

        public static bool IsCopyCodec(string codec)
        {
            return string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase);
        }
    }
}
