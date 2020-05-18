#pragma warning disable CS1591
#pragma warning disable CA1819 // Properties should not return arrays

using System;

namespace MediaBrowser.Model.Session
{
    public enum TranscodeReason
    {
        /// <summary>
        /// Container not supported
        /// </summary>
        ContainerNotSupported = 0,

        /// <summary>
        /// Video codec not supported
        /// </summary>
        VideoCodecNotSupported = 1,

        /// <summary>
        /// Audio codec not supported
        /// </summary>
        AudioCodecNotSupported = 2,

        /// <summary>
        /// Container bitrate exceeds limit
        /// </summary>
        ContainerBitrateExceedsLimit = 3,

        /// <summary>
        /// Audio bitrate not supported
        /// </summary>
        AudioBitrateNotSupported = 4,

        /// <summary>
        /// Audio channels not supported
        /// </summary>
        AudioChannelsNotSupported = 5,

        /// <summary>
        /// Video resolution not supported
        /// </summary>
        VideoResolutionNotSupported = 6,

        /// <summary>
        /// Unknown video stream info
        /// </summary>
        UnknownVideoStreamInfo = 7,

        /// <summary>
        /// Unknown audio stream info
        /// </summary>
        UnknownAudioStreamInfo = 8,

        /// <summary>
        /// Audio profile not supported
        /// </summary>
        AudioProfileNotSupported = 9,

        /// <summary>
        /// Audio sample rate not supported
        /// </summary>
        AudioSampleRateNotSupported = 10,

        /// <summary>
        /// Anamorphic video not supported
        /// </summary>
        AnamorphicVideoNotSupported = 11,

        /// <summary>
        /// Interlaced video not supported
        /// </summary>
        InterlacedVideoNotSupported = 12,

        /// <summary>
        /// Secondary audio not supported
        /// </summary>
        SecondaryAudioNotSupported = 13,

        /// <summary>
        /// Ref frames not supported
        /// </summary>
        RefFramesNotSupported = 14,

        /// <summary>
        /// Video bit depth not supported
        /// </summary>
        VideoBitDepthNotSupported = 15,

        /// <summary>
        /// Video bitrate not supported
        /// </summary>
        VideoBitrateNotSupported = 16,

        /// <summary>
        /// Video framerate not supported
        /// </summary>
        VideoFramerateNotSupported = 17,

        /// <summary>
        /// Video level not supported
        /// </summary>
        VideoLevelNotSupported = 18,

        /// <summary>
        /// Video profile not supported
        /// </summary>
        VideoProfileNotSupported = 19,

        /// <summary>
        /// Audio bit depth not supported
        /// </summary>
        AudioBitDepthNotSupported = 20,

        /// <summary>
        /// Subtitle codec not supported
        /// </summary>
        SubtitleCodecNotSupported = 21,

        /// <summary>
        /// Direct play error
        /// </summary>
        DirectPlayError = 22
    }

    public class TranscodingInfo
    {
        public TranscodingInfo()
        {
            TranscodeReasons = Array.Empty<TranscodeReason>();
        }

        public string AudioCodec { get; set; }

        public string VideoCodec { get; set; }

        public string Container { get; set; }

        public bool IsVideoDirect { get; set; }

        public bool IsAudioDirect { get; set; }

        public int? Bitrate { get; set; }

        public float? Framerate { get; set; }

        public double? CompletionPercentage { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? AudioChannels { get; set; }

        public TranscodeReason[] TranscodeReasons { get; set; }
    }
}
