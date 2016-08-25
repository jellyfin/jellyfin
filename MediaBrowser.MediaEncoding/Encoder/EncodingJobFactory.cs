using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class EncodingJobFactory
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IConfigurationManager _config;

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        public EncodingJobFactory(ILogger logger, ILibraryManager libraryManager, IMediaSourceManager mediaSourceManager, IConfigurationManager config)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
            _config = config;
        }

        public async Task<EncodingJob> CreateJob(EncodingJobOptions options, bool isVideoRequest, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var request = options;

            if (string.IsNullOrEmpty(request.AudioCodec))
            {
                request.AudioCodec = InferAudioCodec(request.OutputContainer);
            }

            var state = new EncodingJob(_logger, _mediaSourceManager)
            {
                Options = options,
                IsVideoRequest = isVideoRequest,
                Progress = progress
            };

            if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                state.SupportedAudioCodecs = request.AudioCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault();
            }

            var item = _libraryManager.GetItemById(request.ItemId);
            state.ItemType = item.GetType().Name;

            state.IsInputVideo = string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);

            var primaryImage = item.GetImageInfo(ImageType.Primary, 0) ??
                               item.Parents.Select(i => i.GetImageInfo(ImageType.Primary, 0)).FirstOrDefault(i => i != null);

            if (primaryImage != null)
            {
                state.AlbumCoverPath = primaryImage.Path;
            }

            var mediaSources = await _mediaSourceManager.GetPlayackMediaSources(request.ItemId, null, false, new[] { MediaType.Audio, MediaType.Video }, cancellationToken).ConfigureAwait(false);

            var mediaSource = string.IsNullOrEmpty(request.MediaSourceId)
               ? mediaSources.First()
               : mediaSources.First(i => string.Equals(i.Id, request.MediaSourceId));

            var videoRequest = state.Options;

            AttachMediaSourceInfo(state, mediaSource, videoRequest);

            //var container = Path.GetExtension(state.RequestedUrl);

            //if (string.IsNullOrEmpty(container))
            //{
            //    container = request.Static ?
            //        state.InputContainer :
            //        (Path.GetExtension(GetOutputFilePath(state)) ?? string.Empty).TrimStart('.');
            //}

            //state.OutputContainer = (container ?? string.Empty).TrimStart('.');

            state.OutputAudioBitrate = GetAudioBitrateParam(state.Options, state.AudioStream);
            state.OutputAudioSampleRate = request.AudioSampleRate;

            state.OutputAudioCodec = state.Options.AudioCodec;

            state.OutputAudioChannels = GetNumAudioChannelsParam(state.Options, state.AudioStream, state.OutputAudioCodec);

            if (videoRequest != null)
            {
                state.OutputVideoCodec = state.Options.VideoCodec;
                state.OutputVideoBitrate = GetVideoBitrateParamValue(state.Options, state.VideoStream, state.OutputVideoCodec);

                if (state.OutputVideoBitrate.HasValue)
                {
                    var resolution = ResolutionNormalizer.Normalize(
                        state.VideoStream == null ? (int?)null : state.VideoStream.BitRate,
                        state.OutputVideoBitrate.Value,
                        state.VideoStream == null ? null : state.VideoStream.Codec,
                        state.OutputVideoCodec,
                        videoRequest.MaxWidth,
                        videoRequest.MaxHeight);

                    videoRequest.MaxWidth = resolution.MaxWidth;
                    videoRequest.MaxHeight = resolution.MaxHeight;
                }
            }

            ApplyDeviceProfileSettings(state);

            if (videoRequest != null)
            {
                TryStreamCopy(state, videoRequest);
            }

            //state.OutputFilePath = GetOutputFilePath(state);

            return state;
        }

        internal static void TryStreamCopy(EncodingJob state,
            EncodingJobOptions videoRequest)
        {
            if (state.IsVideoRequest)
            {
                if (state.VideoStream != null && CanStreamCopyVideo(videoRequest, state.VideoStream))
                {
                    state.OutputVideoCodec = "copy";
                }

                if (state.AudioStream != null && CanStreamCopyAudio(videoRequest, state.AudioStream, state.SupportedAudioCodecs))
                {
                    state.OutputAudioCodec = "copy";
                }
            }
        }

        internal static void AttachMediaSourceInfo(EncodingJob state,
            MediaSourceInfo mediaSource,
            EncodingJobOptions videoRequest)
        {
            state.MediaPath = mediaSource.Path;
            state.InputProtocol = mediaSource.Protocol;
            state.InputContainer = mediaSource.Container;
            state.InputFileSize = mediaSource.Size;
            state.InputBitrate = mediaSource.Bitrate;
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
            state.InputBitrate = mediaSource.Bitrate;
            state.InputFileSize = mediaSource.Size;
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

            if (videoRequest != null)
            {
                if (string.IsNullOrEmpty(videoRequest.VideoCodec))
                {
                    videoRequest.VideoCodec = InferVideoCodec(videoRequest.OutputContainer);
                }

                state.VideoStream = GetMediaStream(mediaStreams, videoRequest.VideoStreamIndex, MediaStreamType.Video);
                state.SubtitleStream = GetMediaStream(mediaStreams, videoRequest.SubtitleStreamIndex, MediaStreamType.Subtitle, false);
                state.AudioStream = GetMediaStream(mediaStreams, videoRequest.AudioStreamIndex, MediaStreamType.Audio);

                if (state.SubtitleStream != null && !state.SubtitleStream.IsExternal)
                {
                    state.InternalSubtitleStreamOffset = mediaStreams.Where(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal).ToList().IndexOf(state.SubtitleStream);
                }

                if (state.VideoStream != null && state.VideoStream.IsInterlaced)
                {
                    state.DeInterlace = true;
                }

                EnforceResolutionLimit(state, videoRequest);
            }
            else
            {
                state.AudioStream = GetMediaStream(mediaStreams, null, MediaStreamType.Audio, true);
            }

            state.MediaSource = mediaSource;
        }

        protected EncodingOptions GetEncodingOptions()
        {
            return _config.GetConfiguration<EncodingOptions>("encoding");
        }

        /// <summary>
        /// Infers the video codec.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>System.Nullable{VideoCodecs}.</returns>
        private static string InferVideoCodec(string container)
        {
            var ext = "." + (container ?? string.Empty);

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

        private string InferAudioCodec(string container)
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
        /// Determines which stream will be used for playback
        /// </summary>
        /// <param name="allStream">All stream.</param>
        /// <param name="desiredIndex">Index of the desired.</param>
        /// <param name="type">The type.</param>
        /// <param name="returnFirstIfNoIndex">if set to <c>true</c> [return first if no index].</param>
        /// <returns>MediaStream.</returns>
        private static MediaStream GetMediaStream(IEnumerable<MediaStream> allStream, int? desiredIndex, MediaStreamType type, bool returnFirstIfNoIndex = true)
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

            if (type == MediaStreamType.Video)
            {
                streams = streams.Where(i => !string.Equals(i.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase)).ToList();
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
        /// Enforces the resolution limit.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoRequest">The video request.</param>
        private static void EnforceResolutionLimit(EncodingJob state, EncodingJobOptions videoRequest)
        {
            // Switch the incoming params to be ceilings rather than fixed values
            videoRequest.MaxWidth = videoRequest.MaxWidth ?? videoRequest.Width;
            videoRequest.MaxHeight = videoRequest.MaxHeight ?? videoRequest.Height;

            videoRequest.Width = null;
            videoRequest.Height = null;
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="outputAudioCodec">The output audio codec.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private int? GetNumAudioChannelsParam(EncodingJobOptions request, MediaStream audioStream, string outputAudioCodec)
        {
            var inputChannels = audioStream == null
                ? null
                : audioStream.Channels;

            if (inputChannels <= 0)
            {
                inputChannels = null;
            }

            var codec = outputAudioCodec ?? string.Empty;

            if (codec.IndexOf("wma", StringComparison.OrdinalIgnoreCase) != -1)
            {
                // wmav2 currently only supports two channel output
                return Math.Min(2, inputChannels ?? 2);
            }

            if (request.MaxAudioChannels.HasValue)
            {
                var channelLimit = codec.IndexOf("mp3", StringComparison.OrdinalIgnoreCase) != -1
                   ? 2
                   : 6;

                if (inputChannels.HasValue)
                {
                    channelLimit = Math.Min(channelLimit, inputChannels.Value);
                }

                // If we don't have any media info then limit it to 5 to prevent encoding errors due to asking for too many channels
                return Math.Min(request.MaxAudioChannels.Value, channelLimit);
            }

            return request.AudioChannels;
        }

        private int? GetVideoBitrateParamValue(EncodingJobOptions request, MediaStream videoStream, string outputVideoCodec)
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

        protected string GetVideoBitrateParam(EncodingJob state, string videoCodec, bool isHls)
        {
            var bitrate = state.OutputVideoBitrate;

            if (bitrate.HasValue)
            {
                if (string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase))
                {
                    // With vpx when crf is used, b:v becomes a max rate
                    // https://trac.ffmpeg.org/wiki/vpxEncodingGuide. But higher bitrate source files -b:v causes judder so limite the bitrate but dont allow it to "saturate" the bitrate. So dont contrain it down just up.
                    return string.Format(" -maxrate:v {0} -bufsize:v ({0}*2) -b:v {0}", bitrate.Value.ToString(UsCulture));
                }

                if (string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format(" -b:v {0}", bitrate.Value.ToString(UsCulture));
                }

                // h264
                return string.Format(" -b:v {0} -maxrate {0} -bufsize {1}",
                    bitrate.Value.ToString(UsCulture),
                    (bitrate.Value * 2).ToString(UsCulture));
            }

            return string.Empty;
        }

        private int? GetAudioBitrateParam(EncodingJobOptions request, MediaStream audioStream)
        {
            if (request.AudioBitRate.HasValue)
            {
                // Make sure we don't request a bitrate higher than the source
                var currentBitrate = audioStream == null ? request.AudioBitRate.Value : audioStream.BitRate ?? request.AudioBitRate.Value;

                return request.AudioBitRate.Value;
                //return Math.Min(currentBitrate, request.AudioBitRate.Value);
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified stream is H264.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns><c>true</c> if the specified stream is H264; otherwise, <c>false</c>.</returns>
        protected bool IsH264(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1 ||
                   codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>
        /// Gets the name of the output audio codec
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        internal static string GetAudioEncoder(EncodingJob state)
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
        /// Gets the name of the output video codec
        /// </summary>
        /// <returns>System.String.</returns>
        internal static string GetVideoEncoder(IMediaEncoder mediaEncoder, EncodingJob state, EncodingOptions options)
        {
            var codec = state.OutputVideoCodec;

            if (!string.IsNullOrEmpty(codec))
            {
                if (string.Equals(codec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    return GetH264Encoder(mediaEncoder, state, options);
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

        private static string GetAvailableEncoder(IMediaEncoder mediaEncoder, string preferredEncoder, string defaultEncoder)
        {
            if (mediaEncoder.SupportsEncoder(preferredEncoder))
            {
                return preferredEncoder;
            }
            return defaultEncoder;
        }

        internal static string GetH264Encoder(IMediaEncoder mediaEncoder, EncodingJob state, EncodingOptions options)
        {
            var defaultEncoder = "libx264";

            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is expiremental anyway, it's not worth adding additional variables such as this.
            if (state.VideoType == VideoType.VideoFile)
            {
                var hwType = options.HardwareAccelerationType;

                if (string.Equals(hwType, "qsv", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(hwType, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder(mediaEncoder, "h264_qsv", defaultEncoder);
                }

                if (string.Equals(hwType, "nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder(mediaEncoder, "h264_nvenc", defaultEncoder);
                }
                if (string.Equals(hwType, "h264_omx", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder(mediaEncoder, "h264_omx", defaultEncoder);
                }
                if (string.Equals(hwType, "vaapi", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(options.VaapiDevice))
                {
                    return GetAvailableEncoder(mediaEncoder, "h264_vaapi", defaultEncoder);
                }
            }

            return defaultEncoder;
        }

        internal static bool CanStreamCopyVideo(EncodingJobOptions request, MediaStream videoStream)
        {
            if (videoStream.IsInterlaced)
            {
                return false;
            }

            if (videoStream.IsAnamorphic ?? false)
            {
                return false;
            }

            // Can't stream copy if we're burning in subtitles
            if (request.SubtitleStreamIndex.HasValue)
            {
                if (request.SubtitleMethod == SubtitleDeliveryMethod.Encode)
                {
                    return false;
                }
            }

            // Source and target codecs must match
            if (!string.Equals(request.VideoCodec, videoStream.Codec, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
            {
                if (videoStream.IsAVC.HasValue && !videoStream.IsAVC.Value)
                {
                    return false;
                }
            }

            // If client is requesting a specific video profile, it must match the source
            if (!string.IsNullOrEmpty(request.Profile))
            {
                if (string.IsNullOrEmpty(videoStream.Profile))
                {
                    return false;
                }

                if (!string.Equals(request.Profile, videoStream.Profile, StringComparison.OrdinalIgnoreCase))
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
            if (request.Level.HasValue)
            {
                if (!videoStream.Level.HasValue)
                {
                    return false;
                }

                if (videoStream.Level.Value > request.Level.Value)
                {
                    return false;
                }
            }

            return request.EnableAutoStreamCopy;
        }

        private static int GetVideoProfileScore(string profile)
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

        internal static bool CanStreamCopyAudio(EncodingJobOptions request, MediaStream audioStream, List<string> supportedAudioCodecs)
        {
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

        private void ApplyDeviceProfileSettings(EncodingJob state)
        {
            var profile = state.Options.DeviceProfile;

            if (profile == null)
            {
                // Don't use settings from the default profile. 
                // Only use a specific profile if it was requested.
                return;
            }

            var audioCodec = state.ActualOutputAudioCodec;

            var videoCodec = state.ActualOutputVideoCodec;
            var outputContainer = state.Options.OutputContainer;

            var mediaProfile = state.IsVideoRequest ?
                profile.GetAudioMediaProfile(outputContainer, audioCodec, state.OutputAudioChannels, state.OutputAudioBitrate) :
                profile.GetVideoMediaProfile(outputContainer,
                audioCodec,
                videoCodec,
                state.OutputWidth,
                state.OutputHeight,
                state.TargetVideoBitDepth,
                state.OutputVideoBitrate,
                state.TargetVideoProfile,
                state.TargetVideoLevel,
                state.TargetFramerate,
                state.TargetPacketLength,
                state.TargetTimestamp,
                state.IsTargetAnamorphic,
                state.TargetRefFrames,
                state.TargetVideoStreamCount,
                state.TargetAudioStreamCount,
                state.TargetVideoCodecTag);

            if (mediaProfile != null)
            {
                state.MimeType = mediaProfile.MimeType;
            }

            var transcodingProfile = state.IsVideoRequest ?
                profile.GetAudioTranscodingProfile(outputContainer, audioCodec) :
                profile.GetVideoTranscodingProfile(outputContainer, audioCodec, videoCodec);

            if (transcodingProfile != null)
            {
                state.EstimateContentLength = transcodingProfile.EstimateContentLength;
                state.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;
                state.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

                state.Options.CopyTimestamps = transcodingProfile.CopyTimestamps;
            }
        }
    }
}
