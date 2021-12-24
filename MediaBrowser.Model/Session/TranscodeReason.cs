#pragma warning disable CS1591

namespace MediaBrowser.Model.Session
{
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
        DirectPlayError = 22,
        AudioIsExternal = 23
    }
}
