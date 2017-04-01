using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class EncodingHelper
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly ISubtitleEncoder _subtitleEncoder;

        public EncodingHelper(IMediaEncoder mediaEncoder, IServerConfigurationManager config, IFileSystem fileSystem, ISubtitleEncoder subtitleEncoder)
        {
            _mediaEncoder = mediaEncoder;
            _config = config;
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

                if (string.Equals(hwType, "qsv", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(hwType, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_qsv", defaultEncoder);
                }

                if (string.Equals(hwType, "nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_nvenc", defaultEncoder);
                }
                if (string.Equals(hwType, "h264_omx", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_omx", defaultEncoder);
                }
                if (string.Equals(hwType, "vaapi", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(encodingOptions.VaapiDevice))
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

            if (!string.IsNullOrWhiteSpace(useragent))
            {
                return "-user-agent \"" + useragent + "\"";
            }

            return string.Empty;
        }

        public string GetInputFormat(string container)
        {
            if (string.Equals(container, "mkv", StringComparison.OrdinalIgnoreCase))
            {
                return "matroska";
            }
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

            return codec;
        }

        /// <summary>
        /// Infers the audio codec based on the url
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Nullable{AudioCodecs}.</returns>
        public string InferAudioCodec(string url)
        {
            var ext = Path.GetExtension(url);

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
            var list = new List<string>
            {
                "Constrained Baseline",
                "Baseline",
                "Extended",
                "Main",
                "High",
                "Progressive High",
                "Constrained High"
            };

            return Array.FindIndex(list.ToArray(), t => string.Equals(t, profile, StringComparison.OrdinalIgnoreCase));
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

            return codec.ToLower();
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        public string GetInputArgument(EncodingJobInfo state, EncodingOptions encodingOptions)
        {
            var request = state.BaseRequest;

            var arg = string.Format("-i {0}", GetInputPathArgument(state));

            if (state.SubtitleStream != null && request.SubtitleMethod == SubtitleDeliveryMethod.Encode)
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
                    var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && request.SubtitleMethod == SubtitleDeliveryMethod.Encode;
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
                    // https://trac.ffmpeg.org/wiki/vpxEncodingGuide. But higher bitrate source files -b:v causes judder so limite the bitrate but dont allow it to "saturate" the bitrate. So dont contrain it down just up.
                    return string.Format(" -maxrate:v {0} -bufsize:v ({0}*2) -b:v {0}", bitrate.Value.ToString(_usCulture));
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
            double requestLevel;

            // Clients may direct play higher than level 41, but there's no reason to transcode higher
            if (double.TryParse(level, NumberStyles.Any, _usCulture, out requestLevel))
            {
                if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    if (requestLevel > 41)
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

            var setPtsParam = state.CopyTimestamps
                ? string.Empty
                : string.Format(",setpts=PTS -{0}/TB", seconds.ToString(_usCulture));

            if (state.SubtitleStream.IsExternal)
            {
                var subtitlePath = state.SubtitleStream.Path;

                var charsetParam = string.Empty;

                if (!string.IsNullOrEmpty(state.SubtitleStream.Language))
                {
                    var charenc = _subtitleEncoder.GetSubtitleFileCharacterSet(subtitlePath, state.SubtitleStream.Language, state.MediaSource.Protocol, CancellationToken.None).Result;

                    if (!string.IsNullOrEmpty(charenc))
                    {
                        charsetParam = ":charenc=" + charenc;
                    }
                }

                // TODO: Perhaps also use original_size=1920x800 ??
                return string.Format("subtitles=filename='{0}'{1}{2}",
                    _mediaEncoder.EscapeSubtitleFilterPath(subtitlePath),
                    charsetParam,
                    setPtsParam);
            }

            var mediaPath = state.MediaPath ?? string.Empty;

            return string.Format("subtitles='{0}:si={1}'{2}",
                _mediaEncoder.EscapeSubtitleFilterPath(mediaPath),
                state.InternalSubtitleStreamOffset.ToString(_usCulture),
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
                if (!string.IsNullOrWhiteSpace(encodingOptions.H264Preset))
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
                param += "-preset 7 -look_ahead 0";

            }

            // h264 (h264_nvenc)
            else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase))
            {
                param += "-preset default";
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

            if (!string.IsNullOrEmpty(state.OutputVideoSync))
            {
                param += " -vsync " + state.OutputVideoSync;
            }

            var request = state.BaseRequest;

            if (!string.IsNullOrEmpty(request.Profile))
            {
                if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    // not supported by h264_omx
                    param += " -profile:v " + request.Profile;
                }
            }

            if (!string.IsNullOrEmpty(request.Level))
            {
                var level = NormalizeTranscodingLevel(state.OutputVideoCodec, request.Level);

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
                if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase)){
                    param += "";
                }
                else if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
                
            }

            if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                param += " -x264opts:0 subme=0:rc_lookahead=10:me_range=4:me=dia:no_chroma_me:8x8dct=0:partitions=none";
            }

            if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                param = "-pix_fmt yuv420p " + param;
            }

            return param;
        }

        public bool CanStreamCopyVideo(EncodingJobInfo state, MediaStream videoStream)
        {
            if (!videoStream.AllowStreamCopy)
            {
                return false;
            }

            var request = state.BaseRequest;

            if (!request.AllowVideoStreamCopy)
            {
                return false;
            }

            if (videoStream.IsInterlaced)
            {
                if (request.DeInterlace)
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
                if (request.SubtitleMethod == SubtitleDeliveryMethod.Encode)
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

            // If client is requesting a specific video profile, it must match the source
            if (!string.IsNullOrEmpty(request.Profile))
            {
                if (string.IsNullOrEmpty(videoStream.Profile))
                {
                    //return false;
                }

                if (!string.IsNullOrEmpty(videoStream.Profile) && !string.Equals(request.Profile, videoStream.Profile, StringComparison.OrdinalIgnoreCase))
                {
                    var currentScore = GetVideoProfileScore(videoStream.Profile);
                    var requestedScore = GetVideoProfileScore(request.Profile);

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

            if (request.MaxVideoBitDepth.HasValue)
            {
                if (videoStream.BitDepth.HasValue && videoStream.BitDepth.Value > request.MaxVideoBitDepth.Value)
                {
                    return false;
                }
            }

            if (request.MaxRefFrames.HasValue)
            {
                if (videoStream.RefFrames.HasValue && videoStream.RefFrames.Value > request.MaxRefFrames.Value)
                {
                    return false;
                }
            }

            // If a specific level was requested, the source must match or be less than
            if (!string.IsNullOrEmpty(request.Level))
            {
                double requestLevel;

                if (double.TryParse(request.Level, NumberStyles.Any, _usCulture, out requestLevel))
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

            return request.EnableAutoStreamCopy;
        }

        public bool CanStreamCopyAudio(EncodingJobInfo state, MediaStream audioStream, List<string> supportedAudioCodecs)
        {
            if (!audioStream.AllowStreamCopy)
            {
                return false;
            }

            var request = state.BaseRequest;

            if (!request.AllowAudioStreamCopy)
            {
                return false;
            }

            // Source and target codecs must match
            if (string.IsNullOrEmpty(audioStream.Codec) || !supportedAudioCodecs.Contains(audioStream.Codec, StringComparer.OrdinalIgnoreCase))
            {
                return false;
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

            // Channels must fall within requested value
            var channels = request.AudioChannels ?? request.MaxAudioChannels;
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

            return request.EnableAutoStreamCopy;
        }

        public int? GetVideoBitrateParamValue(BaseEncodingJobOptions request, MediaStream videoStream, string outputVideoCodec)
        {
            var bitrate = request.VideoBitRate;

            if (videoStream != null)
            {
                var isUpscaling = request.Height.HasValue && videoStream.Height.HasValue &&
                                   request.Height.Value > videoStream.Height.Value;

                if (request.Width.HasValue && videoStream.Width.HasValue &&
                    request.Width.Value > videoStream.Width.Value)
                {
                    isUpscaling = true;
                }

                // Don't allow bitrate increases unless upscaling
                if (!isUpscaling)
                {
                    if (bitrate.HasValue && videoStream.BitRate.HasValue)
                    {
                        bitrate = Math.Min(bitrate.Value, videoStream.BitRate.Value);
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
            var volParam = string.Empty;
            var audioSampleRate = string.Empty;

            var channels = state.OutputAudioChannels;

            // Boost volume to 200% when downsampling from 6ch to 2ch
            if (channels.HasValue && channels.Value <= 2)
            {
                if (state.AudioStream != null && state.AudioStream.Channels.HasValue && state.AudioStream.Channels.Value > 5 && !encodingOptions.DownMixAudioBoost.Equals(1))
                {
                    volParam = ",volume=" + encodingOptions.DownMixAudioBoost.ToString(_usCulture);
                }
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                audioSampleRate = state.OutputAudioSampleRate.Value + ":";
            }

            var adelay = isHls ? "adelay=1," : string.Empty;

            var pts = string.Empty;

            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.BaseRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode && !state.CopyTimestamps)
            {
                var seconds = TimeSpan.FromTicks(state.StartTimeTicks ?? 0).TotalSeconds;

                pts = string.Format(",asetpts=PTS-{0}/TB", Math.Round(seconds).ToString(_usCulture));
            }

            return string.Format("-af \"{0}aresample={1}async={4}{2}{3}\"",

                adelay,
                audioSampleRate,
                volParam,
                pts,
                state.OutputAudioSync);
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="outputAudioCodec">The output audio codec.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        public int? GetNumAudioChannelsParam(BaseEncodingJobOptions request, MediaStream audioStream, string outputAudioCodec)
        {
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

            int? resultChannels = null;
            if (isTranscodingAudio)
            {
                resultChannels = request.TranscodingMaxAudioChannels;
            }
            resultChannels = resultChannels ?? request.MaxAudioChannels ?? request.AudioChannels;

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

            return resultChannels ?? request.AudioChannels;
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

            var subtitleMethod = state.BaseRequest.SubtitleMethod;
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
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <returns>System.String.</returns>
        public string GetGraphicalSubtitleParam(EncodingJobInfo state, string outputVideoCodec)
        {
            var outputSizeParam = string.Empty;

            var request = state.BaseRequest;

            // Add resolution params, if specified
            if (request.Width.HasValue || request.Height.HasValue || request.MaxHeight.HasValue || request.MaxWidth.HasValue)
            {
                outputSizeParam = GetOutputSizeParam(state, outputVideoCodec).TrimEnd('"');

                if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    outputSizeParam = "," + outputSizeParam.Substring(outputSizeParam.IndexOf("format", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    outputSizeParam = "," + outputSizeParam.Substring(outputSizeParam.IndexOf("scale", StringComparison.OrdinalIgnoreCase));
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
            }

            var mapPrefix = state.SubtitleStream.IsExternal ?
                1 :
                0;

            var subtitleStreamIndex = state.SubtitleStream.IsExternal
                ? 0
                : state.SubtitleStream.Index;

            return string.Format(" -filter_complex \"[{0}:{1}]{4}[sub] ; [0:{2}] [sub] overlay{3}\"",
                mapPrefix.ToString(_usCulture),
                subtitleStreamIndex.ToString(_usCulture),
                state.VideoStream.Index.ToString(_usCulture),
                outputSizeParam,
                videoSizeParam);
        }

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <param name="allowTimeStampCopy">if set to <c>true</c> [allow time stamp copy].</param>
        /// <returns>System.String.</returns>
        public string GetOutputSizeParam(EncodingJobInfo state,
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
            else if (state.DeInterlace && !string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("yadif=0:-1:0");
            }

            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                // Work around vaapi's reduced scaling features
                var scaler = "scale_vaapi";

                // Given the input dimensions (inputWidth, inputHeight), determine the output dimensions
                // (outputWidth, outputHeight). The user may request precise output dimensions or maximum
                // output dimensions. Output dimensions are guaranteed to be even.
                decimal inputWidth = Convert.ToDecimal(state.VideoStream.Width);
                decimal inputHeight = Convert.ToDecimal(state.VideoStream.Height);
                decimal outputWidth = request.Width.HasValue ? Convert.ToDecimal(request.Width.Value) : inputWidth;
                decimal outputHeight = request.Height.HasValue ? Convert.ToDecimal(request.Height.Value) : inputHeight;
                decimal maximumWidth = request.MaxWidth.HasValue ? Convert.ToDecimal(request.MaxWidth.Value) : outputWidth;
                decimal maximumHeight = request.MaxHeight.HasValue ? Convert.ToDecimal(request.MaxHeight.Value) : outputHeight;

                if (outputWidth > maximumWidth || outputHeight > maximumHeight)
                {
                    var scale = Math.Min(maximumWidth / outputWidth, maximumHeight / outputHeight);
                    outputWidth = Math.Min(maximumWidth, Math.Truncate(outputWidth * scale));
                    outputHeight = Math.Min(maximumHeight, Math.Truncate(outputHeight * scale));
                }

                outputWidth = 2 * Math.Truncate(outputWidth / 2);
                outputHeight = 2 * Math.Truncate(outputHeight / 2);

                if (outputWidth != inputWidth || outputHeight != inputHeight)
                {
                    filters.Add(string.Format("{0}=w={1}:h={2}", scaler, outputWidth.ToString(_usCulture), outputHeight.ToString(_usCulture)));
                }
            }
            else
            {
                // If fixed dimensions were supplied
                if (request.Width.HasValue && request.Height.HasValue)
                {
                    var widthParam = request.Width.Value.ToString(_usCulture);
                    var heightParam = request.Height.Value.ToString(_usCulture);

                    filters.Add(string.Format("scale=trunc({0}/2)*2:trunc({1}/2)*2", widthParam, heightParam));
                }

                // If Max dimensions were supplied, for width selects lowest even number between input width and width req size and selects lowest even number from in width*display aspect and requested size
                else if (request.MaxWidth.HasValue && request.MaxHeight.HasValue)
                {
                    var maxWidthParam = request.MaxWidth.Value.ToString(_usCulture);
                    var maxHeightParam = request.MaxHeight.Value.ToString(_usCulture);

                    filters.Add(string.Format("scale=trunc(min(max(iw\\,ih*dar)\\,min({0}\\,{1}*dar))/2)*2:trunc(min(max(iw/dar\\,ih)\\,min({0}/dar\\,{1}))/2)*2", maxWidthParam, maxHeightParam));
                }

                // If a fixed width was requested
                else if (request.Width.HasValue)
                {
                    var widthParam = request.Width.Value.ToString(_usCulture);

                    filters.Add(string.Format("scale={0}:trunc(ow/a/2)*2", widthParam));
                }

                // If a fixed height was requested
                else if (request.Height.HasValue)
                {
                    var heightParam = request.Height.Value.ToString(_usCulture);

                    filters.Add(string.Format("scale=trunc(oh*a/2)*2:{0}", heightParam));
                }

                // If a max width was requested
                else if (request.MaxWidth.HasValue)
                {
                    var maxWidthParam = request.MaxWidth.Value.ToString(_usCulture);

                    filters.Add(string.Format("scale=trunc(min(max(iw\\,ih*dar)\\,{0})/2)*2:trunc(ow/dar/2)*2", maxWidthParam));
                }

                // If a max height was requested
                else if (request.MaxHeight.HasValue)
                {
                    var maxHeightParam = request.MaxHeight.Value.ToString(_usCulture);

                    filters.Add(string.Format("scale=trunc(oh*a/2)*2:min(max(iw/dar\\,ih)\\,{0})", maxHeightParam));
                }
            }

            var output = string.Empty;

            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && request.SubtitleMethod == SubtitleDeliveryMethod.Encode)
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
        /// <returns>System.Int32.</returns>
        public int GetNumberOfThreads(EncodingJobInfo state, EncodingOptions encodingOptions, bool isWebm)
        {
            var threads = GetNumberOfThreadsInternal(state, encodingOptions, isWebm);

            if (state.BaseRequest.CpuCoreLimit.HasValue && state.BaseRequest.CpuCoreLimit.Value > 0)
            {
                threads = Math.Min(threads, state.BaseRequest.CpuCoreLimit.Value);
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

            var numInputFiles = state.PlayableStreamFileNames.Count > 0 ? state.PlayableStreamFileNames.Count : 1;
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

            if (!string.IsNullOrWhiteSpace(probeSizeArgument))
            {
                inputModifier += " " + probeSizeArgument;
            }

            if (!string.IsNullOrWhiteSpace(analyzeDurationArgument))
            {
                inputModifier += " " + analyzeDurationArgument;
            }

            inputModifier = inputModifier.Trim();

            var userAgentParam = GetUserAgentParam(state);

            if (!string.IsNullOrWhiteSpace(userAgentParam))
            {
                inputModifier += " " + userAgentParam;
            }

            inputModifier = inputModifier.Trim();

            inputModifier += " " + GetFastSeekCommandLineParameter(state.BaseRequest);
            inputModifier = inputModifier.Trim();

            //inputModifier += " -fflags +genpts+ignidx+igndts";
            //if (state.IsVideoRequest && genPts)
            //{
            //    inputModifier += " -fflags +genpts";
            //}

            if (!string.IsNullOrEmpty(state.InputAudioSync))
            {
                inputModifier += " -async " + state.InputAudioSync;
            }

            if (!string.IsNullOrEmpty(state.InputVideoSync))
            {
                inputModifier += " -vsync " + state.InputVideoSync;
            }

            if (state.ReadInputAtNativeFramerate)
            {
                inputModifier += " -re";
            }

            var videoDecoder = GetVideoDecoder(state, encodingOptions);
            if (!string.IsNullOrWhiteSpace(videoDecoder))
            {
                inputModifier += " " + videoDecoder;
            }

            if (state.IsVideoRequest)
            {
                // Important: If this is ever re-enabled, make sure not to use it with wtv because it breaks seeking
                if (string.Equals(state.OutputContainer, "mkv", StringComparison.OrdinalIgnoreCase) && state.CopyTimestamps)
                {
                    //inputModifier += " -noaccurate_seek";
                }

                if (!string.IsNullOrWhiteSpace(state.InputContainer) && state.VideoType == VideoType.VideoFile && string.IsNullOrWhiteSpace(encodingOptions.HardwareAccelerationType))
                {
                    var inputFormat = GetInputFormat(state.InputContainer);
                    if (!string.IsNullOrWhiteSpace(inputFormat))
                    {
                        inputModifier += " -f " + inputFormat;
                    }
                }

                // Only do this for video files due to sometimes unpredictable codec names coming from BDInfo
                if (state.RunTimeTicks.HasValue && state.VideoType == VideoType.VideoFile && string.IsNullOrWhiteSpace(encodingOptions.HardwareAccelerationType))
                {
                    foreach (var stream in state.MediaSource.MediaStreams)
                    {
                        if (!stream.IsExternal && stream.Type != MediaStreamType.Subtitle)
                        {
                            if (!string.IsNullOrWhiteSpace(stream.Codec) && stream.Index != -1)
                            {
                                var decoder = GetDecoderFromCodec(stream.Codec);

                                if (!string.IsNullOrWhiteSpace(decoder))
                                {
                                    inputModifier += " -codec:" + stream.Index.ToString(_usCulture) + " " + decoder;
                                }
                            }
                        }
                    }
                }
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

            state.MediaPath = mediaSource.Path;
            state.InputProtocol = mediaSource.Protocol;
            state.InputContainer = mediaSource.Container;
            state.RunTimeTicks = mediaSource.RunTimeTicks;
            state.RemoteHttpHeaders = mediaSource.RequiredHttpHeaders;

            if (mediaSource.VideoType.HasValue)
            {
                state.VideoType = mediaSource.VideoType.Value;
            }

            state.IsoType = mediaSource.IsoType;

            state.PlayableStreamFileNames = mediaSource.PlayableStreamFileNames.ToList();

            if (mediaSource.Timestamp.HasValue)
            {
                state.InputTimestamp = mediaSource.Timestamp.Value;
            }

            state.InputProtocol = mediaSource.Protocol;
            state.MediaPath = mediaSource.Path;
            state.RunTimeTicks = mediaSource.RunTimeTicks;
            state.RemoteHttpHeaders = mediaSource.RequiredHttpHeaders;
            state.ReadInputAtNativeFramerate = mediaSource.ReadAtNativeFramerate;

            if (state.ReadInputAtNativeFramerate ||
                mediaSource.Protocol == MediaProtocol.File && string.Equals(mediaSource.Container, "wtv", StringComparison.OrdinalIgnoreCase))
            {
                state.OutputAudioSync = "1000";
                state.InputVideoSync = "-1";
                state.InputAudioSync = "1";
            }

            if (string.Equals(mediaSource.Container, "wma", StringComparison.OrdinalIgnoreCase))
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
                    if (string.IsNullOrWhiteSpace(requestedUrl))
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

                if (state.VideoStream != null && state.VideoStream.IsInterlaced)
                {
                    state.DeInterlace = true;
                }

                EnforceResolutionLimit(state);
            }
            else
            {
                state.AudioStream = GetMediaStream(mediaStreams, null, MediaStreamType.Audio, true);
            }

            state.MediaSource = mediaSource;
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

            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is expiremental anyway, it's not worth adding additional variables such as this.
            if (state.VideoType != VideoType.VideoFile)
            {
                return null;
            }

            if (state.VideoStream != null && !string.IsNullOrWhiteSpace(state.VideoStream.Codec))
            {
                if (string.Equals(encodingOptions.HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
                {
                    switch (state.MediaSource.VideoStream.Codec.ToLower())
                    {
                        case "avc":
                        case "h264":
                            if (_mediaEncoder.SupportsDecoder("h264_qsv"))
                            {
                                // qsv decoder does not support 10-bit input
                                if ((state.VideoStream.BitDepth ?? 8) > 8)
                                {
                                    return null;
                                }
                                return "-c:v h264_qsv ";
                            }
                            break;
                        //case "hevc":
                        //case "h265":
                        //    if (_mediaEncoder.SupportsDecoder("hevc_qsv"))
                        //    {
                        //        return "-c:v hevc_qsv ";
                        //    }
                        //    break;
                        case "mpeg2video":
                            if (_mediaEncoder.SupportsDecoder("mpeg2_qsv"))
                            {
                                return "-c:v mpeg2_qsv ";
                            }
                            break;
                        case "vc1":
                            if (_mediaEncoder.SupportsDecoder("vc1_qsv"))
                            {
                                return "-c:v vc1_qsv ";
                            }
                            break;
                    }
                }
            }

            // leave blank so ffmpeg will decide
            return null;
        }

        /// <summary>
        /// Gets the number of threads.
        /// </summary>
        /// <returns>System.Int32.</returns>
        private int GetNumberOfThreadsInternal(EncodingJobInfo state, EncodingOptions encodingOptions, bool isWebm)
        {
            var threads = encodingOptions.EncodingThreadCount;

            if (isWebm)
            {
                // Recommended per docs
                return Math.Max(Environment.ProcessorCount - 1, 2);
            }

            // Automatic
            if (threads == -1)
            {
                return 0;
            }

            return threads;
        }

        public string GetSubtitleEmbedArguments(EncodingJobInfo state)
        {
            if (state.SubtitleStream == null || state.SubtitleDeliveryMethod != SubtitleDeliveryMethod.Embed)
            {
                return string.Empty;
            }

            var format = state.SupportedSubtitleCodecs.FirstOrDefault();
            string codec;

            if (string.IsNullOrWhiteSpace(format) || string.Equals(format, state.SubtitleStream.Codec, StringComparison.OrdinalIgnoreCase))
            {
                codec = "copy";
            }
            else
            {
                codec = format;
            }

            // Muxing in dvbsub via either copy or -codec dvbsub does not seem to work
            // It doesn't throw any errors but vlc on android will not render them
            // They will need to be converted to an alternative format
            // TODO: This is incorrectly assuming that dvdsub will be supported by the player
            // The api will need to be expanded to accomodate this.
            if (string.Equals(state.SubtitleStream.Codec, "DVBSUB", StringComparison.OrdinalIgnoreCase))
            {
                codec = "dvdsub";
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

            var threads = GetNumberOfThreads(state, encodingOptions, string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase));

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

        public string GetProgressiveVideoArguments(EncodingJobInfo state, EncodingOptions encodingOptions, string videoCodec, string defaultH264Preset)
        {
            var args = "-codec:v:0 " + videoCodec;

            if (state.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            if (string.Equals(videoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                if (state.VideoStream != null && IsH264(state.VideoStream) && string.Equals(state.OutputContainer, "ts", StringComparison.OrdinalIgnoreCase) && !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
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

                return args;
            }

            var keyFrameArg = string.Format(" -force_key_frames \"expr:gte(t,n_forced*{0})\"",
                5.ToString(_usCulture));

            args += keyFrameArg;

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.BaseRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode;

            var hasCopyTs = false;
            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                var outputSizeParam = GetOutputSizeParam(state, videoCodec);
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

            var qualityParam = GetVideoQualityParam(state, videoCodec, encodingOptions, defaultH264Preset);

            if (!string.IsNullOrEmpty(qualityParam))
            {
                args += " " + qualityParam.Trim();
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetGraphicalSubtitleParam(state, videoCodec);
            }

            if (!state.RunTimeTicks.HasValue)
            {
                args += " -flags -global_header";
            }

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
            
            args += " " + GetAudioFilterParam(state, encodingOptions, false);

            return args;
        }
    }
}
