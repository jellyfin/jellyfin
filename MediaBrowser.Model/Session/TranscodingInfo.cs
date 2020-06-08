#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Session
{
    public class TranscodingInfo
    {
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

        public TranscodingInfo()
        {
            TranscodeReasons = Array.Empty<TranscodeReason>();
        }
    }

    public enum TranscodeReason
    {
        ContainerNotSupported = 0,
        VideoCodecNotSupported = 1,
        AudioCodecNotSupported = 2,
        ContainerBitrateExceedsLimit = 3,
        AudioBitrateNotSupported = 4,
        AudioChannelsNotSupported = 5,
        VideoResolutionNotSupported = 6,
        UnknownVideoStreamInfo = 7,
        UnknownAudioStreamInfo = 8,
        AudioProfileNotSupported = 9,
        AudioSampleRateNotSupported = 10,
        AnamorphicVideoNotSupported = 11,
        InterlacedVideoNotSupported = 12,
        SecondaryAudioNotSupported = 13,
        RefFramesNotSupported = 14,
        VideoBitDepthNotSupported = 15,
        VideoBitrateNotSupported = 16,
        VideoFramerateNotSupported = 17,
        VideoLevelNotSupported = 18,
        VideoProfileNotSupported = 19,
        AudioBitDepthNotSupported = 20,
        SubtitleCodecNotSupported = 21,
        DirectPlayError = 22
    }
}
