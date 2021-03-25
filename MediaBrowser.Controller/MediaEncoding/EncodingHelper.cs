#pragma warning disable CS1591

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
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class EncodingHelper
    {
        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly IConfiguration _configuration;

        private static readonly string[] _videoProfiles = new[]
        {
            "ConstrainedBaseline",
            "Baseline",
            "Extended",
            "Main",
            "High",
            "ProgressiveHigh",
            "ConstrainedHigh"
        };

        public EncodingHelper(
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            ISubtitleEncoder subtitleEncoder,
            IConfiguration configuration)
        {
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _subtitleEncoder = subtitleEncoder;
            _configuration = configuration;
        }

        public string GetH264Encoder(EncodingJobInfo state, EncodingOptions encodingOptions)
            => GetH264OrH265Encoder("libx264", "h264", state, encodingOptions);

        public string GetH265Encoder(EncodingJobInfo state, EncodingOptions encodingOptions)
            => GetH264OrH265Encoder("libx265", "hevc", state, encodingOptions);

        private string GetH264OrH265Encoder(string defaultEncoder, string hwEncoder, EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is experimental anyway, it's not worth adding additional variables such as this.
            if (state.VideoType == VideoType.VideoFile)
            {
                var hwType = encodingOptions.HardwareAccelerationType;

                var codecMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "qsv",                  hwEncoder + "_qsv" },
                    { hwEncoder + "_qsv",     hwEncoder + "_qsv" },
                    { "nvenc",                hwEncoder + "_nvenc" },
                    { "amf",                  hwEncoder + "_amf" },
                    { "omx",                  hwEncoder + "_omx" },
                    { hwEncoder + "_v4l2m2m", hwEncoder + "_v4l2m2m" },
                    { "mediacodec",           hwEncoder + "_mediacodec" },
                    { "vaapi",                hwEncoder + "_vaapi" },
                    { "videotoolbox",         hwEncoder + "_videotoolbox" }
                };

                if (!string.IsNullOrEmpty(hwType)
                    && encodingOptions.EnableHardwareEncoding
                    && codecMap.ContainsKey(hwType))
                {
                    var preferredEncoder = codecMap[hwType];

                    if (_mediaEncoder.SupportsEncoder(preferredEncoder))
                    {
                        return preferredEncoder;
                    }
                }
            }

            return defaultEncoder;
        }

        private bool IsVaapiSupported(EncodingJobInfo state)
        {
            var videoStream = state.VideoStream;

            // vaapi will throw an error with this input
            // [vaapi @ 0x7faed8000960] No VAAPI support for codec mpeg4 profile -99.
            if (string.Equals(videoStream?.Codec, "mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _mediaEncoder.SupportsHwaccel("vaapi");
        }

        private bool IsCudaSupported()
        {
            return _mediaEncoder.SupportsHwaccel("cuda")
                   && _mediaEncoder.SupportsFilter("scale_cuda", null)
                   && _mediaEncoder.SupportsFilter("yadif_cuda", null);
        }

        private bool IsTonemappingSupported(EncodingJobInfo state, EncodingOptions options)
        {
            var videoStream = state.VideoStream;
            return IsColorDepth10(state)
                   && _mediaEncoder.SupportsHwaccel("opencl")
                   && options.EnableTonemapping
                   && string.Equals(videoStream.VideoRange, "HDR", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsVppTonemappingSupported(EncodingJobInfo state, EncodingOptions options)
        {
            var videoStream = state.VideoStream;
            if (videoStream == null)
            {
                // Remote stream doesn't have media info, disable vpp tonemapping.
                return false;
            }

            var codec = videoStream.Codec;
            if (string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                // Limited to HEVC for now since the filter doesn't accept master data from VP9.
                return IsColorDepth10(state)
                       && string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase)
                       && _mediaEncoder.SupportsHwaccel("vaapi")
                       && options.EnableVppTonemapping
                       && string.Equals(videoStream.ColorTransfer, "smpte2084", StringComparison.OrdinalIgnoreCase);
            }

            // Hybrid VPP tonemapping for QSV with VAAPI
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isLinux && string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
            {
                // Limited to HEVC for now since the filter doesn't accept master data from VP9.
                return IsColorDepth10(state)
                       && string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase)
                       && _mediaEncoder.SupportsHwaccel("vaapi")
                       && _mediaEncoder.SupportsHwaccel("qsv")
                       && options.EnableVppTonemapping
                       && string.Equals(videoStream.ColorTransfer, "smpte2084", StringComparison.OrdinalIgnoreCase);
            }

            // Native VPP tonemapping may come to QSV in the future.
            return false;
        }

        /// <summary>
        /// Gets the name of the output video codec.
        /// </summary>
        public string GetVideoEncoder(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var codec = state.OutputVideoCodec;

            if (!string.IsNullOrEmpty(codec))
            {
                if (string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase))
                {
                    return GetH265Encoder(state, encodingOptions);
                }

                if (string.Equals(codec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    return GetH264Encoder(state, encodingOptions);
                }

                if (string.Equals(codec, "vpx", StringComparison.OrdinalIgnoreCase))
                {
                    return "libvpx";
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
            var ext = Path.GetExtension(url);

            if (string.Equals(ext, ".asf", StringComparison.OrdinalIgnoreCase))
            {
                return "wmv";
            }

            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "vpx";
            }

            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "theora";
            }

            if (string.Equals(ext, ".m3u8", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ts", StringComparison.OrdinalIgnoreCase))
            {
                return "h264";
            }

            return "copy";
        }

        public int GetVideoProfileScore(string profile)
        {
            // strip spaces because they may be stripped out on the query string
            profile = profile.Replace(" ", string.Empty, StringComparison.Ordinal);
            return Array.FindIndex(_videoProfiles, x => string.Equals(x, profile, StringComparison.OrdinalIgnoreCase));
        }

        public string GetInputPathArgument(EncodingJobInfo state)
        {
            var mediaPath = state.MediaPath ?? string.Empty;

            return _mediaEncoder.GetInputArgument(mediaPath, state.MediaSource);
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
                // flac is experimental in mp4 muxer
                return "flac -strict -2";
            }

            return codec.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        public string GetInputArgument(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var arg = new StringBuilder();
            var videoDecoder = GetHardwareAcceleratedVideoDecoder(state, encodingOptions) ?? string.Empty;
            var outputVideoCodec = GetVideoEncoder(state, encodingOptions) ?? string.Empty;
            var isSwDecoder = string.IsNullOrEmpty(videoDecoder);
            var isD3d11vaDecoder = videoDecoder.IndexOf("d3d11va", StringComparison.OrdinalIgnoreCase) != -1;
            var isVaapiDecoder = videoDecoder.IndexOf("vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isVaapiEncoder = outputVideoCodec.IndexOf("vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isQsvDecoder = videoDecoder.IndexOf("qsv", StringComparison.OrdinalIgnoreCase) != -1;
            var isQsvEncoder = outputVideoCodec.IndexOf("qsv", StringComparison.OrdinalIgnoreCase) != -1;
            var isNvdecDecoder = videoDecoder.Contains("cuda", StringComparison.OrdinalIgnoreCase);
            var isCuvidHevcDecoder = videoDecoder.Contains("hevc_cuvid", StringComparison.OrdinalIgnoreCase);
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            var isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            var isTonemappingSupported = IsTonemappingSupported(state, encodingOptions);
            var isVppTonemappingSupported = IsVppTonemappingSupported(state, encodingOptions);

            if (!IsCopyCodec(outputVideoCodec))
            {
                if (state.IsVideoRequest
                    && _mediaEncoder.SupportsHwaccel("vaapi")
                    && string.Equals(encodingOptions.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    if (isVaapiDecoder)
                    {
                        if (isTonemappingSupported && !isVppTonemappingSupported)
                        {
                           arg.Append("-init_hw_device vaapi=va:")
                                .Append(encodingOptions.VaapiDevice)
                                .Append(' ')
                                .Append("-init_hw_device opencl=ocl@va ")
                                .Append("-hwaccel_device va ")
                                .Append("-hwaccel_output_format vaapi ")
                                .Append("-filter_hw_device ocl ");
                        }
                        else
                        {
                            arg.Append("-hwaccel_output_format vaapi ")
                                .Append("-vaapi_device ")
                                .Append(encodingOptions.VaapiDevice)
                                .Append(' ');
                        }
                    }
                    else if (!isVaapiDecoder && isVaapiEncoder)
                    {
                        arg.Append("-vaapi_device ")
                            .Append(encodingOptions.VaapiDevice)
                            .Append(' ');
                    }
                }

                if (state.IsVideoRequest
                    && string.Equals(encodingOptions.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
                {
                    var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

                    if (isQsvEncoder)
                    {
                        if (isQsvDecoder)
                        {
                            if (isLinux)
                            {
                                if (hasGraphicalSubs)
                                {
                                    arg.Append("-init_hw_device qsv=hw -filter_hw_device hw ");
                                }
                                else
                                {
                                    arg.Append("-hwaccel qsv ");
                                }
                            }

                            if (isWindows)
                            {
                                arg.Append("-hwaccel qsv ");
                            }
                        }

                        // While using SW decoder
                        else if (isSwDecoder)
                        {
                            arg.Append("-init_hw_device qsv=hw -filter_hw_device hw ");
                        }

                        // Hybrid VPP tonemapping with VAAPI
                        else if (isVaapiDecoder && isVppTonemappingSupported)
                        {
                            arg.Append("-init_hw_device vaapi=va:")
                                .Append(encodingOptions.VaapiDevice)
                                .Append(' ')
                                .Append("-init_hw_device qsv@va ")
                                .Append("-hwaccel_output_format vaapi ");
                        }
                    }
                }

                if (state.IsVideoRequest
                    && string.Equals(encodingOptions.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase)
                    && isNvdecDecoder)
                {
                    arg.Append("-hwaccel_output_format cuda ");
                }

                if (state.IsVideoRequest
                    && ((string.Equals(encodingOptions.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase)
                         && (isNvdecDecoder || isCuvidHevcDecoder || isSwDecoder))
                        || (string.Equals(encodingOptions.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase) 
                            && (isD3d11vaDecoder || isSwDecoder))))
                {
                    if (isTonemappingSupported)
                    {
                        arg.Append("-init_hw_device opencl=ocl:")
                            .Append(encodingOptions.OpenclDevice)
                            .Append(' ')
                            .Append("-filter_hw_device ocl ");
                    }
                }

                if (state.IsVideoRequest
                    && string.Equals(encodingOptions.HardwareAccelerationType, "videotoolbox", StringComparison.OrdinalIgnoreCase))
                {
                    arg.Append("-hwaccel videotoolbox ");
                }
            }

            arg.Append("-i ")
                .Append(GetInputPathArgument(state));

            if (state.SubtitleStream != null
                && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode
                && state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream)
            {
                var subtitlePath = state.SubtitleStream.Path;

                if (string.Equals(Path.GetExtension(subtitlePath), ".sub", StringComparison.OrdinalIgnoreCase))
                {
                    var idxFile = Path.ChangeExtension(subtitlePath, ".idx");
                    if (File.Exists(idxFile))
                    {
                        subtitlePath = idxFile;
                    }
                }

                arg.Append(" -i \"").Append(subtitlePath).Append('\"');
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
            else if (IsH265(stream))
            {
                return "-bsf:v hevc_mp4toannexb";
            }
            else if (IsAAC(stream))
            {
                // Convert adts header(mpegts) to asc header(mp4).
                return "-bsf:a aac_adtstoasc";
            }
            else
            {
                return null;
            }
        }

        public static string GetAudioBitStreamArguments(EncodingJobInfo state, string segmentContainer, string mediaSourceContainer)
        {
            var bitStreamArgs = string.Empty;
            var segmentFormat = GetSegmentFileExtension(segmentContainer).TrimStart('.');

            // Apply aac_adtstoasc bitstream filter when media source is in mpegts.
            if (string.Equals(segmentFormat, "mp4", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(mediaSourceContainer, "mpegts", StringComparison.OrdinalIgnoreCase)
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
            var bitrate = state.OutputVideoBitrate;

            if (bitrate.HasValue)
            {
                if (string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase))
                {
                    // When crf is used with vpx, b:v becomes a max rate
                    // https://trac.ffmpeg.org/wiki/Encode/VP9
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        " -maxrate:v {0} -bufsize:v {1} -b:v {0}",
                        bitrate.Value,
                        bitrate.Value * 2);
                }

                if (string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        " -b:v {0}",
                        bitrate.Value);
                }

                if (string.Equals(videoCodec, "libx264", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(videoCodec, "libx265", StringComparison.OrdinalIgnoreCase))
                {
                    // h264
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        " -maxrate {0} -bufsize {1}",
                        bitrate.Value,
                        bitrate.Value * 2);
                }

                // h264
                return string.Format(
                    CultureInfo.InvariantCulture,
                    " -b:v {0} -maxrate {0} -bufsize {1}",
                    bitrate.Value,
                    bitrate.Value * 2);
            }

            return string.Empty;
        }

        public static string NormalizeTranscodingLevel(EncodingJobInfo state, string level)
        {
            if (double.TryParse(level, NumberStyles.Any, _usCulture, out double requestLevel))
            {
                if (string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(state.ActualOutputVideoCodec, "h265", StringComparison.OrdinalIgnoreCase))
                {
                    // Transcode to level 5.0 and lower for maximum compatibility.
                    // Level 5.0 is suitable for up to 4k 30fps hevc encoding, otherwise let the encoder to handle it.
                    // https://en.wikipedia.org/wiki/High_Efficiency_Video_Coding_tiers_and_levels
                    // MaxLumaSampleRate = 3840*2160*30 = 248832000 < 267386880.
                    if (requestLevel >= 150)
                    {
                        return "150";
                    }
                }
                else if (string.Equals(state.ActualOutputVideoCodec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    // Clients may direct play higher than level 41, but there's no reason to transcode higher.
                    if (requestLevel >= 41)
                    {
                        return "41";
                    }
                }
            }

            return level;
        }

        /// <summary>
        /// Gets the text subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        public string GetTextSubtitleParam(EncodingJobInfo state)
        {
            var seconds = Math.Round(TimeSpan.FromTicks(state.StartTimeTicks ?? 0).TotalSeconds);

            // hls always copies timestamps
            var setPtsParam = state.CopyTimestamps || state.TranscodingType != TranscodingJobType.Progressive
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, ",setpts=PTS -{0}/TB", seconds);

            // TODO
            // var fallbackFontPath = Path.Combine(_appPaths.ProgramDataPath, "fonts", "DroidSansFallback.ttf");
            // string fallbackFontParam = string.Empty;

            // if (!File.Exists(fallbackFontPath))
            // {
            //     _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(fallbackFontPath));
            //     using (var stream = _assemblyInfo.GetManifestResourceStream(GetType(), GetType().Namespace + ".DroidSansFallback.ttf"))
            //     {
            //         using (var fileStream = new FileStream(fallbackFontPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            //         {
            //             stream.CopyTo(fileStream);
            //         }
            //     }
            // }

            // fallbackFontParam = string.Format(CultureInfo.InvariantCulture, ":force_style='FontName=Droid Sans Fallback':fontsdir='{0}'", _mediaEncoder.EscapeSubtitleFilterPath(_fileSystem.GetDirectoryName(fallbackFontPath)));

            if (state.SubtitleStream.IsExternal)
            {
                var subtitlePath = state.SubtitleStream.Path;

                var charsetParam = string.Empty;

                if (!string.IsNullOrEmpty(state.SubtitleStream.Language))
                {
                    var charenc = _subtitleEncoder.GetSubtitleFileCharacterSet(
                        subtitlePath,
                        state.SubtitleStream.Language,
                        state.MediaSource.Protocol,
                        CancellationToken.None).GetAwaiter().GetResult();

                    if (!string.IsNullOrEmpty(charenc))
                    {
                        charsetParam = ":charenc=" + charenc;
                    }
                }

                // TODO: Perhaps also use original_size=1920x800 ??
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "subtitles=filename='{0}'{1}{2}",
                    _mediaEncoder.EscapeSubtitleFilterPath(subtitlePath),
                    charsetParam,
                    // fallbackFontParam,
                    setPtsParam);
            }

            var mediaPath = state.MediaPath ?? string.Empty;

            return string.Format(
                CultureInfo.InvariantCulture,
                "subtitles='{0}:si={1}'{2}",
                _mediaEncoder.EscapeSubtitleFilterPath(mediaPath),
                state.InternalSubtitleStreamOffset.ToString(_usCulture),
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

            if (maxrate.HasValue && state.VideoStream != null)
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
            var keyFrameArg = string.Empty;
            if (isEventPlaylist)
            {
                keyFrameArg = string.Format(
                    CultureInfo.InvariantCulture,
                    " -force_key_frames:0 \"expr:gte(t,n_forced*{0})\"",
                    segmentLength);
            }
            else if (startNumber.HasValue)
            {
                keyFrameArg = string.Format(
                    CultureInfo.InvariantCulture,
                    " -force_key_frames:0 \"expr:gte(t,{0}+n_forced*{1})\"",
                    startNumber.Value * segmentLength,
                    segmentLength);
            }

            var framerate = state.VideoStream?.RealFrameRate;
            if (framerate.HasValue)
            {
                // This is to make sure keyframe interval is limited to our segment,
                // as forcing keyframes is not enough.
                // Example: we encoded half of desired length, then codec detected
                // scene cut and inserted a keyframe; next forced keyframe would
                // be created outside of segment, which breaks seeking.
                // -sc_threshold 0 is used to prevent the hardware encoder from post processing to break the set keyframe.
                gopArg = string.Format(
                    CultureInfo.InvariantCulture,
                    " -g:v:0 {0} -keyint_min:v:0 {0} -sc_threshold:v:0 0",
                    Math.Ceiling(segmentLength * framerate.Value));
            }

            // Unable to force key frames using these encoders, set key frames by GOP.
            if (string.Equals(codec, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "h264_amf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc_qsv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc_nvenc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc_amf", StringComparison.OrdinalIgnoreCase))
            {
                args += gopArg;
            }
            else if (string.Equals(codec, "libx264", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(codec, "libx265", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(codec, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(codec, "hevc_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                args += " " + keyFrameArg;
            }
            else
            {
                args += " " + keyFrameArg + gopArg;
            }

            return args;
        }

        /// <summary>
        /// Gets the video bitrate to specify on the command line.
        /// </summary>
        public string GetVideoQualityParam(EncodingJobInfo state, string videoEncoder, EncodingOptions encodingOptions, string defaultPreset)
        {
            var param = string.Empty;

            if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "hevc_vaapi", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase))
            {
                param += " -pix_fmt yuv420p";
            }

            if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase))
            {
                var videoStream = state.VideoStream;
                var isColorDepth10 = IsColorDepth10(state);
                var videoDecoder = GetHardwareAcceleratedVideoDecoder(state, encodingOptions) ?? string.Empty;
                var isNvdecDecoder = videoDecoder.Contains("cuda", StringComparison.OrdinalIgnoreCase);

                if (!isNvdecDecoder)
                {
                    if (isColorDepth10
                        && _mediaEncoder.SupportsHwaccel("opencl")
                        && encodingOptions.EnableTonemapping
                        && !string.IsNullOrEmpty(videoStream.VideoRange)
                        && videoStream.VideoRange.Contains("HDR", StringComparison.OrdinalIgnoreCase))
                    {
                        param += " -pix_fmt nv12";
                    }
                    else
                    {
                        param += " -pix_fmt yuv420p";
                    }
                }
            }

            if (string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase))
            {
                param += " -pix_fmt nv21";
            }

            var isVc1 = state.VideoStream != null &&
                string.Equals(state.VideoStream.Codec, "vc1", StringComparison.OrdinalIgnoreCase);
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
            else if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) // h264 (h264_qsv)
                     || string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase)) // hevc (hevc_qsv)
            {
                string[] valid_h264_qsv = { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast" };

                if (valid_h264_qsv.Contains(encodingOptions.EncoderPreset, StringComparer.OrdinalIgnoreCase))
                {
                    param += " -preset " + encodingOptions.EncoderPreset;
                }
                else
                {
                    param += " -preset 7";
                }

                param += " -look_ahead 0";
            }
            else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase) // h264 (h264_nvenc)
                     || string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase)) // hevc (hevc_nvenc)
            {
                // following preset will be deprecated in ffmpeg 4.4, use p1~p7 instead.
                switch (encodingOptions.EncoderPreset)
                {
                    case "veryslow":

                        param += " -preset slow"; // lossless is only supported on maxwell and newer(2014+)
                        break;

                    case "slow":
                    case "slower":
                        param += " -preset slow";
                        break;

                    case "medium":
                        param += " -preset medium";
                        break;

                    case "fast":
                    case "faster":
                    case "veryfast":
                    case "superfast":
                    case "ultrafast":
                        param += " -preset fast";
                        break;

                    default:
                        param += " -preset default";
                        break;
                }
            }
            else if (string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase) // h264 (h264_amf)
                     || string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase)) // hevc (hevc_amf)
            {
                switch (encodingOptions.EncoderPreset)
                {
                    case "veryslow":
                    case "slow":
                    case "slower":
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

                var videoStream = state.VideoStream;
                var isColorDepth10 = IsColorDepth10(state);

                if (isColorDepth10
                    && _mediaEncoder.SupportsHwaccel("opencl")
                    && encodingOptions.EnableTonemapping
                    && !string.IsNullOrEmpty(videoStream.VideoRange)
                    && videoStream.VideoRange.Contains("HDR", StringComparison.OrdinalIgnoreCase))
                {
                    // Enhance workload when tone mapping with AMF on some APUs
                    param += " -preanalysis true";
                }

                if (string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -header_insertion_mode gop -gops_per_idr 1";
                }
            }
            else if (string.Equals(videoEncoder, "libvpx", StringComparison.OrdinalIgnoreCase)) // webm
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
                param += string.Format(CultureInfo.InvariantCulture, " -speed 16 -quality good -profile:v {0} -slices 8 -crf {1} -qmin {2} -qmax {3}",
                    profileScore.ToString(_usCulture),
                    crf,
                    qmin,
                    qmax);
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
                param += string.Format(CultureInfo.InvariantCulture, " -r {0}", framerate.Value.ToString(_usCulture));
            }

            var targetVideoCodec = state.ActualOutputVideoCodec;
            if (string.Equals(targetVideoCodec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase))
            {
                targetVideoCodec = "hevc";
            }

            var profile = state.GetRequestedProfiles(targetVideoCodec).FirstOrDefault() ?? string.Empty;
            profile = Regex.Replace(profile, @"\s+", string.Empty);

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
                && profile.Contains("constrainedbaseline", StringComparison.OrdinalIgnoreCase))
            {
                profile = "constrained_baseline";
            }

            if (string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase)
                && profile.Contains("constrainedhigh", StringComparison.OrdinalIgnoreCase))
            {
                profile = "constrained_high";
            }

            // Currently hevc_amf only support encoding HEVC Main Profile, otherwise force Main Profile.
            if (string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase)
                && profile.Contains("main10", StringComparison.OrdinalIgnoreCase))
            {
                profile = "main";
            }

            if (!string.IsNullOrEmpty(profile))
            {
                if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase))
                {
                    // not supported by h264_omx
                    param += " -profile:v:0 " + profile;
                }
            }

            var level = state.GetRequestedLevel(targetVideoCodec);

            if (!string.IsNullOrEmpty(level))
            {
                level = NormalizeTranscodingLevel(state, level);

                // libx264, QSV, AMF, VAAPI can adjust the given level to match the output.
                if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
                else if (string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    // hevc_qsv use -level 51 instead of -level 153.
                    if (double.TryParse(level, NumberStyles.Any, _usCulture, out double hevcLevel))
                    {
                        param += " -level " + hevcLevel / 3;
                    }
                }
                else if (string.Equals(videoEncoder, "h264_amf", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "hevc_amf", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
                else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    // level option may cause NVENC to fail.
                    // NVENC cannot adjust the given level, just throw an error.
                }
                else if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase)
                         || !string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
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
                || !state.SupportedVideoCodecs.Contains(videoStream.Codec, StringComparer.OrdinalIgnoreCase))
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
                if (!string.IsNullOrEmpty(videoStream.Profile) && !requestedProfiles.Contains(videoStream.Profile.Replace(" ", ""), StringComparer.OrdinalIgnoreCase))
                {
                    var currentScore = GetVideoProfileScore(videoStream.Profile);
                    var requestedScore = GetVideoProfileScore(requestedProfile);

                    if (currentScore == -1 || currentScore > requestedScore)
                    {
                        return false;
                    }
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
            if (!string.IsNullOrEmpty(level)
                && double.TryParse(level, NumberStyles.Any, _usCulture, out var requestLevel))
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

            return request.EnableAutoStreamCopy;
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
                || !supportedAudioCodecs.Contains(audioStream.Codec, StringComparer.OrdinalIgnoreCase))
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

            // Video bitrate must fall within requested value
            if (request.AudioBitRate.HasValue)
            {
                if (!audioStream.BitRate.HasValue || audioStream.BitRate.Value <= 0)
                {
                    return false;
                }

                if (audioStream.BitRate.Value > request.AudioBitRate.Value)
                {
                    return false;
                }
            }

            return request.EnableAutoStreamCopy;
        }

        public int? GetVideoBitrateParamValue(BaseEncodingJobOptions request, MediaStream videoStream, string outputVideoCodec)
        {
            var bitrate = request.VideoBitRate;

            if (videoStream != null)
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

            return bitrate;
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
            if (string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "vp9", StringComparison.OrdinalIgnoreCase))
            {
                return .6;
            }

            return 1;
        }

        private static int ScaleBitrate(int bitrate, string inputVideoCodec, string outputVideoCodec)
        {
            var inputScaleFactor = GetVideoBitrateScaleFactor(inputVideoCodec);
            var outputScaleFactor = GetVideoBitrateScaleFactor(outputVideoCodec);
            var scaleFactor = outputScaleFactor / inputScaleFactor;

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

        public int? GetAudioBitrateParam(BaseEncodingJobOptions request, MediaStream audioStream)
        {
            return GetAudioBitrateParam(request.AudioBitRate, request.AudioCodec, audioStream);
        }

        public int? GetAudioBitrateParam(int? audioBitRate, string audioCodec, MediaStream audioStream)
        {
            if (audioStream == null)
            {
                return null;
            }

            if (audioBitRate.HasValue && string.IsNullOrEmpty(audioCodec))
            {
                return Math.Min(384000, audioBitRate.Value);
            }

            if (audioBitRate.HasValue && !string.IsNullOrEmpty(audioCodec))
            {
                if (string.Equals(audioCodec, "aac", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "mp3", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "ac3", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "eac3", StringComparison.OrdinalIgnoreCase))
                {
                    if ((audioStream.Channels ?? 0) >= 6)
                    {
                        return Math.Min(640000, audioBitRate.Value);
                    }

                    return Math.Min(384000, audioBitRate.Value);
                }

                if (string.Equals(audioCodec, "flac", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(audioCodec, "alac", StringComparison.OrdinalIgnoreCase))
                {
                    if ((audioStream.Channels ?? 0) >= 6)
                    {
                        return Math.Min(3584000, audioBitRate.Value);
                    }

                    return Math.Min(1536000, audioBitRate.Value);
                }
            }

            // Empty bitrate area is not allow on iOS
            // Default audio bitrate to 128K if it is not being requested
            // https://ffmpeg.org/ffmpeg-codecs.html#toc-Codec-Options
            return 128000;
        }

        public string GetAudioFilterParam(EncodingJobInfo state, EncodingOptions encodingOptions, bool isHls)
        {
            var channels = state.OutputAudioChannels;

            var filters = new List<string>();

            // Boost volume to 200% when downsampling from 6ch to 2ch
            if (channels.HasValue
                && channels.Value <= 2
                && state.AudioStream != null
                && state.AudioStream.Channels.HasValue
                && state.AudioStream.Channels.Value > 5
                && !encodingOptions.DownMixAudioBoost.Equals(1))
            {
                filters.Add("volume=" + encodingOptions.DownMixAudioBoost.ToString(_usCulture));
            }

            var isCopyingTimestamps = state.CopyTimestamps || state.TranscodingType != TranscodingJobType.Progressive;
            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode && !isCopyingTimestamps)
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
                return " -af \"" + string.Join(",", filters) + "\"";
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
            if (audioStream == null)
            {
                return null;
            }

            var request = state.BaseRequest;

            var inputChannels = audioStream?.Channels;

            if (inputChannels <= 0)
            {
                inputChannels = null;
            }

            var codec = outputAudioCodec ?? string.Empty;

            int? transcoderChannelLimit;
            if (codec.IndexOf("wma", StringComparison.OrdinalIgnoreCase) != -1)
            {
                // wmav2 currently only supports two channel output
                transcoderChannelLimit = 2;
            }
            else if (codec.IndexOf("mp3", StringComparison.OrdinalIgnoreCase) != -1)
            {
                // libmp3lame currently only supports two channel output
                transcoderChannelLimit = 2;
            }
            else if (codec.IndexOf("aac", StringComparison.OrdinalIgnoreCase) != -1)
            {
                // aac is able to handle 8ch(7.1 layout)
                transcoderChannelLimit = 8;
            }
            else
            {
                // If we don't have any media info then limit it to 6 to prevent encoding errors due to asking for too many channels
                transcoderChannelLimit = 6;
            }

            var isTranscodingAudio = !IsCopyCodec(codec);

            int? resultChannels = state.GetRequestedAudioChannels(codec);
            if (isTranscodingAudio)
            {
                resultChannels = GetMinValue(request.TranscodingMaxAudioChannels, resultChannels);
            }

            if (inputChannels.HasValue)
            {
                resultChannels = resultChannels.HasValue
                    ? Math.Min(resultChannels.Value, inputChannels.Value)
                    : inputChannels.Value;
            }

            if (isTranscodingAudio && transcoderChannelLimit.HasValue)
            {
                resultChannels = resultChannels.HasValue
                    ? Math.Min(resultChannels.Value, transcoderChannelLimit.Value)
                    : transcoderChannelLimit.Value;
            }

            // Avoid transcoding to audio channels other than 1ch, 2ch, 6ch (5.1 layout) and 8ch (7.1 layout).
            // https://developer.apple.com/documentation/http_live_streaming/hls_authoring_specification_for_apple_devices
            if (isTranscodingAudio
                && state.TranscodingType != TranscodingJobType.Progressive
                && resultChannels.HasValue
                && (resultChannels.Value > 2 && resultChannels.Value < 6 || resultChannels.Value == 7))
            {
                resultChannels = 2;
            }

            return resultChannels;
        }

        private int? GetMinValue(int? val1, int? val2)
        {
            if (!val1.HasValue)
            {
                return val2;
            }

            if (!val2.HasValue)
            {
                return val1;
            }

            return Math.Min(val1.Value, val2.Value);
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
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        /// <value>The fast seek command line parameter.</value>
        public string GetFastSeekCommandLineParameter(BaseEncodingJobOptions request)
        {
            var time = request.StartTimeTicks ?? 0;

            if (time > 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "-ss {0}", _mediaEncoder.GetTimeParameter(time));
            }

            return string.Empty;
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
            if (state.VideoStream == null && state.AudioStream == null)
            {
                return state.IsInputVideo ? "-sn" : string.Empty;
            }

            // We have media info, but we don't know the stream indexes
            if (state.VideoStream != null && state.VideoStream.Index == -1)
            {
                return "-sn";
            }

            // We have media info, but we don't know the stream indexes
            if (state.AudioStream != null && state.AudioStream.Index == -1)
            {
                return state.IsInputVideo ? "-sn" : string.Empty;
            }

            var args = string.Empty;

            if (state.VideoStream != null)
            {
                args += string.Format(
                    CultureInfo.InvariantCulture,
                    "-map 0:{0}",
                    state.VideoStream.Index);
            }
            else
            {
                // No known video stream
                args += "-vn";
            }

            if (state.AudioStream != null)
            {
                args += string.Format(
                    CultureInfo.InvariantCulture,
                    " -map 0:{0}",
                    state.AudioStream.Index);
            }
            else
            {
                args += " -map -0:a";
            }

            var subtitleMethod = state.SubtitleDeliveryMethod;
            if (state.SubtitleStream == null || subtitleMethod == SubtitleDeliveryMethod.Hls)
            {
                args += " -map -0:s";
            }
            else if (subtitleMethod == SubtitleDeliveryMethod.Embed)
            {
                args += string.Format(
                    CultureInfo.InvariantCulture,
                    " -map 0:{0}",
                    state.SubtitleStream.Index);
            }
            else if (state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream)
            {
                args += " -map 1:0 -sn";
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

                if (stream != null)
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

        /// <summary>
        /// Gets the graphical subtitle param.
        /// </summary>
        public string GetGraphicalSubtitleParam(
            EncodingJobInfo state,
            EncodingOptions options,
            string outputVideoCodec)
        {
            outputVideoCodec ??= string.Empty;

            var outputSizeParam = ReadOnlySpan<char>.Empty;
            var request = state.BaseRequest;

            outputSizeParam = GetOutputSizeParamInternal(state, options, outputVideoCodec);

            var videoSizeParam = string.Empty;
            var videoDecoder = GetHardwareAcceleratedVideoDecoder(state, options) ?? string.Empty;
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            var isVaapiDecoder = videoDecoder.IndexOf("vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isVaapiH264Encoder = outputVideoCodec.IndexOf("h264_vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isVaapiHevcEncoder = outputVideoCodec.IndexOf("hevc_vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isQsvH264Encoder = outputVideoCodec.Contains("h264_qsv", StringComparison.OrdinalIgnoreCase);
            var isQsvHevcEncoder = outputVideoCodec.Contains("hevc_qsv", StringComparison.OrdinalIgnoreCase);
            var isNvdecDecoder = videoDecoder.Contains("cuda", StringComparison.OrdinalIgnoreCase);
            var isNvencEncoder = outputVideoCodec.Contains("nvenc", StringComparison.OrdinalIgnoreCase);
            var isTonemappingSupported = IsTonemappingSupported(state, options);
            var isVppTonemappingSupported = IsVppTonemappingSupported(state, options);
            var isTonemappingSupportedOnVaapi = string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase) && isVaapiDecoder && (isVaapiH264Encoder || isVaapiHevcEncoder);
            var isTonemappingSupportedOnQsv = string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase) && isVaapiDecoder && (isQsvH264Encoder || isQsvHevcEncoder);

            // Tonemapping and burn-in graphical subtitles requires overlay_vaapi.
            // But it's still in ffmpeg mailing list. Disable it for now.
            if (isTonemappingSupportedOnVaapi && isTonemappingSupported && !isVppTonemappingSupported)
            {
                return GetOutputSizeParam(state, options, outputVideoCodec);
            }

            // Setup subtitle scaling
            if (state.VideoStream != null && state.VideoStream.Width.HasValue && state.VideoStream.Height.HasValue)
            {
                // Adjust the size of graphical subtitles to fit the video stream.
                var videoStream = state.VideoStream;
                var inputWidth = videoStream?.Width;
                var inputHeight = videoStream?.Height;
                var (width, height) = GetFixedOutputSize(inputWidth, inputHeight, request.Width, request.Height, request.MaxWidth, request.MaxHeight);

                if (width.HasValue && height.HasValue)
                {
                    videoSizeParam = string.Format(
                        CultureInfo.InvariantCulture,
                        "scale={0}x{1}",
                        width.Value,
                        height.Value);
                }

                if (!string.IsNullOrEmpty(videoSizeParam)
                    && !(isTonemappingSupportedOnQsv && isVppTonemappingSupported))
                {
                    // For QSV, feed it into hardware encoder now
                    if (isLinux && (string.Equals(outputVideoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(outputVideoCodec, "hevc_qsv", StringComparison.OrdinalIgnoreCase)))
                    {
                        videoSizeParam += ",hwupload=extra_hw_frames=64";
                    }
                }
            }

            var mapPrefix = state.SubtitleStream.IsExternal ?
                1 :
                0;

            var subtitleStreamIndex = state.SubtitleStream.IsExternal
                ? 0
                : state.SubtitleStream.Index;

            // Setup default filtergraph utilizing FFMpeg overlay() and FFMpeg scale() (see the return of this function for index reference)
            // Always put the scaler before the overlay for better performance
            var retStr = !outputSizeParam.IsEmpty
                ? " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3}[base];[base][sub]overlay\""
                : " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}][sub]overlay\"";

            // When the input may or may not be hardware VAAPI decodable
            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                || string.Equals(outputVideoCodec, "hevc_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                /*
                    [base]: HW scaling video to OutputSize
                    [sub]: SW scaling subtitle to FixedOutputSize
                    [base][sub]: SW overlay
                */
                retStr = !outputSizeParam.IsEmpty
                    ? " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3},hwdownload[base];[base][sub]overlay,format=nv12,hwupload\""
                    : " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]hwdownload[base];[base][sub]overlay,format=nv12,hwupload\"";
            }

            // If we're hardware VAAPI decoding and software encoding, download frames from the decoder first
            else if (_mediaEncoder.SupportsHwaccel("vaapi") && videoDecoder.IndexOf("vaapi", StringComparison.OrdinalIgnoreCase) != -1
                         && (string.Equals(outputVideoCodec, "libx264", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(outputVideoCodec, "libx265", StringComparison.OrdinalIgnoreCase)))
            {
                /*
                    [base]: SW scaling video to OutputSize
                    [sub]: SW scaling subtitle to FixedOutputSize
                    [base][sub]: SW overlay
                */
                retStr = !outputSizeParam.IsEmpty
                    ? " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3}[base];[base][sub]overlay\""
                    : " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}][sub]overlay\"";
            }
            else if (string.Equals(outputVideoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(outputVideoCodec, "hevc_qsv", StringComparison.OrdinalIgnoreCase))
            {
                /*
                    QSV in FFMpeg can now setup hardware overlay for transcodes.
                    For software decoding and hardware encoding option, frames must be hwuploaded into hardware
                    with fixed frame size.
                    Currently only supports linux.
                */
                if (isTonemappingSupportedOnQsv && isVppTonemappingSupported)
                {
                    retStr = " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3},hwdownload,format=nv12[base];[base][sub]overlay\"";
                }
                else if (isLinux)
                {
                    retStr = !outputSizeParam.IsEmpty
                        ? " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3}[base];[base][sub]overlay_qsv\""
                        : " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}][sub]overlay_qsv\"";
                }
            }
            else if (isNvdecDecoder && isNvencEncoder)
            {
                retStr = !outputSizeParam.IsEmpty
                    ? " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3}[base];[base][sub]overlay,format=nv12|yuv420p,hwupload_cuda\""
                    : " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}][sub]overlay,format=nv12|yuv420p,hwupload_cuda\"";
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                retStr,
                mapPrefix,
                subtitleStreamIndex,
                state.VideoStream.Index,
                outputSizeParam.ToString(),
                videoSizeParam);
        }

        public static (int? width, int? height) GetFixedOutputSize(
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

            decimal inputWidth = Convert.ToDecimal(videoWidth ?? requestedWidth);
            decimal inputHeight = Convert.ToDecimal(videoHeight ?? requestedHeight);
            decimal outputWidth = requestedWidth.HasValue ? Convert.ToDecimal(requestedWidth.Value) : inputWidth;
            decimal outputHeight = requestedHeight.HasValue ? Convert.ToDecimal(requestedHeight.Value) : inputHeight;
            decimal maximumWidth = requestedMaxWidth.HasValue ? Convert.ToDecimal(requestedMaxWidth.Value) : outputWidth;
            decimal maximumHeight = requestedMaxHeight.HasValue ? Convert.ToDecimal(requestedMaxHeight.Value) : outputHeight;

            if (outputWidth > maximumWidth || outputHeight > maximumHeight)
            {
                var scale = Math.Min(maximumWidth / outputWidth, maximumHeight / outputHeight);
                outputWidth = Math.Min(maximumWidth, Math.Truncate(outputWidth * scale));
                outputHeight = Math.Min(maximumHeight, Math.Truncate(outputHeight * scale));
            }

            outputWidth = 2 * Math.Truncate(outputWidth / 2);
            outputHeight = 2 * Math.Truncate(outputHeight / 2);

            return (Convert.ToInt32(outputWidth), Convert.ToInt32(outputHeight));
        }

        public List<string> GetScalingFilters(
            EncodingJobInfo state,
            EncodingOptions options,
            int? videoWidth,
            int? videoHeight,
            Video3DFormat? threedFormat,
            string videoDecoder,
            string videoEncoder,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight)
        {
            var filters = new List<string>();
            var (width, height) = GetFixedOutputSize(
                videoWidth,
                videoHeight,
                requestedWidth,
                requestedHeight,
                requestedMaxWidth,
                requestedMaxHeight);

            if ((string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "hevc_vaapi", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase))
                && width.HasValue
                && height.HasValue)
            {
                // Given the input dimensions (inputWidth, inputHeight), determine the output dimensions
                // (outputWidth, outputHeight). The user may request precise output dimensions or maximum
                // output dimensions. Output dimensions are guaranteed to be even.
                var outputWidth = width.Value;
                var outputHeight = height.Value;
                var qsv_or_vaapi = string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoEncoder, "hevc_qsv", StringComparison.OrdinalIgnoreCase);
                var isDeintEnabled = state.DeInterlace("h264", true)
                    || state.DeInterlace("avc", true)
                    || state.DeInterlace("h265", true)
                    || state.DeInterlace("hevc", true);

                var isVaapiDecoder = videoDecoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase);
                var isVaapiH264Encoder = videoEncoder.Contains("h264_vaapi", StringComparison.OrdinalIgnoreCase);
                var isVaapiHevcEncoder = videoEncoder.Contains("hevc_vaapi", StringComparison.OrdinalIgnoreCase);
                var isQsvH264Encoder = videoEncoder.Contains("h264_qsv", StringComparison.OrdinalIgnoreCase);
                var isQsvHevcEncoder = videoEncoder.Contains("hevc_qsv", StringComparison.OrdinalIgnoreCase);
                var isTonemappingSupported = IsTonemappingSupported(state, options);
                var isVppTonemappingSupported = IsVppTonemappingSupported(state, options);
                var isTonemappingSupportedOnVaapi = string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase)&& isVaapiDecoder && (isVaapiH264Encoder || isVaapiHevcEncoder);
                var isTonemappingSupportedOnQsv = string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase) && isVaapiDecoder && (isQsvH264Encoder || isQsvHevcEncoder);
                var isP010PixFmtRequired = (isTonemappingSupportedOnVaapi && (isTonemappingSupported || isVppTonemappingSupported))
                    || (isTonemappingSupportedOnQsv && isVppTonemappingSupported);


                var outputPixFmt = "format=nv12";
                if (isP010PixFmtRequired)
                {
                    outputPixFmt = "format=p010";
                }

                if (isTonemappingSupportedOnQsv && isVppTonemappingSupported)
                {
                    qsv_or_vaapi = false;
                }

                if (!videoWidth.HasValue
                    || outputWidth != videoWidth.Value
                    || !videoHeight.HasValue
                    || outputHeight != videoHeight.Value)
                {
                    // Force nv12 pixel format to enable 10-bit to 8-bit colour conversion.
                    // use vpp_qsv filter to avoid green bar when the fixed output size is requested.
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}=w={1}:h={2}{3}{4}",
                            qsv_or_vaapi ? "vpp_qsv" : "scale_vaapi",
                            outputWidth,
                            outputHeight,
                            ":" + outputPixFmt,
                            (qsv_or_vaapi && isDeintEnabled) ? ":deinterlace=1" : string.Empty));
                }

                // Assert 10-bit is P010 so as we can avoid the extra scaler to get a bit more fps on high res HDR videos.
                else if (!isP010PixFmtRequired)
                {
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}={1}{2}",
                            qsv_or_vaapi ? "vpp_qsv" : "scale_vaapi",
                            outputPixFmt,
                            (qsv_or_vaapi && isDeintEnabled) ? ":deinterlace=1" : string.Empty));
                }
            }
            else if ((videoDecoder ?? string.Empty).Contains("cuda", StringComparison.OrdinalIgnoreCase)
                     && width.HasValue
                     && height.HasValue)
            {
                var outputWidth = width.Value;
                var outputHeight = height.Value;

                var isTonemappingSupported = IsTonemappingSupported(state, options);
                var isTonemappingSupportedOnNvenc = string.Equals(options.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase);
                var isCudaFormatConversionSupported = _mediaEncoder.SupportsFilter("scale_cuda", "Output format (default \"same\")");

                var outputPixFmt = string.Empty;
                if (isCudaFormatConversionSupported)
                {
                    outputPixFmt = "format=nv12";
                    if (isTonemappingSupported && isTonemappingSupportedOnNvenc)
                    {
                        outputPixFmt = "format=p010";
                    }
                }

                if (!videoWidth.HasValue
                    || outputWidth != videoWidth.Value
                    || !videoHeight.HasValue
                    || outputHeight != videoHeight.Value)
                {
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "scale_cuda=w={0}:h={1}{2}",
                            outputWidth,
                            outputHeight,
                            isCudaFormatConversionSupported ? (":" + outputPixFmt) : string.Empty));
                }
                else if (isCudaFormatConversionSupported)
                {
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "scale_cuda={0}",
                            outputPixFmt));
                }
            }
            else if ((videoDecoder ?? string.Empty).IndexOf("cuvid", StringComparison.OrdinalIgnoreCase) != -1
                && width.HasValue
                && height.HasValue)
            {
                // Nothing to do, it's handled as an input resize filter
            }
            else
            {
                var isExynosV4L2 = string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase);

                // If fixed dimensions were supplied
                if (requestedWidth.HasValue && requestedHeight.HasValue)
                {
                    if (isExynosV4L2)
                    {
                        var widthParam = requestedWidth.Value.ToString(_usCulture);
                        var heightParam = requestedHeight.Value.ToString(_usCulture);

                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc({0}/64)*64:trunc({1}/2)*2",
                                widthParam,
                                heightParam));
                    }
                    else
                    {
                        filters.Add(GetFixedSizeScalingFilter(threedFormat, requestedWidth.Value, requestedHeight.Value));
                    }
                }

                // If Max dimensions were supplied, for width selects lowest even number between input width and width req size and selects lowest even number from in width*display aspect and requested size
                else if (requestedMaxWidth.HasValue && requestedMaxHeight.HasValue)
                {
                    var maxWidthParam = requestedMaxWidth.Value.ToString(_usCulture);
                    var maxHeightParam = requestedMaxHeight.Value.ToString(_usCulture);

                    if (isExynosV4L2)
                    {
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc(min(max(iw\\,ih*dar)\\,min({0}\\,{1}*dar))/64)*64:trunc(min(max(iw/dar\\,ih)\\,min({0}/dar\\,{1}))/2)*2",
                                maxWidthParam,
                                maxHeightParam));
                    }
                    else
                    {
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc(min(max(iw\\,ih*dar)\\,min({0}\\,{1}*dar))/2)*2:trunc(min(max(iw/dar\\,ih)\\,min({0}/dar\\,{1}))/2)*2",
                                maxWidthParam,
                                maxHeightParam));
                    }
                }

                // If a fixed width was requested
                else if (requestedWidth.HasValue)
                {
                    if (threedFormat.HasValue)
                    {
                        // This method can handle 0 being passed in for the requested height
                        filters.Add(GetFixedSizeScalingFilter(threedFormat, requestedWidth.Value, 0));
                    }
                    else
                    {
                        var widthParam = requestedWidth.Value.ToString(_usCulture);

                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale={0}:trunc(ow/a/2)*2",
                                widthParam));
                    }
                }

                // If a fixed height was requested
                else if (requestedHeight.HasValue)
                {
                    var heightParam = requestedHeight.Value.ToString(_usCulture);

                    if (isExynosV4L2)
                    {
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc(oh*a/64)*64:{0}",
                                heightParam));
                    }
                    else
                    {
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc(oh*a/2)*2:{0}",
                                heightParam));
                    }
                }

                // If a max width was requested
                else if (requestedMaxWidth.HasValue)
                {
                    var maxWidthParam = requestedMaxWidth.Value.ToString(_usCulture);

                    if (isExynosV4L2)
                    {
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc(min(max(iw\\,ih*dar)\\,{0})/64)*64:trunc(ow/dar/2)*2",
                                maxWidthParam));
                    }
                    else
                    {
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc(min(max(iw\\,ih*dar)\\,{0})/2)*2:trunc(ow/dar/2)*2",
                                maxWidthParam));
                    }
                }

                // If a max height was requested
                else if (requestedMaxHeight.HasValue)
                {
                    var maxHeightParam = requestedMaxHeight.Value.ToString(_usCulture);

                    if (isExynosV4L2)
                    {
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc(oh*a/64)*64:min(max(iw/dar\\,ih)\\,{0})",
                                maxHeightParam));
                    }
                    else
                    {
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "scale=trunc(oh*a/2)*2:min(max(iw/dar\\,ih)\\,{0})",
                                maxHeightParam));
                    }
                }
            }

            return filters;
        }

        private string GetFixedSizeScalingFilter(Video3DFormat? threedFormat, int requestedWidth, int requestedHeight)
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
            if (filter == null)
            {
                if (requestedHeight > 0)
                {
                    filter = "scale=trunc({0}/2)*2:trunc({1}/2)*2";
                }
                else
                {
                    filter = "scale={0}:trunc({0}/dar/2)*2";
                }
            }

            return string.Format(CultureInfo.InvariantCulture, filter, widthParam, heightParam);
        }

        public string GetOutputSizeParam(
            EncodingJobInfo state,
            EncodingOptions options,
            string outputVideoCodec)
        {
            string filters = GetOutputSizeParamInternal(state, options, outputVideoCodec);
            return string.IsNullOrEmpty(filters) ? string.Empty : " -vf \"" + filters + "\"";
        }

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it.
        /// </summary>
        public string GetOutputSizeParamInternal(
            EncodingJobInfo state,
            EncodingOptions options,
            string outputVideoCodec)
        {
            // http://sonnati.wordpress.com/2012/10/19/ffmpeg-the-swiss-army-knife-of-internet-streaming-part-vi/

            var request = state.BaseRequest;
            var videoStream = state.VideoStream;
            var filters = new List<string>();

            var videoDecoder = GetHardwareAcceleratedVideoDecoder(state, options) ?? string.Empty;
            var inputWidth = videoStream?.Width;
            var inputHeight = videoStream?.Height;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var isSwDecoder = string.IsNullOrEmpty(videoDecoder);
            var isD3d11vaDecoder = videoDecoder.IndexOf("d3d11va", StringComparison.OrdinalIgnoreCase) != -1;
            var isVaapiDecoder = videoDecoder.IndexOf("vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isVaapiEncoder = outputVideoCodec.IndexOf("vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isVaapiH264Encoder = outputVideoCodec.IndexOf("h264_vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isVaapiHevcEncoder = outputVideoCodec.IndexOf("hevc_vaapi", StringComparison.OrdinalIgnoreCase) != -1;
            var isQsvH264Encoder = outputVideoCodec.IndexOf("h264_qsv", StringComparison.OrdinalIgnoreCase) != -1;
            var isQsvHevcEncoder = outputVideoCodec.IndexOf("hevc_qsv", StringComparison.OrdinalIgnoreCase) != -1;
            var isNvdecDecoder = videoDecoder.Contains("cuda", StringComparison.OrdinalIgnoreCase);
            var isNvencEncoder = outputVideoCodec.Contains("nvenc", StringComparison.OrdinalIgnoreCase);
            var isCuvidH264Decoder = videoDecoder.Contains("h264_cuvid", StringComparison.OrdinalIgnoreCase);
            var isCuvidHevcDecoder = videoDecoder.Contains("hevc_cuvid", StringComparison.OrdinalIgnoreCase);
            var isLibX264Encoder = outputVideoCodec.IndexOf("libx264", StringComparison.OrdinalIgnoreCase) != -1;
            var isLibX265Encoder = outputVideoCodec.IndexOf("libx265", StringComparison.OrdinalIgnoreCase) != -1;
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            var isColorDepth10 = IsColorDepth10(state);
            var isTonemappingSupported = IsTonemappingSupported(state, options);
            var isVppTonemappingSupported = IsVppTonemappingSupported(state, options);
            var isTonemappingSupportedOnNvenc = string.Equals(options.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase) && (isNvdecDecoder || isCuvidHevcDecoder || isSwDecoder);
            var isTonemappingSupportedOnAmf = string.Equals(options.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase) && (isD3d11vaDecoder || isSwDecoder);
            var isTonemappingSupportedOnVaapi = string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase) && isVaapiDecoder && (isVaapiH264Encoder || isVaapiHevcEncoder);
            var isTonemappingSupportedOnQsv = string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase) && isVaapiDecoder && (isQsvH264Encoder || isQsvHevcEncoder);

            var hasSubs = state.SubtitleStream != null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasTextSubs = state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

            // If double rate deinterlacing is enabled and the input framerate is 30fps or below, otherwise the output framerate will be too high for many devices
            var doubleRateDeinterlace = options.DeinterlaceDoubleRate && (videoStream?.AverageFrameRate ?? 60) <= 30;

            var isScalingInAdvance = false;
            var isCudaDeintInAdvance = false;
            var isHwuploadCudaRequired = false;
            var isDeinterlaceH264 = state.DeInterlace("h264", true) || state.DeInterlace("avc", true);
            var isDeinterlaceHevc = state.DeInterlace("h265", true) || state.DeInterlace("hevc", true);

            // Add OpenCL tonemapping filter for NVENC/AMF/VAAPI.
            if (isTonemappingSupportedOnNvenc || isTonemappingSupportedOnAmf || (isTonemappingSupportedOnVaapi && !isVppTonemappingSupported))
            {
                // Currently only with the use of NVENC decoder can we get a decent performance.
                // Currently only the HEVC/H265 format is supported with NVDEC decoder.
                // NVIDIA Pascal and Turing or higher are recommended.
                // AMD Polaris and Vega or higher are recommended.
                // Intel Kaby Lake or newer is required.
                if (isTonemappingSupported)
                {
                    var parameters = "tonemap_opencl=format=nv12:primaries=bt709:transfer=bt709:matrix=bt709:tonemap={0}:desat={1}:threshold={2}:peak={3}";

                    if (options.TonemappingParam != 0)
                    {
                        parameters += ":param={4}";
                    }

                    if (!string.Equals(options.TonemappingRange, "auto", StringComparison.OrdinalIgnoreCase))
                    {
                        parameters += ":range={5}";
                    }

                    if (isSwDecoder || isD3d11vaDecoder)
                    {
                        isScalingInAdvance = true;
                        // Add zscale filter before tone mapping filter for performance.
                        var (width, height) = GetFixedOutputSize(inputWidth, inputHeight, request.Width, request.Height, request.MaxWidth, request.MaxHeight);
                        if (width.HasValue && height.HasValue)
                        {
                            filters.Add(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "zscale=s={0}x{1}",
                                    width.Value,
                                    height.Value));
                        }

                        // Convert to hardware pixel format p010 when using SW decoder.
                        filters.Add("format=p010");
                    }

                    if ((isDeinterlaceH264 || isDeinterlaceHevc) && isNvdecDecoder)
                    {
                        isCudaDeintInAdvance = true;
                        filters.Add(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "yadif_cuda={0}:-1:0",
                                doubleRateDeinterlace ? "1" : "0"));
                    }

                    if (isVaapiDecoder || isNvdecDecoder)
                    {
                        isScalingInAdvance = true;
                        filters.AddRange(
                            GetScalingFilters(
                                state,
                                options,
                                inputWidth,
                                inputHeight,
                                threeDFormat,
                                videoDecoder,
                                outputVideoCodec,
                                request.Width,
                                request.Height,
                                request.MaxWidth,
                                request.MaxHeight));
                    }

                    // hwmap the HDR data to opencl device by cl-va p010 interop.
                    if (isVaapiDecoder)
                    {
                        filters.Add("hwmap");
                    }

                    // convert cuda device data to p010 host data.
                    if (isNvdecDecoder)
                    {
                        filters.Add("hwdownload,format=p010");
                    }

                    if (isNvdecDecoder || isCuvidHevcDecoder || isSwDecoder || isD3d11vaDecoder)
                    {
                        // Upload the HDR10 or HLG data to the OpenCL device,
                        // use tonemap_opencl filter for tone mapping,
                        // and then download the SDR data to memory.
                        filters.Add("hwupload");
                    }

                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            parameters,
                            options.TonemappingAlgorithm,
                            options.TonemappingDesat,
                            options.TonemappingThreshold,
                            options.TonemappingPeak,
                            options.TonemappingParam,
                            options.TonemappingRange));

                    if (isNvdecDecoder || isCuvidHevcDecoder || isSwDecoder || isD3d11vaDecoder)
                    {
                        filters.Add("hwdownload");
                        filters.Add("format=nv12");
                    }

                    if (isNvdecDecoder && isNvencEncoder)
                    {
                        isHwuploadCudaRequired = true;
                    }

                    if (isVaapiDecoder)
                    {
                        // Reverse the data route from opencl to vaapi.
                        filters.Add("hwmap=derive_device=vaapi:reverse=1");
                    }
                }
            }

            // When the input may or may not be hardware VAAPI decodable.
            if ((isVaapiH264Encoder || isVaapiHevcEncoder)
                && !(isTonemappingSupportedOnVaapi && (isTonemappingSupported || isVppTonemappingSupported)))
            {
                filters.Add("format=nv12|vaapi");
                filters.Add("hwupload");
            }

            // When burning in graphical subtitles using overlay_qsv, upload videostream to the same qsv context.
            else if (isLinux && hasGraphicalSubs && (isQsvH264Encoder || isQsvHevcEncoder)
                     && !(isTonemappingSupportedOnQsv && isVppTonemappingSupported))
            {
                filters.Add("hwupload=extra_hw_frames=64");
            }

            // If we're hardware VAAPI decoding and software encoding, download frames from the decoder first.
            else if ((IsVaapiSupported(state) && isVaapiDecoder) && (isLibX264Encoder || isLibX265Encoder)
                     && !(isTonemappingSupportedOnQsv && isVppTonemappingSupported))
            {
                var codec = videoStream.Codec;

                // Assert 10-bit hardware VAAPI decodable
                if (isColorDepth10 && (string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "vp9", StringComparison.OrdinalIgnoreCase)))
                {
                    /*
                        Download data from GPU to CPU as p010le format.
                        Colorspace conversion is unnecessary here as libx264 will handle it.
                        If this step is missing, it will fail on AMD but not on intel.
                    */
                    filters.Add("hwdownload");
                    filters.Add("format=p010le");
                }

                // Assert 8-bit hardware VAAPI decodable
                else if (!isColorDepth10)
                {
                    filters.Add("hwdownload");
                    filters.Add("format=nv12");
                }
            }

            // Add hardware deinterlace filter before scaling filter.
            if (isDeinterlaceH264 || isDeinterlaceHevc)
            {
                if (isVaapiEncoder
                    || (isTonemappingSupportedOnQsv && isVppTonemappingSupported))
                {
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "deinterlace_vaapi=rate={0}",
                            doubleRateDeinterlace ? "field" : "frame"));
                }
                else if (isNvdecDecoder && !isCudaDeintInAdvance)
                {
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "yadif_cuda={0}:-1:0",
                            doubleRateDeinterlace ? "1" : "0"));
                }
            }

            // Add software deinterlace filter before scaling filter.
            if ((isDeinterlaceH264 || isDeinterlaceHevc)
                && !isVaapiH264Encoder
                && !isVaapiHevcEncoder
                && !isQsvH264Encoder
                && !isQsvHevcEncoder
                && !isNvdecDecoder
                && !isCuvidH264Decoder)
            {
                if (string.Equals(options.DeinterlaceMethod, "bwdif", StringComparison.OrdinalIgnoreCase))
                {
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "bwdif={0}:-1:0",
                            doubleRateDeinterlace ? "1" : "0"));
                }
                else
                {
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "yadif={0}:-1:0",
                            doubleRateDeinterlace ? "1" : "0"));
                }
            }

            // Add scaling filter: scale_*=format=nv12 or scale_*=w=*:h=*:format=nv12 or scale=expr
            if (!isScalingInAdvance)
            {
                filters.AddRange(
                    GetScalingFilters(
                        state,
                        options,
                        inputWidth,
                        inputHeight,
                        threeDFormat,
                        videoDecoder,
                        outputVideoCodec,
                        request.Width,
                        request.Height,
                        request.MaxWidth,
                        request.MaxHeight));
            }

            // Add VPP tonemapping filter for VAAPI.
            // Full hardware based video post processing, faster than OpenCL but lacks fine tuning options.
            if ((isTonemappingSupportedOnVaapi || isTonemappingSupportedOnQsv)
                && isVppTonemappingSupported)
            {
                filters.Add("tonemap_vaapi=format=nv12:transfer=bt709:matrix=bt709:primaries=bt709");
            }

            // Another case is when using Nvenc decoder.
            if (isNvdecDecoder && !isTonemappingSupported)
            {
                var codec = videoStream.Codec;
                var isCudaFormatConversionSupported = _mediaEncoder.SupportsFilter("scale_cuda", "Output format (default \"same\")");

                // Assert 10-bit hardware decodable
                if (isColorDepth10 && (string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "vp9", StringComparison.OrdinalIgnoreCase)))
                {
                    if (isCudaFormatConversionSupported)
                    {
                        if (isLibX264Encoder || isLibX265Encoder || hasSubs)
                        {
                            if (isNvencEncoder)
                            {
                                isHwuploadCudaRequired = true;
                            }

                            filters.Add("hwdownload");
                            filters.Add("format=nv12");
                        }
                    }
                    else
                    {
                        // Download data from GPU to CPU as p010 format.
                        filters.Add("hwdownload");
                        filters.Add("format=p010");

                        // Cuda lacks of a pixel format converter.
                        if (isNvencEncoder)
                        {
                            isHwuploadCudaRequired = true;
                            filters.Add("format=yuv420p");
                        }
                    }
                }

                // Assert 8-bit hardware decodable
                else if (!isColorDepth10 && (isLibX264Encoder || isLibX265Encoder || hasSubs))
                {
                    if (isNvencEncoder)
                    {
                        isHwuploadCudaRequired = true;
                    }

                    filters.Add("hwdownload");
                    filters.Add("format=nv12");
                }
            }

            // Add parameters to use VAAPI with burn-in text subtitles (GH issue #642)
            if (isVaapiH264Encoder
                || isVaapiHevcEncoder
                || (isTonemappingSupportedOnQsv && isVppTonemappingSupported))
            {
                if (hasTextSubs)
                {
                    // Convert hw context from ocl to va.
                    // For tonemapping and text subs burn-in.
                    if (isTonemappingSupportedOnVaapi && isTonemappingSupported && !isVppTonemappingSupported)
                    {
                        filters.Add("scale_vaapi");
                    }

                    // Test passed on Intel and AMD gfx
                    filters.Add("hwmap=mode=read+write");
                    filters.Add("format=nv12");
                }
            }

            if (hasTextSubs)
            {
                var subParam = GetTextSubtitleParam(state);

                filters.Add(subParam);

                // Ensure proper filters are passed to ffmpeg in case of hardware acceleration via VA-API
                // Reference: https://trac.ffmpeg.org/wiki/Hardware/VAAPI
                if (isVaapiH264Encoder || isVaapiHevcEncoder)
                {
                    filters.Add("hwmap");
                }

                if (isTonemappingSupportedOnQsv && isVppTonemappingSupported)
                {
                    filters.Add("hwmap,format=vaapi");
                }

                if (isNvdecDecoder && isNvencEncoder)
                {
                    isHwuploadCudaRequired = true;
                }
            }

            // Interop the VAAPI data to QSV for hybrid tonemapping
            if (isTonemappingSupportedOnQsv && isVppTonemappingSupported && !hasGraphicalSubs)
            {
                filters.Add("hwmap=derive_device=qsv,scale_qsv");
            }

            if (isHwuploadCudaRequired && !hasGraphicalSubs)
            {
                filters.Add("hwupload_cuda");
            }

            var output = string.Empty;
            if (filters.Count > 0)
            {
                output += string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}",
                    string.Join(",", filters));
            }

            return output;
        }

        /// <summary>
        /// Gets the number of threads.
        /// </summary>
