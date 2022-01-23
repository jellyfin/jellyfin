#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Session
{
    [Flags]
    public enum TranscodeReason
    {
        None = 0,

        // Primary
        ContainerNotSupported = 1 << 0,
        VideoCodecNotSupported = 1 << 1,
        AudioCodecNotSupported = 1 << 2,
        SubtitleCodecNotSupported = 1 << 3,
        AudioIsExternal = 1 << 4,
        SecondaryAudioNotSupported = 1 << 5,

        // Video Constraints
        VideoProfileNotSupported = 1 << 6,
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
        AudioBitDepthNotSupported = 1 << 20,

        // Bitrate Constraints
        ContainerBitrateExceedsLimit = 1 << 17,
        VideoBitrateNotSupported = 1 << 18,
        AudioBitrateNotSupported = 1 << 19,

        // Errors
        UnknownVideoStreamInfo = 1 << 20,
        UnknownAudioStreamInfo = 1 << 21,
        DirectPlayError = 1 << 22,

        // Aliases
        ContainerReasons = ContainerNotSupported | ContainerBitrateExceedsLimit,
        AudioReasons = AudioCodecNotSupported | AudioBitrateNotSupported | AudioChannelsNotSupported | AudioProfileNotSupported | AudioSampleRateNotSupported | SecondaryAudioNotSupported | AudioBitDepthNotSupported | AudioIsExternal,
        VideoReasons = VideoCodecNotSupported | VideoResolutionNotSupported | AnamorphicVideoNotSupported | InterlacedVideoNotSupported | VideoBitDepthNotSupported | VideoBitrateNotSupported | VideoFramerateNotSupported | VideoLevelNotSupported | RefFramesNotSupported,
    }
}
