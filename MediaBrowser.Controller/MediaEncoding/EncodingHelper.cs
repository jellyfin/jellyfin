using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

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
            // Since transcoding of folder rips is expiremental anyway, it's not worth adding additional variables such as this.
            if (state.VideoType == VideoType.VideoFile)
            {
                var hwType = encodingOptions.HardwareAccelerationType;

                var codecMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"qsv",                  hwEncoder + "_qsv"},
                    {hwEncoder + "_qsv",     hwEncoder + "_qsv"},
                    {"nvenc",                hwEncoder + "_nvenc"},
                    {"amf",                  hwEncoder + "_amf"},
                    {"omx",                  hwEncoder + "_omx"},
                    {hwEncoder + "_v4l2m2m", hwEncoder + "_v4l2m2m"},
                    {"mediacodec",           hwEncoder + "_mediacodec"},
                    {"vaapi",                hwEncoder + "_vaapi"}
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

            return true;
        }

        /// <summary>
        /// Gets the name of the output video codec
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

            // Seeing reported failures here, not sure yet if this is related to specfying input format
            if (string.Equals(container, "m4v", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // obviously don't do this for strm files
            if (string.Equals(container, "strm", StringComparison.OrdinalIgnoreCase))
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
        /// Infers the audio codec based on the url
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
            profile = profile.Replace(" ", "");
            return Array.FindIndex(_videoProfiles, x => string.Equals(x, profile, StringComparison.OrdinalIgnoreCase));
        }

        public string GetInputPathArgument(EncodingJobInfo state)
        {
            var protocol = state.InputProtocol;
            var mediaPath = state.MediaPath ?? string.Empty;

            string[] inputPath;
            if (state.IsInputVideo
                && !(state.VideoType == VideoType.Iso && state.IsoMount == null))
            {
                inputPath = MediaEncoderHelpers.GetInputArgument(
                    _fileSystem,
                    mediaPath,
                    state.IsoMount,
                    state.PlayableStreamFileNames);
            }
            else
            {
                inputPath = new[] { mediaPath };
            }

            return _mediaEncoder.GetInputArgument(inputPath, protocol);
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

            return codec.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        public string GetInputArgument(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var arg = new StringBuilder();

            if (state.IsVideoRequest
                && string.Equals(encodingOptions.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
            {
                arg.Append("-hwaccel vaapi -hwaccel_output_format vaapi")
                    .Append(" -vaapi_device ")
                    .Append(encodingOptions.VaapiDevice)
                    .Append(' ');
            }

            if (state.IsVideoRequest
                && string.Equals(encodingOptions.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
            {
                var videoDecoder = GetHardwareAcceleratedVideoDecoder(state, encodingOptions);
                var outputVideoCodec = GetVideoEncoder(state, encodingOptions);

                var hasTextSubs = state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

                if (!hasTextSubs)
                {
                    // While using QSV encoder
                    if ((outputVideoCodec ?? string.Empty).IndexOf("qsv", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        // While using QSV decoder
                        if ((videoDecoder ?? string.Empty).IndexOf("qsv", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            arg.Append("-hwaccel qsv ");
                        }
                        // While using SW decoder
                        else
                        {
                            arg.Append("-init_hw_device qsv=hw -filter_hw_device hw ");
                        }
                    }
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
        public bool IsH264(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1
                    || codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        public bool IsH265(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("265", StringComparison.OrdinalIgnoreCase) != -1
                || codec.IndexOf("hevc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        public string GetBitStreamArgs(MediaStream stream)
        {
            if (IsH264(stream))
            {
                return "-bsf:v h264_mp4toannexb";
            }
            else if (IsH265(stream))
            {
                return "-bsf:v hevc_mp4toannexb";
            }
            else
            {
                return null;
            }
        }

        public string GetVideoBitrateParam(EncodingJobInfo state, string videoCodec)
        {
            var bitrate = state.OutputVideoBitrate;

            if (bitrate.HasValue)
            {
                if (string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase))
                {
                    // With vpx when crf is used, b:v becomes a max rate
                    // https://trac.ffmpeg.org/wiki/vpxEncodingGuide.
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

        public string NormalizeTranscodingLevel(string videoCodec, string level)
        {
            // Clients may direct play higher than level 41, but there's no reason to transcode higher
            if (double.TryParse(level, NumberStyles.Any, _usCulture, out double requestLevel)
                && requestLevel > 41
                && (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoCodec, "h265", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoCodec, "hevc", StringComparison.OrdinalIgnoreCase)))
            {
                return "41";
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

            // fallbackFontParam = string.Format(":force_style='FontName=Droid Sans Fallback':fontsdir='{0}'", _mediaEncoder.EscapeSubtitleFilterPath(_fileSystem.GetDirectoryName(fallbackFontPath)));

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

        /// <summary>
        /// Gets the video bitrate to specify on the command line
        /// </summary>
        public string GetVideoQualityParam(EncodingJobInfo state, string videoEncoder, EncodingOptions encodingOptions, string defaultPreset)
        {
            var param = string.Empty;

            var isVc1 = state.VideoStream != null &&
                string.Equals(state.VideoStream.Codec, "vc1", StringComparison.OrdinalIgnoreCase);
            var isLibX265 = string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase) || isLibX265)
            {
                if (!string.IsNullOrEmpty(encodingOptions.EncoderPreset))
                {
                    param += "-preset " + encodingOptions.EncoderPreset;
                }
                else
                {
                    param += "-preset " + defaultPreset;
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
            else if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)) // h264 (h264_qsv)
            {
                string[] valid_h264_qsv = { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast" };

                if (valid_h264_qsv.Contains(encodingOptions.EncoderPreset, StringComparer.OrdinalIgnoreCase))
                {
                    param += "-preset " + encodingOptions.EncoderPreset;
                }
                else
                {
                    param += "-preset 7";
                }

                param += " -look_ahead 0";

            }
            else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase) // h264 (h264_nvenc)
                || string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase))
            {
                switch (encodingOptions.EncoderPreset)
                {
                    case "veryslow":

                        param += "-preset slow"; //lossless is only supported on maxwell and newer(2014+)
                        break;

                    case "slow":
                    case "slower":
                        param += "-preset slow";
                        break;

                    case "medium":
                        param += "-preset medium";
                        break;

                    case "fast":
                    case "faster":
                    case "veryfast":
                    case "superfast":
                    case "ultrafast":
                        param += "-preset fast";
                        break;

                    default:
                        param += "-preset default";
                        break;
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
                param += string.Format("-speed 16 -quality good -profile:v {0} -slices 8 -crf {1} -qmin {2} -qmax {3}",
                    profileScore.ToString(_usCulture),
                    crf,
                    qmin,
                    qmax);
            }
            else if (string.Equals(videoEncoder, "mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                param += "-mbd rd -flags +mv4+aic -trellis 2 -cmp 2 -subcmp 2 -bf 2";
            }
            else if (string.Equals(videoEncoder, "wmv2", StringComparison.OrdinalIgnoreCase)) // asf/wmv
            {
                param += "-qmin 2";
            }
            else if (string.Equals(videoEncoder, "msmpeg4", StringComparison.OrdinalIgnoreCase))
            {
                param += "-mbd 2";
            }

            param += GetVideoBitrateParam(state, videoEncoder);

            var framerate = GetFramerateParam(state);
            if (framerate.HasValue)
            {
                param += string.Format(" -r {0}", framerate.Value.ToString(_usCulture));
            }

            var targetVideoCodec = state.ActualOutputVideoCodec;

            var profile = state.GetRequestedProfiles(targetVideoCodec).FirstOrDefault();

            // vaapi does not support Baseline profile, force Constrained Baseline in this case,
            // which is compatible (and ugly)
            if (string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                && profile != null
                && profile.IndexOf("baseline", StringComparison.OrdinalIgnoreCase) != -1)
            {
                profile = "constrained_baseline";
            }

            if (!string.IsNullOrEmpty(profile))
            {
                if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase))
                {
                    // not supported by h264_omx
                    param += " -profile:v " + profile;
                }
            }

            var level = state.GetRequestedLevel(targetVideoCodec);

            if (!string.IsNullOrEmpty(level))
            {
                level = NormalizeTranscodingLevel(state.OutputVideoCodec, level);

                // h264_qsv and h264_nvenc expect levels to be expressed as a decimal. libx264 supports decimal and non-decimal format
                // also needed for libx264 due to https://trac.ffmpeg.org/ticket/3307
                if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
                {
                    switch (level)
                    {
                        case "30":
                            param += " -level 3.0";
                            break;
                        case "31":
                            param += " -level 3.1";
                            break;
                        case "32":
                            param += " -level 3.2";
                            break;
                        case "40":
                            param += " -level 4.0";
                            break;
                        case "41":
                            param += " -level 4.1";
                            break;
                        case "42":
                            param += " -level 4.2";
                            break;
                        case "50":
                            param += " -level 5.0";
                            break;
                        case "51":
                            param += " -level 5.1";
                            break;
                        case "52":
                            param += " -level 5.2";
                            break;
                        default:
                            param += " -level " + level;
                            break;
                    }
                }
                else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(videoEncoder, "hevc_nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    // nvenc doesn't decode with param -level set ?!
                    // TODO:
                }
                else if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase))
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
                // todo
            }

            if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase))
            {
                param = "-pix_fmt yuv420p " + param;
            }

            if (string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase))
            {
                param = "-pix_fmt nv21 " + param;
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
                    //return false;
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
                    //return false;
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
                return .5;
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
            if (request.AudioBitRate.HasValue)
            {
                // Don't encode any higher than this
                return Math.Min(384000, request.AudioBitRate.Value);
            }

            return null;
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
                return "-af \"" + string.Join(",", filters) + "\"";
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="outputAudioCodec">The output audio codec.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        public int? GetNumAudioChannelsParam(EncodingJobInfo state, MediaStream audioStream, string outputAudioCodec)
        {
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
            else
            {
                // If we don't have any media info then limit it to 6 to prevent encoding errors due to asking for too many channels
                transcoderChannelLimit = 6;
            }

            var isTranscodingAudio = !EncodingHelper.IsCopyCodec(codec);

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
                return string.Format("-ss {0}", _mediaEncoder.GetTimeParameter(time));
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
        /// Determines which stream will be used for playback
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
            var outputSizeParam = string.Empty;

            var request = state.BaseRequest;

            // Add resolution params, if specified
            if (request.Width.HasValue
                || request.Height.HasValue
                || request.MaxHeight.HasValue
                || request.MaxWidth.HasValue)
            {
                outputSizeParam = GetOutputSizeParam(state, options, outputVideoCodec).TrimEnd('"');

                var index = outputSizeParam.IndexOf("hwdownload", StringComparison.OrdinalIgnoreCase);
                if (index != -1)
                {
                    outputSizeParam = "," + outputSizeParam.Substring(index);
                }
                else
                {
                    index = outputSizeParam.IndexOf("format", StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                    {
                        outputSizeParam = "," + outputSizeParam.Substring(index);
                    }
                    else
                    {
                        index = outputSizeParam.IndexOf("yadif", StringComparison.OrdinalIgnoreCase);
                        if (index != -1)
                        {
                            outputSizeParam = "," + outputSizeParam.Substring(index);
                        }
                        else
                        {
                            index = outputSizeParam.IndexOf("scale", StringComparison.OrdinalIgnoreCase);
                            if (index != -1)
                            {
                                outputSizeParam = "," + outputSizeParam.Substring(index);
                            }
                        }
                    }
                }
            }

            var videoSizeParam = string.Empty;
            var videoDecoder = GetHardwareAcceleratedVideoDecoder(state, options);

            // Setup subtitle scaling
            if (state.VideoStream != null && state.VideoStream.Width.HasValue && state.VideoStream.Height.HasValue)
            {
                // force_original_aspect_ratio=decrease
                // Enable decreasing output video width or height if necessary to keep the original aspect ratio
                videoSizeParam = string.Format(
                    CultureInfo.InvariantCulture,
                    "scale={0}:{1}:force_original_aspect_ratio=decrease",
                    state.VideoStream.Width.Value,
                    state.VideoStream.Height.Value);

                // For QSV, feed it into hardware encoder now
                if (string.Equals(outputVideoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    videoSizeParam += ",hwupload=extra_hw_frames=64";
                }

                // For VAAPI and CUVID decoder
                // these encoders cannot automatically adjust the size of graphical subtitles to fit the output video,
                // thus needs to be manually adjusted.
                if ((IsVaapiSupported(state) && string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase))
                    || (videoDecoder ?? string.Empty).IndexOf("cuvid", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    var videoStream = state.VideoStream;
                    var inputWidth = videoStream?.Width;
                    var inputHeight = videoStream?.Height;
                    var (width, height) = GetFixedOutputSize(inputWidth, inputHeight, request.Width, request.Height, request.MaxWidth, request.MaxHeight);

                    if (width.HasValue && height.HasValue)
                    {
                        videoSizeParam = string.Format(
                        CultureInfo.InvariantCulture,
                        "scale={0}:{1}:force_original_aspect_ratio=decrease",
                        width.Value,
                        height.Value);
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
            var retStr = " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}][sub]overlay{3}\"";

            // When the input may or may not be hardware VAAPI decodable
            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                /*
                    [base]: HW scaling video to OutputSize
                    [sub]: SW scaling subtitle to FixedOutputSize
                    [base][sub]: SW overlay
                */
                outputSizeParam = outputSizeParam.TrimStart(',');
                retStr = " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3},hwdownload[base];[base][sub]overlay,format=nv12,hwupload\"";
            }

            // If we're hardware VAAPI decoding and software encoding, download frames from the decoder first
            else if (IsVaapiSupported(state) && string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase)
                && string.Equals(outputVideoCodec, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                /*
                    [base]: SW scaling video to OutputSize
                    [sub]: SW scaling subtitle to FixedOutputSize
                    [base][sub]: SW overlay
                */
                outputSizeParam = outputSizeParam.TrimStart(',');
                retStr = " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]{3}[base];[base][sub]overlay\"";
            }

            else if (string.Equals(outputVideoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase))
            {
                /*
                    QSV in FFMpeg can now setup hardware overlay for transcodes.
                    For software decoding and hardware encoding option, frames must be hwuploaded into hardware
                    with fixed frame size.
                */
                if (!string.IsNullOrEmpty(videoDecoder) && videoDecoder.Contains("qsv", StringComparison.OrdinalIgnoreCase))
                {
                    retStr = " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}][sub]overlay_qsv=x=(W-w)/2:y=(H-h)/2{3}\"";
                }
                else
                {
                    retStr = " -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}]hwupload=extra_hw_frames=64[v];[v][sub]overlay_qsv=x=(W-w)/2:y=(H-h)/2{3}\"";
                }
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                retStr,
                mapPrefix,
                subtitleStreamIndex,
                state.VideoStream.Index,
                outputSizeParam,
                videoSizeParam);
        }

        private (int? width, int? height) GetFixedOutputSize(
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

        public List<string> GetScalingFilters(EncodingJobInfo state,
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

            var hasTextSubs = state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

            if ((string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) && !hasTextSubs))
                && width.HasValue
                && height.HasValue)
            {
                // Given the input dimensions (inputWidth, inputHeight), determine the output dimensions
                // (outputWidth, outputHeight). The user may request precise output dimensions or maximum
                // output dimensions. Output dimensions are guaranteed to be even.
                var outputWidth = width.Value;
                var outputHeight = height.Value;
                var vaapi_or_qsv = string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) ? "qsv" : "vaapi";

                if (!videoWidth.HasValue
                    || outputWidth != videoWidth.Value
                    || !videoHeight.HasValue
                    || outputHeight != videoHeight.Value)
                {
                    // Force nv12 pixel format to enable 10-bit to 8-bit colour conversion.
                    filters.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "scale_{0}=w={1}:h={2}:format=nv12",
                            vaapi_or_qsv,
                            outputWidth,
                            outputHeight));
                }
                else
                {
                    filters.Add(string.Format(CultureInfo.InvariantCulture, "scale_{0}=format=nv12", vaapi_or_qsv));
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
                        //fsbs crop width in half,set the display aspect,crop out any black bars we may have made the scale width to requestedWidth.
                        break;
                    case Video3DFormat.HalfTopAndBottom:
                        filter = "crop=iw:ih/2:0:0,scale=(iw*2):ih),setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale={0}:trunc({0}/dar/2)*2";
                        //htab crop height in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to requestedWidth
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

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it
        /// </summary>
        public string GetOutputSizeParam(
            EncodingJobInfo state,
            EncodingOptions options,
            string outputVideoCodec)
        {
            // http://sonnati.wordpress.com/2012/10/19/ffmpeg-the-swiss-army-knife-of-internet-streaming-part-vi/

            var request = state.BaseRequest;

            var videoStream = state.VideoStream;
            var filters = new List<string>();

            var videoDecoder = GetHardwareAcceleratedVideoDecoder(state, options);
            var inputWidth = videoStream?.Width;
            var inputHeight = videoStream?.Height;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var hasTextSubs = state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

            // When the input may or may not be hardware VAAPI decodable
            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("format=nv12|vaapi");
                filters.Add("hwupload");
            }

            // When the input may or may not be hardware QSV decodable
            else if (string.Equals(outputVideoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasTextSubs)
                {
                    filters.Add("format=nv12|qsv");
                    filters.Add("hwupload=extra_hw_frames=64");
                }
            }

            // If we're hardware VAAPI decoding and software encoding, download frames from the decoder first
            else if (IsVaapiSupported(state) && string.Equals(options.HardwareAccelerationType, "vaapi", StringComparison.OrdinalIgnoreCase)
                && string.Equals(outputVideoCodec, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                var codec = videoStream.Codec.ToLowerInvariant();
                var isColorDepth10 = !string.IsNullOrEmpty(videoStream.Profile) && (videoStream.Profile.Contains("Main 10", StringComparison.OrdinalIgnoreCase)
                    || videoStream.Profile.Contains("High 10", StringComparison.OrdinalIgnoreCase));

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

            // Add hardware deinterlace filter before scaling filter
            if (state.DeInterlace("h264", true))
            {
                if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    filters.Add(string.Format(CultureInfo.InvariantCulture, "deinterlace_vaapi"));
                }
                else if (string.Equals(outputVideoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasTextSubs)
                    {
                        filters.Add(string.Format(CultureInfo.InvariantCulture, "deinterlace_qsv"));
                    }
                }
            }

            // Add software deinterlace filter before scaling filter
            if (((state.DeInterlace("h264", true) || state.DeInterlace("h265", true) || state.DeInterlace("hevc", true))
                && !string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(outputVideoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                    || (hasTextSubs && state.DeInterlace("h264", true) && string.Equals(outputVideoCodec, "h264_qsv", StringComparison.OrdinalIgnoreCase)))
            {
                var inputFramerate = videoStream?.RealFrameRate;

                // If it is already 60fps then it will create an output framerate that is much too high for roku and others to handle
                if (string.Equals(options.DeinterlaceMethod, "yadif_bob", StringComparison.OrdinalIgnoreCase) && (inputFramerate ?? 60) <= 30)
                {
                    filters.Add("yadif=1:-1:0");
                }
                else
                {
                    filters.Add("yadif=0:-1:0");
                }
            }

            // Add scaling filter: scale_*=format=nv12 or scale_*=w=*:h=*:format=nv12 or scale=expr
            filters.AddRange(GetScalingFilters(state, inputWidth, inputHeight, threeDFormat, videoDecoder, outputVideoCodec, request.Width, request.Height, request.MaxWidth, request.MaxHeight));

            // Add parameters to use VAAPI with burn-in text subttiles (GH issue #642)
            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                if (state.SubtitleStream != null
                    && state.SubtitleStream.IsTextSubtitleStream
                    && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
                {
                    // Test passed on Intel and AMD gfx
                    filters.Add("hwmap=mode=read+write");
                    filters.Add("format=nv12");
                }
            }

            var output = string.Empty;

            if (state.SubtitleStream != null
                && state.SubtitleStream.IsTextSubtitleStream
                && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
            {
                var subParam = GetTextSubtitleParam(state);

                filters.Add(subParam);

                // Ensure proper filters are passed to ffmpeg in case of hardware acceleration via VA-API
                // Reference: https://trac.ffmpeg.org/wiki/Hardware/VAAPI
                if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    filters.Add("hwmap");
                }
            }

            if (filters.Count > 0)
            {
                output += string.Format(
                    CultureInfo.InvariantCulture,
                    " -vf \"{0}\"",
                    string.Join(",", filters));
            }

            return output;
        }


        /// <summary>
        /// Gets the number of threads.
        /// </summary>
        public int GetNumberOfThreads(EncodingJobInfo state, EncodingOptions encodingOptions, string outputVideoCodec)
        {
            if (string.Equals(outputVideoCodec, "libvpx", StringComparison.OrdinalIgnoreCase))
            {
                // per docs:
                // -threads    number of threads to use for encoding, can't be 0 [auto] with VP8
                //             (recommended value : number of real cores - 1)
                return Math.Max(Environment.ProcessorCount - 1, 1);
            }

            var threads = state.BaseRequest.CpuCoreLimit ?? encodingOptions.EncodingThreadCount;

            // Automatic
            if (threads <= 0 || threads >= Environment.ProcessorCount)
            {
                return 0;
            }

            return threads;
        }

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
                if (user != null && !user.Policy.EnableVideoPlaybackTranscoding)
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
                if (user != null && !user.Policy.EnableAudioPlaybackTranscoding)
                {
                    state.OutputAudioCodec = "copy";
                }
            }
        }

        public string GetProbeSizeArgument(int numInputFiles)
            => numInputFiles > 1 ? "-probesize " + _configuration.GetFFmpegProbeSize() : string.Empty;

        public string GetAnalyzeDurationArgument(int numInputFiles)
            => numInputFiles > 1 ? "-analyzeduration " + _configuration.GetFFmpegAnalyzeDuration() : string.Empty;

        public string GetInputModifier(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var inputModifier = string.Empty;

            var numInputFiles = state.PlayableStreamFileNames.Length > 0 ? state.PlayableStreamFileNames.Length : 1;
            var probeSizeArgument = GetProbeSizeArgument(numInputFiles);

            string analyzeDurationArgument;
            if (state.MediaSource.AnalyzeDurationMs.HasValue)
            {
                analyzeDurationArgument = "-analyzeduration " + (state.MediaSource.AnalyzeDurationMs.Value * 1000).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                analyzeDurationArgument = GetAnalyzeDurationArgument(numInputFiles);
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

            if (state.GenPtsInput || EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
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

                if ((videoDecoder ?? string.Empty).IndexOf("cuvid", StringComparison.OrdinalIgnoreCase) != -1)
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
                        inputModifier += string.Format(
                            CultureInfo.InvariantCulture,
                            " -resize {0}x{1}",
                            width.Value,
                            height.Value);
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

            if (mediaSource.VideoType.HasValue)
            {
                state.VideoType = mediaSource.VideoType.Value;

                if (mediaSource.VideoType.Value == VideoType.BluRay || mediaSource.VideoType.Value == VideoType.Dvd)
                {
                    state.PlayableStreamFileNames = Video.QueryPlayableStreamFiles(state.MediaPath, mediaSource.VideoType.Value).Select(Path.GetFileName).ToArray();
                }
                else if (mediaSource.VideoType.Value == VideoType.Iso && state.IsoType == IsoType.BluRay)
                {
                    state.PlayableStreamFileNames = Video.QueryPlayableStreamFiles(state.MediaPath, VideoType.BluRay).Select(Path.GetFileName).ToArray();
                }
                else if (mediaSource.VideoType.Value == VideoType.Iso && state.IsoType == IsoType.Dvd)
                {
                    state.PlayableStreamFileNames = Video.QueryPlayableStreamFiles(state.MediaPath, VideoType.Dvd).Select(Path.GetFileName).ToArray();
                }
                else
                {
                    state.PlayableStreamFileNames = Array.Empty<string>();
                }
            }
            else
            {
                state.PlayableStreamFileNames = Array.Empty<string>();
            }

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
            if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                var supportedAudioCodecsList = request.AudioCodec.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                ShiftAudioCodecsIfNeeded(supportedAudioCodecsList, state.AudioStream);

                state.SupportedAudioCodecs = supportedAudioCodecsList.ToArray();

                request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(i => _mediaEncoder.CanEncodeToAudioCodec(i))
                    ?? state.SupportedAudioCodecs.FirstOrDefault();
            }
        }

        private void ShiftAudioCodecsIfNeeded(List<string> audioCodecs, MediaStream audioStream)
        {
            // Nothing to do here
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
        /// Gets the name of the output video codec
        /// </summary>
        protected string GetHardwareAcceleratedVideoDecoder(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
            {
                return null;
            }

            return GetHardwareAcceleratedVideoDecoder(state.MediaSource.VideoType ?? VideoType.VideoFile, state.VideoStream, encodingOptions);
        }

        public string GetHardwareAcceleratedVideoDecoder(VideoType videoType, MediaStream videoStream, EncodingOptions encodingOptions)
        {
            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is expiremental anyway, it's not worth adding additional variables such as this.
            if (videoType != VideoType.VideoFile)
            {
                return null;
            }

            if (videoStream != null
                && !string.IsNullOrEmpty(videoStream.Codec)
                && !string.IsNullOrEmpty(encodingOptions.HardwareAccelerationType))
            {
                if (string.Equals(encodingOptions.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_qsv") && encodingOptions.HardwareDecodingCodecs.Contains("h264", StringComparer.OrdinalIgnoreCase))
                            {
                                // qsv decoder does not support 10-bit input
                                if ((videoStream.BitDepth ?? 8) > 8)
                                {
                                    encodingOptions.HardwareDecodingCodecs = Array.Empty<string>();
                                    return null;
                                }
                                return "-c:v h264_qsv";
                            }
                            break;
                        case "hevc":
                        case "h265":
                            if (_mediaEncoder.SupportsDecoder("hevc_qsv") && encodingOptions.HardwareDecodingCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase))
                            {
                                //return "-c:v hevc_qsv -load_plugin hevc_hw ";
                                return "-c:v hevc_qsv";
                            }
                            break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("mpeg2_qsv") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg2video", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg2_qsv";
                            }
                            break;
                        case "vc1":
                            if (_mediaEncoder.SupportsDecoder("vc1_qsv") && encodingOptions.HardwareDecodingCodecs.Contains("vc1", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vc1_qsv";
                            }
                            break;
                    }
                }

                else if (string.Equals(encodingOptions.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("h264", StringComparer.OrdinalIgnoreCase))
                            {
                                // cuvid decoder does not support 10-bit input
                                if ((videoStream.BitDepth ?? 8) > 8)
                                {
                                    encodingOptions.HardwareDecodingCodecs = Array.Empty<string>();
                                    return null;
                                }
                                return "-c:v h264_cuvid";
                            }
                            break;
                        case "hevc":
                        case "h265":
                            if (_mediaEncoder.SupportsDecoder("hevc_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v hevc_cuvid";
                            }
                            break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("mpeg2_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg2video", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg2_cuvid";
                            }
                            break;
                        case "vc1":
                            if (_mediaEncoder.SupportsDecoder("vc1_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("vc1", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vc1_cuvid";
                            }
                            break;
                        case "mpeg4":
                            if (_mediaEncoder.SupportsDecoder("mpeg4_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg4", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg4_cuvid";
                            }
                            break;
                    }
                }

                else if (string.Equals(encodingOptions.HardwareAccelerationType, "mediacodec", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("h264", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v h264_mediacodec";
                            }
                            break;
                        case "hevc":
                        case "h265":
                            if (_mediaEncoder.SupportsDecoder("hevc_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v hevc_mediacodec";
                            }
                            break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("mpeg2_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg2video", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg2_mediacodec";
                            }
                            break;
                        case "mpeg4":
                            if (_mediaEncoder.SupportsDecoder("mpeg4_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg4", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg4_mediacodec";
                            }
                            break;
                        case "vp8":
                            if (_mediaEncoder.SupportsDecoder("vp8_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("vp8", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vp8_mediacodec";
                            }
                            break;
                        case "vp9":
                            if (_mediaEncoder.SupportsDecoder("vp9_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("vp9", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vp9_mediacodec";
                            }
                            break;
                    }
                }

                else if (string.Equals(encodingOptions.HardwareAccelerationType, "omx", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLowerInvariant())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_mmal") && encodingOptions.HardwareDecodingCodecs.Contains("h264", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v h264_mmal";
                            }
                            break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("mpeg2_mmal") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg2video", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg2_mmal";
                            }
                            break;
                        case "mpeg4":
                            if (_mediaEncoder.SupportsDecoder("mpeg4_mmal") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg4", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg4_mmal";
                            }
                            break;
                        case "vc1":
                            if (_mediaEncoder.SupportsDecoder("vc1_mmal") && encodingOptions.HardwareDecodingCodecs.Contains("vc1", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vc1_mmal";
                            }
                            break;
                    }
                }

                else if (string.Equals(encodingOptions.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
                {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        if (Environment.OSVersion.Version.Major > 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 1))
                            return "-hwaccel d3d11va";
                        else
                            return "-hwaccel dxva2";
                    }
                    else
                    {
                        return "-hwaccel vaapi";
                    }
                }
            }

            // Avoid a second attempt if no hardware acceleration is being used
            encodingOptions.HardwareDecodingCodecs = Array.Empty<string>();

            // leave blank so ffmpeg will decide
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

            if (EncodingHelper.IsCopyCodec(videoCodec))
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

            if (EncodingHelper.IsCopyCodec(codec))
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

            args += " " + GetAudioFilterParam(state, encodingOptions, false);

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
    }
}
