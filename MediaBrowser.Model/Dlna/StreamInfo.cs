#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public StreamInfo()
        {
            AudioCodecs = Array.Empty<string>();
            VideoCodecs = Array.Empty<string>();
            SubtitleCodecs = Array.Empty<string>();
            TranscodeReasons = Array.Empty<TranscodeReason>();
            StreamOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public void SetOption(string qualifier, string name, string value)
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

        public void SetOption(string name, string value)
        {
            StreamOptions[name] = value;
        }

        public string GetOption(string qualifier, string name)
        {
            var value = GetOption(qualifier + "-" + name);

            if (string.IsNullOrEmpty(value))
            {
                value = GetOption(name);
            }

            return value;
        }

        public string GetOption(string name)
        {
            if (StreamOptions.TryGetValue(name, out var value))
            {
                return value;
            }

            return null;
        }

        public Guid ItemId { get; set; }

        public PlayMethod PlayMethod { get; set; }
        public EncodingContext Context { get; set; }

        public DlnaProfileType MediaType { get; set; }

        public string Container { get; set; }

        public string SubProtocol { get; set; }

        public long StartPositionTicks { get; set; }

        public int? SegmentLength { get; set; }
        public int? MinSegments { get; set; }
        public bool BreakOnNonKeyFrames { get; set; }

        public bool RequireAvc { get; set; }
        public bool RequireNonAnamorphic { get; set; }
        public bool CopyTimestamps { get; set; }
        public bool EnableMpegtsM2TsMode { get; set; }
        public bool EnableSubtitlesInManifest { get; set; }
        public string[] AudioCodecs { get; set; }
        public string[] VideoCodecs { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? TranscodingMaxAudioChannels { get; set; }
        public int? GlobalMaxAudioChannels { get; set; }

        public int? AudioBitrate { get; set; }

        public int? VideoBitrate { get; set; }

        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public float? MaxFramerate { get; set; }

        public DeviceProfile DeviceProfile { get; set; }
        public string DeviceProfileId { get; set; }
        public string DeviceId { get; set; }

        public long? RunTimeTicks { get; set; }

        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public bool EstimateContentLength { get; set; }

        public MediaSourceInfo MediaSource { get; set; }

        public string[] SubtitleCodecs { get; set; }
        public SubtitleDeliveryMethod SubtitleDeliveryMethod { get; set; }
        public string SubtitleFormat { get; set; }

        public string PlaySessionId { get; set; }
        public TranscodeReason[] TranscodeReasons { get; set; }

        public Dictionary<string, string> StreamOptions { get; private set; }

        public string MediaSourceId => MediaSource == null ? null : MediaSource.Id;

        public bool IsDirectStream =>
            PlayMethod == PlayMethod.DirectStream ||
            PlayMethod == PlayMethod.DirectPlay;

        public string ToUrl(string baseUrl, string accessToken)
        {
            if (PlayMethod == PlayMethod.DirectPlay)
            {
                return MediaSource.Path;
            }

            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            var list = new List<string>();
            foreach (NameValuePair pair in BuildParams(this, accessToken))
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                // Try to keep the url clean by omitting defaults
                if (string.Equals(pair.Name, "StartTimeTicks", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(pair.Value, "0", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (string.Equals(pair.Name, "SubtitleStreamIndex", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(pair.Value, "-1", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (string.Equals(pair.Name, "Static", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(pair.Value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var encodedValue = pair.Value.Replace(" ", "%20");

                list.Add(string.Format("{0}={1}", pair.Name, encodedValue));
            }

            string queryString = string.Join("&", list.ToArray());

            return GetUrl(baseUrl, queryString);
        }

        private string GetUrl(string baseUrl, string queryString)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            string extension = string.IsNullOrEmpty(Container) ? string.Empty : "." + Container;

            baseUrl = baseUrl.TrimEnd('/');

            if (MediaType == DlnaProfileType.Audio)
            {
                if (string.Equals(SubProtocol, "hls", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format("{0}/audio/{1}/master.m3u8?{2}", baseUrl, ItemId, queryString);
                }

                return string.Format("{0}/audio/{1}/stream{2}?{3}", baseUrl, ItemId, extension, queryString);
            }

            if (string.Equals(SubProtocol, "hls", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format("{0}/videos/{1}/master.m3u8?{2}", baseUrl, ItemId, queryString);
            }

            return string.Format("{0}/videos/{1}/stream{2}?{3}", baseUrl, ItemId, extension, queryString);
        }

        private static List<NameValuePair> BuildParams(StreamInfo item, string accessToken)
        {
            var list = new List<NameValuePair>();

            string audioCodecs = item.AudioCodecs.Length == 0 ?
                string.Empty :
                string.Join(",", item.AudioCodecs);

            string videoCodecs = item.VideoCodecs.Length == 0 ?
                string.Empty :
                string.Join(",", item.VideoCodecs);

            list.Add(new NameValuePair("DeviceProfileId", item.DeviceProfileId ?? string.Empty));
            list.Add(new NameValuePair("DeviceId", item.DeviceId ?? string.Empty));
            list.Add(new NameValuePair("MediaSourceId", item.MediaSourceId ?? string.Empty));
            list.Add(new NameValuePair("Static", item.IsDirectStream.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
            list.Add(new NameValuePair("VideoCodec", videoCodecs));
            list.Add(new NameValuePair("AudioCodec", audioCodecs));
            list.Add(new NameValuePair("AudioStreamIndex", item.AudioStreamIndex.HasValue ? item.AudioStreamIndex.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
            list.Add(new NameValuePair("SubtitleStreamIndex", item.SubtitleStreamIndex.HasValue && item.SubtitleDeliveryMethod != SubtitleDeliveryMethod.External ? item.SubtitleStreamIndex.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
            list.Add(new NameValuePair("VideoBitrate", item.VideoBitrate.HasValue ? item.VideoBitrate.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
            list.Add(new NameValuePair("AudioBitrate", item.AudioBitrate.HasValue ? item.AudioBitrate.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));

            list.Add(new NameValuePair("MaxFramerate", item.MaxFramerate.HasValue ? item.MaxFramerate.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
            list.Add(new NameValuePair("MaxWidth", item.MaxWidth.HasValue ? item.MaxWidth.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
            list.Add(new NameValuePair("MaxHeight", item.MaxHeight.HasValue ? item.MaxHeight.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));

            long startPositionTicks = item.StartPositionTicks;

            var isHls = string.Equals(item.SubProtocol, "hls", StringComparison.OrdinalIgnoreCase);

            if (isHls)
            {
                list.Add(new NameValuePair("StartTimeTicks", string.Empty));
            }
            else
            {
                list.Add(new NameValuePair("StartTimeTicks", startPositionTicks.ToString(CultureInfo.InvariantCulture)));
            }

            list.Add(new NameValuePair("PlaySessionId", item.PlaySessionId ?? string.Empty));
            list.Add(new NameValuePair("api_key", accessToken ?? string.Empty));

            string liveStreamId = item.MediaSource?.LiveStreamId;
            list.Add(new NameValuePair("LiveStreamId", liveStreamId ?? string.Empty));

            list.Add(new NameValuePair("SubtitleMethod", item.SubtitleStreamIndex.HasValue && item.SubtitleDeliveryMethod != SubtitleDeliveryMethod.External ? item.SubtitleDeliveryMethod.ToString() : string.Empty));


            if (!item.IsDirectStream)
            {
                if (item.RequireNonAnamorphic)
                {
                    list.Add(new NameValuePair("RequireNonAnamorphic", item.RequireNonAnamorphic.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
                }

                list.Add(new NameValuePair("TranscodingMaxAudioChannels", item.TranscodingMaxAudioChannels.HasValue ? item.TranscodingMaxAudioChannels.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));

                if (item.EnableSubtitlesInManifest)
                {
                    list.Add(new NameValuePair("EnableSubtitlesInManifest", item.EnableSubtitlesInManifest.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
                }

                if (item.EnableMpegtsM2TsMode)
                {
                    list.Add(new NameValuePair("EnableMpegtsM2TsMode", item.EnableMpegtsM2TsMode.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
                }

                if (item.EstimateContentLength)
                {
                    list.Add(new NameValuePair("EstimateContentLength", item.EstimateContentLength.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
                }

                if (item.TranscodeSeekInfo != TranscodeSeekInfo.Auto)
                {
                    list.Add(new NameValuePair("TranscodeSeekInfo", item.TranscodeSeekInfo.ToString().ToLowerInvariant()));
                }

                if (item.CopyTimestamps)
                {
                    list.Add(new NameValuePair("CopyTimestamps", item.CopyTimestamps.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
                }

                list.Add(new NameValuePair("RequireAvc", item.RequireAvc.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
            }

            list.Add(new NameValuePair("Tag", item.MediaSource.ETag ?? string.Empty));

            string subtitleCodecs = item.SubtitleCodecs.Length == 0 ?
               string.Empty :
               string.Join(",", item.SubtitleCodecs);

            list.Add(new NameValuePair("SubtitleCodec", item.SubtitleStreamIndex.HasValue && item.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Embed ? subtitleCodecs : string.Empty));

            if (isHls)
            {
                list.Add(new NameValuePair("SegmentContainer", item.Container ?? string.Empty));

                if (item.SegmentLength.HasValue)
                {
                    list.Add(new NameValuePair("SegmentLength", item.SegmentLength.Value.ToString(CultureInfo.InvariantCulture)));
                }

                if (item.MinSegments.HasValue)
                {
                    list.Add(new NameValuePair("MinSegments", item.MinSegments.Value.ToString(CultureInfo.InvariantCulture)));
                }

                list.Add(new NameValuePair("BreakOnNonKeyFrames", item.BreakOnNonKeyFrames.ToString(CultureInfo.InvariantCulture)));
            }

            foreach (var pair in item.StreamOptions)
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                // strip spaces to avoid having to encode h264 profile names
                list.Add(new NameValuePair(pair.Key, pair.Value.Replace(" ", "")));
            }

            if (!item.IsDirectStream)
            {
                list.Add(new NameValuePair("TranscodeReasons", string.Join(",", item.TranscodeReasons.Distinct().Select(i => i.ToString()))));
            }

            return list;
        }

        public List<SubtitleStreamInfo> GetExternalSubtitles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, string baseUrl, string accessToken)
        {
            return GetExternalSubtitles(transcoderSupport, includeSelectedTrackOnly, false, baseUrl, accessToken);
        }

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

        public List<SubtitleStreamInfo> GetSubtitleProfiles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, string baseUrl, string accessToken)
        {
            return GetSubtitleProfiles(transcoderSupport, includeSelectedTrackOnly, false, baseUrl, accessToken);
        }

        public List<SubtitleStreamInfo> GetSubtitleProfiles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, bool enableAllProfiles, string baseUrl, string accessToken)
        {
            var list = new List<SubtitleStreamInfo>();

            // HLS will preserve timestamps so we can just grab the full subtitle stream
            long startPositionTicks = string.Equals(SubProtocol, "hls", StringComparison.OrdinalIgnoreCase)
                ? 0
                : (PlayMethod == PlayMethod.Transcode && !CopyTimestamps ? StartPositionTicks : 0);

            // First add the selected track
            if (SubtitleStreamIndex.HasValue)
            {
                foreach (var stream in MediaSource.MediaStreams)
                {
                    if (stream.Type == MediaStreamType.Subtitle && stream.Index == SubtitleStreamIndex.Value)
                    {
                        AddSubtitleProfiles(list, stream, transcoderSupport, enableAllProfiles, baseUrl, accessToken, startPositionTicks);
                    }
                }
            }

            if (!includeSelectedTrackOnly)
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

        private void AddSubtitleProfiles(List<SubtitleStreamInfo> list, MediaStream stream, ITranscoderSupport transcoderSupport, bool enableAllProfiles, string baseUrl, string accessToken, long startPositionTicks)
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

        private SubtitleStreamInfo GetSubtitleStreamInfo(MediaStream stream, string baseUrl, string accessToken, long startPositionTicks, SubtitleProfile[] subtitleProfiles, ITranscoderSupport transcoderSupport)
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
                if (MediaSource.Protocol == MediaProtocol.File || !string.Equals(stream.Codec, subtitleProfile.Format, StringComparison.OrdinalIgnoreCase) || !stream.IsExternal)
                {
                    info.Url = string.Format("{0}/Videos/{1}/{2}/Subtitles/{3}/{4}/Stream.{5}",
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

        /// <summary>
        /// Returns the audio stream that will be used
        /// </summary>
        public MediaStream TargetAudioStream
        {
            get
            {
                if (MediaSource != null)
                {
                    return MediaSource.GetDefaultAudioStream(AudioStreamIndex);
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the video stream that will be used
        /// </summary>
        public MediaStream TargetVideoStream
        {
            get
            {
                if (MediaSource != null)
                {
                    return MediaSource.VideoStream;
                }

                return null;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public int? TargetAudioSampleRate
        {
            get
            {
                var stream = TargetAudioStream;
                return stream == null ? null : stream.SampleRate;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
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
        /// Predicts the audio sample rate that will be in the output stream
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
        /// <value>The target reference frames.</value>
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
        /// Predicts the audio sample rate that will be in the output stream
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
        /// Predicts the audio sample rate that will be in the output stream
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

        public int? GetTargetVideoBitDepth(string codec)
        {
            var value = GetOption(codec, "videobitdepth");
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

        public int? GetTargetRefFrames(string codec)
        {
            var value = GetOption(codec, "maxrefframes");
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
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public int? TargetPacketLength
        {
            get
            {
                var stream = TargetVideoStream;
                return !IsDirectStream
                    ? null
                    : stream == null ? null : stream.PacketLength;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public string TargetVideoProfile
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream == null ? null : TargetVideoStream.Profile;
                }

                var targetVideoCodecs = TargetVideoCodec;
                var videoCodec = targetVideoCodecs.Length == 0 ? null : targetVideoCodecs[0];
                if (!string.IsNullOrEmpty(videoCodec))
                {
                    return GetOption(videoCodec, "profile");
                }

                return TargetVideoStream == null ? null : TargetVideoStream.Profile;
            }
        }

        /// <summary>
        /// Gets the target video codec tag.
        /// </summary>
        /// <value>The target video codec tag.</value>
        public string TargetVideoCodecTag
        {
            get
            {
                var stream = TargetVideoStream;
                return !IsDirectStream
                    ? null
                    : stream == null ? null : stream.CodecTag;
            }
        }

        /// <summary>
        /// Predicts the audio bitrate that will be in the output stream
        /// </summary>
        public int? TargetAudioBitrate
        {
            get
            {
                var stream = TargetAudioStream;
                return AudioBitrate.HasValue && !IsDirectStream
                    ? AudioBitrate
                    : stream == null ? null : stream.BitRate;
            }
        }

        /// <summary>
        /// Predicts the audio channels that will be in the output stream
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

        public int? GetTargetAudioChannels(string codec)
        {
            var defaultValue = GlobalMaxAudioChannels;

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
        /// Predicts the audio codec that will be in the output stream
        /// </summary>
        public string[] TargetAudioCodec
        {
            get
            {
                var stream = TargetAudioStream;

                string inputCodec = stream == null ? null : stream.Codec;

                if (IsDirectStream)
                {
                    return string.IsNullOrEmpty(inputCodec) ? new string[] { } : new[] { inputCodec };
                }

                foreach (string codec in AudioCodecs)
                {
                    if (string.Equals(codec, inputCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        return string.IsNullOrEmpty(codec) ? new string[] { } : new[] { codec };
                    }
                }

                return AudioCodecs;
            }
        }

        public string[] TargetVideoCodec
        {
            get
            {
                var stream = TargetVideoStream;

                string inputCodec = stream == null ? null : stream.Codec;

                if (IsDirectStream)
                {
                    return string.IsNullOrEmpty(inputCodec) ? new string[] { } : new[] { inputCodec };
                }

                foreach (string codec in VideoCodecs)
                {
                    if (string.Equals(codec, inputCodec, StringComparison.OrdinalIgnoreCase))
                    {
                        return string.IsNullOrEmpty(codec) ? new string[] { } : new[] { codec };
                    }
                }

                return VideoCodecs;
            }
        }

        /// <summary>
        /// Predicts the audio channels that will be in the output stream
        /// </summary>
        public long? TargetSize
        {
            get
            {
                if (IsDirectStream)
                {
                    return MediaSource.Size;
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

        public int? TargetVideoBitrate
        {
            get
            {
                var stream = TargetVideoStream;

                return VideoBitrate.HasValue && !IsDirectStream
                    ? VideoBitrate
                    : stream == null ? null : stream.BitRate;
            }
        }

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

        public int? TargetTotalBitrate => (TargetAudioBitrate ?? 0) + (TargetVideoBitrate ?? 0);

        public bool? IsTargetAnamorphic
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream == null ? null : TargetVideoStream.IsAnamorphic;
                }

                return false;
            }
        }

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

        public bool? IsTargetAVC
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream == null ? null : TargetVideoStream.IsAVC;
                }

                return true;
            }
        }

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

        private int? GetMediaStreamCount(MediaStreamType type, int limit)
        {
            var count = MediaSource.GetStreamCount(type);

            if (count.HasValue)
            {
                count = Math.Min(count.Value, limit);
            }

            return count;
        }

        public List<MediaStream> GetSelectableAudioStreams()
        {
            return GetSelectableStreams(MediaStreamType.Audio);
        }

        public List<MediaStream> GetSelectableSubtitleStreams()
        {
            return GetSelectableStreams(MediaStreamType.Subtitle);
        }

        public List<MediaStream> GetSelectableStreams(MediaStreamType type)
        {
            var list = new List<MediaStream>();

            foreach (var stream in MediaSource.MediaStreams)
            {
                if (type == stream.Type)
                {
                    list.Add(stream);
                }
            }

            return list;
        }
    }
}