#nullable enable
        public static int GetNumberOfThreads(EncodingJobInfo? state, EncodingOptions encodingOptions, string? outputVideoCodec)
        {
            if (string.Equals(outputVideoCodec, "libvpx", StringComparison.OrdinalIgnoreCase))
            {
                // per docs:
                // -threads    number of threads to use for encoding, can't be 0 [auto] with VP8
                //             (recommended value : number of real cores - 1)
                return Math.Max(Environment.ProcessorCount - 1, 1);
            }

            var threads = state?.BaseRequest.CpuCoreLimit ?? encodingOptions.EncodingThreadCount;

            // Automatic
            if (threads <= 0)
            {
                return 0;
            } 
            else if (threads >= Environment.ProcessorCount)
            {
                return Environment.ProcessorCount;
            }

            return threads;
        }
#nullable disable
        public void TryStreamCopy(EncodingJobInfo state)
        {
            if (state.VideoStream != null && CanStreamCopyVideo(state, state.VideoStream))
            {
                state.OutputVideoCodec = "copy";
            }
            else
            {
                var user = state.User;

                // If the user doesn't have access to transcoding, then force stream copy, regardless of whether it will be compatible or not
                if (user != null && !user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding))
                {
                    state.OutputVideoCodec = "copy";
                }
            }

            if (state.AudioStream != null
                && CanStreamCopyAudio(state, state.AudioStream, state.SupportedAudioCodecs))
            {
                state.OutputAudioCodec = "copy";
            }
            else
            {
                var user = state.User;

                // If the user doesn't have access to transcoding, then force stream copy, regardless of whether it will be compatible or not
                if (user != null && !user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding))
                {
                    state.OutputAudioCodec = "copy";
                }
            }
        }

        public string GetInputModifier(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var inputModifier = string.Empty;
            var probeSizeArgument = string.Empty;

            string analyzeDurationArgument;
            if (state.MediaSource.AnalyzeDurationMs.HasValue)
            {
                analyzeDurationArgument = "-analyzeduration " + (state.MediaSource.AnalyzeDurationMs.Value * 1000).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                analyzeDurationArgument = string.Empty;
            }

            if (!string.IsNullOrEmpty(probeSizeArgument))
            {
                inputModifier += " " + probeSizeArgument;
            }

            if (!string.IsNullOrEmpty(analyzeDurationArgument))
            {
                inputModifier += " " + analyzeDurationArgument;
            }

            inputModifier = inputModifier.Trim();

            var userAgentParam = GetUserAgentParam(state);

            if (!string.IsNullOrEmpty(userAgentParam))
            {
                inputModifier += " " + userAgentParam;
            }

            inputModifier = inputModifier.Trim();

            inputModifier += " " + GetFastSeekCommandLineParameter(state.BaseRequest);
            inputModifier = inputModifier.Trim();

            if (state.InputProtocol == MediaProtocol.Rtsp)
            {
                inputModifier += " -rtsp_transport tcp -rtsp_transport udp -rtsp_flags prefer_tcp";
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

            var videoDecoder = GetHardwareAcceleratedVideoDecoder(state, encodingOptions);

            if (!string.IsNullOrEmpty(videoDecoder))
            {
                inputModifier += " " + videoDecoder;

                if (!IsCopyCodec(state.OutputVideoCodec)
                    && (videoDecoder ?? string.Empty).IndexOf("cuvid", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    var videoStream = state.VideoStream;
                    var inputWidth = videoStream?.Width;
                    var inputHeight = videoStream?.Height;
                    var request = state.BaseRequest;

                    var (width, height) = GetFixedOutputSize(inputWidth, inputHeight, request.Width, request.Height, request.MaxWidth, request.MaxHeight);

                    if ((videoDecoder ?? string.Empty).IndexOf("cuvid", StringComparison.OrdinalIgnoreCase) != -1
                        && width.HasValue
                        && height.HasValue)
                    {
                        if (width.HasValue && height.HasValue)
                        {
                            inputModifier += string.Format(
                                CultureInfo.InvariantCulture,
                                " -resize {0}x{1}",
                                width.Value,
                                height.Value);
                        }

                        if (state.DeInterlace("h264", true))
                        {
                            inputModifier += " -deint 1";

                            if (!encodingOptions.DeinterlaceDoubleRate || (videoStream?.AverageFrameRate ?? 60) > 30)
                            {
                                inputModifier += " -drop_second_field 1";
                            }
                        }
                    }
                }
            }

            if (state.IsVideoRequest)
            {
                var outputVideoCodec = GetVideoEncoder(state, encodingOptions);

                // Important: If this is ever re-enabled, make sure not to use it with wtv because it breaks seeking
                if (!string.Equals(state.InputContainer, "wtv", StringComparison.OrdinalIgnoreCase)
                    && state.TranscodingType != TranscodingJobType.Progressive
                    && !state.EnableBreakOnNonKeyFrames(outputVideoCodec)
                    && (state.BaseRequest.StartTimeTicks ?? 0) > 0)
                {
                    inputModifier += " -noaccurate_seek";
                }

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
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (mediaSource == null)
            {
                throw new ArgumentNullException(nameof(mediaSource));
            }

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
                || mediaSource.Protocol == MediaProtocol.File
                && string.Equals(mediaSource.Container, "wtv", StringComparison.OrdinalIgnoreCase))
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

                if (state.SubtitleStream != null && !state.SubtitleStream.IsExternal)
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
            if (request != null && supportedAudioCodecs != null && supportedAudioCodecs.Length > 0)
            {
                var supportedAudioCodecsList = supportedAudioCodecs.ToList();

                ShiftAudioCodecsIfNeeded(supportedAudioCodecsList, state.AudioStream);

                state.SupportedAudioCodecs = supportedAudioCodecsList.ToArray();

                request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(i => _mediaEncoder.CanEncodeToAudioCodec(i))
                    ?? state.SupportedAudioCodecs.FirstOrDefault();
            }

            var supportedVideoCodecs = state.SupportedVideoCodecs;
            if (request != null && supportedVideoCodecs != null && supportedVideoCodecs.Length > 0)
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

            var inputChannels = audioStream == null ? 6 : audioStream.Channels ?? 6;
            if (inputChannels >= 6)
            {
                return;
            }

            // Transcoding to 2ch ac3 almost always causes a playback failure
            // Keep it in the supported codecs list, but shift it to the end of the list so that if transcoding happens, another codec is used
            var shiftAudioCodecs = new[] { "ac3", "eac3" };
            if (audioCodecs.All(i => shiftAudioCodecs.Contains(i, StringComparer.OrdinalIgnoreCase)))
            {
                return;
            }

            while (shiftAudioCodecs.Contains(audioCodecs[0], StringComparer.OrdinalIgnoreCase))
            {
                var removed = shiftAudioCodecs[0];
                audioCodecs.RemoveAt(0);
                audioCodecs.Add(removed);
            }
        }

        private void ShiftVideoCodecsIfNeeded(List<string> videoCodecs, EncodingOptions encodingOptions)
        {
            // Shift hevc/h265 to the end of list if hevc encoding is not allowed.
            if (encodingOptions.AllowHevcEncoding)
            {
                return;
            }

            // No need to shift if there is only one supported video codec.
            if (videoCodecs.Count < 2)
            {
                return;
            }

            var shiftVideoCodecs = new[] { "hevc", "h265" };
            if (videoCodecs.All(i => shiftVideoCodecs.Contains(i, StringComparer.OrdinalIgnoreCase)))
            {
                return;
            }

            while (shiftVideoCodecs.Contains(videoCodecs[0], StringComparer.OrdinalIgnoreCase))
            {
                var removed = shiftVideoCodecs[0];
                videoCodecs.RemoveAt(0);
                videoCodecs.Add(removed);
            }
        }

        private void NormalizeSubtitleEmbed(EncodingJobInfo state)
        {
            if (state.SubtitleStream == null || state.SubtitleDeliveryMethod != SubtitleDeliveryMethod.Embed)
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

        /// <summary>
        /// Gets the ffmpeg option string for the hardware accelerated video decoder.
        /// </summary>
        /// <param name="state">The encoding job info.</param>
        /// <param name="encodingOptions">The encoding options.</param>
        /// <returns>The option string or null if none available.</returns>
        protected string GetHardwareAcceleratedVideoDecoder(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var videoStream = state.VideoStream;

            if (videoStream == null)
            {
                return null;
            }

            var videoType = state.MediaSource.VideoType ?? VideoType.VideoFile;
            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is experimental anyway, it's not worth adding additional variables such as this.
            if (videoType != VideoType.VideoFile)
            {
                return null;
            }

            if (IsCopyCodec(state.OutputVideoCodec))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(videoStream.Codec) && !string.IsNullOrEmpty(encodingOptions.HardwareAccelerationType))
            {
                var isColorDepth10 = IsColorDepth10(state);

                // Only hevc and vp9 formats have 10-bit hardware decoder support now.
                if (isColorDepth10 && !(string.Equals(videoStream.Codec, "hevc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.Codec, "h265", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoStream.Codec, "vp9", StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                // Hybrid VPP tonemapping with VAAPI
                if (string.Equals(encodingOptions.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase)
                    && IsVppTonemappingSupported(state, encodingOptions))
                {
                    // Since tonemap_vaapi only support HEVC for now, no need to check the codec again.
                    return GetHwaccelType(state, encodingOptions, "hevc", isColorDepth10);
                }

                if (string.Equals(encodingOptions.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            return GetHwDecoderName(encodingOptions, "h264_qsv", "h264", isColorDepth10);
                        case "hevc":
                        case "h265":
                            return GetHwDecoderName(encodingOptions, "hevc_qsv", "hevc", isColorDepth10);
                        case "mpeg2video":
                            return GetHwDecoderName(encodingOptions, "mpeg2_qsv", "mpeg2video", isColorDepth10);
                        case "vc1":
                            return GetHwDecoderName(encodingOptions, "vc1_qsv", "vc1", isColorDepth10);
                        case "vp8":
                            return GetHwDecoderName(encodingOptions, "vp8_qsv", "vp8", isColorDepth10);
                        case "vp9":
                            return GetHwDecoderName(encodingOptions, "vp9_qsv", "vp9", isColorDepth10);
                    }
                }
                else if (string.Equals(encodingOptions.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            return encodingOptions.EnableEnhancedNvdecDecoder && IsCudaSupported()
                                ? GetHwaccelType(state, encodingOptions, "h264", isColorDepth10)
                                : GetHwDecoderName(encodingOptions, "h264_cuvid", "h264", isColorDepth10);
                        case "hevc":
                        case "h265":
                            return encodingOptions.EnableEnhancedNvdecDecoder && IsCudaSupported()
                                ? GetHwaccelType(state, encodingOptions, "hevc", isColorDepth10)
                                : GetHwDecoderName(encodingOptions, "hevc_cuvid", "hevc", isColorDepth10);
                        case "mpeg2video":
                            return encodingOptions.EnableEnhancedNvdecDecoder && IsCudaSupported()
                                ? GetHwaccelType(state, encodingOptions, "mpeg2video", isColorDepth10)
                                : GetHwDecoderName(encodingOptions, "mpeg2_cuvid", "mpeg2video", isColorDepth10);
                        case "vc1":
                            return encodingOptions.EnableEnhancedNvdecDecoder && IsCudaSupported()
                                ? GetHwaccelType(state, encodingOptions, "vc1", isColorDepth10)
                                : GetHwDecoderName(encodingOptions, "vc1_cuvid", "vc1", isColorDepth10);
                        case "mpeg4":
                            return encodingOptions.EnableEnhancedNvdecDecoder && IsCudaSupported()
                                ? GetHwaccelType(state, encodingOptions, "mpeg4", isColorDepth10)
                                : GetHwDecoderName(encodingOptions, "mpeg4_cuvid", "mpeg4", isColorDepth10);
                        case "vp8":
                            return encodingOptions.EnableEnhancedNvdecDecoder && IsCudaSupported()
                                ? GetHwaccelType(state, encodingOptions, "vp8", isColorDepth10)
                                : GetHwDecoderName(encodingOptions, "vp8_cuvid", "vp8", isColorDepth10);
                        case "vp9":
                            return encodingOptions.EnableEnhancedNvdecDecoder && IsCudaSupported()
                                ? GetHwaccelType(state, encodingOptions, "vp9", isColorDepth10)
                                : GetHwDecoderName(encodingOptions, "vp9_cuvid", "vp9", isColorDepth10);
                    }
                }
                else if (string.Equals(encodingOptions.HardwareAccelerationType, "mediacodec", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            return GetHwDecoderName(encodingOptions, "h264_mediacodec", "h264", isColorDepth10);
                        case "hevc":
                        case "h265":
                            return GetHwDecoderName(encodingOptions, "hevc_mediacodec", "hevc", isColorDepth10);
                        case "mpeg2video":
                            return GetHwDecoderName(encodingOptions, "mpeg2_mediacodec", "mpeg2video", isColorDepth10);
                        case "mpeg4":
                            return GetHwDecoderName(encodingOptions, "mpeg4_mediacodec", "mpeg4", isColorDepth10);
                        case "vp8":
                            return GetHwDecoderName(encodingOptions, "vp8_mediacodec", "vp8", isColorDepth10);
                        case "vp9":
                            return GetHwDecoderName(encodingOptions, "vp9_mediacodec", "vp9", isColorDepth10);
                    }
                }
                else if (string.Equals(encodingOptions.HardwareAccelerationType, "omx", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            return GetHwDecoderName(encodingOptions, "h264_mmal", "h264", isColorDepth10);
                        case "mpeg2video":
                            return GetHwDecoderName(encodingOptions, "mpeg2_mmal", "mpeg2video", isColorDepth10);
                        case "mpeg4":
                            return GetHwDecoderName(encodingOptions, "mpeg4_mmal", "mpeg4", isColorDepth10);
                        case "vc1":
                            return GetHwDecoderName(encodingOptions, "vc1_mmal", "vc1", isColorDepth10);
                    }
                }
                else if (string.Equals(encodingOptions.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            return GetHwaccelType(state, encodingOptions, "h264", isColorDepth10);
                        case "hevc":
                        case "h265":
                            return GetHwaccelType(state, encodingOptions, "hevc", isColorDepth10);
                        case "mpeg2video":
                            return GetHwaccelType(state, encodingOptions, "mpeg2video", isColorDepth10);
                        case "vc1":
                            return GetHwaccelType(state, encodingOptions, "vc1", isColorDepth10);
                        case "mpeg4":
                            return GetHwaccelType(state, encodingOptions, "mpeg4", isColorDepth10);
                        case "vp9":
                            return GetHwaccelType(state, encodingOptions, "vp9", isColorDepth10);
                    }
                }
                else if (string.Equals(encodingOptions.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            return GetHwaccelType(state, encodingOptions, "h264", isColorDepth10);
                        case "hevc":
                        case "h265":
                            return GetHwaccelType(state, encodingOptions, "hevc", isColorDepth10);
                        case "mpeg2video":
                            return GetHwaccelType(state, encodingOptions, "mpeg2video", isColorDepth10);
                        case "vc1":
                            return GetHwaccelType(state, encodingOptions, "vc1", isColorDepth10);
                        case "vp8":
                            return GetHwaccelType(state, encodingOptions, "vp8", isColorDepth10);
                        case "vp9":
                            return GetHwaccelType(state, encodingOptions, "vp9", isColorDepth10);
                    }
                }
                else if (string.Equals(encodingOptions.HardwareAccelerationType, "videotoolbox", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            return GetHwDecoderName(encodingOptions, "h264_opencl", "h264", isColorDepth10);
                        case "hevc":
                        case "h265":
                            return GetHwDecoderName(encodingOptions, "hevc_opencl", "hevc", isColorDepth10);
                        case "mpeg2video":
                            return GetHwDecoderName(encodingOptions, "mpeg2_opencl", "mpeg2video", isColorDepth10);
                        case "mpeg4":
                            return GetHwDecoderName(encodingOptions, "mpeg4_opencl", "mpeg4", isColorDepth10);
                        case "vc1":
                            return GetHwDecoderName(encodingOptions, "vc1_opencl", "vc1", isColorDepth10);
                        case "vp8":
                            return GetHwDecoderName(encodingOptions, "vp8_opencl", "vp8", isColorDepth10);
                        case "vp9":
                            return GetHwDecoderName(encodingOptions, "vp9_opencl", "vp9", isColorDepth10);
                    }
                }
            }

            var whichCodec = videoStream.Codec?.ToLowerInvariant();
            switch (whichCodec)
            {
                case "avc":
                    whichCodec = "h264";
                    break;
                case "h265":
                    whichCodec = "hevc";
                    break;
            }

            // Avoid a second attempt if no hardware acceleration is being used
            encodingOptions.HardwareDecodingCodecs = encodingOptions.HardwareDecodingCodecs.Where(val => val != whichCodec).ToArray();

            // leave blank so ffmpeg will decide
            return null;
        }

        /// <summary>
        /// Gets a hw decoder name
        /// </summary>
        public string GetHwDecoderName(EncodingOptions options, string decoder, string videoCodec, bool isColorDepth10)
        {
            var isCodecAvailable = _mediaEncoder.SupportsDecoder(decoder) && options.HardwareDecodingCodecs.Contains(videoCodec, StringComparer.OrdinalIgnoreCase);
            if (isColorDepth10 && isCodecAvailable)
            {
                if ((options.HardwareDecodingCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase) && !options.EnableDecodingColorDepth10Hevc)
                    || (options.HardwareDecodingCodecs.Contains("vp9", StringComparer.OrdinalIgnoreCase) && !options.EnableDecodingColorDepth10Vp9))
                {
                    return null;
                }
            }

            return isCodecAvailable ? ("-c:v " + decoder) : null;
        }

        /// <summary>
        /// Gets a hwaccel type to use as a hardware decoder(dxva/vaapi) depending on the system
        /// </summary>
        public string GetHwaccelType(EncodingJobInfo state, EncodingOptions options, string videoCodec, bool isColorDepth10)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            var isWindows8orLater = Environment.OSVersion.Version.Major > 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 1);
            var isDxvaSupported = _mediaEncoder.SupportsHwaccel("dxva2") || _mediaEncoder.SupportsHwaccel("d3d11va");
            var isCodecAvailable = options.HardwareDecodingCodecs.Contains(videoCodec, StringComparer.OrdinalIgnoreCase);

            if (isColorDepth10 && isCodecAvailable)
            {
                if ((options.HardwareDecodingCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase) && !options.EnableDecodingColorDepth10Hevc)
                    || (options.HardwareDecodingCodecs.Contains("vp9", StringComparer.OrdinalIgnoreCase) && !options.EnableDecodingColorDepth10Vp9))
                {
                    return null;
                }
            }

            if (string.Equals(options.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
            {
                // Currently there is no AMF decoder on Linux, only have h264 encoder.
                if (isDxvaSupported && options.HardwareDecodingCodecs.Contains(videoCodec, StringComparer.OrdinalIgnoreCase))
                {
                    if (isWindows && isWindows8orLater)
                    {
                        return "-hwaccel d3d11va";
                    }

                    if (isWindows && !isWindows8orLater)
                    {
                        return "-hwaccel dxva2";
                    }
                }
            }

            if (string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(options.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase)
                    && IsVppTonemappingSupported(state, options)))
            {
                if (IsVaapiSupported(state) && options.HardwareDecodingCodecs.Contains(videoCodec, StringComparer.OrdinalIgnoreCase))
                {
                    if (isLinux)
                    {
                        return "-hwaccel vaapi";
                    }
                }
            }

            if (string.Equals(options.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
            {
                if (options.HardwareDecodingCodecs.Contains(videoCodec, StringComparer.OrdinalIgnoreCase))
                {
                    return "-hwaccel cuda";
                }
            }

            return null;
        }

        public string GetSubtitleEmbedArguments(EncodingJobInfo state)
        {
            if (state.SubtitleStream == null || state.SubtitleDeliveryMethod != SubtitleDeliveryMethod.Embed)
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

            if (string.Equals(Path.GetExtension(outputPath), ".mp4", StringComparison.OrdinalIgnoreCase)
                && state.BaseRequest.Context == EncodingContext.Streaming)
            {
                // Comparison: https://github.com/jansmolders86/mediacenterjs/blob/master/lib/transcoding/desktop.js
                format = " -f mp4 -movflags frag_keyframe+empty_moov";
            }

            var threads = GetNumberOfThreads(state, encodingOptions, videoCodec);

            var inputModifier = GetInputModifier(state, encodingOptions);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}{2} {3} {4} -map_metadata -1 -map_chapters -1 -threads {5} {6}{7}{8} -y \"{9}\"",
                inputModifier,
                GetInputArgument(state, encodingOptions),
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
                return " -fflags " + string.Join("", flags);
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
                if (state.VideoStream != null
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

                var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

                var hasCopyTs = false;

                // Add resolution params, if specified
                if (!hasGraphicalSubs)
                {
                    var outputSizeParam = GetOutputSizeParam(state, encodingOptions, videoCodec);

                    args += outputSizeParam;

                    hasCopyTs = outputSizeParam.IndexOf("copyts", StringComparison.OrdinalIgnoreCase) != -1;
                }

                // This is for graphical subs
                if (hasGraphicalSubs)
                {
                    var graphicalSubtitleParam = GetGraphicalSubtitleParam(state, encodingOptions, videoCodec);

                    args += graphicalSubtitleParam;

                    hasCopyTs = graphicalSubtitleParam.IndexOf("copyts", StringComparison.OrdinalIgnoreCase) != -1;
                }

                if (state.RunTimeTicks.HasValue && state.BaseRequest.CopyTimestamps)
                {
                    if (!hasCopyTs)
                    {
                        args += " -copyts";
                    }

                    args += " -avoid_negative_ts disabled";

                    if (!(state.SubtitleStream != null && state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream))
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
            if (state.AudioStream == null && state.VideoStream != null)
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

            // Add the number of audio channels
            var channels = state.OutputAudioChannels;

            if (channels.HasValue)
            {
                args += " -ac " + channels.Value;
            }

            var bitrate = state.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                args += " -ab " + bitrate.Value.ToString(_usCulture);
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                args += " -ar " + state.OutputAudioSampleRate.Value.ToString(_usCulture);
            }

            args += GetAudioFilterParam(state, encodingOptions, false);

            return args;
        }

        public string GetProgressiveAudioFullCommandLine(EncodingJobInfo state, EncodingOptions encodingOptions, string outputPath)
        {
            var audioTranscodeParams = new List<string>();

            var bitrate = state.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                audioTranscodeParams.Add("-ab " + bitrate.Value.ToString(_usCulture));
            }

            if (state.OutputAudioChannels.HasValue)
            {
                audioTranscodeParams.Add("-ac " + state.OutputAudioChannels.Value.ToString(_usCulture));
            }

            // opus will fail on 44100
            if (!string.Equals(state.OutputAudioCodec, "opus", StringComparison.OrdinalIgnoreCase))
            {
                if (state.OutputAudioSampleRate.HasValue)
                {
                    audioTranscodeParams.Add("-ar " + state.OutputAudioSampleRate.Value.ToString(_usCulture));
                }
            }

            var threads = GetNumberOfThreads(state, encodingOptions, null);

            var inputModifier = GetInputModifier(state, encodingOptions);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}{7}{8} -threads {2}{3} {4} -id3v2_version 3 -write_id3v1 1{6} -y \"{5}\"",
                inputModifier,
                GetInputArgument(state, encodingOptions),
                threads,
                " -vn",
                string.Join(" ", audioTranscodeParams),
                outputPath,
                string.Empty,
                string.Empty,
                string.Empty).Trim();
        }

        public static bool IsCopyCodec(string codec)
        {
            return string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsColorDepth10(EncodingJobInfo state)
        {
            var result = false;
            var videoStream = state.VideoStream;

            if (videoStream != null)
            {
                if (!string.IsNullOrEmpty(videoStream.PixelFormat))
                {
                    result = videoStream.PixelFormat.Contains("p10", StringComparison.OrdinalIgnoreCase);
                    if (result)
                    {
                        return true;
                    }
                }

                if (!string.IsNullOrEmpty(videoStream.Profile))
                {
                    result = videoStream.Profile.Contains("Main 10", StringComparison.OrdinalIgnoreCase)
                        || videoStream.Profile.Contains("High 10", StringComparison.OrdinalIgnoreCase)
                        || videoStream.Profile.Contains("Profile 2", StringComparison.OrdinalIgnoreCase);
                    if (result)
                    {
                        return true;
                    }
                }

                result = (videoStream.BitDepth ?? 8) == 10;
                if (result)
                {
                    return true;
                }
            }

            return result;
        }
    }
}
