using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Class StreamInfo.
    /// </summary>
    public class StreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamInfo"/> class.
        /// </summary>
        /// <param name="itemId">The <see cref="Guid"/>.</param>
        /// <param name="mediaType">The <see cref="DlnaProfileType"/>.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
        public StreamInfo(Guid itemId, DlnaProfileType mediaType, DeviceProfile profile)
        {
            ItemId = itemId;
            MediaType = mediaType;
            DeviceProfile = profile;
            AudioCodecs = Array.Empty<string>();
            VideoCodecs = Array.Empty<string>();
            SubtitleCodecs = Array.Empty<string>();
            TranscodeReasons = Array.Empty<TranscodeReason>();
            StreamOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamInfo"/> class.
        /// </summary>
        /// <param name="itemId">The <see cref="Guid"/>.</param>
        /// <param name="mediaType">The <see cref="DlnaProfileType"/>.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/>.</param>
        /// <param name="mediaSource">The <see cref="MediaSourceInfo"/>.</param>
        /// <param name="runtimeTicks">Optional runtime ticks.</param>
        /// <param name="context">The <see cref="EncodingContext"/>.</param>
        public StreamInfo(Guid itemId, DlnaProfileType mediaType, DeviceProfile profile, MediaSourceInfo mediaSource, long? runtimeTicks, EncodingContext context)
        {
            ItemId = itemId;
            MediaType = mediaType;
            DeviceProfile = profile;
            MediaSource = mediaSource;
            RunTimeTicks = runtimeTicks;
            Context = context;
            AudioCodecs = Array.Empty<string>();
            VideoCodecs = Array.Empty<string>();
            SubtitleCodecs = Array.Empty<string>();
            TranscodeReasons = Array.Empty<TranscodeReason>();
            StreamOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the ItemId.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the play method.
        /// </summary>
        public PlayMethod PlayMethod { get; set; }

        /// <summary>
        /// Gets or sets the encoding context.
        /// </summary>
        public EncodingContext Context { get; set; }

        /// <summary>
        /// Gets or sets the media type.
        /// </summary>
        public DlnaProfileType MediaType { get; set; }

        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        public string? Container { get; set; }

        /// <summary>
        /// Gets or sets the sub protocol.
        /// </summary>
        public string? SubProtocol { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        public long StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the segment length.
        /// </summary>
        public int? SegmentLength { get; set; }

        /// <summary>
        /// Gets or sets the min number of segments.
        /// </summary>
        public int? MinSegments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to break on non key frames.
        /// </summary>
        public bool BreakOnNonKeyFrames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the stream requires Avc.
        /// </summary>
        public bool RequireAvc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the stream requires non anamorphic.
        /// </summary>
        public bool RequireNonAnamorphic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to copy the timestamps.
        /// </summary>
        public bool CopyTimestamps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable MpegtsM2Ts mode.
        /// </summary>
        public bool EnableMpegtsM2TsMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable the subtitles in the manifest.
        /// </summary>
        public bool EnableSubtitlesInManifest { get; set; }

        /// <summary>
        /// Gets or sets the audio codecs.
        /// </summary>
        public string[] AudioCodecs { get; set; }

        /// <summary>
        /// Gets or sets the video codecs.
        /// </summary>
        public string[] VideoCodecs { get; set; }

        /// <summary>
        /// Gets or sets the audio stream index.
        /// </summary>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the subtitle stream index.
        /// </summary>
        public int? SubtitleStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of audio channels if transcoding.
        /// </summary>
        public int? TranscodingMaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the global maximum number of audio channels.
        /// </summary>
        public int? GlobalMaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the audio bitrate.
        /// </summary>
        public int? AudioBitrate { get; set; }

        /// <summary>
        /// Gets or sets the audio sample rate.
        /// </summary>
        public int? AudioSampleRate { get; set; }

        /// <summary>
        /// Gets or sets the video bitrate.
        /// </summary>
        public int? VideoBitrate { get; set; }

        /// <summary>
        /// Gets or sets the max width.
        /// </summary>
        public int? MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the max height.
        /// </summary>
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the max frame rate.
        /// </summary>
        public float? MaxFramerate { get; set; }

        /// <summary>
        /// Gets or sets the device profile.
        /// </summary>
        public DeviceProfile DeviceProfile { get; set; }

        /// <summary>
        /// Gets or sets the device profile id to use.
        /// </summary>
        public string? DeviceProfileId { get; set; }

        /// <summary>
        /// Gets or sets the device Id.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the transcode seek info.
        /// </summary>
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to estimate the content length.
        /// </summary>
        public bool EstimateContentLength { get; set; }

        /// <summary>
        /// Gets or sets the media source.
        /// </summary>
        public MediaSourceInfo? MediaSource { get; set; }

        /// <summary>
        /// Gets or sets the subtitle codecs.
        /// </summary>
        public string[] SubtitleCodecs { get; set; }

        /// <summary>
        /// Gets or sets the subtitle delivery method.
        /// </summary>
        public SubtitleDeliveryMethod SubtitleDeliveryMethod { get; set; }

        /// <summary>
        /// Gets or sets the subtitle format.
        /// </summary>
        public string? SubtitleFormat { get; set; }

        /// <summary>
        /// Gets or sets the play sessionId.
        /// </summary>
        public string? PlaySessionId { get; set; }

        /// <summary>
        /// Gets or sets the transcode reasons.
        /// </summary>
        public TranscodeReason[] TranscodeReasons { get; set; }

        /// <summary>
        /// Gets the stream options.
        /// </summary>
        public Dictionary<string, string> StreamOptions { get; private set; }

        /// <summary>
        /// Gets the media sourceId.
        /// </summary>
        public string? MediaSourceId => MediaSource?.Id;

        /// <summary>
        /// Gets a value indicating whether the stream is a direct stream.
        /// </summary>
        public bool IsDirectStream =>
            PlayMethod == PlayMethod.DirectStream ||
            PlayMethod == PlayMethod.DirectPlay;

        /// <summary>
        /// Gets the audio stream that will be used.
        /// </summary>
        public MediaStream? TargetAudioStream => MediaSource?.GetDefaultAudioStream(AudioStreamIndex);

        /// <summary>
        /// Gets the video stream that will be used.
        /// </summary>
        public MediaStream? TargetVideoStream => MediaSource?.VideoStream;

        /// <summary>
        /// Gets the audio sample rate that will be in the output stream.
        /// </summary>
        public int? TargetAudioSampleRate
        {
            get
            {
                var stream = TargetAudioStream;
                return AudioSampleRate.HasValue && !IsDirectStream ? AudioSampleRate : stream?.SampleRate;
            }
        }

        /// <summary>
        /// Gets the audio sample rate that will be in the output stream.
        /// </summary>
        public int? TargetAudioBitDepth
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetAudioStream == null ? (int?)null : TargetAudioStream.BitDepth;
                }

                var targetAudioCodecs = TargetAudioCodec;
                var audioCodec = targetAudioCodecs.Length == 0 ? null : targetAudioCodecs[0];
                if (!string.IsNullOrEmpty(audioCodec))
                {
                    return GetTargetAudioBitDepth(audioCodec);
                }

                return TargetAudioStream == null ? (int?)null : TargetAudioStream.BitDepth;
            }
        }

        /// <summary>
        /// Gets the audio sample rate that will be in the output stream.
        /// </summary>
        public int? TargetVideoBitDepth
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream == null ? (int?)null : TargetVideoStream.BitDepth;
                }

                var targetVideoCodecs = TargetVideoCodec;
                var videoCodec = targetVideoCodecs.Length == 0 ? null : targetVideoCodecs[0];
                if (!string.IsNullOrEmpty(videoCodec))
                {
                    return GetTargetVideoBitDepth(videoCodec);
                }

                return TargetVideoStream == null ? (int?)null : TargetVideoStream.BitDepth;
            }
        }

        /// <summary>
        /// Gets the target reference frames.
        /// </summary>
        public int? TargetRefFrames
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream == null ? (int?)null : TargetVideoStream.RefFrames;
                }

                var targetVideoCodecs = TargetVideoCodec;
                var videoCodec = targetVideoCodecs.Length == 0 ? null : targetVideoCodecs[0];
                if (!string.IsNullOrEmpty(videoCodec))
                {
                    return GetTargetRefFrames(videoCodec);
                }

                return TargetVideoStream == null ? (int?)null : TargetVideoStream.RefFrames;
            }
        }

        /// <summary>
        /// Gets the audio sample rate that will be in the output stream.
        /// </summary>
        public float? TargetFramerate
        {
            get
            {
                var stream = TargetVideoStream;
                return MaxFramerate.HasValue && !IsDirectStream
                    ? MaxFramerate
                    : stream == null ? null : stream.AverageFrameRate ?? stream.RealFrameRate;
            }
        }

        /// <summary>
        /// Gets the audio sample rate that will be in the output stream.
        /// </summary>
        public double? TargetVideoLevel
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream == null ? (double?)null : TargetVideoStream.Level;
                }

                var targetVideoCodecs = TargetVideoCodec;
                var videoCodec = targetVideoCodecs.Length == 0 ? null : targetVideoCodecs[0];
                if (!string.IsNullOrEmpty(videoCodec))
                {
                    return GetTargetVideoLevel(videoCodec);
                }

                return TargetVideoStream == null ? (double?)null : TargetVideoStream.Level;
            }
        }

        /// <summary>
        /// Gets the audio sample rate that will be in the output stream.
        /// </summary>
        public int? TargetPacketLength
        {
            get
            {
                var stream = TargetVideoStream;
                return !IsDirectStream ? null : stream?.PacketLength;
            }
        }

        /// <summary>
        /// Gets the audio sample rate that will be in the output stream.
        /// </summary>
        public string? TargetVideoProfile
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream?.Profile;
                }

                var targetVideoCodecs = TargetVideoCodec;
                var videoCodec = targetVideoCodecs.Length == 0 ? null : targetVideoCodecs[0];
                if (!string.IsNullOrEmpty(videoCodec))
                {
                    return GetOption(videoCodec, "profile");
                }

                return TargetVideoStream?.Profile;
            }
        }

        /// <summary>
        /// Gets the target video codec tag.
        /// </summary>
        public string? TargetVideoCodecTag
        {
            get
            {
                var stream = TargetVideoStream;
                return !IsDirectStream ? null : stream?.CodecTag;
            }
        }

        /// <summary>
        /// Gets the audio bitrate that will be in the output stream.
        /// </summary>
        public int? TargetAudioBitrate
        {
            get
            {
                var stream = TargetAudioStream;
                return AudioBitrate.HasValue && !IsDirectStream ? AudioBitrate : stream?.BitRate;
            }
        }

        /// <summary>
        /// Gets the audio channels that will be in the output stream.
        /// </summary>
        public int? TargetAudioChannels
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetAudioStream == null ? (int?)null : TargetAudioStream.Channels;
                }

                var targetAudioCodecs = TargetAudioCodec;
                var codec = targetAudioCodecs.Length == 0 ? null : targetAudioCodecs[0];
                if (!string.IsNullOrEmpty(codec))
                {
                    return GetTargetRefFrames(codec);
                }

                return TargetAudioStream == null ? (int?)null : TargetAudioStream.Channels;
            }
        }

        /// <summary>
        /// Gets the audio codec that will be in the output stream.
        /// </summary>
        public string[] TargetAudioCodec
        {
            get
            {
                var stream = TargetAudioStream;

                string? inputCodec = stream?.Codec;

                if (IsDirectStream)
                {
                    return string.IsNullOrEmpty(inputCodec) ? Array.Empty<string>() : new[] { inputCodec };
                }

                foreach (string codec in AudioCodecs)
                {
                    if (string.Equals(codec, inputCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        return string.IsNullOrEmpty(codec) ? Array.Empty<string>() : new[] { codec };
                    }
                }

                return AudioCodecs;
            }
        }

        /// <summary>
        /// Gets a value indicating the target video codec.
        /// </summary>
        public string[] TargetVideoCodec
        {
            get
            {
                var stream = TargetVideoStream;

                string? inputCodec = stream?.Codec;

                if (IsDirectStream)
                {
                    return string.IsNullOrEmpty(inputCodec) ? Array.Empty<string>() : new[] { inputCodec };
                }

                foreach (string codec in VideoCodecs)
                {
                    if (string.Equals(codec, inputCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        return string.IsNullOrEmpty(codec) ? Array.Empty<string>() : new[] { codec };
                    }
                }

                return VideoCodecs;
            }
        }

        /// <summary>
        /// Gets the audio channels that will be in the output stream.
        /// </summary>
        public long? TargetSize
        {
            get
            {
                if (IsDirectStream)
                {
                    return MediaSource?.Size;
                }

                if (RunTimeTicks.HasValue)
                {
                    int? totalBitrate = TargetTotalBitrate;

                    double totalSeconds = RunTimeTicks.Value;
                    // Convert to ms
                    totalSeconds /= 10000;
                    // Convert to seconds
                    totalSeconds /= 1000;

                    return totalBitrate.HasValue ?
                        Convert.ToInt64(totalBitrate.Value * totalSeconds) :
                        (long?)null;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating the video bitrate.
        /// </summary>
        public int? TargetVideoBitrate
        {
            get
            {
                var stream = TargetVideoStream;

                return VideoBitrate.HasValue && !IsDirectStream ? VideoBitrate : stream?.BitRate;
            }
        }

        /// <summary>
        /// Gets a value indicating the target timestamp.
        /// </summary>
        public TransportStreamTimestamp TargetTimestamp
        {
            get
            {
                var defaultValue = string.Equals(Container, "m2ts", StringComparison.OrdinalIgnoreCase)
                    ? TransportStreamTimestamp.Valid
                    : TransportStreamTimestamp.None;

                return !IsDirectStream
                    ? defaultValue
                    : MediaSource == null ? defaultValue : MediaSource.Timestamp ?? TransportStreamTimestamp.None;
            }
        }

        /// <summary>
        /// Gets a value indicating the target total bitrate.
        /// </summary>
        public int? TargetTotalBitrate => (TargetAudioBitrate ?? 0) + (TargetVideoBitrate ?? 0);

        /// <summary>
        /// Gets a value indicating whether the target is anamorphic.
        /// </summary>
        public bool? IsTargetAnamorphic => IsDirectStream ? TargetVideoStream?.IsAnamorphic : false;

        /// <summary>
        /// Gets a value indicating whether the stream is target interlaced.
        /// </summary>
        public bool? IsTargetInterlaced
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream == null ? (bool?)null : TargetVideoStream.IsInterlaced;
                }

                var targetVideoCodecs = TargetVideoCodec;
                var videoCodec = targetVideoCodecs.Length == 0 ? null : targetVideoCodecs[0];
                if (!string.IsNullOrEmpty(videoCodec))
                {
                    if (string.Equals(GetOption(videoCodec, "deinterlace"), "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return TargetVideoStream == null ? (bool?)null : TargetVideoStream.IsInterlaced;
            }
        }

        /// <summary>
        /// Gets a value indicating the whether the Target is AVC.
        /// </summary>
        public bool? IsTargetAVC
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream?.IsAVC;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating the target width.
        /// </summary>
        public int? TargetWidth
        {
            get
            {
                var videoStream = TargetVideoStream;

                if (videoStream != null && videoStream.Width.HasValue && videoStream.Height.HasValue)
                {
                    ImageDimensions size = new ImageDimensions(videoStream.Width.Value, videoStream.Height.Value);

                    size = DrawingUtils.Resize(size, 0, 0, MaxWidth ?? 0, MaxHeight ?? 0);

                    return size.Width;
                }

                return MaxWidth;
            }
        }

        /// <summary>
        /// Gets a value indicating the target height.
        /// </summary>
        public int? TargetHeight
        {
            get
            {
                var videoStream = TargetVideoStream;

                if (videoStream != null && videoStream.Width.HasValue && videoStream.Height.HasValue)
                {
                    ImageDimensions size = new ImageDimensions(videoStream.Width.Value, videoStream.Height.Value);

                    size = DrawingUtils.Resize(size, 0, 0, MaxWidth ?? 0, MaxHeight ?? 0);

                    return size.Height;
                }

                return MaxHeight;
            }
        }

        /// <summary>
        /// Gets a value indicating the target video stream count.
        /// </summary>
        public int? TargetVideoStreamCount
        {
            get
            {
                if (IsDirectStream)
                {
                    return GetMediaStreamCount(MediaStreamType.Video, int.MaxValue);
                }

                return GetMediaStreamCount(MediaStreamType.Video, 1);
            }
        }

        /// <summary>
        /// Gets a value indicating the target audio stream count.
        /// </summary>
        public int? TargetAudioStreamCount
        {
            get
            {
                if (IsDirectStream)
                {
                    return GetMediaStreamCount(MediaStreamType.Audio, int.MaxValue);
                }

                return GetMediaStreamCount(MediaStreamType.Audio, 1);
            }
        }

        /// <summary>
        /// Gets the target audio channels.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>The target audio channel or null if not set.</returns>
        public int? GetTargetAudioChannels(string? codec)
        {
            var defaultValue = GlobalMaxAudioChannels ?? TranscodingMaxAudioChannels;
            if (string.IsNullOrEmpty(codec))
            {
                return defaultValue;
            }

            var value = GetOption(codec, "audiochannels");
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return Math.Min(result, defaultValue ?? result);
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets a stream option.
        /// </summary>
        /// <param name="qualifier">The qualifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void SetOption(string? qualifier, string name, string value)
        {
            if (string.IsNullOrEmpty(qualifier))
            {
                SetOption(name, value);
            }
            else
            {
                SetOption(qualifier + "-" + name, value);
            }
        }

        /// <summary>
        /// Sets a stream option.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void SetOption(string name, string value)
        {
            StreamOptions[name] = value;
        }

        /// <summary>
        /// Gets a stream option.
        /// </summary>
        /// <param name="qualifier">The qualifier.</param>
        /// <param name="name">The name.</param>
        /// <returns>The option or null if not found.</returns>
        public string? GetOption(string qualifier, string name)
        {
            var value = GetOption(qualifier + "-" + name);

            if (string.IsNullOrEmpty(value))
            {
                value = GetOption(name);
            }

            return value;
        }

        /// <summary>
        /// Gets a stream option.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The option's value, or null if not found.</returns>
        public string? GetOption(string name)
        {
            if (StreamOptions.TryGetValue(name, out var value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Returns a list of the external subtitles.
        /// </summary>
        /// <param name="transcoderSupport">The transcoderSupport<see cref="ITranscoderSupport"/>.</param>
        /// <param name="includeSelectedTrackOnly">The includeSelectedTrackOnly<see cref="bool"/>.</param>
        /// <param name="baseUrl">The baseUrl.</param>
        /// <param name="accessToken">The accessToken.</param>
        /// <returns>The <see cref="List{SubtitleStreamInfo}"/>.</returns>
        public List<SubtitleStreamInfo> GetExternalSubtitles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, string baseUrl, string accessToken)
        {
            return GetExternalSubtitles(transcoderSupport, includeSelectedTrackOnly, false, baseUrl, accessToken);
        }

        /// <summary>
        /// Returns a list of the external subtitles.
        /// </summary>
        /// <param name="transcoderSupport">The transcoderSupport<see cref="ITranscoderSupport"/>.</param>
        /// <param name="includeSelectedTrackOnly">The includeSelectedTrackOnly<see cref="bool"/>.</param>
        /// <param name="enableAllProfiles">The enableAllProfiles<see cref="bool"/>.</param>
        /// <param name="baseUrl">The baseUrl.</param>
        /// <param name="accessToken">The accessToken.</param>
        /// <returns>The <see cref="List{SubtitleStreamInfo}"/>.</returns>
        public List<SubtitleStreamInfo> GetExternalSubtitles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, bool enableAllProfiles, string baseUrl, string accessToken)
        {
            var list = GetSubtitleProfiles(transcoderSupport, includeSelectedTrackOnly, enableAllProfiles, baseUrl, accessToken);
            var newList = new List<SubtitleStreamInfo>();

            // First add the selected track
            foreach (SubtitleStreamInfo stream in list)
            {
                if (stream.DeliveryMethod == SubtitleDeliveryMethod.External)
                {
                    newList.Add(stream);
                }
            }

            return newList;
        }

        /// <summary>
        /// Returns a list of the subtitle profiles.
        /// </summary>
        /// <param name="transcoderSupport">The transcoderSupport<see cref="ITranscoderSupport"/>.</param>
        /// <param name="includeSelectedTrackOnly">The includeSelectedTrackOnly<see cref="bool"/>.</param>
        /// <param name="baseUrl">The baseUrl.</param>
        /// <param name="accessToken">The accessToken.</param>
        /// <returns>The <see cref="List{SubtitleStreamInfo}"/>.</returns>
        public List<SubtitleStreamInfo> GetSubtitleProfiles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, string baseUrl, string? accessToken)
        {
            return GetSubtitleProfiles(transcoderSupport, includeSelectedTrackOnly, false, baseUrl, accessToken);
        }

        /// <summary>
        /// Returns a list of the subtitle profiles.
        /// </summary>
        /// <param name="transcoderSupport">The transcoderSupport<see cref="ITranscoderSupport"/>.</param>
        /// <param name="includeSelectedTrackOnly">The includeSelectedTrackOnly<see cref="bool"/>.</param>
        /// <param name="enableAllProfiles">The enableAllProfiles<see cref="bool"/>.</param>
        /// <param name="baseUrl">The baseUrl.</param>
        /// <param name="accessToken">The accessToken.</param>
        /// <returns>The <see cref="List{SubtitleStreamInfo}"/>.</returns>
        public List<SubtitleStreamInfo> GetSubtitleProfiles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, bool enableAllProfiles, string baseUrl, string? accessToken)
        {
            var list = new List<SubtitleStreamInfo>();

            // HLS will preserve timestamps so we can just grab the full subtitle stream
            long startPositionTicks = string.Equals(SubProtocol, "hls", StringComparison.OrdinalIgnoreCase)
                ? 0
                : (PlayMethod == PlayMethod.Transcode && !CopyTimestamps ? StartPositionTicks : 0);

            // First add the selected track
            if (SubtitleStreamIndex.HasValue && MediaSource != null)
            {
                foreach (var stream in MediaSource.MediaStreams)
                {
                    if (stream.Type == MediaStreamType.Subtitle && stream.Index == SubtitleStreamIndex.Value)
                    {
                        AddSubtitleProfiles(list, stream, transcoderSupport, enableAllProfiles, baseUrl, accessToken, startPositionTicks);
                    }
                }
            }

            if (!includeSelectedTrackOnly && MediaSource != null)
            {
                foreach (var stream in MediaSource.MediaStreams)
                {
                    if (stream.Type == MediaStreamType.Subtitle && (!SubtitleStreamIndex.HasValue || stream.Index != SubtitleStreamIndex.Value))
                    {
                        AddSubtitleProfiles(list, stream, transcoderSupport, enableAllProfiles, baseUrl, accessToken, startPositionTicks);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the target video bit depth.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>The target video bit depth or null if not found.</returns>
        public int? GetTargetVideoBitDepth(string? codec)
        {
            string? value;
            if (string.IsNullOrEmpty(codec))
            {
                value = GetOption("videobitdepth");
            }
            else
            {
                value = GetOption(codec, "videobitdepth");
            }

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets the target audio bit depth.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>The target audio bit depth, or null if not set.</returns>
        public int? GetTargetAudioBitDepth(string codec)
        {
            var value = GetOption(codec, "audiobitdepth");
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets the target video level.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>The target video level or null if not found.</returns>
        public double? GetTargetVideoLevel(string codec)
        {
            var value = GetOption(codec, "level");
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets the target ref frames.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <returns>The target ref frames or null if not defined.</returns>
        public int? GetTargetRefFrames(string? codec)
        {
            string? value;
            if (string.IsNullOrEmpty(codec))
            {
                value = GetOption("maxrefframes");
            }
            else
            {
                value = GetOption(codec, "maxrefframes");
            }

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets the selectable audio streams.
        /// </summary>
        /// <returns>The <see cref="List{MediaStream}"/>.</returns>
        public List<MediaStream> GetSelectableAudioStreams()
        {
            return GetSelectableStreams(MediaStreamType.Audio);
        }

        /// <summary>
        /// Gets the selectable subtitle streams.
        /// </summary>
        /// <returns>The <see cref="List{MediaStream}"/>.</returns>
        public List<MediaStream> GetSelectableSubtitleStreams()
        {
            return GetSelectableStreams(MediaStreamType.Subtitle);
        }

        /// <summary>
        /// Gets the selectable streams.
        /// </summary>
        /// <param name="type">The <see cref="MediaStreamType"/>.</param>
        /// <returns>The <see cref="List{MediaStream}"/>.</returns>
        public List<MediaStream> GetSelectableStreams(MediaStreamType type)
        {
            var list = new List<MediaStream>();
            if (MediaSource != null)
            {
                foreach (var stream in MediaSource.MediaStreams)
                {
                    if (type == stream.Type)
                    {
                        list.Add(stream);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Returns this class as a url.
        /// </summary>
        /// <param name="baseUrl">The baseUrl.</param>
        /// <param name="accessToken">The accessToken.</param>
        /// <param name="query">Optional extra query.</param>
        /// <returns>A querystring representation of this object.</returns>
        public string ToUrl(string? baseUrl, string? accessToken, string? query = null)
        {
            if (PlayMethod == PlayMethod.DirectPlay)
            {
                return MediaSource?.Path ?? string.Empty;
            }

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            var sb = new StringBuilder(2000);

            if (!string.IsNullOrEmpty(baseUrl))
            {
                sb.Append(baseUrl.TrimEnd('/'));
            }

            if (MediaType == DlnaProfileType.Audio)
            {
                sb.Append("/audio/");
                sb.Append(ItemId);

                if (string.Equals(SubProtocol, "hls", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("/master.m3u8?");
                }
                else
                {
                    sb.Append("/stream");

                    if (!string.IsNullOrEmpty(Container))
                    {
                        sb.Append('.');
                        sb.Append(Container);
                        sb.Append('?');
                    }
                }
            }
            else
            {
                sb.Append("/videos/");
                sb.Append(ItemId);

                if (string.Equals(SubProtocol, "hls", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("/master.m3u8?");
                }
                else
                {
                    sb.Append("/stream");

                    if (!string.IsNullOrEmpty(Container))
                    {
                        sb.Append('.');
                        sb.Append(Container);
                        sb.Append('?');
                    }
                }
            }

            if (!string.IsNullOrEmpty(DeviceProfileId))
            {
                sb.Append("&DeviceProfileId=");
                sb.Append(DeviceProfileId);
            }

            if (!string.IsNullOrEmpty(DeviceId))
            {
                sb.Append("&DeviceId=");
                sb.Append(DeviceId);
            }

            if (!string.IsNullOrEmpty(MediaSourceId))
            {
                sb.Append("&MediaSourceId=");
                sb.Append(MediaSourceId);
            }

            // default true so don't store.
            if (IsDirectStream)
            {
                sb.Append("&Static=true");
            }

            if (VideoCodecs.Length != 0)
            {
                sb.Append("&VideoCodec=");
                sb.Append(string.Join(",", VideoCodecs));
            }

            if (AudioCodecs.Length != 0)
            {
                sb.Append("&AudioCodec=");
                sb.Append(string.Join(",", AudioCodecs));
            }

            if (AudioStreamIndex.HasValue)
            {
                sb.Append("&AudioStreamIndex=");
                sb.Append(AudioStreamIndex.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (SubtitleStreamIndex.HasValue && SubtitleDeliveryMethod != SubtitleDeliveryMethod.External && SubtitleStreamIndex != -1)
            {
                sb.Append("&SubtitleStreamIndex=");
                sb.Append(SubtitleStreamIndex.Value.ToString(CultureInfo.InvariantCulture));
                sb.Append("&SubtitleMethod=");
                sb.Append(SubtitleDeliveryMethod.ToString());
            }

            if (VideoBitrate.HasValue)
            {
                sb.Append("&VideoBitrate=");
                sb.Append(VideoBitrate.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (AudioBitrate.HasValue)
            {
                sb.Append("&AudioBitrate=");
                sb.Append(AudioBitrate.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (AudioSampleRate.HasValue)
            {
                sb.Append("&AudioSampleRate=");
                sb.Append(AudioSampleRate.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (MaxFramerate.HasValue)
            {
                sb.Append("&MaxFramerate=");
                sb.Append(MaxFramerate.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (MaxWidth.HasValue)
            {
                sb.Append("&MaxWidth=");
                sb.Append(MaxWidth.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (MaxHeight.HasValue)
            {
                sb.Append("&MaxHeight=");
                sb.Append(MaxHeight.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.Equals(SubProtocol, "hls", StringComparison.OrdinalIgnoreCase))
            {
                if (StartPositionTicks != 0)
                {
                    sb.Append("&StartTimeTicks=");
                    sb.Append(StartPositionTicks.ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Container))
                {
                    sb.Append("&SegmentContainer=");
                    sb.Append(Container);
                }

                if (SegmentLength.HasValue)
                {
                    sb.Append("&SegmentLength=");
                    sb.Append(SegmentLength.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (MinSegments.HasValue)
                {
                    sb.Append("&MinSegments=");
                    sb.Append(MinSegments.Value.ToString(CultureInfo.InvariantCulture));
                }

                sb.Append("&BreakOnNonKeyFrames=");
                sb.Append(BreakOnNonKeyFrames.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(PlaySessionId))
            {
                sb.Append("&PlaySessionId=");
                sb.Append(PlaySessionId);
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                sb.Append("&api_key=");
                sb.Append(accessToken);
            }

            var liveStreamId = MediaSource?.LiveStreamId;
            if (!string.IsNullOrEmpty(liveStreamId))
            {
                sb.Append("&LiveStreamId=");
                sb.Append(liveStreamId);
            }

            if (!IsDirectStream)
            {
                if (RequireNonAnamorphic)
                {
                    sb.Append("&RequireNonAnamorphic=");
                    sb.Append(RequireNonAnamorphic.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                }

                if (TranscodingMaxAudioChannels.HasValue)
                {
                    sb.Append("&TranscodingMaxAudioChannels=");
                    sb.Append(TranscodingMaxAudioChannels.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (EnableSubtitlesInManifest)
                {
                    sb.Append("&EnableSubtitlesInManifest=");
                    sb.Append(EnableSubtitlesInManifest.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                }

                if (EnableMpegtsM2TsMode)
                {
                    sb.Append("&EnableMpegtsM2TsMode=");
                    sb.Append(EnableMpegtsM2TsMode.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                }

                if (EstimateContentLength)
                {
                    sb.Append("&EstimateContentLength=");
                    sb.Append(EstimateContentLength.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                }

                if (TranscodeSeekInfo != TranscodeSeekInfo.Auto)
                {
                    sb.Append("&TranscodeSeekInfo=");
                    sb.Append(TranscodeSeekInfo.ToString().ToLowerInvariant());
                }

                if (CopyTimestamps)
                {
                    sb.Append("&CopyTimestamps=");
                    sb.Append(CopyTimestamps.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
                }

                sb.Append("&RequireAvc=");
                sb.Append(RequireAvc.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            var etag = MediaSource?.ETag;
            if (!string.IsNullOrEmpty(etag))
            {
                sb.Append("&Tag=");
                sb.Append(etag);
            }

            if (SubtitleStreamIndex.HasValue && SubtitleDeliveryMethod == SubtitleDeliveryMethod.Embed && SubtitleCodecs.Length != 0)
            {
                sb.Append("&SubtitleCodec=");
                sb.Append(string.Join(",", SubtitleCodecs));
            }

            foreach (var pair in StreamOptions)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                // strip spaces to avoid having to encode h264 profile names
                sb.Append('&');
                sb.Append(pair.Key);
                sb.Append('=');
                sb.Append(pair.Value.Replace(" ", string.Empty, StringComparison.Ordinal));
            }

            if (!IsDirectStream)
            {
                sb.Append("&TranscodeReasons=");
                sb.Append(string.Join(
                    ",",
                    TranscodeReasons.Distinct()
                        .Select(i => i.ToString()))
                    .Replace(" ", "%20", StringComparison.Ordinal));
            }

            if (query != null)
            {
                sb.Append(query);
            }

            return sb.ToString();
        }

        private SubtitleStreamInfo GetSubtitleStreamInfo(MediaStream stream, string baseUrl, string? accessToken, long startPositionTicks, SubtitleProfile[] subtitleProfiles, ITranscoderSupport transcoderSupport)
        {
            var subtitleProfile = StreamBuilder.GetSubtitleProfile(MediaSource, stream, subtitleProfiles, PlayMethod, transcoderSupport, Container, SubProtocol);
            var info = new SubtitleStreamInfo
            {
                IsForced = stream.IsForced,
                Language = stream.Language,
                Name = stream.Language ?? "Unknown",
                Format = subtitleProfile.Format,
                Index = stream.Index,
                DeliveryMethod = subtitleProfile.Method,
                DisplayTitle = stream.DisplayTitle
            };

            if (info.DeliveryMethod == SubtitleDeliveryMethod.External)
            {
                if (MediaSource?.Protocol == MediaProtocol.File || !string.Equals(stream.Codec, subtitleProfile.Format, StringComparison.OrdinalIgnoreCase) || !stream.IsExternal)
                {
                    info.Url = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/Videos/{1}/{2}/Subtitles/{3}/{4}/Stream.{5}",
                        baseUrl,
                        ItemId,
                        MediaSourceId,
                        stream.Index.ToString(CultureInfo.InvariantCulture),
                        startPositionTicks.ToString(CultureInfo.InvariantCulture),
                        subtitleProfile.Format);

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        info.Url += "?api_key=" + accessToken;
                    }

                    info.IsExternalUrl = false;
                }
                else
                {
                    info.Url = stream.Path;
                    info.IsExternalUrl = true;
                }
            }

            return info;
        }

        private int? GetMediaStreamCount(MediaStreamType type, int limit)
        {
            var count = MediaSource?.GetStreamCount(type);

            if (count.HasValue)
            {
                count = Math.Min(count.Value, limit);
            }

            return count;
        }

        private void AddSubtitleProfiles(List<SubtitleStreamInfo> list, MediaStream stream, ITranscoderSupport transcoderSupport, bool enableAllProfiles, string baseUrl, string? accessToken, long startPositionTicks)
        {
            if (enableAllProfiles)
            {
                foreach (var profile in DeviceProfile.SubtitleProfiles)
                {
                    var info = GetSubtitleStreamInfo(stream, baseUrl, accessToken, startPositionTicks, new[] { profile }, transcoderSupport);

                    list.Add(info);
                }
            }
            else
            {
                var info = GetSubtitleStreamInfo(stream, baseUrl, accessToken, startPositionTicks, DeviceProfile.SubtitleProfiles, transcoderSupport);

                list.Add(info);
            }
        }
    }
}
