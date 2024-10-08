#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Session
{
    [Flags]
    public enum TranscodeReason
    {
        // Primary
        ContainerNotSupported = 1 << 0,
        VideoCodecNotSupported = 1 << 1,
        AudioCodecNotSupported = 1 << 2,
        SubtitleCodecNotSupported = 1 << 3,
        AudioIsExternal = 1 << 4,
        SecondaryAudioNotSupported = 1 << 5,

        // Video Constraints
        VideoProfileNotSupported = 1 << 6,
        VideoRangeTypeNotSupported = 1 << 24,
        VideoCodecTagNotSupported = 1 << 25,
        VideoLevelNotSupported = 1 << 7,
        VideoResolutionNotSupported = 1 << 8,
        VideoBitDepthNotSupported = 1 << 9,
        VideoFramerateNotSupported = 1 << 10,
        RefFramesNotSupported = 1 << 11,
        AnamorphicVideoNotSupported = 1 << 12,
        InterlacedVideoNotSupported = 1 << 13,

        // Audio Constraints
        AudioChannelsNotSupported = 1 << 14,
        AudioProfileNotSupported = 1 << 15,
        AudioSampleRateNotSupported = 1 << 16,
        AudioBitDepthNotSupported = 1 << 17,

        // Bitrate Constraints
        ContainerBitrateExceedsLimit = 1 << 18,
        VideoBitrateNotSupported = 1 << 19,
        AudioBitrateNotSupported = 1 << 20,

        // Errors
        UnknownVideoStreamInfo = 1 << 21,
        UnknownAudioStreamInfo = 1 << 22,
        DirectPlayError = 1 << 23,
    }
}
