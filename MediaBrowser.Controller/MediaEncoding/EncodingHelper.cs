using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class EncodingHelper
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly ISubtitleEncoder _subtitleEncoder;

        public EncodingHelper(IMediaEncoder mediaEncoder, IFileSystem fileSystem, ISubtitleEncoder subtitleEncoder)
        {
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _subtitleEncoder = subtitleEncoder;
        }

        public string GetH264Encoder(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var defaultEncoder = "libx264";

            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is expiremental anyway, it's not worth adding additional variables such as this.
            if (state.VideoType == VideoType.VideoFile)
            {
                var hwType = encodingOptions.HardwareAccelerationType;

                if (!encodingOptions.EnableHardwareEncoding)
                {
                    hwType = null;
                }

                if (string.Equals(hwType, "qsv", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(hwType, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_qsv", defaultEncoder);
                }

                if (string.Equals(hwType, "nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_nvenc", defaultEncoder);
                }
                if (string.Equals(hwType, "amf", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_amf", defaultEncoder);
                }
                if (string.Equals(hwType, "omx", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_omx", defaultEncoder);
                }
                if (string.Equals(hwType, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_v4l2m2m", defaultEncoder);
                }
                if (string.Equals(hwType, "mediacodec", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_mediacodec", defaultEncoder);
                }
                if (string.Equals(hwType, "vaapi", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(encodingOptions.VaapiDevice))
                {
                    if (IsVaapiSupported(state))
                    {
                        return GetAvailableEncoder("h264_vaapi", defaultEncoder);
                    }
                }
            }

            return defaultEncoder;
        }

        private string GetAvailableEncoder(string preferredEncoder, string defaultEncoder)
        {
            if (_mediaEncoder.SupportsEncoder(preferredEncoder))
            {
                return preferredEncoder;
            }
            return defaultEncoder;
        }

        private bool IsVaapiSupported(EncodingJobInfo state)
        {
            var videoStream = state.VideoStream;

            if (videoStream != null)
            {
                // vaapi will throw an error with this input
                // [vaapi @ 0x7faed8000960] No VAAPI support for codec mpeg4 profile -99.
                if (string.Equals(videoStream.Codec, "mpeg4", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
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

                return codec.ToLower();
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
            string useragent = null;

            state.RemoteHttpHeaders.TryGetValue("User-Agent", out useragent);

            if (!string.IsNullOrEmpty(useragent))
            {
                return "-user_agent \"" + useragent + "\"";
            }

            return string.Empty;
        }

        public string GetInputFormat(string container)
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
            string[] list =
            {
                "ConstrainedBaseline",
                "Baseline",
                "Extended",
                "Main",
                "High",
                "ProgressiveHigh",
                "ConstrainedHigh"
            };

            // strip spaces because they may be stripped out on the query string
            return Array.FindIndex(list, t => string.Equals(t, profile.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        }

        public string GetInputPathArgument(EncodingJobInfo state)
        {
            var protocol = state.InputProtocol;
            var mediaPath = state.MediaPath ?? string.Empty;

            var inputPath = new[] { mediaPath };

            if (state.IsInputVideo)
            {
                if (!(state.VideoType == VideoType.Iso && state.IsoMount == null))
                {
                    inputPath = MediaEncoderHelpers.GetInputArgument(_fileSystem, mediaPath, state.InputProtocol, state.IsoMount, state.PlayableStreamFileNames);
                }
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
                return "aac -strict experimental";
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

            return codec.ToLower();
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        public string GetInputArgument(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var request = state.BaseRequest;

            var arg = string.Format("-i {0}", GetInputPathArgument(state));

            if (state.SubtitleStream != null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
            {
                if (state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream)
                {
                    if (state.VideoStream != null && state.VideoStream.Width.HasValue)
                    {
                        // This is hacky but not sure how to get the exact subtitle resolution
                        double height = state.VideoStream.Width.Value;
                        height /= 16;
                        height *= 9;

                        arg += string.Format(" -canvas_size {0}:{1}", state.VideoStream.Width.Value.ToString(CultureInfo.InvariantCulture), Convert.ToInt32(height).ToString(CultureInfo.InvariantCulture));
                    }

                    var subtitlePath = state.SubtitleStream.Path;

                    if (string.Equals(Path.GetExtension(subtitlePath), ".sub", StringComparison.OrdinalIgnoreCase))
                    {
                        var idxFile = Path.ChangeExtension(subtitlePath, ".idx");
                        if (_fileSystem.FileExists(idxFile))
                        {
                            subtitlePath = idxFile;
                        }
                    }

                    arg += " -i \"" + subtitlePath + "\"";
                }
            }

            if (state.IsVideoRequest)
            {
                if (GetVideoEncoder(state, encodingOptions).IndexOf("vaapi", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;
                    var hwOutputFormat = "vaapi";

                    if (hasGraphicalSubs)
                    {
                        hwOutputFormat = "yuv420p";
                    }

                    arg = "-hwaccel vaapi -hwaccel_output_format " + hwOutputFormat + " -vaapi_device " + encodingOptions.VaapiDevice + " " + arg;
                }
            }

            return arg.Trim();
        }

        /// <summary>
        /// Determines whether the specified stream is H264.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns><c>true</c> if the specified stream is H264; otherwise, <c>false</c>.</returns>
        public bool IsH264(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1 ||
                   codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1;
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
                    return string.Format(" -maxrate:v {0} -bufsize:v {1} -b:v {0}", bitrate.Value.ToString(_usCulture), (bitrate.Value * 2).ToString(_usCulture));
                }

                if (string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format(" -b:v {0}", bitrate.Value.ToString(_usCulture));
                }

                if (string.Equals(videoCodec, "libx264", StringComparison.OrdinalIgnoreCase))
                {
                    // h264
                    return string.Format(" -maxrate {0} -bufsize {1}",
                        bitrate.Value.ToString(_usCulture),
                        (bitrate.Value * 2).ToString(_usCulture));
                }

                // h264
                return string.Format(" -b:v {0} -maxrate {0} -bufsize {1}",
                    bitrate.Value.ToString(_usCulture),
                    (bitrate.Value * 2).ToString(_usCulture));
            }

            return string.Empty;
        }

        public string NormalizeTranscodingLevel(string videoCodec, string level)
        {
            // Clients may direct play higher than level 41, but there's no reason to transcode higher
            if (double.TryParse(level, NumberStyles.Any, _usCulture, out double requestLevel)
                && string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase)
                && requestLevel > 41)
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
                : string.Format(",setpts=PTS -{0}/TB", seconds.ToString(_usCulture));

            string fallbackFontParam = string.Empty;

            var mediaPath = state.MediaPath ?? string.Empty;

            return string.Format("subtitles='{0}:si={1}'{2}{3}",
                _mediaEncoder.EscapeSubtitleFilterPath(mediaPath),
                state.InternalSubtitleStreamOffset.ToString(_usCulture),
                fallbackFontParam,
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
        public string GetVideoQualityParam(EncodingJobInfo state, string videoEncoder, EncodingOptions encodingOptions, string defaultH264Preset)
        {
            var param = string.Empty;

            var isVc1 = state.VideoStream != null &&
                string.Equals(state.VideoStream.Codec, "vc1", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(encodingOptions.H264Preset))
                {
                    param += "-preset " + encodingOptions.H264Preset;
                }
                else
                {
                    param += "-preset " + defaultH264Preset;
                }

                if (encodingOptions.H264Crf >= 0 && encodingOptions.H264Crf <= 51)
                {
                    param += " -crf " + encodingOptions.H264Crf.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    param += " -crf 23";
                }
            }

            else if (string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
            {
                param += "-preset fast";

                param += " -crf 28";
            }

            // h264 (h264_qsv)
            else if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase))
            {
                string[] valid_h264_qsv = { "veryslow", "slower", "slow", "medium", "fast", "faster", "veryfast" };

                if (valid_h264_qsv.Contains(encodingOptions.H264Preset, StringComparer.OrdinalIgnoreCase))
                {
                    param += "-preset " + encodingOptions.H264Preset;
                }
                else
                {
                    param += "-preset 7";
                }

                param += " -look_ahead 0";

            }

            // h264 (h264_nvenc)
            else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase))
            {
                switch (encodingOptions.H264Preset)
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

            // webm
            else if (string.Equals(videoEncoder, "libvpx", StringComparison.OrdinalIgnoreCase))
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

            // asf/wmv
            else if (string.Equals(videoEncoder, "wmv2", StringComparison.OrdinalIgnoreCase))
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

            var request = state.BaseRequest;
            var profile = state.GetRequestedProfiles(targetVideoCodec).FirstOrDefault();
            if (string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                param += " -profile:v 578";
            }
            else if (!string.IsNullOrEmpty(profile))
            {
                if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase))
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
                if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
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
                // nvenc doesn't decode with param -level set ?!
                else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    //param += "";
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

            if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(videoEncoder, "h264_v4l2m2m", StringComparison.OrdinalIgnoreCase))
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

            if (videoStream.IsInterlaced)
            {
                if (state.DeInterlace(videoStream.Codec, false))
                {
                    return false;
                }
            }

            if (videoStream.IsAnamorphic ?? false)
            {
                if (request.RequireNonAnamorphic)
                {
                    return false;
                }
            }

            // Can't stream copy if we're burning in subtitles
            if (request.SubtitleStreamIndex.HasValue)
            {
                if (state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
                {
                    return false;
                }
            }

            if (string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
            {
                if (videoStream.IsAVC.HasValue && !videoStream.IsAVC.Value && request.RequireAvc)
                {
                    return false;
                }
            }

            // Source and target codecs must match
            if (string.IsNullOrEmpty(videoStream.Codec) || !state.SupportedVideoCodecs.Contains(videoStream.Codec, StringComparer.OrdinalIgnoreCase))
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
            if (request.MaxWidth.HasValue)
            {
                if (!videoStream.Width.HasValue || videoStream.Width.Value > request.MaxWidth.Value)
                {
                    return false;
                }
            }

            // Video height must fall within requested value
            if (request.MaxHeight.HasValue)
            {
                if (!videoStream.Height.HasValue || videoStream.Height.Value > request.MaxHeight.Value)
                {
                    return false;
                }
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
            if (request.VideoBitRate.HasValue)
            {
                if (!videoStream.BitRate.HasValue || videoStream.BitRate.Value > request.VideoBitRate.Value)
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
            if (maxRefFrames.HasValue)
            {
                if (videoStream.RefFrames.HasValue && videoStream.RefFrames.Value > maxRefFrames.Value)
                {
                    return false;
                }
            }

            // If a specific level was requested, the source must match or be less than
            var level = state.GetRequestedLevel(videoStream.Codec);
            if (!string.IsNullOrEmpty(level))
            {
                double requestLevel;

                if (double.TryParse(level, NumberStyles.Any, _usCulture, out requestLevel))
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
            }

            if (string.Equals(state.InputContainer, "avi", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(videoStream.Codec, "h264", StringComparison.OrdinalIgnoreCase) &&
                !(videoStream.IsAVC ?? false))
            {
                // see Coach S01E01 - Kelly and the Professor(0).avi
                return false;
            }

            return request.EnableAutoStreamCopy;
        }

        public bool CanStreamCopyAudio(EncodingJobInfo state, MediaStream audioStream, string[] supportedAudioCodecs)
        {
            var request = state.BaseRequest;

            if (!request.AllowAudioStreamCopy)
            {
                return false;
            }

            var maxBitDepth = state.GetRequestedAudioBitDepth(audioStream.Codec);
            if (maxBitDepth.HasValue)
            {
                if (audioStream.BitDepth.HasValue && audioStream.BitDepth.Value > maxBitDepth.Value)
                {
                    return false;
                }
            }

            // Source and target codecs must match
            if (string.IsNullOrEmpty(audioStream.Codec) || !supportedAudioCodecs.Contains(audioStream.Codec, StringComparer.OrdinalIgnoreCase))
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
                var isUpscaling = request.Height.HasValue && videoStream.Height.HasValue &&
                                   request.Height.Value > videoStream.Height.Value && request.Width.HasValue && videoStream.Width.HasValue &&
                    request.Width.Value > videoStream.Width.Value;

                // Don't allow bitrate increases unless upscaling
                if (!isUpscaling)
                {
                    if (bitrate.HasValue && videoStream.BitRate.HasValue)
                    {
                        bitrate = GetMinBitrate(videoStream.BitRate.Value, bitrate.Value);
                    }
                }
            }

            if (bitrate.HasValue)
            {
                var inputVideoCodec = videoStream == null ? null : videoStream.Codec;
                bitrate = ResolutionNormalizer.ScaleBitrate(bitrate.Value, inputVideoCodec, outputVideoCodec);

                // If a max bitrate was requested, don't let the scaled bitrate exceed it
                if (request.VideoBitRate.HasValue)
                {
                    bitrate = Math.Min(bitrate.Value, request.VideoBitRate.Value);
                }
            }

            return bitrate;
        }

        private int GetMinBitrate(int sourceBitrate, int requestedBitrate)
        {
            if (sourceBitrate <= 2000000)
            {
                sourceBitrate = Convert.ToInt32(sourceBitrate * 2.5);
            }
            else if (sourceBitrate <= 3000000)
            {
                sourceBitrate = Convert.ToInt32(sourceBitrate * 2);
            }

            var bitrate = Math.Min(sourceBitrate, requestedBitrate);

            return bitrate;
        }

        public int? GetAudioBitrateParam(BaseEncodingJobOptions request, MediaStream audioStream)
        {
            if (request.AudioBitRate.HasValue)
            {
                // Make sure we don't request a bitrate higher than the source
                var currentBitrate = audioStream == null ? request.AudioBitRate.Value : audioStream.BitRate ?? request.AudioBitRate.Value;

                // Don't encode any higher than this
                return Math.Min(384000, request.AudioBitRate.Value);
                //return Math.Min(currentBitrate, request.AudioBitRate.Value);
            }

            return null;
        }

        public string GetAudioFilterParam(EncodingJobInfo state, EncodingOptions encodingOptions, bool isHls)
        {
            var channels = state.OutputAudioChannels;

            var filters = new List<string>();

            // Boost volume to 200% when downsampling from 6ch to 2ch
            if (channels.HasValue && channels.Value <= 2)
            {
                if (state.AudioStream != null && state.AudioStream.Channels.HasValue && state.AudioStream.Channels.Value > 5 && !encodingOptions.DownMixAudioBoost.Equals(1))
                {
                    filters.Add("volume=" + encodingOptions.DownMixAudioBoost.ToString(_usCulture));
                }
            }

            var isCopyingTimestamps = state.CopyTimestamps || state.TranscodingType != TranscodingJobType.Progressive;
            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode && !isCopyingTimestamps)
            {
                var seconds = TimeSpan.FromTicks(state.StartTimeTicks ?? 0).TotalSeconds;

                filters.Add(string.Format("asetpts=PTS-{0}/TB", Math.Round(seconds).ToString(_usCulture)));
            }

            if (filters.Count > 0)
            {
                return "-af \"" + string.Join(",", filters.ToArray()) + "\"";
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="outputAudioCodec">The output audio codec.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        public int? GetNumAudioChannelsParam(EncodingJobInfo state, MediaStream audioStream, string outputAudioCodec)
        {
            var request = state.BaseRequest;

            var inputChannels = audioStream == null
                ? null
                : audioStream.Channels;

            if (inputChannels <= 0)
            {
                inputChannels = null;
            }

            int? transcoderChannelLimit = null;
            var codec = outputAudioCodec ?? string.Empty;

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

            var isTranscodingAudio = !string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase);

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
                args += string.Format("-map 0:{0}", state.VideoStream.Index);
            }
            else
            {
                // No known video stream
                args += "-vn";
            }

            if (state.AudioStream != null)
            {
                args += string.Format(" -map 0:{0}", state.AudioStream.Index);
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
                args += string.Format(" -map 0:{0}", state.SubtitleStream.Index);
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
        /// Gets the internal graphical subtitle param.
        /// </summary>
        public string GetGraphicalSubtitleParam(EncodingJobInfo state, EncodingOptions options, string outputVideoCodec)
        {
            var outputSizeParam = string.Empty;

            var request = state.BaseRequest;

            // Add resolution params, if specified
            if (request.Width.HasValue || request.Height.HasValue || request.MaxHeight.HasValue || request.MaxWidth.HasValue)
            {
                outputSizeParam = GetOutputSizeParam(state, options, outputVideoCodec).TrimEnd('"');

                if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    var index = outputSizeParam.IndexOf("format", StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                    {
                        outputSizeParam = "," + outputSizeParam.Substring(index);
                    }
                }
                else
                {
                    var index = outputSizeParam.IndexOf("scale", StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                    {
                        outputSizeParam = "," + outputSizeParam.Substring(index);
                    }
                }
            }

            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase) && outputSizeParam.Length == 0)
            {
                outputSizeParam = ",format=nv12|vaapi,hwupload";
            }

            var videoSizeParam = string.Empty;

            if (state.VideoStream != null && state.VideoStream.Width.HasValue && state.VideoStream.Height.HasValue)
            {
                videoSizeParam = string.Format("scale={0}:{1}", state.VideoStream.Width.Value.ToString(_usCulture), state.VideoStream.Height.Value.ToString(_usCulture));

                videoSizeParam += ":force_original_aspect_ratio=decrease";
            }

            var mapPrefix = state.SubtitleStream.IsExternal ?
                1 :
                0;

            var subtitleStreamIndex = state.SubtitleStream.IsExternal
                ? 0
                : state.SubtitleStream.Index;

            return string.Format(" -filter_complex \"[{0}:{1}]{4}[sub];[0:{2}][sub]overlay{3}\"",
                mapPrefix.ToString(_usCulture),
                subtitleStreamIndex.ToString(_usCulture),
                state.VideoStream.Index.ToString(_usCulture),
                outputSizeParam,
                videoSizeParam);
        }

        private Tuple<int?, int?> GetFixedOutputSize(int? videoWidth,
            int? videoHeight,
            int? requestedWidth,
            int? requestedHeight,
            int? requestedMaxWidth,
            int? requestedMaxHeight)
        {
            if (!videoWidth.HasValue && !requestedWidth.HasValue)
            {
                return new Tuple<int?, int?>(null, null);
            }
            if (!videoHeight.HasValue && !requestedHeight.HasValue)
            {
                return new Tuple<int?, int?>(null, null);
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

            return new Tuple<int?, int?>(Convert.ToInt32(outputWidth), Convert.ToInt32(outputHeight));
        }

        public List<string> GetScalingFilters(int? videoWidth,
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
            var fixedOutputSize = GetFixedOutputSize(videoWidth, videoHeight, requestedWidth, requestedHeight, requestedMaxWidth, requestedMaxHeight);

            if (string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase) && fixedOutputSize.Item1.HasValue && fixedOutputSize.Item2.HasValue)
            {
                // Work around vaapi's reduced scaling features
                var scaler = "scale_vaapi";

                // Given the input dimensions (inputWidth, inputHeight), determine the output dimensions
                // (outputWidth, outputHeight). The user may request precise output dimensions or maximum
                // output dimensions. Output dimensions are guaranteed to be even.
                var outputWidth = fixedOutputSize.Item1.Value;
                var outputHeight = fixedOutputSize.Item2.Value;

                if (!videoWidth.HasValue || outputWidth != videoWidth.Value || !videoHeight.HasValue || outputHeight != videoHeight.Value)
                {
                    filters.Add(string.Format("{0}=w={1}:h={2}", scaler, outputWidth.ToString(_usCulture), outputHeight.ToString(_usCulture)));
                }
            }
            else if ((videoDecoder ?? string.Empty).IndexOf("_cuvid", StringComparison.OrdinalIgnoreCase) != -1 && fixedOutputSize.Item1.HasValue && fixedOutputSize.Item2.HasValue)
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

                        filters.Add(string.Format("scale=trunc({0}/64)*64:trunc({1}/2)*2", widthParam, heightParam));
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
                        filters.Add(string.Format("scale=trunc(min(max(iw\\,ih*dar)\\,min({0}\\,{1}*dar))/64)*64:trunc(min(max(iw/dar\\,ih)\\,min({0}/dar\\,{1}))/2)*2", maxWidthParam, maxHeightParam));
                    }
                    else
                    {
                        filters.Add(string.Format("scale=trunc(min(max(iw\\,ih*dar)\\,min({0}\\,{1}*dar))/2)*2:trunc(min(max(iw/dar\\,ih)\\,min({0}/dar\\,{1}))/2)*2", maxWidthParam, maxHeightParam));
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

                        filters.Add(string.Format("scale={0}:trunc(ow/a/2)*2", widthParam));
                    }
                }

                // If a fixed height was requested
                else if (requestedHeight.HasValue)
                {
                    var heightParam = requestedHeight.Value.ToString(_usCulture);

                    if (isExynosV4L2)
                    {
                        filters.Add(string.Format("scale=trunc(oh*a/64)*64:{0}", heightParam));
                    }
                    else
                    {
                        filters.Add(string.Format("scale=trunc(oh*a/2)*2:{0}", heightParam));
                    }
                }

                // If a max width was requested
                else if (requestedMaxWidth.HasValue)
                {
                    var maxWidthParam = requestedMaxWidth.Value.ToString(_usCulture);

                    if (isExynosV4L2)
                    {
                        filters.Add(string.Format("scale=trunc(min(max(iw\\,ih*dar)\\,{0})/64)*64:trunc(ow/dar/2)*2", maxWidthParam));
                    }
                    else
                    {
                        filters.Add(string.Format("scale=trunc(min(max(iw\\,ih*dar)\\,{0})/2)*2:trunc(ow/dar/2)*2", maxWidthParam));
                    }
                }

                // If a max height was requested
                else if (requestedMaxHeight.HasValue)
                {
                    var maxHeightParam = requestedMaxHeight.Value.ToString(_usCulture);

                    if (isExynosV4L2)
                    {
                        filters.Add(string.Format("scale=trunc(oh*a/64)*64:min(max(iw/dar\\,ih)\\,{0})", maxHeightParam));
                    }
                    else
                    {
                        filters.Add(string.Format("scale=trunc(oh*a/2)*2:min(max(iw/dar\\,ih)\\,{0})", maxHeightParam));
                    }
                }
            }

            return filters;
        }

        private string GetFixedSizeScalingFilter(Video3DFormat? threedFormat, int requestedWidth, int requestedHeight)
        {
            var widthParam = requestedWidth.ToString(_usCulture);
            var heightParam = requestedHeight.ToString(_usCulture);

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

            return string.Format(filter, widthParam, heightParam);
        }

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it
        /// </summary>
        public string GetOutputSizeParam(EncodingJobInfo state,
            EncodingOptions options,
            string outputVideoCodec,
            bool allowTimeStampCopy = true)
        {
            // http://sonnati.wordpress.com/2012/10/19/ffmpeg-the-swiss-army-knife-of-internet-streaming-part-vi/

            var request = state.BaseRequest;

            var filters = new List<string>();

            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("format=nv12|vaapi");
                filters.Add("hwupload");
            }

            if (state.DeInterlace("h264", true) && string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add(string.Format("deinterlace_vaapi"));
            }

            var videoStream = state.VideoStream;

            if (state.DeInterlace("h264", true) && !string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                var inputFramerate = videoStream == null ? null : videoStream.RealFrameRate;

                // If it is already 60fps then it will create an output framerate that is much too high for roku and others to handle
                if (string.Equals(options.DeinterlaceMethod, "bobandweave", StringComparison.OrdinalIgnoreCase) && (inputFramerate ?? 60) <= 30)
                {
                    filters.Add("yadif=1:-1:0");
                }
                else
                {
                    filters.Add("yadif=0:-1:0");
                }
            }

            var inputWidth = videoStream == null ? null : videoStream.Width;
            var inputHeight = videoStream == null ? null : videoStream.Height;
            var threeDFormat = state.MediaSource.Video3DFormat;

            var videoDecoder = GetVideoDecoder(state, options);

            filters.AddRange(GetScalingFilters(inputWidth, inputHeight, threeDFormat, videoDecoder, outputVideoCodec, request.Width, request.Height, request.MaxWidth, request.MaxHeight));

            var output = string.Empty;

            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
            {
                var subParam = GetTextSubtitleParam(state);

                filters.Add(subParam);

                if (allowTimeStampCopy)
                {
                    output += " -copyts";
                }
            }

            if (filters.Count > 0)
            {
                output += string.Format(" -vf \"{0}\"", string.Join(",", filters.ToArray()));
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
                // -threads	number of threads to use for encoding, can't be 0 [auto] with VP8	(recommended value : number of real cores - 1)
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

            if (state.AudioStream != null && CanStreamCopyAudio(state, state.AudioStream, state.SupportedAudioCodecs))
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

        public static string GetProbeSizeArgument(int numInputFiles)
        {
            return numInputFiles > 1 ? "-probesize 1G" : "";
        }

        public static string GetAnalyzeDurationArgument(int numInputFiles)
        {
            return numInputFiles > 1 ? "-analyzeduration 200M" : "";
        }

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
            if (state.GenPtsInput)
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
                inputModifier += " -fflags " + string.Join("", flags.ToArray());
            }

            var videoDecoder = GetVideoDecoder(state, encodingOptions);
            if (!string.IsNullOrEmpty(videoDecoder))
            {
                inputModifier += " " + videoDecoder;

                var videoStream = state.VideoStream;
                var inputWidth = videoStream == null ? null : videoStream.Width;
                var inputHeight = videoStream == null ? null : videoStream.Height;
                var request = state.BaseRequest;

                var fixedOutputSize = GetFixedOutputSize(inputWidth, inputHeight, request.Width, request.Height, request.MaxWidth, request.MaxHeight);

                if ((videoDecoder ?? string.Empty).IndexOf("_cuvid", StringComparison.OrdinalIgnoreCase) != -1 && fixedOutputSize.Item1.HasValue && fixedOutputSize.Item2.HasValue)
                {
                    inputModifier += string.Format(" -resize {0}x{1}", fixedOutputSize.Item1.Value.ToString(_usCulture), fixedOutputSize.Item2.Value.ToString(_usCulture));
                }
            }

            if (state.IsVideoRequest)
            {
                var outputVideoCodec = GetVideoEncoder(state, encodingOptions);

                // Important: If this is ever re-enabled, make sure not to use it with wtv because it breaks seeking
                if (!string.Equals(state.InputContainer, "wtv", StringComparison.OrdinalIgnoreCase) &&
                    state.TranscodingType != TranscodingJobType.Progressive &&
                    state.EnableBreakOnNonKeyFrames(outputVideoCodec))
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
                inputModifier += " -stream_loop -1";
            }

            return inputModifier;
        }


        public void AttachMediaSourceInfo(EncodingJobInfo state,
          MediaSourceInfo mediaSource,
          string requestedUrl)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }
            if (mediaSource == null)
            {
                throw new ArgumentNullException("mediaSource");
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

            if (state.ReadInputAtNativeFramerate ||
                mediaSource.Protocol == MediaProtocol.File && string.Equals(mediaSource.Container, "wtv", StringComparison.OrdinalIgnoreCase))
            {
                state.InputVideoSync = "-1";
                state.InputAudioSync = "1";
            }

            if (string.Equals(mediaSource.Container, "wma", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mediaSource.Container, "asf", StringComparison.OrdinalIgnoreCase))
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
                        requestedUrl = "test." + videoRequest.OutputContainer;
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
        protected string GetVideoDecoder(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            if (string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return GetVideoDecoder(state.MediaSource.VideoType ?? VideoType.VideoFile, state.VideoStream, encodingOptions);
        }

        public string GetVideoDecoder(VideoType videoType, MediaStream videoStream, EncodingOptions encodingOptions)
        {
            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is expiremental anyway, it's not worth adding additional variables such as this.
            if (videoType != VideoType.VideoFile)
            {
                return null;
            }

            if (videoStream != null &&
                !string.IsNullOrEmpty(videoStream.Codec) &&
                !string.IsNullOrEmpty(encodingOptions.HardwareAccelerationType))
            {
                if (string.Equals(encodingOptions.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLower())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_qsv") && encodingOptions.HardwareDecodingCodecs.Contains("h264", StringComparer.OrdinalIgnoreCase))
                            {
                                // qsv decoder does not support 10-bit input
                                if ((videoStream.BitDepth ?? 8) > 8)
                                {
                                    return null;
                                }
                                return "-c:v h264_qsv ";
                            }
                            break;
                        case "hevc":
                        case "h265":
                            if (_mediaEncoder.SupportsDecoder("hevc_qsv") && encodingOptions.HardwareDecodingCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase))
                            {
                                //return "-c:v hevc_qsv -load_plugin hevc_hw ";
                                return "-c:v hevc_qsv ";
                            }
                            break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("mpeg2_qsv") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg2video", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg2_qsv ";
                            }
                            break;
                        case "vc1":
                            if (_mediaEncoder.SupportsDecoder("vc1_qsv") && encodingOptions.HardwareDecodingCodecs.Contains("vc1", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vc1_qsv ";
                            }
                            break;
                    }
                }

                else if (string.Equals(encodingOptions.HardwareAccelerationType, "nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLower())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("h264", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v h264_cuvid ";
                            }
                            break;
                        case "hevc":
                        case "h265":
                            if (_mediaEncoder.SupportsDecoder("hevc_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v hevc_cuvid ";
                            }
                            break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("mpeg2_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg2video", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg2_cuvid ";
                            }
                            break;
                        case "vc1":
                            if (_mediaEncoder.SupportsDecoder("vc1_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("vc1", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vc1_cuvid ";
                            }
                            break;
                        case "mpeg4":
                            if (_mediaEncoder.SupportsDecoder("mpeg4_cuvid") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg4", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg4_cuvid ";
                            }
                            break;
                    }
                }

                else if (string.Equals(encodingOptions.HardwareAccelerationType, "mediacodec", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLower())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("h264", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v h264_mediacodec ";
                            }
                            break;
                        case "hevc":
                        case "h265":
                            if (_mediaEncoder.SupportsDecoder("hevc_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("hevc", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v hevc_mediacodec ";
                            }
                            break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("mpeg2_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg2video", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg2_mediacodec ";
                            }
                            break;
                        case "mpeg4":
                            if (_mediaEncoder.SupportsDecoder("mpeg4_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg4", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg4_mediacodec ";
                            }
                            break;
                        case "vp8":
                            if (_mediaEncoder.SupportsDecoder("vp8_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("vp8", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vp8_mediacodec ";
                            }
                            break;
                        case "vp9":
                            if (_mediaEncoder.SupportsDecoder("vp9_mediacodec") && encodingOptions.HardwareDecodingCodecs.Contains("vp9", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v vp9_mediacodec ";
                            }
                            break;
                    }
                }

                else if (string.Equals(encodingOptions.HardwareAccelerationType, "omx", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLower())
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
                    }
                }

                else if (string.Equals(encodingOptions.HardwareAccelerationType, "amf", StringComparison.OrdinalIgnoreCase))
                {
                    switch (videoStream.Codec.ToLower())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_amf") && encodingOptions.HardwareDecodingCodecs.Contains("h264", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v h264_amf";
                            }
                            break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("hevc_amf") && encodingOptions.HardwareDecodingCodecs.Contains("mpeg2video", StringComparer.OrdinalIgnoreCase))
                            {
                                return "-c:v mpeg2_mmal";
                            }
                            break;
                    }
                }
            }

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

            var args = " -codec:s:0 " + codec;

            args += " -disposition:s:0 default";

            return args;
        }

        public string GetProgressiveVideoFullCommandLine(EncodingJobInfo state, EncodingOptions encodingOptions, string outputPath, string defaultH264Preset)
        {
            // Get the output codec name
            var videoCodec = GetVideoEncoder(state, encodingOptions);

            var format = string.Empty;
            var keyFrame = string.Empty;

            if (string.Equals(Path.GetExtension(outputPath), ".mp4", StringComparison.OrdinalIgnoreCase) &&
                state.BaseRequest.Context == EncodingContext.Streaming)
            {
                // Comparison: https://github.com/jansmolders86/mediacenterjs/blob/master/lib/transcoding/desktop.js
                format = " -f mp4 -movflags frag_keyframe+empty_moov";
            }

            var threads = GetNumberOfThreads(state, encodingOptions, videoCodec);

            var inputModifier = GetInputModifier(state, encodingOptions);

            return string.Format("{0} {1}{2} {3} {4} -map_metadata -1 -map_chapters -1 -threads {5} {6}{7}{8} -y \"{9}\"",
                inputModifier,
                GetInputArgument(state, encodingOptions),
                keyFrame,
                GetMapArgs(state),
                GetProgressiveVideoArguments(state, encodingOptions, videoCodec, defaultH264Preset),
                threads,
                GetProgressiveVideoAudioArguments(state, encodingOptions),
                GetSubtitleEmbedArguments(state),
                format,
                outputPath
                ).Trim();
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
                return " -fflags " + string.Join("", flags.ToArray());
            }

            return string.Empty;
        }

        public string GetProgressiveVideoArguments(EncodingJobInfo state, EncodingOptions encodingOptions, string videoCodec, string defaultH264Preset)
        {
            var args = "-codec:v:0 " + videoCodec;

            if (state.BaseRequest.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            if (string.Equals(videoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                if (state.VideoStream != null && IsH264(state.VideoStream) &&
                    string.Equals(state.OutputContainer, "ts", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
                {
                    args += " -bsf:v h264_mp4toannexb";
                }

                if (state.RunTimeTicks.HasValue && state.BaseRequest.CopyTimestamps)
                {
                    args += " -copyts -avoid_negative_ts disabled -start_at_zero";
                }

                if (!state.RunTimeTicks.HasValue)
                {
                    args += " -flags -global_header -fflags +genpts";
                }
            }
            else
            {
                var keyFrameArg = string.Format(" -force_key_frames \"expr:gte(t,n_forced*{0})\"",
                    5.ToString(_usCulture));

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

                if (state.RunTimeTicks.HasValue && state.BaseRequest.CopyTimestamps)
                {
                    if (!hasCopyTs)
                    {
                        args += " -copyts";
                    }
                    args += " -avoid_negative_ts disabled -start_at_zero";
                }

                // This is for internal graphical subs
                if (hasGraphicalSubs)
                {
                    args += GetGraphicalSubtitleParam(state, encodingOptions, videoCodec);
                }

                var qualityParam = GetVideoQualityParam(state, videoCodec, encodingOptions, defaultH264Preset);

                if (!string.IsNullOrEmpty(qualityParam))
                {
                    args += " " + qualityParam.Trim();
                }

                if (!state.RunTimeTicks.HasValue)
                {
                    args += " -flags -global_header";
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

            if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
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

            var albumCoverInput = string.Empty;
            var mapArgs = string.Empty;
            var metadata = string.Empty;
            var vn = string.Empty;

            var hasArt = !string.IsNullOrEmpty(state.AlbumCoverPath);
            hasArt = false;

            if (hasArt)
            {
                albumCoverInput = " -i \"" + state.AlbumCoverPath + "\"";
                mapArgs = " -map 0:a -map 1:v -c:1:v copy";
                metadata = " -metadata:s:v title=\"Album cover\" -metadata:s:v comment=\"Cover(Front)\"";
            }
            else
            {
                vn = " -vn";
            }

            var threads = GetNumberOfThreads(state, encodingOptions, null);

            var inputModifier = GetInputModifier(state, encodingOptions);

            return string.Format("{0} {1}{7}{8} -threads {2}{3} {4} -id3v2_version 3 -write_id3v1 1{6} -y \"{5}\"",
                inputModifier,
                GetInputArgument(state, encodingOptions),
                threads,
                vn,
                string.Join(" ", audioTranscodeParams.ToArray()),
                outputPath,
                metadata,
                albumCoverInput,
                mapArgs).Trim();
        }

    }
}
