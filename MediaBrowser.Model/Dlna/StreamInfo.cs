using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Class StreamInfo.
    /// </summary>
    public class StreamInfo
    {
        public string ItemId { get; set; }

        public bool IsDirectStream { get; set; }

        public DlnaProfileType MediaType { get; set; }

        public string Container { get; set; }

        public string Protocol { get; set; }

        public long StartPositionTicks { get; set; }

        public string VideoCodec { get; set; }

        public string AudioCodec { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? MaxAudioChannels { get; set; }

        public int? AudioBitrate { get; set; }

        public int? VideoBitrate { get; set; }

        public int? VideoLevel { get; set; }

        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public int? MaxFramerate { get; set; }

        public string DeviceProfileId { get; set; }
        public string DeviceId { get; set; }

        public long? RunTimeTicks { get; set; }

        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public bool EstimateContentLength { get; set; }

        public MediaSourceInfo MediaSource { get; set; }

        public string MediaSourceId
        {
            get
            {
                return MediaSource == null ? null : MediaSource.Id;
            }
        }

        public string ToUrl(string baseUrl)
        {
            return ToDlnaUrl(baseUrl);
        }

        public string ToDlnaUrl(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(baseUrl);
            }

            var dlnaCommand = BuildDlnaParam(this);

            var extension = string.IsNullOrEmpty(Container) ? string.Empty : "." + Container;

            baseUrl = baseUrl.TrimEnd('/');

            if (MediaType == DlnaProfileType.Audio)
            {
                return string.Format("{0}/audio/{1}/stream{2}?{3}", baseUrl, ItemId, extension, dlnaCommand);
            }

            if (string.Equals(Protocol, "hls", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format("{0}/videos/{1}/stream.m3u8?{2}", baseUrl, ItemId, dlnaCommand);
            }

            return string.Format("{0}/videos/{1}/stream{2}?{3}", baseUrl, ItemId, extension, dlnaCommand);
        }

        private static string BuildDlnaParam(StreamInfo item)
        {
            var usCulture = new CultureInfo("en-US");

            var list = new List<string>
            {
                item.DeviceProfileId ?? string.Empty,
                item.DeviceId ?? string.Empty,
                item.MediaSourceId ?? string.Empty,
                (item.IsDirectStream).ToString().ToLower(),
                item.VideoCodec ?? string.Empty,
                item.AudioCodec ?? string.Empty,
                item.AudioStreamIndex.HasValue ? item.AudioStreamIndex.Value.ToString(usCulture) : string.Empty,
                item.SubtitleStreamIndex.HasValue ? item.SubtitleStreamIndex.Value.ToString(usCulture) : string.Empty,
                item.VideoBitrate.HasValue ? item.VideoBitrate.Value.ToString(usCulture) : string.Empty,
                item.AudioBitrate.HasValue ? item.AudioBitrate.Value.ToString(usCulture) : string.Empty,
                item.MaxAudioChannels.HasValue ? item.MaxAudioChannels.Value.ToString(usCulture) : string.Empty,
                item.MaxFramerate.HasValue ? item.MaxFramerate.Value.ToString(usCulture) : string.Empty,
                item.MaxWidth.HasValue ? item.MaxWidth.Value.ToString(usCulture) : string.Empty,
                item.MaxHeight.HasValue ? item.MaxHeight.Value.ToString(usCulture) : string.Empty,
                item.StartPositionTicks.ToString(usCulture),
                item.VideoLevel.HasValue ? item.VideoLevel.Value.ToString(usCulture) : string.Empty
            };

            return string.Format("Params={0}", string.Join(";", list.ToArray()));
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
                    var audioStreams = MediaSource.MediaStreams.Where(i => i.Type == MediaStreamType.Audio);

                    if (AudioStreamIndex.HasValue)
                    {
                        return audioStreams.FirstOrDefault(i => i.Index == AudioStreamIndex.Value);
                    }

                    return audioStreams.FirstOrDefault();
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
                    return MediaSource.MediaStreams
                        .FirstOrDefault(i => i.Type == MediaStreamType.Video && (i.Codec ?? string.Empty).IndexOf("jpeg", StringComparison.OrdinalIgnoreCase) == -1);
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
                var stream = TargetAudioStream;

                return MaxAudioChannels.HasValue && !IsDirectStream
                    ? (stream.Channels.HasValue ? Math.Min(MaxAudioChannels.Value, stream.Channels.Value) : MaxAudioChannels.Value)
                    : stream == null ? null : stream.Channels;
            }
        }

        /// <summary>
        /// Predicts the audio codec that will be in the output stream
        /// </summary>
        public string TargetAudioCodec
        {
            get
            {
                var stream = TargetAudioStream;

                return IsDirectStream
                 ? (stream == null ? null : stream.Codec)
                 : AudioCodec;
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
                    return MediaSource.Bitrate;
                }

                if (RunTimeTicks.HasValue)
                {
                    var totalBitrate = 0;

                    if (AudioBitrate.HasValue)
                    {
                        totalBitrate += AudioBitrate.Value;
                    }
                    if (VideoBitrate.HasValue)
                    {
                        totalBitrate += VideoBitrate.Value;
                    }

                    return Convert.ToInt64(totalBitrate * TimeSpan.FromTicks(RunTimeTicks.Value).TotalSeconds);
                }
                var stream = TargetAudioStream;

                return MaxAudioChannels.HasValue && !IsDirectStream
                    ? (stream.Channels.HasValue ? Math.Min(MaxAudioChannels.Value, stream.Channels.Value) : MaxAudioChannels.Value)
                    : stream == null ? null : stream.Channels;
            }
        }
    }

    /// <summary>
    /// Class AudioOptions.
    /// </summary>
    public class AudioOptions
    {
        public string ItemId { get; set; }
        public List<MediaSourceInfo> MediaSources { get; set; }
        public DeviceProfile Profile { get; set; }

        /// <summary>
        /// Optional. Only needed if a specific AudioStreamIndex or SubtitleStreamIndex are requested.
        /// </summary>
        public string MediaSourceId { get; set; }

        public string DeviceId { get; set; }

        /// <summary>
        /// Allows an override of supported number of audio channels
        /// Example: DeviceProfile supports five channel, but user only has stereo speakers
        /// </summary>
        public int? MaxAudioChannels { get; set; }

        /// <summary>
        /// The application's configured quality setting
        /// </summary>
        public int? MaxBitrate { get; set; }
    }

    /// <summary>
    /// Class VideoOptions.
    /// </summary>
    public class VideoOptions : AudioOptions
    {
        public int? AudioStreamIndex { get; set; }
        public int? SubtitleStreamIndex { get; set; }
        public int? MaxAudioTranscodingBitrate { get; set; }

        public VideoOptions()
        {
            MaxAudioTranscodingBitrate = 128000;
        }
    }
}
