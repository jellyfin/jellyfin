#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class BaseEncodingJobOptions
    {
        public BaseEncodingJobOptions()
        {
            EnableAutoStreamCopy = true;
            AllowVideoStreamCopy = true;
            AllowAudioStreamCopy = true;
            Context = EncodingContext.Streaming;
            StreamOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }

        public string MediaSourceId { get; set; }

        public string DeviceId { get; set; }

        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        /// <value>The audio codec.</value>
        public string AudioCodec { get; set; }

        public bool EnableAutoStreamCopy { get; set; }

        public bool AllowVideoStreamCopy { get; set; }

        public bool AllowAudioStreamCopy { get; set; }

        public bool BreakOnNonKeyFrames { get; set; }

        /// <summary>
        /// Gets or sets the audio sample rate.
        /// </summary>
        /// <value>The audio sample rate.</value>
        public int? AudioSampleRate { get; set; }

        public int? MaxAudioBitDepth { get; set; }

        /// <summary>
        /// Gets or sets the audio bit rate.
        /// </summary>
        /// <value>The audio bit rate.</value>
        public int? AudioBitRate { get; set; }

        /// <summary>
        /// Gets or sets the audio channels.
        /// </summary>
        /// <value>The audio channels.</value>
        public int? AudioChannels { get; set; }

        public int? MaxAudioChannels { get; set; }

        public bool Static { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the video range type.
        /// </summary>
        /// <value>The video range type.</value>
        public string VideoRangeType { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        public string Level { get; set; }

        /// <summary>
        /// Gets or sets the codec tag.
        /// </summary>
        /// <value>The codec tag.</value>
        public string CodecTag { get; set; }

        /// <summary>
        /// Gets or sets the framerate.
        /// </summary>
        /// <value>The framerate.</value>
        public float? Framerate { get; set; }

        public float? MaxFramerate { get; set; }

        public bool CopyTimestamps { get; set; }

        /// <summary>
        /// Gets or sets the start time ticks.
        /// </summary>
        /// <value>The start time ticks.</value>
        public long? StartTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the width of the max.
        /// </summary>
        /// <value>The width of the max.</value>
        public int? MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the max.
        /// </summary>
        /// <value>The height of the max.</value>
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the video bit rate.
        /// </summary>
        /// <value>The video bit rate.</value>
        public int? VideoBitRate { get; set; }

        /// <summary>
        /// Gets or sets the index of the subtitle stream.
        /// </summary>
        /// <value>The index of the subtitle stream.</value>
        public int? SubtitleStreamIndex { get; set; }

        public SubtitleDeliveryMethod SubtitleMethod { get; set; }

        public int? MaxRefFrames { get; set; }

        public int? MaxVideoBitDepth { get; set; }

        public bool RequireAvc { get; set; }

        public bool DeInterlace { get; set; }

        public bool RequireNonAnamorphic { get; set; }

        public int? TranscodingMaxAudioChannels { get; set; }

        public int? CpuCoreLimit { get; set; }

        public string LiveStreamId { get; set; }

        public bool EnableMpegtsM2TsMode { get; set; }

        /// <summary>
        /// Gets or sets the video codec.
        /// </summary>
        /// <value>The video codec.</value>
        public string VideoCodec { get; set; }

        public string SubtitleCodec { get; set; }

        public string TranscodeReasons { get; set; }

        /// <summary>
        /// Gets or sets the index of the audio stream.
        /// </summary>
        /// <value>The index of the audio stream.</value>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the video stream.
        /// </summary>
        /// <value>The index of the video stream.</value>
        public int? VideoStreamIndex { get; set; }

        public EncodingContext Context { get; set; }

        public Dictionary<string, string> StreamOptions { get; set; }

        public bool EnableAudioVbrEncoding { get; set; }

        public bool AlwaysBurnInSubtitleWhenTranscoding { get; set; }

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
    }
}
