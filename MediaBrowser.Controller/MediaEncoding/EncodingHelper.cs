#nullable disable

#pragma warning disable CS1591
// We need lowercase normalized string for ffmpeg
#pragma warning disable CA1308

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace MediaBrowser.Controller.MediaEncoding
{
    public partial class EncodingHelper
    {
        /// <summary>
        /// The codec validation regex.
        /// This regular expression matches strings that consist of alphanumeric characters, hyphens,
        /// periods, underscores, commas, and vertical bars, with a length between 0 and 40 characters.
        /// This should matches all common valid codecs.
        /// </summary>
        public const string ContainerValidationRegex = @"^[a-zA-Z0-9\-\._,|]{0,40}$";

        /// <summary>
        /// The level validation regex.
        /// This regular expression matches strings representing a double.
        /// </summary>
        public const string LevelValidationRegex = @"-?[0-9]+(?:\.[0-9]+)?";

        private const string _defaultMjpegEncoder = "mjpeg";

        private const string QsvAlias = "qs";
        private const string VaapiAlias = "va";
        private const string D3d11vaAlias = "dx11";
        private const string VideotoolboxAlias = "vt";
        private const string RkmppAlias = "rk";
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
        private readonly Version _minKernelVersionAmdVkFmtModifier = new Version(5, 15);

        private readonly Version _minFFmpegImplictHwaccel = new Version(6, 0);
        private readonly Version _minFFmpegHwaUnsafeOutput = new Version(6, 0);
        private readonly Version _minFFmpegOclCuTonemapMode = new Version(5, 1, 3);
        private readonly Version _minFFmpegSvtAv1Params = new Version(5, 1);
        private readonly Version _minFFmpegVaapiH26xEncA53CcSei = new Version(6, 0);
        private readonly Version _minFFmpegReadrateOption = new Version(5, 0);
        private readonly Version _minFFmpegWorkingVtHwSurface = new Version(7, 0, 1);
        private readonly Version _minFFmpegDisplayRotationOption = new Version(6, 0);
        private readonly Version _minFFmpegAdvancedTonemapMode = new Version(7, 0, 1);
        private readonly Version _minFFmpegAlteredVaVkInterop = new Version(7, 0, 1);
        private readonly Version _minFFmpegQsvVppTonemapOption = new Version(7, 0, 1);
        private readonly Version _minFFmpegQsvVppOutRangeOption = new Version(7, 0, 1);
        private readonly Version _minFFmpegVaapiDeviceVendorId = new Version(7, 0, 1);
        private readonly Version _minFFmpegQsvVppScaleModeOption = new Version(6, 0);

        private static readonly Regex _containerValidationRegex = new(ContainerValidationRegex, RegexOptions.Compiled);

        private static readonly string[] _videoProfilesH264 =
        [
            "ConstrainedBaseline",
            "Baseline",
            "Extended",
            "Main",
            "High",
            "ProgressiveHigh",
            "ConstrainedHigh",
            "High10"
        ];

        private static readonly string[] _videoProfilesH265 =
        [
            "Main",
            "Main10"
        ];

        private static readonly string[] _videoProfilesAv1 =
        [
            "Main",
            "High",
            "Professional",
        ];

        private static readonly HashSet<string> _mp4ContainerNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "mp4",
            "m4a",
            "m4p",
            "m4b",
            "m4r",
            "m4v",
        };

        private static readonly TonemappingMode[] _legacyTonemapModes = [TonemappingMode.max, TonemappingMode.rgb];
        private static readonly TonemappingMode[] _advancedTonemapModes = [TonemappingMode.lum, TonemappingMode.itp];

        // Set max transcoding channels for encoders that can't handle more than a set amount of channels
        // AAC, FLAC, ALAC, libopus, libvorbis encoders all support at least 8 channels
        private static readonly Dictionary<string, int> _audioTranscodeChannelLookup = new(StringComparer.OrdinalIgnoreCase)
        {
            { "libmp3lame", 2 },
            { "libfdk_aac", 6 },
            { "ac3", 6 },
            { "eac3", 6 },
            { "dca", 6 },
            { "mlp", 6 },
            { "truehd", 6 },
        };

        private static readonly Dictionary<HardwareAccelerationType, string> _mjpegCodecMap = new()
        {
            { HardwareAccelerationType.vaapi, _defaultMjpegEncoder + "_vaapi" },
            { HardwareAccelerationType.qsv, _defaultMjpegEncoder + "_qsv" },
            { HardwareAccelerationType.videotoolbox, _defaultMjpegEncoder + "_videotoolbox" },
            { HardwareAccelerationType.rkmpp, _defaultMjpegEncoder + "_rkmpp" }
        };

        public static readonly string[] LosslessAudioCodecs =
        [
            "alac",
            "ape",
            "flac",
            "mlp",
            "truehd",
            "wavpack"
        ];

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

                var codecMap = new Dictionary<HardwareAccelerationType, string>()
                {
                    { HardwareAccelerationType.amf,                  hwEncoder + "_amf" },
                    { HardwareAccelerationType.nvenc,                hwEncoder + "_nvenc" },
                    { HardwareAccelerationType.qsv,                  hwEncoder + "_qsv" },
                    { HardwareAccelerationType.vaapi,                hwEncoder + "_vaapi" },
                    { HardwareAccelerationType.videotoolbox,         hwEncoder + "_videotoolbox" },
                    { HardwareAccelerationType.v4l2m2m,              hwEncoder + "_v4l2m2m" },
                    { HardwareAccelerationType.rkmpp,                hwEncoder + "_rkmpp" },
                };

                if (hwType != HardwareAccelerationType.none
                    && encodingOptions.EnableHardwareEncoding
                    && codecMap.TryGetValue(hwType, out var preferredEncoder)
                    && _mediaEncoder.SupportsEncoder(preferredEncoder))
                {
                    return preferredEncoder;
                }
            }

            return defaultEncoder;
        }

        private string GetMjpegEncoder(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            if (state.VideoType == VideoType.VideoFile)
            {
                var hwType = encodingOptions.HardwareAccelerationType;

                // Only Intel has VA-API MJPEG encoder
                if (hwType == HardwareAccelerationType.vaapi
                    && !(_mediaEncoder.IsVaapiDeviceInteliHD
                         || _mediaEncoder.IsVaapiDeviceInteli965))
                {
                    return _defaultMjpegEncoder;
                }

                if (hwType != HardwareAccelerationType.none
                    && encodingOptions.EnableHardwareEncoding
                    && _mjpegCodecMap.TryGetValue(hwType, out var preferredEncoder)
                    && _mediaEncoder.SupportsEncoder(preferredEncoder))
                {
                    return preferredEncoder;
                }
            }

            return _defaultMjpegEncoder;
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
                   && _mediaEncoder.SupportsFilter("transpose_vaapi")
                   && _mediaEncoder.SupportsFilter("hwupload_vaapi");
        }

        private bool IsRkmppFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("rkmpp")
                   && _mediaEncoder.SupportsFilter("scale_rkrga")
                   && _mediaEncoder.SupportsFilter("vpp_rkrga")
                   && _mediaEncoder.SupportsFilter("overlay_rkrga");
        }

        private bool IsOpenclFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("opencl")
                   && _mediaEncoder.SupportsFilter("scale_opencl")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.TonemapOpenclBt2390)
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.OverlayOpenclFrameSync);

            // Let transpose_opencl optional for the time being.
        }

        private bool IsCudaFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("cuda")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.ScaleCudaFormat)
                   && _mediaEncoder.SupportsFilter("yadif_cuda")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.TonemapCudaName)
                   && _mediaEncoder.SupportsFilter("overlay_cuda")
                   && _mediaEncoder.SupportsFilter("hwupload_cuda");

            // Let transpose_cuda optional for the time being.
        }

        private bool IsVulkanFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("vulkan")
                   && _mediaEncoder.SupportsFilter("libplacebo")
                   && _mediaEncoder.SupportsFilter("scale_vulkan")
                   && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.OverlayVulkanFrameSync)
                   && _mediaEncoder.SupportsFilter("transpose_vulkan")
                   && _mediaEncoder.SupportsFilter("flip_vulkan");
        }

        private bool IsVideoToolboxFullSupported()
        {
            return _mediaEncoder.SupportsHwaccel("videotoolbox")
                && _mediaEncoder.SupportsFilter("yadif_videotoolbox")
                && _mediaEncoder.SupportsFilter("overlay_videotoolbox")
                && _mediaEncoder.SupportsFilter("tonemap_videotoolbox")
                && _mediaEncoder.SupportsFilter("scale_vt");

            // Let transpose_vt optional for the time being.
        }

        private bool IsSwTonemapAvailable(EncodingJobInfo state, EncodingOptions options)
        {
            if (state.VideoStream is null
                || GetVideoColorBitDepth(state) < 10
                || !_mediaEncoder.SupportsFilter("tonemapx"))
            {
                return false;
            }

            return state.VideoStream.VideoRange == VideoRange.HDR;
        }

        private bool IsHwTonemapAvailable(EncodingJobInfo state, EncodingOptions options)
        {
            if (state.VideoStream is null
                || !options.EnableTonemapping
                || GetVideoColorBitDepth(state) < 10)
            {
                return false;
            }

            if (state.VideoStream.VideoRange == VideoRange.HDR
                && state.VideoStream.VideoRangeType == VideoRangeType.DOVI)
            {
                // Only native SW decoder and HW accelerator can parse dovi rpu.
                var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
                var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
                var isNvdecDecoder = vidDecoder.Contains("cuda", StringComparison.OrdinalIgnoreCase);
                var isVaapiDecoder = vidDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
                var isD3d11vaDecoder = vidDecoder.Contains("d3d11va", StringComparison.OrdinalIgnoreCase);
                var isVideoToolBoxDecoder = vidDecoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase);
                return isSwDecoder || isNvdecDecoder || isVaapiDecoder || isD3d11vaDecoder || isVideoToolBoxDecoder;
            }

            return state.VideoStream.VideoRange == VideoRange.HDR
                   && (state.VideoStream.VideoRangeType == VideoRangeType.HDR10
                       || state.VideoStream.VideoRangeType == VideoRangeType.HLG
                       || state.VideoStream.VideoRangeType == VideoRangeType.DOVIWithHDR10
                       || state.VideoStream.VideoRangeType == VideoRangeType.DOVIWithHLG);
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

        private bool IsIntelVppTonemapAvailable(EncodingJobInfo state, EncodingOptions options)
        {
            if (state.VideoStream is null
                || !options.EnableVppTonemapping
                || GetVideoColorBitDepth(state) < 10)
            {
                return false;
            }

            // prefer 'tonemap_vaapi' over 'vpp_qsv' on Linux for supporting Gen9/KBLx.
            // 'vpp_qsv' requires VPL, which is only supported on Gen12/TGLx and newer.
            if (OperatingSystem.IsWindows()
                && options.HardwareAccelerationType == HardwareAccelerationType.qsv
                && _mediaEncoder.EncoderVersion < _minFFmpegQsvVppTonemapOption)
            {
                return false;
            }

            return state.VideoStream.VideoRange == VideoRange.HDR
                   && (state.VideoStream.VideoRangeType == VideoRangeType.HDR10
                       || state.VideoStream.VideoRangeType == VideoRangeType.DOVIWithHDR10);
        }

        private bool IsVideoToolboxTonemapAvailable(EncodingJobInfo state, EncodingOptions options)
        {
            if (state.VideoStream is null
                || !options.EnableVideoToolboxTonemapping
                || GetVideoColorBitDepth(state) < 10)
            {
                return false;
            }

            // Certain DV profile 5 video works in Safari with direct playing, but the VideoToolBox does not produce correct mapping results with transcoding.
            // All other HDR formats working.
            return state.VideoStream.VideoRange == VideoRange.HDR
                   && state.VideoStream.VideoRangeType is VideoRangeType.HDR10 or VideoRangeType.HLG or VideoRangeType.HDR10Plus or VideoRangeType.DOVIWithHDR10 or VideoRangeType.DOVIWithHLG;
        }

        private bool IsVideoStreamHevcRext(EncodingJobInfo state)
        {
            var videoStream = state.VideoStream;
            if (videoStream is null)
            {
                return false;
            }

            return string.Equals(videoStream.Codec, "hevc", StringComparison.OrdinalIgnoreCase)
                   && (string.Equals(videoStream.Profile, "Rext", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(videoStream.PixelFormat, "yuv420p12le", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(videoStream.PixelFormat, "yuv422p", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(videoStream.PixelFormat, "yuv422p10le", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(videoStream.PixelFormat, "yuv422p12le", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(videoStream.PixelFormat, "yuv444p", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(videoStream.PixelFormat, "yuv444p10le", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(videoStream.PixelFormat, "yuv444p12le", StringComparison.OrdinalIgnoreCase));
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

                if (string.Equals(codec, "mjpeg", StringComparison.OrdinalIgnoreCase))
                {
                    return GetMjpegEncoder(state, encodingOptions);
                }

                if (_containerValidationRegex.IsMatch(codec))
                {
                    return codec.ToLowerInvariant();
                }
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
            if (string.IsNullOrEmpty(container) || !_containerValidationRegex.IsMatch(container))
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
            if (string.IsNullOrWhiteSpace(container))
            {
                // this may not work, but if the client is that broken we can not do anything better
                return "aac";
            }

            var inferredCodec = container.ToLowerInvariant();

            return inferredCodec switch
            {
                "ogg" or "oga" or "ogv" or "webm" or "webma" => "opus",
                "m4a" or "m4b" or "mp4" or "mov" or "mkv" or "mka" => "aac",
                "ts" or "avi" or "flv" or "f4v" or "swf" => "mp3",
                _ => inferredCodec
            };
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

        /// <summary>
        /// Gets the audio encoder.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        public string GetAudioEncoder(EncodingJobInfo state)
        {
            var codec = state.OutputAudioCodec;

            if (!_containerValidationRegex.IsMatch(codec))
            {
                codec = "aac";
            }

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

            if (string.Equals(codec, "alac", StringComparison.OrdinalIgnoreCase))
            {
                // The ffmpeg upstream breaks the AudioToolbox ALAC encoder in version 6.1 but fixes it in version 7.0.
                // Since ALAC is lossless in quality and the AudioToolbox encoder is not faster,
                // its only benefit is a smaller file size.
                // To prevent problems, use the ffmpeg native encoder instead.
                return "alac";
            }

            return codec.ToLowerInvariant();
        }

        private string GetRkmppDeviceArgs(string alias)
        {
            alias ??= RkmppAlias;

            // device selection in rk is not supported.
            return " -init_hw_device rkmpp=" + alias;
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

        private string GetVaapiDeviceArgs(string renderNodePath, string driver, string kernelDriver, string vendorId, string srcDeviceAlias, string alias)
        {
            alias ??= VaapiAlias;
            var haveVendorId = !string.IsNullOrEmpty(vendorId)
                && _mediaEncoder.EncoderVersion >= _minFFmpegVaapiDeviceVendorId;

            // Priority: 'renderNodePath' > 'vendorId' > 'kernelDriver'
            var driverOpts = string.IsNullOrEmpty(renderNodePath)
                ? (haveVendorId ? $",vendor_id={vendorId}" : (string.IsNullOrEmpty(kernelDriver) ? string.Empty : $",kernel_driver={kernelDriver}"))
                : renderNodePath;

            // 'driver' behaves similarly to env LIBVA_DRIVER_NAME
            driverOpts += string.IsNullOrEmpty(driver) ? string.Empty : ",driver=" + driver;

            var options = string.IsNullOrEmpty(srcDeviceAlias)
                ? (string.IsNullOrEmpty(driverOpts) ? string.Empty : ":" + driverOpts)
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

        private string GetQsvDeviceArgs(string renderNodePath, string alias)
        {
            var arg = " -init_hw_device qsv=" + (alias ?? QsvAlias);
            if (OperatingSystem.IsLinux())
            {
                // derive qsv from vaapi device
                return GetVaapiDeviceArgs(renderNodePath, "iHD", "i915", "0x8086", null, VaapiAlias) + arg + "@" + VaapiAlias;
            }

            if (OperatingSystem.IsWindows())
            {
                // on Windows, the deviceIndex is an int
                if (int.TryParse(renderNodePath, NumberStyles.Integer, CultureInfo.InvariantCulture, out int deviceIndex))
                {
                    return GetD3d11vaDeviceArgs(deviceIndex, string.Empty, D3d11vaAlias) + arg + "@" + D3d11vaAlias;
                }

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
            // DVBSUB uses the fixed canvas size 720x576
            if (state.SubtitleStream is not null
                && ShouldEncodeSubtitle(state)
                && !state.SubtitleStream.IsTextSubtitleStream
                && !string.Equals(state.SubtitleStream.Codec, "DVBSUB", StringComparison.OrdinalIgnoreCase))
            {
                var subtitleWidth = state.SubtitleStream?.Width;
                var subtitleHeight = state.SubtitleStream?.Height;

                if (subtitleWidth.HasValue
                    && subtitleHeight.HasValue
                    && subtitleWidth.Value > 0
                    && subtitleHeight.Value > 0)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        " -canvas_size {0}x{1}",
                        subtitleWidth.Value,
                        subtitleHeight.Value);
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

            if (optHwaccelType == HardwareAccelerationType.vaapi)
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
                    args.Append(GetVaapiDeviceArgs(options.VaapiDevice, "iHD", null, null, null, VaapiAlias));
                }
                else if (_mediaEncoder.IsVaapiDeviceInteli965)
                {
                    // Only override i965 since it has lower priority than iHD in libva lookup.
                    Environment.SetEnvironmentVariable("LIBVA_DRIVER_NAME", "i965");
                    Environment.SetEnvironmentVariable("LIBVA_DRIVER_NAME_JELLYFIN", "i965");
                    args.Append(GetVaapiDeviceArgs(options.VaapiDevice, "i965", null, null, null, VaapiAlias));
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
                        && _mediaEncoder.IsVaapiDeviceSupportVulkanDrmInterop
                        && Environment.OSVersion.Version >= _minKernelVersionAmdVkFmtModifier)
                    {
                        args.Append(GetDrmDeviceArgs(options.VaapiDevice, DrmAlias));
                        args.Append(GetVaapiDeviceArgs(null, null, null, null, DrmAlias, VaapiAlias));
                        args.Append(GetVulkanDeviceArgs(0, null, DrmAlias, VulkanAlias));

                        // libplacebo wants an explicitly set vulkan filter device.
                        filterDevArgs = GetFilterHwDeviceArgs(VulkanAlias);
                    }
                    else
                    {
                        args.Append(GetVaapiDeviceArgs(options.VaapiDevice, null, null, null, null, VaapiAlias));
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
            else if (optHwaccelType == HardwareAccelerationType.qsv)
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

                args.Append(GetQsvDeviceArgs(options.QsvDevice, QsvAlias));
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
            else if (optHwaccelType == HardwareAccelerationType.nvenc)
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
            else if (optHwaccelType == HardwareAccelerationType.amf)
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
            else if (optHwaccelType == HardwareAccelerationType.videotoolbox)
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

                // videotoolbox hw filter does not require device selection
                args.Append(GetVideoToolboxDeviceArgs(VideotoolboxAlias));
            }
            else if (optHwaccelType == HardwareAccelerationType.rkmpp)
            {
                if (!isLinux || !_mediaEncoder.SupportsHwaccel("rkmpp"))
                {
                    return string.Empty;
                }

                var isRkmppDecoder = vidDecoder.Contains("rkmpp", StringComparison.OrdinalIgnoreCase);
                var isRkmppEncoder = vidEncoder.Contains("rkmpp", StringComparison.OrdinalIgnoreCase);
                if (!isRkmppDecoder && !isRkmppEncoder)
                {
                    return string.Empty;
                }

                args.Append(GetRkmppDeviceArgs(RkmppAlias));

                var filterDevArgs = string.Empty;
                var doOclTonemap = isHwTonemapAvailable && IsOpenclFullSupported();

                if (doOclTonemap && !isRkmppDecoder)
                {
                    args.Append(GetOpenclDeviceArgs(0, null, RkmppAlias, OpenclAlias));
                    filterDevArgs = GetFilterHwDeviceArgs(OpenclAlias);
                }

                args.Append(filterDevArgs);
            }

            if (!string.IsNullOrEmpty(vidDecoder))
            {
                args.Append(vidDecoder);
            }

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
                var concatFilePath = Path.Join(_configurationManager.CommonApplicationPaths.CachePath, "concat", state.MediaSource.Id + ".concat");
                if (!File.Exists(concatFilePath))
                {
                    _mediaEncoder.GenerateConcatConfig(state.MediaSource, concatFilePath);
                }

                arg.Append(" -f concat -safe 0 -i \"")
                    .Append(concatFilePath)
                    .Append("\" ");
            }
            else
            {
                arg.Append(" -i ")
                    .Append(_mediaEncoder.GetInputPathArgument(state));
            }

            // sub2video for external graphical subtitles
            if (state.SubtitleStream is not null
                && ShouldEncodeSubtitle(state)
                && !state.SubtitleStream.IsTextSubtitleStream
                && state.SubtitleStream.IsExternal)
            {
                var subtitlePath = state.SubtitleStream.Path;
                var subtitleExtension = Path.GetExtension(subtitlePath.AsSpan());

                // dvdsub/vobsub graphical subtitles use .sub+.idx pairs
                if (subtitleExtension.Equals(".sub", StringComparison.OrdinalIgnoreCase))
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
            if (!isSwDecoder)
            {
                arg.Append(" -noautoscale");
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

            return codec.Contains("264", StringComparison.OrdinalIgnoreCase)
                    || codec.Contains("avc", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsH265(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.Contains("265", StringComparison.OrdinalIgnoreCase)
                || codec.Contains("hevc", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAAC(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.Contains("aac", StringComparison.OrdinalIgnoreCase);
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
                && (string.Equals(mediaSourceContainer, "ts", StringComparison.OrdinalIgnoreCase)
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

        private string GetVideoBitrateParam(EncodingJobInfo state, string videoCodec)
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

            if (string.Equals(videoCodec, "h264_videotoolbox", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoCodec, "hevc_videotoolbox", StringComparison.OrdinalIgnoreCase))
            {
                // The `maxrate` and `bufsize` options can potentially lead to performance regression
                // and even encoder hangs, especially when the value is very high.
                return FormattableString.Invariant($" -b:v {bitrate} -qmin -1 -qmax -1");
            }

            return FormattableString.Invariant($" -b:v {bitrate} -maxrate {bitrate} -bufsize {bufsize}");
        }

        private string GetEncoderParam(EncoderPreset? preset, EncoderPreset defaultPreset, EncodingOptions encodingOptions, string videoEncoder, bool isLibX265)
        {
            var param = string.Empty;
            var encoderPreset = preset ?? defaultPreset;
            if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase) || isLibX265)
            {
                var presetString = encoderPreset switch
                {
                    EncoderPreset.auto => EncoderPreset.veryfast.ToString().ToLowerInvariant(),
                    _ => encoderPreset.ToString().ToLowerInvariant()
                };

                param += " -preset " + presetString;

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
                param += encoderPreset switch
                {
                    EncoderPreset.veryslow => " -preset 5",
                    EncoderPreset.slower => " -preset 6",
                    EncoderPreset.slow => " -preset 7",
                    EncoderPreset.medium => " -preset 8",
                    EncoderPreset.fast => " -preset 9",
                    EncoderPreset.faster => " -preset 10",
                    EncoderPreset.veryfast => " -preset 11",
                    EncoderPreset.superfast => " -preset 12",
                    EncoderPreset.ultrafast => " -preset 13",
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
                    param += encoderPreset switch
                    {
                        EncoderPreset.veryslow => " -compression_level 1",
                        EncoderPreset.slower => " -compression_level 2",
                        EncoderPreset.slow => " -compression_level 3",
                        EncoderPreset.medium => " -compression_level 4",
                        EncoderPreset.fast => " -compression_level 5",
                        EncoderPreset.faster => " -compression_level 6",
                        EncoderPreset.veryfast => " -compression_level 7",
                        EncoderPreset.superfast => " -compression_level 7",
                        EncoderPreset.ultrafast => " -compression_level 7",
                        _ => string.Empty
                    };
                }
            }
            else if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) // h264 (h264_qsv)
                     || string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase) // hevc (hevc_qsv)
                     || string.Equals(videoEncoder, "av1_qsv", StringComparison.OrdinalIgnoreCase)) // av1 (av1_qsv)
            {
                EncoderPreset[] valid_presets = [EncoderPreset.veryslow, EncoderPreset.slower, EncoderPreset.slow, EncoderPreset.medium, EncoderPreset.fast, EncoderPreset.faster, EncoderPreset.veryfast];

                param += " -preset " + (valid_presets.Contains(encoderPreset) ? encoderPreset : EncoderPreset.veryfast).ToString().ToLowerInvariant();
            }
            else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase) // h264 (h264_nvenc)
                        || string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase) // hevc (hevc_nvenc)
                        || string.Equals(videoEncoder, "av1_nvenc", StringComparison.OrdinalIgnoreCase) // av1 (av1_nvenc)
            )
            {
                param += encoderPreset switch
                {
                        EncoderPreset.veryslow => " -preset p7",
                        EncoderPreset.slower => " -preset p6",
                        EncoderPreset.slow => " -preset p5",
                        EncoderPreset.medium => " -preset p4",
                        EncoderPreset.fast => " -preset p3",
                        EncoderPreset.faster => " -preset p2",
                        _ => " -preset p1"
                };
            }
            else if (string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase) // h264 (h264_amf)
                        || string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase) // hevc (hevc_amf)
                        || string.Equals(videoEncoder, "av1_amf", StringComparison.OrdinalIgnoreCase) // av1 (av1_amf)
            )
            {
                param += encoderPreset switch
                {
                        EncoderPreset.veryslow => " -quality quality",
                        EncoderPreset.slower => " -quality quality",
                        EncoderPreset.slow => " -quality quality",
                        EncoderPreset.medium => " -quality balanced",
                        _ => " -quality speed"
                };

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
            else if (string.Equals(videoEncoder, "h264_videotoolbox", StringComparison.OrdinalIgnoreCase) // h264 (h264_videotoolbox)
                        || string.Equals(videoEncoder, "hevc_videotoolbox", StringComparison.OrdinalIgnoreCase) // hevc (hevc_videotoolbox)
            )
            {
                param += encoderPreset switch
                {
                        EncoderPreset.veryslow => " -prio_speed 0",
                        EncoderPreset.slower => " -prio_speed 0",
                        EncoderPreset.slow => " -prio_speed 0",
                        EncoderPreset.medium => " -prio_speed 0",
                        _ => " -prio_speed 1"
                };
            }

            return param;
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

            var subtitlePath = _subtitleEncoder.GetSubtitleFilePath(
                    state.SubtitleStream,
                    state.MediaSource,
                    CancellationToken.None).GetAwaiter().GetResult();

            return string.Format(
                CultureInfo.InvariantCulture,
                "subtitles=f='{0}'{1}{2}{3}{4}",
                _mediaEncoder.EscapeSubtitleFilterPath(subtitlePath),
                alphaParam,
                sub2videoParam,
                fontParam,
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
                var contentRate = state.VideoStream.ReferenceFrameRate;

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
                || string.Equals(codec, "h264_rkmpp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc_qsv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc_nvenc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc_rkmpp", StringComparison.OrdinalIgnoreCase)
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
        public string GetVideoQualityParam(EncodingJobInfo state, string videoEncoder, EncodingOptions encodingOptions, EncoderPreset defaultPreset)
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

            var hardwareAccelerationType = encodingOptions.HardwareAccelerationType;

            if (hardwareAccelerationType == HardwareAccelerationType.vaapi)
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
            else if (hardwareAccelerationType == HardwareAccelerationType.qsv)
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
                            && !IsIntelVppTonemapAvailable(state, encodingOptions)
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

            var isLibX265 = string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase);
            var encodingPreset = encodingOptions.EncoderPreset;

            param += GetEncoderParam(encodingPreset, defaultPreset, encodingOptions, videoEncoder, isLibX265);
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
            profile = WhiteSpaceRegex().Replace(profile, string.Empty).ToLowerInvariant();

            var videoProfiles = Array.Empty<string>();
            if (string.Equals("h264", targetVideoCodec, StringComparison.OrdinalIgnoreCase))
            {
                videoProfiles = _videoProfilesH264;
            }
            else if (string.Equals("hevc", targetVideoCodec, StringComparison.OrdinalIgnoreCase))
            {
                videoProfiles = _videoProfilesH265;
            }
            else if (string.Equals("av1", targetVideoCodec, StringComparison.OrdinalIgnoreCase))
            {
                videoProfiles = _videoProfilesAv1;
            }

            if (!videoProfiles.Contains(profile, StringComparison.OrdinalIgnoreCase))
            {
                profile = string.Empty;
            }

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

            // libx264, h264_{qsv,nvenc,rkmpp} does not support Constrained Baseline profile, force Baseline in this case.
            if ((string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_rkmpp", StringComparison.OrdinalIgnoreCase))
                && profile.Contains("baseline", StringComparison.OrdinalIgnoreCase))
            {
                profile = "baseline";
            }

            // libx264, h264_{qsv,nvenc,vaapi,rkmpp} does not support Constrained High profile, force High in this case.
            if ((string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_rkmpp", StringComparison.OrdinalIgnoreCase))
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

            if (string.Equals(videoEncoder, "h264_videotoolbox", StringComparison.OrdinalIgnoreCase)
                && profile.Contains("constrainedbaseline", StringComparison.OrdinalIgnoreCase))
            {
                profile = "constrained_baseline";
            }

            if (string.Equals(videoEncoder, "h264_videotoolbox", StringComparison.OrdinalIgnoreCase)
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
                else if (string.Equals(videoEncoder, "h264_rkmpp", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "hevc_rkmpp", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
                else if (!string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
            }

            if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                param += " -x264opts:0 subme=0:me_range=16:rc_lookahead=10:me=hex:open_gop=0";
            }

            if (string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
            {
                // libx265 only accept level option in -x265-params.
                // level option may cause libx265 to fail.
                // libx265 cannot adjust the given level, just throw an error.
                param += " -x265-params:0 no-scenecut=1:no-open-gop=1:no-info=1";

                if (encodingOptions.EncoderPreset < EncoderPreset.ultrafast)
                {
                    // The following params are slower than the ultrafast preset, don't use when ultrafast is selected.
                    param += ":subme=3:merange=25:rc-lookahead=10:me=star:ctu=32:max-tu-size=32:min-cu-size=16:rskip=2:rskip-edge-threshold=2:no-sao=1:no-strong-intra-smoothing=1";
                }
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

                // DOVIWithHDR10 should be compatible with HDR10 supporting players. Same goes with HLG and of course SDR. So allow copy of those formats

                var requestHasHDR10 = requestedRangeTypes.Contains(VideoRangeType.HDR10.ToString(), StringComparison.OrdinalIgnoreCase);
                var requestHasHLG = requestedRangeTypes.Contains(VideoRangeType.HLG.ToString(), StringComparison.OrdinalIgnoreCase);
                var requestHasSDR = requestedRangeTypes.Contains(VideoRangeType.SDR.ToString(), StringComparison.OrdinalIgnoreCase);

                if (!requestedRangeTypes.Contains(videoStream.VideoRangeType.ToString(), StringComparison.OrdinalIgnoreCase)
                     && !((requestHasHDR10 && videoStream.VideoRangeType == VideoRangeType.DOVIWithHDR10)
                            || (requestHasHLG && videoStream.VideoRangeType == VideoRangeType.DOVIWithHLG)
                            || (requestHasSDR && videoStream.VideoRangeType == VideoRangeType.DOVIWithSDR)))
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
                var videoFrameRate = videoStream.ReferenceFrameRate;

                // Add a little tolerance to the framerate check because some videos might record a framerate
                // that is slightly higher than the intended framerate, but the device can still play it correctly.
                // 0.05 fps tolerance should be safe enough.
                if (!videoFrameRate.HasValue || videoFrameRate.Value > requestedFramerate.Value + 0.05f)
                {
                    return false;
                }
            }

            // Video bitrate must fall within requested value
            if (request.VideoBitRate.HasValue
                && (!videoStream.BitRate.HasValue || videoStream.BitRate.Value > request.VideoBitRate.Value))
            {
                // For LiveTV that has no bitrate, let's try copy if other conditions are met
                if (string.IsNullOrWhiteSpace(request.LiveStreamId) || videoStream.BitRate.HasValue)
                {
                    return false;
                }
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

        public static int ScaleBitrate(int bitrate, string inputVideoCodec, string outputVideoCodec)
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
            else if (bitrate >= 30000000)
            {
                // Don't scale beyond 30Mbps, it is hardly visually noticeable for most codecs with our prefer speed encoding
                // and will cause extremely high bitrate to be used for av1->h264 transcoding that will overload clients and encoders
                scaleFactor = 1;
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

        public string GetAudioVbrModeParam(string encoder, int bitrate, int channels)
        {
            var bitratePerChannel = bitrate / Math.Max(channels, 1);
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
                // lame's VBR is only good for a certain bitrate range
                // For very low and very high bitrate, use abr mode
                if (bitratePerChannel is < 122500 and > 48000)
                {
                    return " -qscale:a " + bitratePerChannel switch
                    {
                        < 64000 => "6",
                        < 88000 => "4",
                        < 112000 => "2",
                        _ => "0"
                    };
                }

                return " -abr:a 1" + " -b:a " + bitrate;
            }

            if (string.Equals(encoder, "aac_at", StringComparison.OrdinalIgnoreCase))
            {
                // aac_at's CVBR mode
                return " -aac_at_mode:a 2" + " -b:a " + bitrate;
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

            if (channels is 2 && state.AudioStream?.Channels is > 2)
            {
                var hasDownMixFilter = DownMixAlgorithmsHelper.AlgorithmFilterStrings.TryGetValue((encodingOptions.DownMixStereoAlgorithm, DownMixAlgorithmsHelper.InferChannelLayout(state.AudioStream)), out var downMixFilterString);
                if (hasDownMixFilter)
                {
                    filters.Add(downMixFilterString);
                }

                if (!encodingOptions.DownMixAudioBoost.Equals(1))
                {
                    filters.Add("volume=" + encodingOptions.DownMixAudioBoost.ToString(CultureInfo.InvariantCulture));
                }
            }

            var isCopyingTimestamps = state.CopyTimestamps || state.TranscodingType != TranscodingJobType.Progressive;
            if (state.SubtitleStream is not null && state.SubtitleStream.IsTextSubtitleStream && ShouldEncodeSubtitle(state) && !isCopyingTimestamps)
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
                    // We can let FFMpeg supply an extra LFE channel for 5ch and 7ch to make them 5.1 and 7.1
                    if (resultChannels == 5)
                    {
                        resultChannels = 6;
                    }
                    else if (resultChannels == 7)
                    {
                        resultChannels = 8;
                    }
                    else
                    {
                        // For other weird layout, just downmix to stereo for compatibility
                        resultChannels = 2;
                    }
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
            var maxTime = state.RunTimeTicks ?? 0;
            var seekParam = string.Empty;

            if (time > 0)
            {
                // For direct streaming/remuxing, we seek at the exact position of the keyframe
                // However, ffmpeg will seek to previous keyframe when the exact time is the input
                // Workaround this by adding 0.5s offset to the seeking time to get the exact keyframe on most videos.
                // This will help subtitle syncing.
                var isHlsRemuxing = state.IsVideoRequest && state.TranscodingType is TranscodingJobType.Hls && IsCopyCodec(state.OutputVideoCodec);
                var seekTick = isHlsRemuxing ? time + 5000000L : time;

                // Seeking beyond EOF makes no sense in transcoding. Clamp the seekTick value to
                // [0, RuntimeTicks - 0.5s], so that the muxer gets packets and avoid error codes.
                if (maxTime > 0)
                {
                    seekTick = Math.Clamp(seekTick, 0, Math.Max(maxTime - 5000000L, 0));
                }

                seekParam += string.Format(CultureInfo.InvariantCulture, "-ss {0}", _mediaEncoder.GetTimeParameter(seekTick));

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
                        && ShouldEncodeSubtitle(state)
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
                outputWidth = Math.Min(maximumWidth, Convert.ToInt32(outputWidth * scale));
                outputHeight = Math.Min(maximumHeight, Convert.ToInt32(outputHeight * scale));
            }

            outputWidth = 2 * (outputWidth / 2);
            outputHeight = 2 * (outputHeight / 2);

            return (outputWidth, outputHeight);
        }

        public static bool IsScaleRatioSupported(
            int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight,
            double? maxScaleRatio)
        {
            var (outWidth, outHeight) = GetFixedOutputSize(
                videoWidth,
                videoHeight,
                requestedWidth,
                requestedHeight,
                requestedMaxWidth,
                requestedMaxHeight);

            if (!videoWidth.HasValue
                 || !videoHeight.HasValue
                 || !outWidth.HasValue
                 || !outHeight.HasValue
                 || !maxScaleRatio.HasValue
                 || (maxScaleRatio.Value < 1.0f))
            {
                return false;
            }

            var minScaleRatio = 1.0f / maxScaleRatio;
            var scaleRatioW = (double)outWidth / (double)videoWidth;
            var scaleRatioH = (double)outHeight / (double)videoHeight;

            if (scaleRatioW < minScaleRatio
                || scaleRatioW > maxScaleRatio
                || scaleRatioH < minScaleRatio
                || scaleRatioH > maxScaleRatio)
            {
                return false;
            }

            return true;
        }

        public static string GetHwScaleFilter(
            string hwScalePrefix,
            string hwScaleSuffix,
            string videoFormat,
            bool swapOutputWandH,
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

            var swpOutW = swapOutputWandH ? outHeight.Value : outWidth.Value;
            var swpOutH = swapOutputWandH ? outWidth.Value : outHeight.Value;

            var arg1 = isSizeFixed ? $"=w={swpOutW}:h={swpOutH}" : string.Empty;
            var arg2 = isFormatFixed ? $"format={videoFormat}" : string.Empty;
            if (isFormatFixed)
            {
                arg2 = (isSizeFixed ? ':' : '=') + arg2;
            }

            if (!string.IsNullOrEmpty(hwScaleSuffix) && (isSizeFixed || isFormatFixed))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_{1}{2}{3}",
                    hwScalePrefix ?? "scale",
                    hwScaleSuffix,
                    arg1,
                    arg2);
            }

            return string.Empty;
        }

        public static string GetGraphicalSubPreProcessFilters(
            int? videoWidth,
            int? videoHeight,
            int? subtitleWidth,
            int? subtitleHeight,
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

            if (!outWidth.HasValue
                || !outHeight.HasValue
                || outWidth.Value <= 0
                || outHeight.Value <= 0)
            {
                return string.Empty;
            }

            // Automatically add padding based on subtitle input
            var filters = @"scale,scale=-1:{1}:fast_bilinear,crop,pad=max({0}\,iw):max({1}\,ih):(ow-iw)/2:(oh-ih)/2:black@0,crop={0}:{1}";

            if (subtitleWidth.HasValue
                && subtitleHeight.HasValue
                && subtitleWidth.Value > 0
                && subtitleHeight.Value > 0)
            {
                var videoDar = (double)outWidth.Value / outHeight.Value;
                var subtitleDar = (double)subtitleWidth.Value / subtitleHeight.Value;

                // No need to add padding when DAR is the same -> 1080p PGSSUB on 2160p video
                if (Math.Abs(videoDar - subtitleDar) < 0.01f)
                {
                    filters = @"scale,scale={0}:{1}:fast_bilinear";
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                filters,
                outWidth.Value,
                outHeight.Value);
        }

        public static string GetAlphaSrcFilter(
            EncodingJobInfo state,
            int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight,
            float? framerate)
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
                    framerate ?? 25,
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
            var isMjpeg = videoEncoder is not null && videoEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);
            var scaleVal = isV4l2 ? 64 : 2;
            var targetAr = isMjpeg ? "(a*sar)" : "a"; // manually calculate AR when using mjpeg encoder

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
                    @"scale=trunc(min(max(iw\,ih*{3})\,min({0}\,{1}*{3}))/{2})*{2}:trunc(min(max(iw/{3}\,ih)\,min({0}/{3}\,{1}))/2)*2",
                    maxWidthParam,
                    maxHeightParam,
                    scaleVal,
                    targetAr);
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
                    "scale={0}:trunc(ow/{1}/2)*2",
                    widthParam,
                    targetAr);
            }

            // If a fixed height was requested
            if (requestedHeight.HasValue)
            {
                var heightParam = requestedHeight.Value.ToString(CultureInfo.InvariantCulture);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "scale=trunc(oh*{2}/{1})*{1}:{0}",
                    heightParam,
                    scaleVal,
                    targetAr);
            }

            // If a max width was requested
            if (requestedMaxWidth.HasValue)
            {
                var maxWidthParam = requestedMaxWidth.Value.ToString(CultureInfo.InvariantCulture);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    @"scale=trunc(min(max(iw\,ih*{2})\,{0})/{1})*{1}:trunc(ow/{2}/2)*2",
                    maxWidthParam,
                    scaleVal,
                    targetAr);
            }

            // If a max height was requested
            if (requestedMaxHeight.HasValue)
            {
                var maxHeightParam = requestedMaxHeight.Value.ToString(CultureInfo.InvariantCulture);

                return string.Format(
                    CultureInfo.InvariantCulture,
                    @"scale=trunc(oh*{2}/{1})*{1}:min(max(iw/{2}\,ih)\,{0})",
                    maxHeightParam,
                    scaleVal,
                    targetAr);
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
                        filter = @"crop=iw/2:ih:0:0,scale=(iw*2):ih,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
                        // hsbs crop width in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to requestedWidth. Work out the correct height based on the display aspect it will maintain the aspect where -1 in this case (3d) may not.
                        break;
                    case Video3DFormat.FullSideBySide:
                        filter = @"crop=iw/2:ih:0:0,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
                        // fsbs crop width in half,set the display aspect,crop out any black bars we may have made the scale width to requestedWidth.
                        break;
                    case Video3DFormat.HalfTopAndBottom:
                        filter = @"crop=iw:ih/2:0:0,scale=(iw*2):ih),setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
                        // htab crop height in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to requestedWidth
                        break;
                    case Video3DFormat.FullTopAndBottom:
                        filter = @"crop=iw:ih/2:0:0,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
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
            var doubleRateDeint = options.DeinterlaceDoubleRate && state.VideoStream?.ReferenceFrameRate <= 30;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}:-1:0",
                options.DeinterlaceMethod.ToString().ToLowerInvariant(),
                doubleRateDeint ? "1" : "0");
        }

        public string GetHwDeinterlaceFilter(EncodingJobInfo state, EncodingOptions options, string hwDeintSuffix)
        {
            var doubleRateDeint = options.DeinterlaceDoubleRate && (state.VideoStream?.ReferenceFrameRate ?? 60) <= 30;
            if (hwDeintSuffix.Contains("cuda", StringComparison.OrdinalIgnoreCase))
            {
                var useBwdif = options.DeinterlaceMethod == DeinterlaceMethod.bwdif && _mediaEncoder.SupportsFilter("bwdif_cuda");

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_cuda={1}:-1:0",
                    useBwdif ? "bwdif" : "yadif",
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
                var useBwdif = options.DeinterlaceMethod == DeinterlaceMethod.bwdif && _mediaEncoder.SupportsFilter("bwdif_videotoolbox");

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_videotoolbox={1}:-1:0",
                    useBwdif ? "bwdif" : "yadif",
                    doubleRateDeint ? "1" : "0");
            }

            return string.Empty;
        }

        private string GetHwTonemapFilter(EncodingOptions options, string hwTonemapSuffix, string videoFormat, bool forceFullRange)
        {
            if (string.IsNullOrEmpty(hwTonemapSuffix))
            {
                return string.Empty;
            }

            var args = string.Empty;
            var algorithm = options.TonemappingAlgorithm.ToString().ToLowerInvariant();
            var mode = options.TonemappingMode.ToString().ToLowerInvariant();
            var range = forceFullRange ? TonemappingRange.pc : options.TonemappingRange;
            var rangeString = range.ToString().ToLowerInvariant();

            if (string.Equals(hwTonemapSuffix, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                var doVaVppProcamp = false;
                var procampParams = string.Empty;
                if (options.VppTonemappingBrightness != 0
                    && options.VppTonemappingBrightness >= -100
                    && options.VppTonemappingBrightness <= 100)
                {
                    procampParams += "procamp_vaapi=b={0}";
                    doVaVppProcamp = true;
                }

                if (options.VppTonemappingContrast > 1
                    && options.VppTonemappingContrast <= 10)
                {
                    procampParams += doVaVppProcamp ? ":c={1}" : "procamp_vaapi=c={1}";
                    doVaVppProcamp = true;
                }

                args = procampParams + "{2}tonemap_vaapi=format={3}:p=bt709:t=bt709:m=bt709:extra_hw_frames=32";

                return string.Format(
                        CultureInfo.InvariantCulture,
                        args,
                        options.VppTonemappingBrightness,
                        options.VppTonemappingContrast,
                        doVaVppProcamp ? "," : string.Empty,
                        videoFormat ?? "nv12");
            }
            else
            {
                args = "tonemap_{0}=format={1}:p=bt709:t=bt709:m=bt709:tonemap={2}:peak={3}:desat={4}";

                var useLegacyTonemapModes = _mediaEncoder.EncoderVersion >= _minFFmpegOclCuTonemapMode
                                           && _legacyTonemapModes.Contains(options.TonemappingMode);

                var useAdvancedTonemapModes = _mediaEncoder.EncoderVersion >= _minFFmpegAdvancedTonemapMode
                                              && _advancedTonemapModes.Contains(options.TonemappingMode);

                if (useLegacyTonemapModes || useAdvancedTonemapModes)
                {
                    args += ":tonemap_mode={5}";
                }

                if (options.TonemappingParam != 0)
                {
                    args += ":param={6}";
                }

                if (range == TonemappingRange.tv || range == TonemappingRange.pc)
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
                    mode,
                    options.TonemappingParam,
                    rangeString);
        }

        private string GetLibplaceboFilter(
            EncodingOptions options,
            string videoFormat,
            bool doTonemap,
            int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight,
            bool forceFullRange)
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
                var algorithmString = "clip";
                var mode = options.TonemappingMode;
                var range = forceFullRange ? TonemappingRange.pc : options.TonemappingRange;

                if (algorithm == TonemappingAlgorithm.bt2390)
                {
                    algorithmString = "bt.2390";
                }
                else if (algorithm != TonemappingAlgorithm.none)
                {
                    algorithmString = algorithm.ToString().ToLowerInvariant();
                }

                tonemapArg = $":tonemapping={algorithmString}:peak_detect=0:color_primaries=bt709:color_trc=bt709:colorspace=bt709";

                if (range == TonemappingRange.tv || range == TonemappingRange.pc)
                {
                    tonemapArg += ":range=" + range.ToString().ToLowerInvariant();
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "libplacebo=upscaler=none:downscaler=none{0}{1}{2}",
                sizeArg,
                formatArg,
                tonemapArg);
        }

        public string GetVideoTransposeDirection(EncodingJobInfo state)
        {
            return (state.VideoStream?.Rotation ?? 0) switch
            {
                90 => "cclock",
                180 => "reversal",
                -90 => "clock",
                -180 => "reversal",
                _ => string.Empty
            };
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
            var doToneMap = IsSwTonemapAvailable(state, options);
            var requireDoviReshaping = doToneMap && state.VideoStream.VideoRangeType == VideoRangeType.DOVI;

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;

            var rotation = state.VideoStream?.Rotation ?? 0;
            var swapWAndH = Math.Abs(rotation) == 90;
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            mainFilters.Add(GetOverwriteColorPropertiesParam(state, doToneMap));

            // INPUT sw surface(memory/copy-back from vram)
            // sw deint
            if (doDeintH2645)
            {
                var deintFilter = GetSwDeinterlaceFilter(state, options);
                mainFilters.Add(deintFilter);
            }

            var outFormat = isSwDecoder ? "yuv420p" : "nv12";
            var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
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

            // sw tonemap
            if (doToneMap)
            {
                // tonemapx requires yuv420p10 input for dovi reshaping, let ffmpeg convert the frame when necessary
                var tonemapFormat = requireDoviReshaping ? "yuv420p" : outFormat;
                var tonemapArgString = "tonemapx=tonemap={0}:desat={1}:peak={2}:t=bt709:m=bt709:p=bt709:format={3}";

                if (options.TonemappingParam != 0)
                {
                    tonemapArgString += ":param={4}";
                }

                var range = options.TonemappingRange;
                if (range == TonemappingRange.tv || range == TonemappingRange.pc)
                {
                    tonemapArgString += ":range={5}";
                }

                var tonemapArgs = string.Format(
                    CultureInfo.InvariantCulture,
                    tonemapArgString,
                    options.TonemappingAlgorithm,
                    options.TonemappingDesat,
                    options.TonemappingPeak,
                    tonemapFormat,
                    options.TonemappingParam,
                    options.TonemappingRange);

                mainFilters.Add(tonemapArgs);
            }
            else
            {
                // OUTPUT yuv420p/nv12 surface(memory)
                mainFilters.Add("format=" + outFormat);
            }

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
                var subW = state.SubtitleStream?.Width;
                var subH = state.SubtitleStream?.Height;
                var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                subFilters.Add(subPreProcFilters);
                overlayFilters.Add("overlay=eof_action=pass:repeatlast=0");
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
            if (options.HardwareAccelerationType != HardwareAccelerationType.nvenc)
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
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);
            var isCuInCuOut = isNvDecoder && isNvencEncoder;

            var doubleRateDeint = options.DeinterlaceDoubleRate && (state.VideoStream?.ReferenceFrameRate ?? 60) <= 30;
            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doCuTonemap = IsHwTonemapAvailable(state, options);

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));
            var subW = state.SubtitleStream?.Width;
            var subH = state.SubtitleStream?.Height;

            var rotation = state.VideoStream?.Rotation ?? 0;
            var tranposeDir = rotation == 0 ? string.Empty : GetVideoTransposeDirection(state);
            var doCuTranspose = !string.IsNullOrEmpty(tranposeDir) && _mediaEncoder.SupportsFilter("transpose_cuda");
            var swapWAndH = Math.Abs(rotation) == 90 && (isSwDecoder || (isNvDecoder && doCuTranspose));
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

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
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add($"format={outFormat}");

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

                // hw transpose
                if (doCuTranspose)
                {
                    mainFilters.Add($"transpose_cuda=dir={tranposeDir}");
                }

                var isRext = IsVideoStreamHevcRext(state);
                var outFormat = doCuTonemap ? (isRext ? "p010" : string.Empty) : "yuv420p";
                var hwScaleFilter = GetHwScaleFilter("scale", "cuda", outFormat, false, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);
                // hw scale
                mainFilters.Add(hwScaleFilter);
            }

            // hw tonemap
            if (doCuTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "cuda", "yuv420p", isMjpegEncoder);
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
                        var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                        subFilters.Add(subPreProcFilters);
                        subFilters.Add("format=yuva420p");
                    }
                    else if (hasTextSubs)
                    {
                        var framerate = state.VideoStream?.RealFrameRate;
                        var subFramerate = hasAssSubs ? Math.Min(framerate ?? 25, 60) : 10;

                        // alphasrc=s=1280x720:r=10:start=0,format=yuva420p,subtitles,hwupload
                        var alphaSrcFilter = GetAlphaSrcFilter(state, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH, subFramerate);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=yuva420p");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    subFilters.Add("hwupload=derive_device=cuda");
                    overlayFilters.Add("overlay_cuda=eof_action=pass:repeatlast=0");
                }
            }
            else
            {
                if (hasGraphicalSubs)
                {
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    overlayFilters.Add("overlay=eof_action=pass:repeatlast=0");
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
            if (options.HardwareAccelerationType != HardwareAccelerationType.amf)
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
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);
            var isDxInDxOut = isD3d11vaDecoder && isAmfEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doOclTonemap = IsHwTonemapAvailable(state, options);

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));
            var subW = state.SubtitleStream?.Width;
            var subH = state.SubtitleStream?.Height;

            var rotation = state.VideoStream?.Rotation ?? 0;
            var tranposeDir = rotation == 0 ? string.Empty : GetVideoTransposeDirection(state);
            var doOclTranspose = !string.IsNullOrEmpty(tranposeDir)
                && _mediaEncoder.SupportsFilterWithOption(FilterOptionType.TransposeOpenclReversal);
            var swapWAndH = Math.Abs(rotation) == 90 && (isSwDecoder || (isD3d11vaDecoder && doOclTranspose));
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

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
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add($"format={outFormat}");

                // keep video at memory except ocl tonemap,
                // since the overhead caused by hwupload >>> using sw filter.
                // sw => hw
                if (doOclTonemap)
                {
                    mainFilters.Add("hwupload=derive_device=d3d11va:extra_hw_frames=24");
                    mainFilters.Add("format=d3d11");
                    mainFilters.Add("hwmap=derive_device=opencl:mode=read");
                }
            }

            if (isD3d11vaDecoder)
            {
                // INPUT d3d11 surface(vram)
                // map from d3d11va to opencl via d3d11-opencl interop.
                mainFilters.Add("hwmap=derive_device=opencl:mode=read");

                // hw deint <= TODO: finsh the 'yadif_opencl' filter

                // hw transpose
                if (doOclTranspose)
                {
                    mainFilters.Add($"transpose_opencl=dir={tranposeDir}");
                }

                var outFormat = doOclTonemap ? string.Empty : "nv12";
                var hwScaleFilter = GetHwScaleFilter("scale", "opencl", outFormat, false, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);
                // hw scale
                mainFilters.Add(hwScaleFilter);
            }

            // hw tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12", isMjpegEncoder);
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
                mainFilters.Add("hwmap=derive_device=d3d11va:mode=write:reverse=1");
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
                        var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                        subFilters.Add(subPreProcFilters);
                        subFilters.Add("format=yuva420p");
                    }
                    else if (hasTextSubs)
                    {
                        var framerate = state.VideoStream?.RealFrameRate;
                        var subFramerate = hasAssSubs ? Math.Min(framerate ?? 25, 60) : 10;

                        // alphasrc=s=1280x720:r=10:start=0,format=yuva420p,subtitles,hwupload
                        var alphaSrcFilter = GetAlphaSrcFilter(state, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH, subFramerate);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=yuva420p");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    subFilters.Add("hwupload=derive_device=opencl");
                    overlayFilters.Add("overlay_opencl=eof_action=pass:repeatlast=0");
                    overlayFilters.Add("hwmap=derive_device=d3d11va:mode=write:reverse=1");
                    overlayFilters.Add("format=d3d11");
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    overlayFilters.Add("overlay=eof_action=pass:repeatlast=0");
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
            if (options.HardwareAccelerationType != HardwareAccelerationType.qsv)
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
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);
            var isQsvInQsvOut = isHwDecoder && isQsvEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doVppTonemap = IsIntelVppTonemapAvailable(state, options);
            var doOclTonemap = !doVppTonemap && IsHwTonemapAvailable(state, options);
            var doTonemap = doVppTonemap || doOclTonemap;

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));
            var subW = state.SubtitleStream?.Width;
            var subH = state.SubtitleStream?.Height;

            var rotation = state.VideoStream?.Rotation ?? 0;
            var tranposeDir = rotation == 0 ? string.Empty : GetVideoTransposeDirection(state);
            var doVppTranspose = !string.IsNullOrEmpty(tranposeDir);
            var swapWAndH = Math.Abs(rotation) == 90 && (isSwDecoder || ((isD3d11vaDecoder || isQsvDecoder) && doVppTranspose));
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

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
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                if (isMjpegEncoder && !doOclTonemap)
                {
                    // sw decoder + hw mjpeg encoder
                    swScaleFilter = string.IsNullOrEmpty(swScaleFilter) ? "scale=out_range=pc" : $"{swScaleFilter}:out_range=pc";
                }

                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add($"format={outFormat}");

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
                var isRext = IsVideoStreamHevcRext(state);
                var twoPassVppTonemap = false;
                var doVppFullRangeOut = isMjpegEncoder
                    && _mediaEncoder.EncoderVersion >= _minFFmpegQsvVppOutRangeOption;
                var doVppScaleModeHq = isMjpegEncoder
                    && _mediaEncoder.EncoderVersion >= _minFFmpegQsvVppScaleModeOption;
                var doVppProcamp = false;
                var procampParams = string.Empty;
                var procampParamsString = string.Empty;
                if (doVppTonemap)
                {
                    if (isRext)
                    {
                        // VPP tonemap requires p010 input
                        twoPassVppTonemap = true;
                    }

                    if (options.VppTonemappingBrightness != 0
                        && options.VppTonemappingBrightness >= -100
                        && options.VppTonemappingBrightness <= 100)
                    {
                        procampParamsString += ":brightness={0}";
                        twoPassVppTonemap = doVppProcamp = true;
                    }

                    if (options.VppTonemappingContrast > 1
                        && options.VppTonemappingContrast <= 10)
                    {
                        procampParamsString += ":contrast={1}";
                        twoPassVppTonemap = doVppProcamp = true;
                    }

                    if (doVppProcamp)
                    {
                        procampParamsString += ":procamp=1:async_depth=2";
                        procampParams = string.Format(
                            CultureInfo.InvariantCulture,
                            procampParamsString,
                            options.VppTonemappingBrightness,
                            options.VppTonemappingContrast);
                    }
                }

                var outFormat = doOclTonemap ? ((doVppTranspose || isRext) ? "p010" : string.Empty) : "nv12";
                outFormat = twoPassVppTonemap ? "p010" : outFormat;

                var swapOutputWandH = doVppTranspose && swapWAndH;
                var hwScaleFilter = GetHwScaleFilter("vpp", "qsv", outFormat, swapOutputWandH, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);

                if (!string.IsNullOrEmpty(hwScaleFilter) && doVppTranspose)
                {
                    hwScaleFilter += $":transpose={tranposeDir}";
                }

                if (!string.IsNullOrEmpty(hwScaleFilter) && isMjpegEncoder)
                {
                    hwScaleFilter += (doVppFullRangeOut && !doOclTonemap) ? ":out_range=pc" : string.Empty;
                    hwScaleFilter += doVppScaleModeHq ? ":scale_mode=hq" : string.Empty;
                }

                if (!string.IsNullOrEmpty(hwScaleFilter) && doVppTonemap)
                {
                    hwScaleFilter += doVppProcamp ? procampParams : (twoPassVppTonemap ? string.Empty : ":tonemap=1");
                }

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

                // hw transpose & scale & tonemap(w/o procamp)
                mainFilters.Add(hwScaleFilter);

                // hw tonemap(w/ procamp)
                if (doVppTonemap && twoPassVppTonemap)
                {
                    mainFilters.Add("vpp_qsv=tonemap=1:format=nv12:async_depth=2");
                }

                // force bt709 just in case vpp tonemap is not triggered or using MSDK instead of VPL.
                if (doVppTonemap)
                {
                    mainFilters.Add(GetOverwriteColorPropertiesParam(state, false));
                }
            }

            if (doOclTonemap && isHwDecoder)
            {
                // map from qsv to opencl via qsv(d3d11)-opencl interop.
                mainFilters.Add("hwmap=derive_device=opencl:mode=read");
            }

            // hw tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12", isMjpegEncoder);
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
                mainFilters.Add("hwmap=derive_device=qsv:mode=write:reverse=1");
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
                        // overlay_qsv can handle overlay scaling, setup a smaller height to reduce transfer overhead
                        var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, 1080);
                        subFilters.Add(subPreProcFilters);
                        subFilters.Add("format=bgra");
                    }
                    else if (hasTextSubs)
                    {
                        var framerate = state.VideoStream?.RealFrameRate;
                        var subFramerate = hasAssSubs ? Math.Min(framerate ?? 25, 60) : 10;

                        // alphasrc=s=1280x720:r=10:start=0,format=bgra,subtitles,hwupload
                        var alphaSrcFilter = GetAlphaSrcFilter(state, swpInW, swpInH, reqW, reqH, reqMaxW, 1080, subFramerate);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=bgra");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    // qsv requires a fixed pool size.
                    // default to 64 otherwise it will fail on certain iGPU.
                    subFilters.Add("hwupload=derive_device=qsv:extra_hw_frames=64");

                    var (overlayW, overlayH) = GetFixedOutputSize(swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);
                    var overlaySize = (overlayW.HasValue && overlayH.HasValue)
                        ? $":w={overlayW.Value}:h={overlayH.Value}"
                        : string.Empty;
                    var overlayQsvFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        "overlay_qsv=eof_action=pass:repeatlast=0{0}",
                        overlaySize);
                    overlayFilters.Add(overlayQsvFilter);
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    overlayFilters.Add("overlay=eof_action=pass:repeatlast=0");
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
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);
            var isQsvInQsvOut = isHwDecoder && isQsvEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doVaVppTonemap = IsIntelVppTonemapAvailable(state, options);
            var doOclTonemap = !doVaVppTonemap && IsHwTonemapAvailable(state, options);
            var doTonemap = doVaVppTonemap || doOclTonemap;
            var doDeintH2645 = doDeintH264 || doDeintHevc;

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));
            var subW = state.SubtitleStream?.Width;
            var subH = state.SubtitleStream?.Height;

            var rotation = state.VideoStream?.Rotation ?? 0;
            var tranposeDir = rotation == 0 ? string.Empty : GetVideoTransposeDirection(state);
            var doVppTranspose = !string.IsNullOrEmpty(tranposeDir);
            var swapWAndH = Math.Abs(rotation) == 90 && (isSwDecoder || ((isVaapiDecoder || isQsvDecoder) && doVppTranspose));
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

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
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                if (isMjpegEncoder && !doOclTonemap)
                {
                    // sw decoder + hw mjpeg encoder
                    swScaleFilter = string.IsNullOrEmpty(swScaleFilter) ? "scale=out_range=pc" : $"{swScaleFilter}:out_range=pc";
                }

                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add($"format={outFormat}");

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
                var hwFilterSuffix = isVaapiDecoder ? "vaapi" : "qsv";
                var isRext = IsVideoStreamHevcRext(state);
                var doVppFullRangeOut = isMjpegEncoder
                    && _mediaEncoder.EncoderVersion >= _minFFmpegQsvVppOutRangeOption;
                var doVppScaleModeHq = isMjpegEncoder
                    && _mediaEncoder.EncoderVersion >= _minFFmpegQsvVppScaleModeOption;

                // INPUT vaapi/qsv surface(vram)
                // hw deint
                if (doDeintH2645)
                {
                    var deintFilter = GetHwDeinterlaceFilter(state, options, hwFilterSuffix);
                    mainFilters.Add(deintFilter);
                }

                // hw transpose(vaapi vpp)
                if (isVaapiDecoder && doVppTranspose)
                {
                    mainFilters.Add($"transpose_vaapi=dir={tranposeDir}");
                }

                var outFormat = doTonemap ? (((isQsvDecoder && doVppTranspose) || isRext) ? "p010" : string.Empty) : "nv12";
                var swapOutputWandH = isQsvDecoder && doVppTranspose && swapWAndH;
                var hwScalePrefix = isQsvDecoder ? "vpp" : "scale";
                var hwScaleFilter = GetHwScaleFilter(hwScalePrefix, hwFilterSuffix, outFormat, swapOutputWandH, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);

                if (!string.IsNullOrEmpty(hwScaleFilter) && isQsvDecoder && doVppTranspose)
                {
                    hwScaleFilter += $":transpose={tranposeDir}";
                }

                if (!string.IsNullOrEmpty(hwScaleFilter) && isMjpegEncoder)
                {
                    hwScaleFilter += ((isQsvDecoder && !doVppFullRangeOut) || doOclTonemap) ? string.Empty : ":out_range=pc";
                    hwScaleFilter += isQsvDecoder ? (doVppScaleModeHq ? ":scale_mode=hq" : string.Empty) : ":mode=hq";
                }

                // allocate extra pool sizes for vaapi vpp scale
                if (!string.IsNullOrEmpty(hwScaleFilter) && isVaapiDecoder)
                {
                    hwScaleFilter += ":extra_hw_frames=24";
                }

                // hw transpose(qsv vpp) & scale
                mainFilters.Add(hwScaleFilter);
            }

            // vaapi vpp tonemap
            if (doVaVppTonemap && isHwDecoder)
            {
                if (isQsvDecoder)
                {
                    // map from qsv to vaapi.
                    mainFilters.Add("hwmap=derive_device=vaapi");
                    mainFilters.Add("format=vaapi");
                }

                var tonemapFilter = GetHwTonemapFilter(options, "vaapi", "nv12", isMjpegEncoder);
                mainFilters.Add(tonemapFilter);

                if (isQsvDecoder)
                {
                    // map from vaapi to qsv.
                    mainFilters.Add("hwmap=derive_device=qsv");
                    mainFilters.Add("format=qsv");
                }
            }

            if (doOclTonemap && isHwDecoder)
            {
                // map from qsv to opencl via qsv(vaapi)-opencl interop.
                mainFilters.Add("hwmap=derive_device=opencl:mode=read");
            }

            // ocl tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12", isMjpegEncoder);
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
                    mainFilters.Add("hwmap=derive_device=qsv:mode=write:reverse=1:extra_hw_frames=16");
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
                        // overlay_qsv can handle overlay scaling, setup a smaller height to reduce transfer overhead
                        var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, 1080);
                        subFilters.Add(subPreProcFilters);
                        subFilters.Add("format=bgra");
                    }
                    else if (hasTextSubs)
                    {
                        var framerate = state.VideoStream?.RealFrameRate;
                        var subFramerate = hasAssSubs ? Math.Min(framerate ?? 25, 60) : 10;

                        var alphaSrcFilter = GetAlphaSrcFilter(state, swpInW, swpInH, reqW, reqH, reqMaxW, 1080, subFramerate);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=bgra");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    // qsv requires a fixed pool size.
                    // default to 64 otherwise it will fail on certain iGPU.
                    subFilters.Add("hwupload=derive_device=qsv:extra_hw_frames=64");

                    var (overlayW, overlayH) = GetFixedOutputSize(swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);
                    var overlaySize = (overlayW.HasValue && overlayH.HasValue)
                        ? $":w={overlayW.Value}:h={overlayH.Value}"
                        : string.Empty;
                    var overlayQsvFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        "overlay_qsv=eof_action=pass:repeatlast=0{0}",
                        overlaySize);
                    overlayFilters.Add(overlayQsvFilter);
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    overlayFilters.Add("overlay=eof_action=pass:repeatlast=0");
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
            if (options.HardwareAccelerationType != HardwareAccelerationType.vaapi)
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
                && _mediaEncoder.IsVaapiDeviceSupportVulkanDrmInterop
                && Environment.OSVersion.Version >= _minKernelVersionAmdVkFmtModifier)
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
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);
            var isVaInVaOut = isVaapiDecoder && isVaapiEncoder;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doVaVppTonemap = isVaapiDecoder && IsIntelVppTonemapAvailable(state, options);
            var doOclTonemap = !doVaVppTonemap && IsHwTonemapAvailable(state, options);
            var doTonemap = doVaVppTonemap || doOclTonemap;
            var doDeintH2645 = doDeintH264 || doDeintHevc;

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));
            var subW = state.SubtitleStream?.Width;
            var subH = state.SubtitleStream?.Height;

            var rotation = state.VideoStream?.Rotation ?? 0;
            var tranposeDir = rotation == 0 ? string.Empty : GetVideoTransposeDirection(state);
            var doVaVppTranspose = !string.IsNullOrEmpty(tranposeDir);
            var swapWAndH = Math.Abs(rotation) == 90 && (isSwDecoder || (isVaapiDecoder && doVaVppTranspose));
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

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
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                if (isMjpegEncoder && !doOclTonemap)
                {
                    // sw decoder + hw mjpeg encoder
                    swScaleFilter = string.IsNullOrEmpty(swScaleFilter) ? "scale=out_range=pc" : $"{swScaleFilter}:out_range=pc";
                }

                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add($"format={outFormat}");

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
                var isRext = IsVideoStreamHevcRext(state);

                // INPUT vaapi surface(vram)
                // hw deint
                if (doDeintH2645)
                {
                    var deintFilter = GetHwDeinterlaceFilter(state, options, "vaapi");
                    mainFilters.Add(deintFilter);
                }

                // hw transpose
                if (doVaVppTranspose)
                {
                    mainFilters.Add($"transpose_vaapi=dir={tranposeDir}");
                }

                var outFormat = doTonemap ? (isRext ? "p010" : string.Empty) : "nv12";
                var hwScaleFilter = GetHwScaleFilter("scale", "vaapi", outFormat, false, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);

                if (!string.IsNullOrEmpty(hwScaleFilter) && isMjpegEncoder)
                {
                    hwScaleFilter += doOclTonemap ? string.Empty : ":out_range=pc";
                    hwScaleFilter += ":mode=hq";
                }

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
                var tonemapFilter = GetHwTonemapFilter(options, "vaapi", "nv12", isMjpegEncoder);
                mainFilters.Add(tonemapFilter);
            }

            if (doOclTonemap && isVaapiDecoder)
            {
                // map from vaapi to opencl via vaapi-opencl interop(Intel only).
                mainFilters.Add("hwmap=derive_device=opencl:mode=read");
            }

            // ocl tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12", isMjpegEncoder);
                mainFilters.Add(tonemapFilter);
            }

            if (doOclTonemap && isVaInVaOut)
            {
                // OUTPUT vaapi(nv12) surface(vram)
                // reverse-mapping via vaapi-opencl interop.
                mainFilters.Add("hwmap=derive_device=vaapi:mode=write:reverse=1");
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
                        // overlay_vaapi can handle overlay scaling, setup a smaller height to reduce transfer overhead
                        var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, 1080);
                        subFilters.Add(subPreProcFilters);
                        subFilters.Add("format=bgra");
                    }
                    else if (hasTextSubs)
                    {
                        var framerate = state.VideoStream?.RealFrameRate;
                        var subFramerate = hasAssSubs ? Math.Min(framerate ?? 25, 60) : 10;

                        var alphaSrcFilter = GetAlphaSrcFilter(state, swpInW, swpInH, reqW, reqH, reqMaxW, 1080, subFramerate);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=bgra");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    subFilters.Add("hwupload=derive_device=vaapi");

                    var (overlayW, overlayH) = GetFixedOutputSize(swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);
                    var overlaySize = (overlayW.HasValue && overlayH.HasValue)
                        ? $":w={overlayW.Value}:h={overlayH.Value}"
                        : string.Empty;
                    var overlayVaapiFilter = string.Format(
                        CultureInfo.InvariantCulture,
                        "overlay_vaapi=eof_action=pass:repeatlast=0{0}",
                        overlaySize);
                    overlayFilters.Add(overlayVaapiFilter);
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    overlayFilters.Add("overlay=eof_action=pass:repeatlast=0");

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
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doVkTonemap = IsVulkanHwTonemapAvailable(state, options);
            var doDeintH2645 = doDeintH264 || doDeintHevc;

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));

            var rotation = state.VideoStream?.Rotation ?? 0;
            var tranposeDir = rotation == 0 ? string.Empty : GetVideoTransposeDirection(state);
            var doVkTranspose = isVaapiDecoder && !string.IsNullOrEmpty(tranposeDir);
            var swapWAndH = Math.Abs(rotation) == 90 && (isSwDecoder || (isVaapiDecoder && doVkTranspose));
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

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
                    var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                    mainFilters.Add(swScaleFilter);
                    mainFilters.Add("format=nv12");
                }
            }
            else if (isVaapiDecoder)
            {
                // INPUT vaapi surface(vram)
                if (doVkTranspose || doVkTonemap || hasSubs)
                {
                    // map from vaapi to vulkan/drm via interop (Polaris/gfx8+).
                    if (_mediaEncoder.EncoderVersion >= _minFFmpegAlteredVaVkInterop)
                    {
                        if (doVkTranspose || !_mediaEncoder.IsVaapiDeviceSupportVulkanDrmModifier)
                        {
                            // disable the indirect va-drm-vk mapping since it's no longer reliable.
                            mainFilters.Add("hwmap=derive_device=drm");
                            mainFilters.Add("format=drm_prime");
                            mainFilters.Add("hwmap=derive_device=vulkan");
                            mainFilters.Add("format=vulkan");

                            // workaround for libplacebo using the imported vulkan frame on gfx8.
                            if (!_mediaEncoder.IsVaapiDeviceSupportVulkanDrmModifier)
                            {
                                mainFilters.Add("scale_vulkan");
                            }
                        }
                        else if (doVkTonemap || hasSubs)
                        {
                            // non ad-hoc libplacebo also accepts drm_prime direct input.
                            mainFilters.Add("hwmap=derive_device=drm");
                            mainFilters.Add("format=drm_prime");
                        }
                    }
                    else // legacy va-vk mapping that works only in jellyfin-ffmpeg6
                    {
                        mainFilters.Add("hwmap=derive_device=vulkan");
                        mainFilters.Add("format=vulkan");
                    }
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
                    var hwScaleFilter = GetHwScaleFilter("scale", "vaapi", "nv12", false, inW, inH, reqW, reqH, reqMaxW, reqMaxH);

                    if (!string.IsNullOrEmpty(hwScaleFilter) && isMjpegEncoder && !doVkTonemap)
                    {
                        hwScaleFilter += ":out_range=pc:mode=hq";
                    }

                    mainFilters.Add(hwScaleFilter);
                }
            }

            // vk transpose
            if (doVkTranspose)
            {
                if (string.Equals(tranposeDir, "reversal", StringComparison.OrdinalIgnoreCase))
                {
                    mainFilters.Add("flip_vulkan");
                }
                else
                {
                    mainFilters.Add($"transpose_vulkan=dir={tranposeDir}");
                }
            }

            // vk libplacebo
            if (doVkTonemap || hasSubs)
            {
                var libplaceboFilter = GetLibplaceboFilter(options, "bgra", doVkTonemap, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH, isMjpegEncoder);
                mainFilters.Add(libplaceboFilter);
                mainFilters.Add("format=vulkan");
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
                    var subW = state.SubtitleStream?.Width;
                    var subH = state.SubtitleStream?.Height;
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    subFilters.Add("format=bgra");
                }
                else if (hasTextSubs)
                {
                    var framerate = state.VideoStream?.RealFrameRate;
                    var subFramerate = hasAssSubs ? Math.Min(framerate ?? 25, 60) : 10;

                    var alphaSrcFilter = GetAlphaSrcFilter(state, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH, subFramerate);
                    var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                    subFilters.Add(alphaSrcFilter);
                    subFilters.Add("format=bgra");
                    subFilters.Add(subTextSubtitlesFilter);
                }

                subFilters.Add("hwupload=derive_device=vulkan");
                subFilters.Add("format=vulkan");

                overlayFilters.Add("overlay_vulkan=eof_action=pass:repeatlast=0");

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
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);
            var isVaInVaOut = isVaapiDecoder && isVaapiEncoder;
            var isi965Driver = _mediaEncoder.IsVaapiDeviceInteli965;
            var isAmdDriver = _mediaEncoder.IsVaapiDeviceAmd;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doOclTonemap = IsHwTonemapAvailable(state, options);

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;

            var rotation = state.VideoStream?.Rotation ?? 0;
            var swapWAndH = Math.Abs(rotation) == 90 && isSwDecoder;
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

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
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                if (isMjpegEncoder && !doOclTonemap)
                {
                    // sw decoder + hw mjpeg encoder
                    swScaleFilter = string.IsNullOrEmpty(swScaleFilter) ? "scale=out_range=pc" : $"{swScaleFilter}:out_range=pc";
                }

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
                var hwScaleFilter = GetHwScaleFilter("scale", "vaapi", outFormat, false, inW, inH, reqW, reqH, reqMaxW, reqMaxH);

                if (!string.IsNullOrEmpty(hwScaleFilter) && isMjpegEncoder)
                {
                    hwScaleFilter += doOclTonemap ? string.Empty : ":out_range=pc";
                    hwScaleFilter += ":mode=hq";
                }

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
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12", isMjpegEncoder);
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
                    var subW = state.SubtitleStream?.Width;
                    var subH = state.SubtitleStream?.Height;
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    overlayFilters.Add("overlay=eof_action=pass:repeatlast=0");

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
            if (options.HardwareAccelerationType != HardwareAccelerationType.videotoolbox)
            {
                return (null, null, null);
            }

            // ReSharper disable once InconsistentNaming
            var isMacOS = OperatingSystem.IsMacOS();
            var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
            var isVtDecoder = vidDecoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase);
            var isVtEncoder = vidEncoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase);
            var isVtFullSupported = isMacOS && IsVideoToolboxFullSupported();

            // legacy videotoolbox pipeline (disable hw filters)
            if (!(isVtEncoder || isVtDecoder)
                || !isVtFullSupported
                || !_mediaEncoder.SupportsFilter("alphasrc"))
            {
                return GetSwVidFilterChain(state, options, vidEncoder);
            }

            // preferred videotoolbox + metal filters pipeline
            return GetAppleVidFiltersPreferred(state, options, vidDecoder, vidEncoder);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetAppleVidFiltersPreferred(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidDecoder,
            string vidEncoder)
        {
            var isVtEncoder = vidEncoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase);
            var isVtDecoder = vidDecoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase);
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);

            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doVtTonemap = IsVideoToolboxTonemapAvailable(state, options);
            var doMetalTonemap = !doVtTonemap && IsHwTonemapAvailable(state, options);
            var usingHwSurface = isVtDecoder && (_mediaEncoder.EncoderVersion >= _minFFmpegWorkingVtHwSurface);

            var rotation = state.VideoStream?.Rotation ?? 0;
            var tranposeDir = rotation == 0 ? string.Empty : GetVideoTransposeDirection(state);
            var doVtTranspose = !string.IsNullOrEmpty(tranposeDir) && _mediaEncoder.SupportsFilter("transpose_vt");
            var swapWAndH = Math.Abs(rotation) == 90 && doVtTranspose;
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

            var scaleFormat = string.Empty;
            // Use P010 for Metal tone mapping, otherwise force an 8bit output.
            if (!string.Equals(state.VideoStream.PixelFormat, "yuv420p", StringComparison.OrdinalIgnoreCase))
            {
                if (doMetalTonemap)
                {
                    if (!string.Equals(state.VideoStream.PixelFormat, "yuv420p10le", StringComparison.OrdinalIgnoreCase))
                    {
                        scaleFormat = "p010le";
                    }
                }
                else
                {
                    scaleFormat = "nv12";
                }
            }

            var hwScaleFilter = GetHwScaleFilter("scale", "vt", scaleFormat, false, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));

            /* Make main filters for video stream */
            var mainFilters = new List<string>();

            // hw deint
            if (doDeintH2645)
            {
                var deintFilter = GetHwDeinterlaceFilter(state, options, "videotoolbox");
                mainFilters.Add(deintFilter);
            }

            // hw transpose
            if (doVtTranspose)
            {
                mainFilters.Add($"transpose_vt=dir={tranposeDir}");
            }

            if (doVtTonemap)
            {
                const string VtTonemapArgs = "color_matrix=bt709:color_primaries=bt709:color_transfer=bt709";

                // scale_vt can handle scaling & tonemapping in one shot, just like vpp_qsv.
                hwScaleFilter = string.IsNullOrEmpty(hwScaleFilter)
                    ? "scale_vt=" + VtTonemapArgs
                    : hwScaleFilter + ":" + VtTonemapArgs;
            }

            // hw scale & vt tonemap
            mainFilters.Add(hwScaleFilter);

            // Metal tonemap
            if (doMetalTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "videotoolbox", "nv12", isMjpegEncoder);
                mainFilters.Add(tonemapFilter);
            }

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();

            if (hasSubs)
            {
                if (hasGraphicalSubs)
                {
                    var subW = state.SubtitleStream?.Width;
                    var subH = state.SubtitleStream?.Height;
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    subFilters.Add("format=bgra");
                }
                else if (hasTextSubs)
                {
                    var framerate = state.VideoStream?.RealFrameRate;
                    var subFramerate = hasAssSubs ? Math.Min(framerate ?? 25, 60) : 10;

                    var alphaSrcFilter = GetAlphaSrcFilter(state, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH, subFramerate);
                    var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                    subFilters.Add(alphaSrcFilter);
                    subFilters.Add("format=bgra");
                    subFilters.Add(subTextSubtitlesFilter);
                }

                subFilters.Add("hwupload");
                overlayFilters.Add("overlay_videotoolbox=eof_action=pass:repeatlast=0");
            }

            if (usingHwSurface)
            {
                if (!isVtEncoder)
                {
                    mainFilters.Add("hwdownload");
                    mainFilters.Add("format=nv12");
                }

                return (mainFilters, subFilters, overlayFilters);
            }

            // For old jellyfin-ffmpeg that has broken hwsurface, add a hwupload
            var needFiltering = mainFilters.Any(f => !string.IsNullOrEmpty(f)) ||
                                subFilters.Any(f => !string.IsNullOrEmpty(f)) ||
                                overlayFilters.Any(f => !string.IsNullOrEmpty(f));
            if (needFiltering)
            {
                // INPUT videotoolbox/memory surface(vram/uma)
                // this will pass-through automatically if in/out format matches.
                mainFilters.Insert(0, "hwupload");
                mainFilters.Insert(0, "format=nv12|p010le|videotoolbox_vld");

                if (!isVtEncoder)
                {
                    mainFilters.Add("hwdownload");
                    mainFilters.Add("format=nv12");
                }
            }

            return (mainFilters, subFilters, overlayFilters);
        }

        /// <summary>
        /// Gets the parameter of Rockchip RKMPP/RKRGA filter chain.
        /// </summary>
        /// <param name="state">Encoding state.</param>
        /// <param name="options">Encoding options.</param>
        /// <param name="vidEncoder">Video encoder to use.</param>
        /// <returns>The tuple contains three lists: main, sub and overlay filters.</returns>
        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetRkmppVidFilterChain(
            EncodingJobInfo state,
            EncodingOptions options,
            string vidEncoder)
        {
            if (options.HardwareAccelerationType != HardwareAccelerationType.rkmpp)
            {
                return (null, null, null);
            }

            var isLinux = OperatingSystem.IsLinux();
            var vidDecoder = GetHardwareVideoDecoder(state, options) ?? string.Empty;
            var isSwDecoder = string.IsNullOrEmpty(vidDecoder);
            var isSwEncoder = !vidEncoder.Contains("rkmpp", StringComparison.OrdinalIgnoreCase);
            var isRkmppOclSupported = isLinux && IsRkmppFullSupported() && IsOpenclFullSupported();

            if ((isSwDecoder && isSwEncoder)
                || !isRkmppOclSupported
                || !_mediaEncoder.SupportsFilter("alphasrc"))
            {
                return GetSwVidFilterChain(state, options, vidEncoder);
            }

            // prefered rkmpp + rkrga + opencl filters pipeline
            if (isRkmppOclSupported)
            {
                return GetRkmppVidFiltersPrefered(state, options, vidDecoder, vidEncoder);
            }

            return (null, null, null);
        }

        public (List<string> MainFilters, List<string> SubFilters, List<string> OverlayFilters) GetRkmppVidFiltersPrefered(
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

            var isRkmppDecoder = vidDecoder.Contains("rkmpp", StringComparison.OrdinalIgnoreCase);
            var isRkmppEncoder = vidEncoder.Contains("rkmpp", StringComparison.OrdinalIgnoreCase);
            var isSwDecoder = !isRkmppDecoder;
            var isSwEncoder = !isRkmppEncoder;
            var isMjpegEncoder = vidEncoder.Contains("mjpeg", StringComparison.OrdinalIgnoreCase);
            var isDrmInDrmOut = isRkmppDecoder && isRkmppEncoder;
            var isEncoderSupportAfbc = isRkmppEncoder
                && (vidEncoder.Contains("h264", StringComparison.OrdinalIgnoreCase)
                    || vidEncoder.Contains("hevc", StringComparison.OrdinalIgnoreCase));

            var doDeintH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var doDeintHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);
            var doDeintH2645 = doDeintH264 || doDeintHevc;
            var doOclTonemap = IsHwTonemapAvailable(state, options);

            var hasSubs = state.SubtitleStream != null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;
            var hasAssSubs = hasSubs
                && (string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase));
            var subW = state.SubtitleStream?.Width;
            var subH = state.SubtitleStream?.Height;

            var rotation = state.VideoStream?.Rotation ?? 0;
            var tranposeDir = rotation == 0 ? string.Empty : GetVideoTransposeDirection(state);
            var doRkVppTranspose = !string.IsNullOrEmpty(tranposeDir);
            var swapWAndH = Math.Abs(rotation) == 90 && (isSwDecoder || (isRkmppDecoder && doRkVppTranspose));
            var swpInW = swapWAndH ? inH : inW;
            var swpInH = swapWAndH ? inW : inH;

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
                var swScaleFilter = GetSwScaleFilter(state, options, vidEncoder, swpInW, swpInH, threeDFormat, reqW, reqH, reqMaxW, reqMaxH);
                if (isMjpegEncoder && !doOclTonemap)
                {
                    // sw decoder + hw mjpeg encoder
                    swScaleFilter = string.IsNullOrEmpty(swScaleFilter) ? "scale=out_range=pc" : $"{swScaleFilter}:out_range=pc";
                }

                if (!string.IsNullOrEmpty(swScaleFilter))
                {
                    swScaleFilter += ":flags=fast_bilinear";
                }

                // sw scale
                mainFilters.Add(swScaleFilter);
                mainFilters.Add($"format={outFormat}");

                // keep video at memory except ocl tonemap,
                // since the overhead caused by hwupload >>> using sw filter.
                // sw => hw
                if (doOclTonemap)
                {
                    mainFilters.Add("hwupload=derive_device=opencl");
                }
            }
            else if (isRkmppDecoder)
            {
                // INPUT rkmpp/drm surface(gem/dma-heap)

                var isFullAfbcPipeline = isEncoderSupportAfbc && isDrmInDrmOut && !doOclTonemap;
                var swapOutputWandH = doRkVppTranspose && swapWAndH;
                var outFormat = doOclTonemap ? "p010" : (isMjpegEncoder ? "bgra" : "nv12"); // RGA only support full range in rgb fmts
                var hwScaleFilter = GetHwScaleFilter("vpp", "rkrga", outFormat, swapOutputWandH, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);
                var doScaling = GetHwScaleFilter("vpp", "rkrga", string.Empty, swapOutputWandH, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH);

                if (!hasSubs
                     || doRkVppTranspose
                     || !isFullAfbcPipeline
                     || !string.IsNullOrEmpty(doScaling))
                {
                    // RGA3 hardware only support (1/8 ~ 8) scaling in each blit operation,
                    // but in Trickplay there's a case: (3840/320 == 12), enable 2pass for it
                    if (!string.IsNullOrEmpty(doScaling)
                        && !IsScaleRatioSupported(inW, inH, reqW, reqH, reqMaxW, reqMaxH, 8.0f))
                    {
                        // Vendor provided BSP kernel has an RGA driver bug that causes the output to be corrupted for P010 format.
                        // Use NV15 instead of P010 to avoid the issue.
                        // SDR inputs are using BGRA formats already which is not affected.
                        var intermediateFormat = string.Equals(outFormat, "p010", StringComparison.OrdinalIgnoreCase) ? "nv15" : outFormat;
                        var hwScaleFilterFirstPass = $"scale_rkrga=w=iw/7.9:h=ih/7.9:format={intermediateFormat}:force_divisible_by=4:afbc=1";
                        mainFilters.Add(hwScaleFilterFirstPass);
                    }

                    if (!string.IsNullOrEmpty(hwScaleFilter) && doRkVppTranspose)
                    {
                        hwScaleFilter += $":transpose={tranposeDir}";
                    }

                    // try enabling AFBC to save DDR bandwidth
                    if (!string.IsNullOrEmpty(hwScaleFilter) && isFullAfbcPipeline)
                    {
                        hwScaleFilter += ":afbc=1";
                    }

                    // hw transpose & scale
                    mainFilters.Add(hwScaleFilter);
                }
            }

            if (doOclTonemap && isRkmppDecoder)
            {
                // map from rkmpp/drm to opencl via drm-opencl interop.
                mainFilters.Add("hwmap=derive_device=opencl");
            }

            // ocl tonemap
            if (doOclTonemap)
            {
                var tonemapFilter = GetHwTonemapFilter(options, "opencl", "nv12", isMjpegEncoder);
                mainFilters.Add(tonemapFilter);
            }

            var memoryOutput = false;
            var isUploadForOclTonemap = isSwDecoder && doOclTonemap;
            if ((isRkmppDecoder && isSwEncoder) || isUploadForOclTonemap)
            {
                memoryOutput = true;

                // OUTPUT nv12 surface(memory)
                mainFilters.Add("hwdownload");
                mainFilters.Add("format=nv12");
            }

            // OUTPUT nv12 surface(memory)
            if (isSwDecoder && isRkmppEncoder)
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

            if (isDrmInDrmOut)
            {
                if (doOclTonemap)
                {
                    // OUTPUT drm(nv12) surface(gem/dma-heap)
                    // reverse-mapping via drm-opencl interop.
                    mainFilters.Add("hwmap=derive_device=rkmpp:reverse=1");
                    mainFilters.Add("format=drm_prime");
                }
            }

            /* Make sub and overlay filters for subtitle stream */
            var subFilters = new List<string>();
            var overlayFilters = new List<string>();
            if (isDrmInDrmOut)
            {
                if (hasSubs)
                {
                    if (hasGraphicalSubs)
                    {
                        var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                        subFilters.Add(subPreProcFilters);
                        subFilters.Add("format=bgra");
                    }
                    else if (hasTextSubs)
                    {
                        var framerate = state.VideoStream?.RealFrameRate;
                        var subFramerate = hasAssSubs ? Math.Min(framerate ?? 25, 60) : 10;

                        // alphasrc=s=1280x720:r=10:start=0,format=bgra,subtitles,hwupload
                        var alphaSrcFilter = GetAlphaSrcFilter(state, swpInW, swpInH, reqW, reqH, reqMaxW, reqMaxH, subFramerate);
                        var subTextSubtitlesFilter = GetTextSubtitlesFilter(state, true, true);
                        subFilters.Add(alphaSrcFilter);
                        subFilters.Add("format=bgra");
                        subFilters.Add(subTextSubtitlesFilter);
                    }

                    subFilters.Add("hwupload=derive_device=rkmpp");

                    // try enabling AFBC to save DDR bandwidth
                    var hwOverlayFilter = "overlay_rkrga=eof_action=pass:repeatlast=0:format=nv12";
                    if (isEncoderSupportAfbc)
                    {
                        hwOverlayFilter += ":afbc=1";
                    }

                    overlayFilters.Add(hwOverlayFilter);
                }
            }
            else if (memoryOutput)
            {
                if (hasGraphicalSubs)
                {
                    var subPreProcFilters = GetGraphicalSubPreProcessFilters(swpInW, swpInH, subW, subH, reqW, reqH, reqMaxW, reqMaxH);
                    subFilters.Add(subPreProcFilters);
                    overlayFilters.Add("overlay=eof_action=pass:repeatlast=0");
                }
            }

            return (mainFilters, subFilters, overlayFilters);
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

            var hasSubs = state.SubtitleStream is not null && ShouldEncodeSubtitle(state);
            var hasTextSubs = hasSubs && state.SubtitleStream.IsTextSubtitleStream;
            var hasGraphicalSubs = hasSubs && !state.SubtitleStream.IsTextSubtitleStream;

            List<string> mainFilters;
            List<string> subFilters;
            List<string> overlayFilters;

            (mainFilters, subFilters, overlayFilters) = options.HardwareAccelerationType switch
            {
                HardwareAccelerationType.vaapi => GetVaapiVidFilterChain(state, options, outputVideoCodec),
                HardwareAccelerationType.amf => GetAmdVidFilterChain(state, options, outputVideoCodec),
                HardwareAccelerationType.qsv => GetIntelVidFilterChain(state, options, outputVideoCodec),
                HardwareAccelerationType.nvenc => GetNvidiaVidFilterChain(state, options, outputVideoCodec),
                HardwareAccelerationType.videotoolbox => GetAppleVidFilterChain(state, options, outputVideoCodec),
                HardwareAccelerationType.rkmpp => GetRkmppVidFilterChain(state, options, outputVideoCodec),
                _ => GetSwVidFilterChain(state, options, outputVideoCodec),
            };

            mainFilters?.RemoveAll(string.IsNullOrEmpty);
            subFilters?.RemoveAll(string.IsNullOrEmpty);
            overlayFilters?.RemoveAll(string.IsNullOrEmpty);

            var framerate = GetFramerateParam(state);
            if (framerate.HasValue)
            {
                mainFilters.Insert(0, string.Format(
                    CultureInfo.InvariantCulture,
                    "fps={0}",
                    framerate.Value));
            }

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
                    || string.Equals(videoStream.PixelFormat, "yuv422p", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.PixelFormat, "yuv444p", StringComparison.OrdinalIgnoreCase))
                {
                    return 8;
                }

                if (string.Equals(videoStream.PixelFormat, "yuv420p10le", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.PixelFormat, "yuv422p10le", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.PixelFormat, "yuv444p10le", StringComparison.OrdinalIgnoreCase))
                {
                    return 10;
                }

                if (string.Equals(videoStream.PixelFormat, "yuv420p12le", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.PixelFormat, "yuv422p12le", StringComparison.OrdinalIgnoreCase)
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

            var hardwareAccelerationType = options.HardwareAccelerationType;

            if (!string.IsNullOrEmpty(videoStream.Codec) && hardwareAccelerationType != HardwareAccelerationType.none)
            {
                var bitDepth = GetVideoColorBitDepth(state);

                // Only HEVC, VP9 and AV1 formats have 10-bit hardware decoder support for most platforms
                if (bitDepth == 10
                    && !(string.Equals(videoStream.Codec, "hevc", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoStream.Codec, "h265", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoStream.Codec, "vp9", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoStream.Codec, "av1", StringComparison.OrdinalIgnoreCase)))
                {
                    // RKMPP has H.264 Hi10P decoder
                    bool hasHardwareHi10P = hardwareAccelerationType == HardwareAccelerationType.rkmpp;

                    // VideoToolbox on Apple Silicon has H.264 Hi10P mode enabled after macOS 14.6
                    if (hardwareAccelerationType == HardwareAccelerationType.videotoolbox)
                    {
                        var ver = Environment.OSVersion.Version;
                        var arch = RuntimeInformation.OSArchitecture;
                        if (arch.Equals(Architecture.Arm64) && ver >= new Version(14, 6))
                        {
                            hasHardwareHi10P = true;
                        }
                    }

                    if (!hasHardwareHi10P
                        && string.Equals(videoStream.Codec, "h264", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }

                var decoder = hardwareAccelerationType switch
                {
                    HardwareAccelerationType.vaapi => GetVaapiVidDecoder(state, options, videoStream, bitDepth),
                    HardwareAccelerationType.amf => GetAmfVidDecoder(state, options, videoStream, bitDepth),
                    HardwareAccelerationType.qsv => GetQsvHwVidDecoder(state, options, videoStream, bitDepth),
                    HardwareAccelerationType.nvenc => GetNvdecVidDecoder(state, options, videoStream, bitDepth),
                    HardwareAccelerationType.videotoolbox => GetVideotoolboxVidDecoder(state, options, videoStream, bitDepth),
                    HardwareAccelerationType.rkmpp => GetRkmppVidDecoder(state, options, videoStream, bitDepth),
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(decoder))
                {
                    return decoder;
                }
            }

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

            // VideoToolbox decoders have built-in SW fallback
            if (bitDepth == 10
                && isCodecAvailable
                && (options.HardwareAccelerationType != HardwareAccelerationType.videotoolbox))
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

            if (string.Equals(decoderSuffix, "rkmpp", StringComparison.OrdinalIgnoreCase))
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
            var isRkmppSupported = isLinux && IsRkmppFullSupported();
            var isCodecAvailable = options.HardwareDecodingCodecs.Contains(videoCodec, StringComparison.OrdinalIgnoreCase);
            var hardwareAccelerationType = options.HardwareAccelerationType;

            var ffmpegVersion = _mediaEncoder.EncoderVersion;

            // Set the av1 codec explicitly to trigger hw accelerator, otherwise libdav1d will be used.
            var isAv1 = ffmpegVersion < _minFFmpegImplictHwaccel
                && string.Equals(videoCodec, "av1", StringComparison.OrdinalIgnoreCase);

            // Allow profile mismatch if decoding H.264 baseline with d3d11va and vaapi hwaccels.
            var profileMismatch = string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase)
                && string.Equals(state.VideoStream?.Profile, "baseline", StringComparison.OrdinalIgnoreCase);

            // Disable the extra internal copy in nvdec. We already handle it in filter chain.
            var nvdecNoInternalCopy = ffmpegVersion >= _minFFmpegHwaUnsafeOutput;

            // Strip the display rotation side data from the transposed fmp4 output stream.
            var stripRotationData = (state.VideoStream?.Rotation ?? 0) != 0
                && ffmpegVersion >= _minFFmpegDisplayRotationOption;
            var stripRotationDataArgs = stripRotationData ? " -display_rotation 0" : string.Empty;

            // VideoToolbox decoders have built-in SW fallback
            if (isCodecAvailable
                && (options.HardwareAccelerationType != HardwareAccelerationType.videotoolbox))
            {
                if (string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                    && options.HardwareDecodingCodecs.Contains("hevc", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsVideoStreamHevcRext(state))
                    {
                        if (bitDepth <= 10 && !options.EnableDecodingColorDepth10HevcRext)
                        {
                            return null;
                        }

                        if (bitDepth == 12 && !options.EnableDecodingColorDepth12HevcRext)
                        {
                            return null;
                        }

                        if (hardwareAccelerationType == HardwareAccelerationType.vaapi
                            && !_mediaEncoder.IsVaapiDeviceInteliHD)
                        {
                            return null;
                        }
                    }
                    else if (bitDepth == 10 && !options.EnableDecodingColorDepth10Hevc)
                    {
                        return null;
                    }
                }

                if (string.Equals(videoCodec, "vp9", StringComparison.OrdinalIgnoreCase)
                    && options.HardwareDecodingCodecs.Contains("vp9", StringComparison.OrdinalIgnoreCase)
                    && bitDepth == 10
                    && !options.EnableDecodingColorDepth10Vp9)
                {
                    return null;
                }
            }

            // Intel qsv/d3d11va/vaapi
            if (hardwareAccelerationType == HardwareAccelerationType.qsv)
            {
                if (options.PreferSystemNativeHwDecoder)
                {
                    if (isVaapiSupported && isCodecAvailable)
                    {
                        return " -hwaccel vaapi" + (outputHwSurface ? " -hwaccel_output_format vaapi -noautorotate" + stripRotationDataArgs : string.Empty)
                            + (profileMismatch ? " -hwaccel_flags +allow_profile_mismatch" : string.Empty) + (isAv1 ? " -c:v av1" : string.Empty);
                    }

                    if (isD3d11Supported && isCodecAvailable)
                    {
                        return " -hwaccel d3d11va" + (outputHwSurface ? " -hwaccel_output_format d3d11 -noautorotate" + stripRotationDataArgs : string.Empty)
                            + (profileMismatch ? " -hwaccel_flags +allow_profile_mismatch" : string.Empty) + " -threads 2" + (isAv1 ? " -c:v av1" : string.Empty);
                    }
                }
                else
                {
                    if (isQsvSupported && isCodecAvailable)
                    {
                        return " -hwaccel qsv" + (outputHwSurface ? " -hwaccel_output_format qsv -noautorotate" + stripRotationDataArgs : string.Empty);
                    }
                }
            }

            // Nvidia cuda
            if (hardwareAccelerationType == HardwareAccelerationType.nvenc)
            {
                if (isCudaSupported && isCodecAvailable)
                {
                    if (options.EnableEnhancedNvdecDecoder)
                    {
                        // set -threads 1 to nvdec decoder explicitly since it doesn't implement threading support.
                        return " -hwaccel cuda" + (outputHwSurface ? " -hwaccel_output_format cuda -noautorotate" + stripRotationDataArgs : string.Empty)
                            + (nvdecNoInternalCopy ? " -hwaccel_flags +unsafe_output" : string.Empty) + " -threads 1" + (isAv1 ? " -c:v av1" : string.Empty);
                    }

                    // cuvid decoder doesn't have threading issue.
                    return " -hwaccel cuda" + (outputHwSurface ? " -hwaccel_output_format cuda -noautorotate" + stripRotationDataArgs : string.Empty);
                }
            }

            // Amd d3d11va
            if (hardwareAccelerationType == HardwareAccelerationType.amf)
            {
                if (isD3d11Supported && isCodecAvailable)
                {
                    return " -hwaccel d3d11va" + (outputHwSurface ? " -hwaccel_output_format d3d11 -noautorotate" + stripRotationDataArgs : string.Empty)
                        + (profileMismatch ? " -hwaccel_flags +allow_profile_mismatch" : string.Empty) + (isAv1 ? " -c:v av1" : string.Empty);
                }
            }

            // Vaapi
            if (hardwareAccelerationType == HardwareAccelerationType.vaapi
                && isVaapiSupported
                && isCodecAvailable)
            {
                return " -hwaccel vaapi" + (outputHwSurface ? " -hwaccel_output_format vaapi -noautorotate" + stripRotationDataArgs : string.Empty)
                    + (profileMismatch ? " -hwaccel_flags +allow_profile_mismatch" : string.Empty) + (isAv1 ? " -c:v av1" : string.Empty);
            }

            // Apple videotoolbox
            if (hardwareAccelerationType == HardwareAccelerationType.videotoolbox
                && isVideotoolboxSupported
                && isCodecAvailable)
            {
                return " -hwaccel videotoolbox" + (outputHwSurface ? " -hwaccel_output_format videotoolbox_vld" : string.Empty) + " -noautorotate" + stripRotationDataArgs;
            }

            // Rockchip rkmpp
            if (hardwareAccelerationType == HardwareAccelerationType.rkmpp
                && isRkmppSupported
                && isCodecAvailable)
            {
                return " -hwaccel rkmpp" + (outputHwSurface ? " -hwaccel_output_format drm_prime -noautorotate" + stripRotationDataArgs : string.Empty);
            }

            return null;
        }

        public string GetQsvHwVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            var isWindows = OperatingSystem.IsWindows();
            var isLinux = OperatingSystem.IsLinux();

            if ((!isWindows && !isLinux)
                || options.HardwareAccelerationType != HardwareAccelerationType.qsv)
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
            var is8_10_12bitSwFormatsQsv = is8_10bitSwFormatsQsv
                || string.Equals("yuv422p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv422p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv420p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv422p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
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
                if (string.Equals(videoStream.Codec, "vp9", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, hwSurface) + GetHwDecoderName(options, "vp9", "qsv", "vp9", bitDepth);
                }

                if (string.Equals(videoStream.Codec, "av1", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "av1", bitDepth, hwSurface) + GetHwDecoderName(options, "av1", "qsv", "av1", bitDepth);
                }
            }

            if (is8_10_12bitSwFormatsQsv)
            {
                if (string.Equals(videoStream.Codec, "hevc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.Codec, "h265", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, hwSurface) + GetHwDecoderName(options, "hevc", "qsv", "hevc", bitDepth);
                }
            }

            return null;
        }

        public string GetNvdecVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            if ((!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
                || options.HardwareAccelerationType != HardwareAccelerationType.nvenc)
            {
                return null;
            }

            var hwSurface = IsCudaFullSupported() && _mediaEncoder.SupportsFilter("alphasrc");
            var is8bitSwFormatsNvdec = string.Equals("yuv420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                                       || string.Equals("yuvj420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10bitSwFormatsNvdec = is8bitSwFormatsNvdec || string.Equals("yuv420p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10_12bitSwFormatsNvdec = is8_10bitSwFormatsNvdec
                || string.Equals("yuv444p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv420p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
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
                if (string.Equals("vp9", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, hwSurface) + GetHwDecoderName(options, "vp9", "cuvid", "vp9", bitDepth);
                }

                if (string.Equals("av1", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "av1", bitDepth, hwSurface) + GetHwDecoderName(options, "av1", "cuvid", "av1", bitDepth);
                }
            }

            if (is8_10_12bitSwFormatsNvdec)
            {
                if (string.Equals("hevc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h265", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, hwSurface) + GetHwDecoderName(options, "hevc", "cuvid", "hevc", bitDepth);
                }
            }

            return null;
        }

        public string GetAmfVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            if (!OperatingSystem.IsWindows()
                || options.HardwareAccelerationType != HardwareAccelerationType.amf)
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
                || options.HardwareAccelerationType != HardwareAccelerationType.vaapi)
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
            var is8_10_12bitSwFormatsVaapi = is8_10bitSwFormatsVaapi
                || string.Equals("yuv422p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv422p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv420p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv422p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);

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
                if (string.Equals("vp9", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, hwSurface);
                }

                if (string.Equals("av1", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "av1", bitDepth, hwSurface);
                }
            }

            if (is8_10_12bitSwFormatsVaapi)
            {
                if (string.Equals("hevc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h265", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, hwSurface);
                }
            }

            return null;
        }

        public string GetVideotoolboxVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            if (!OperatingSystem.IsMacOS()
                || options.HardwareAccelerationType != HardwareAccelerationType.videotoolbox)
            {
                return null;
            }

            var is8bitSwFormatsVt = string.Equals("yuv420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals("yuvj420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10bitSwFormatsVt = is8bitSwFormatsVt || string.Equals("yuv420p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10_12bitSwFormatsVt = is8_10bitSwFormatsVt
                || string.Equals("yuv422p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv422p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv420p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv422p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                || string.Equals("yuv444p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);

            // The related patches make videotoolbox hardware surface working is only available in jellyfin-ffmpeg 7.0.1 at the moment.
            bool useHwSurface = (_mediaEncoder.EncoderVersion >= _minFFmpegWorkingVtHwSurface) && IsVideoToolboxFullSupported();

            if (is8bitSwFormatsVt)
            {
                if (string.Equals("vp8", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp8", bitDepth, useHwSurface);
                }
            }

            if (is8_10bitSwFormatsVt)
            {
                if (string.Equals("avc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "h264", bitDepth, useHwSurface);
                }

                if (string.Equals("vp9", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp9", bitDepth, useHwSurface);
                }
            }

            if (is8_10_12bitSwFormatsVt)
            {
                if (string.Equals("hevc", videoStream.Codec, StringComparison.OrdinalIgnoreCase)
                    || string.Equals("h265", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "hevc", bitDepth, useHwSurface);
                }
            }

            return null;
        }

        public string GetRkmppVidDecoder(EncodingJobInfo state, EncodingOptions options, MediaStream videoStream, int bitDepth)
        {
            var isLinux = OperatingSystem.IsLinux();

            if (!isLinux
                || options.HardwareAccelerationType != HardwareAccelerationType.rkmpp)
            {
                return null;
            }

            var inW = state.VideoStream?.Width;
            var inH = state.VideoStream?.Height;
            var reqW = state.BaseRequest.Width;
            var reqH = state.BaseRequest.Height;
            var reqMaxW = state.BaseRequest.MaxWidth;
            var reqMaxH = state.BaseRequest.MaxHeight;

            // rkrga RGA2e supports range from 1/16 to 16
            if (!IsScaleRatioSupported(inW, inH, reqW, reqH, reqMaxW, reqMaxH, 16.0f))
            {
                return null;
            }

            var isRkmppOclSupported = IsRkmppFullSupported() && IsOpenclFullSupported();
            var hwSurface = isRkmppOclSupported
                && _mediaEncoder.SupportsFilter("alphasrc");

            // rkrga RGA3 supports range from 1/8 to 8
            var isAfbcSupported = hwSurface && IsScaleRatioSupported(inW, inH, reqW, reqH, reqMaxW, reqMaxH, 8.0f);

            // TODO: add more 8/10bit and 4:2:2 formats for Rkmpp after finishing the ffcheck tool
            var is8bitSwFormatsRkmpp = string.Equals("yuv420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
                                       || string.Equals("yuvj420p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is10bitSwFormatsRkmpp = string.Equals("yuv420p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
            var is8_10bitSwFormatsRkmpp = is8bitSwFormatsRkmpp || is10bitSwFormatsRkmpp;

            // nv15 and nv20 are bit-stream only formats
            if (is10bitSwFormatsRkmpp && !hwSurface)
            {
                return null;
            }

            if (is8bitSwFormatsRkmpp)
            {
                if (string.Equals(videoStream.Codec, "mpeg1video", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg1video", bitDepth, hwSurface);
                }

                if (string.Equals(videoStream.Codec, "mpeg2video", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg2video", bitDepth, hwSurface);
                }

                if (string.Equals(videoStream.Codec, "mpeg4", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "mpeg4", bitDepth, hwSurface);
                }

                if (string.Equals(videoStream.Codec, "vp8", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "vp8", bitDepth, hwSurface);
                }
            }

            if (is8_10bitSwFormatsRkmpp)
            {
                if (string.Equals(videoStream.Codec, "avc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.Codec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    var accelType = GetHwaccelType(state, options, "h264", bitDepth, hwSurface);
                    return accelType + ((!string.IsNullOrEmpty(accelType) && isAfbcSupported) ? " -afbc rga" : string.Empty);
                }

                if (string.Equals(videoStream.Codec, "hevc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.Codec, "h265", StringComparison.OrdinalIgnoreCase))
                {
                    var accelType = GetHwaccelType(state, options, "hevc", bitDepth, hwSurface);
                    return accelType + ((!string.IsNullOrEmpty(accelType) && isAfbcSupported) ? " -afbc rga" : string.Empty);
                }

                if (string.Equals(videoStream.Codec, "vp9", StringComparison.OrdinalIgnoreCase))
                {
                    var accelType = GetHwaccelType(state, options, "vp9", bitDepth, hwSurface);
                    return accelType + ((!string.IsNullOrEmpty(accelType) && isAfbcSupported) ? " -afbc rga" : string.Empty);
                }

                if (string.Equals(videoStream.Codec, "av1", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHwaccelType(state, options, "av1", bitDepth, hwSurface);
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
            var threads = state?.BaseRequest.CpuCoreLimit ?? encodingOptions.EncodingThreadCount;

            if (threads <= 0)
            {
                // Automatically set thread count
                return 0;
            }

            return Math.Min(threads, Environment.ProcessorCount);
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
                inputModifier += GetVideoSyncOption(state.InputVideoSync, _mediaEncoder.EncoderVersion);
            }

            if (state.ReadInputAtNativeFramerate && state.InputProtocol != MediaProtocol.Rtsp)
            {
                inputModifier += " -re";
            }
            else if (encodingOptions.EnableSegmentDeletion
                && state.VideoStream is not null
                && state.TranscodingType == TranscodingJobType.Hls
                && IsCopyCodec(state.OutputVideoCodec)
                && _mediaEncoder.EncoderVersion >= _minFFmpegReadrateOption)
            {
                // Set an input read rate limit 10x for using SegmentDeletion with stream-copy
                // to prevent ffmpeg from exiting prematurely (due to fast drive)
                inputModifier += " -readrate 10";
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
                if (!string.IsNullOrEmpty(state.InputContainer) && state.VideoType == VideoType.VideoFile && encodingOptions.HardwareAccelerationType != HardwareAccelerationType.none)
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

                request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(_mediaEncoder.CanEncodeToAudioCodec)
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
                shiftAudioCodecs.Add("dts");
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
                var removed = audioCodecs[0];
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
                var removed = videoCodecs[0];
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

        public string GetProgressiveVideoFullCommandLine(EncodingJobInfo state, EncodingOptions encodingOptions, EncoderPreset defaultPreset)
        {
            // Get the output codec name
            var videoCodec = GetVideoEncoder(state, encodingOptions);

            var format = string.Empty;
            var keyFrame = string.Empty;
            var outputPath = state.OutputFilePath;

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

        public string GetProgressiveVideoArguments(EncodingJobInfo state, EncodingOptions encodingOptions, string videoCodec, EncoderPreset defaultPreset)
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

                var hasGraphicalSubs = state.SubtitleStream is not null && !state.SubtitleStream.IsTextSubtitleStream && ShouldEncodeSubtitle(state);

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
                args += GetVideoSyncOption(state.OutputVideoSync, _mediaEncoder.EncoderVersion);
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

            var useDownMixAlgorithm = state.AudioStream is not null
                                      && DownMixAlgorithmsHelper.AlgorithmFilterStrings.ContainsKey((encodingOptions.DownMixStereoAlgorithm, DownMixAlgorithmsHelper.InferChannelLayout(state.AudioStream)));

            if (channels.HasValue && !useDownMixAlgorithm)
            {
                args += " -ac " + channels.Value;
            }

            var bitrate = state.OutputAudioBitrate;
            if (bitrate.HasValue && !LosslessAudioCodecs.Contains(codec, StringComparison.OrdinalIgnoreCase))
            {
                var vbrParam = GetAudioVbrModeParam(codec, bitrate.Value, channels ?? 2);
                if (encodingOptions.EnableAudioVbr && state.EnableAudioVbrEncoding && vbrParam is not null)
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
                var vbrParam = GetAudioVbrModeParam(GetAudioEncoder(state), bitrate.Value, channels ?? 2);
                if (encodingOptions.EnableAudioVbr && state.EnableAudioVbrEncoding && vbrParam is not null)
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

        private static bool ShouldEncodeSubtitle(EncodingJobInfo state)
        {
            return state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode
                   || (state.BaseRequest.AlwaysBurnInSubtitleWhenTranscoding && !IsCopyCodec(state.OutputVideoCodec));
        }

        public static string GetVideoSyncOption(string videoSync, Version encoderVersion)
        {
            if (string.IsNullOrEmpty(videoSync))
            {
                return string.Empty;
            }

            if (encoderVersion >= new Version(5, 1))
            {
                if (int.TryParse(videoSync, CultureInfo.InvariantCulture, out var vsync))
                {
                    return vsync switch
                    {
                        -1 => " -fps_mode auto",
                        0 => " -fps_mode passthrough",
                        1 => " -fps_mode cfr",
                        2 => " -fps_mode vfr",
                        _ => string.Empty
                    };
                }

                return string.Empty;
            }

            // -vsync is deprecated in FFmpeg 5.1 and will be removed in the future.
            return $" -vsync {videoSync}";
        }
    }
}
