#pragma warning disable CS1591

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// This exists simply to identify a set of known commands.
    /// </summary>
    public enum GeneralCommandType
    {
        MoveUp = 0,
        MoveDown = 1,
        MoveLeft = 2,
        MoveRight = 3,
        PageUp = 4,
        PageDown = 5,
        PreviousLetter = 6,
        NextLetter = 7,
        ToggleOsd = 8,
        ToggleContextMenu = 9,
        Select = 10,
        Back = 11,
        TakeScreenshot = 12,
        SendKey = 13,
        SendString = 14,
        GoHome = 15,
        GoToSettings = 16,
        VolumeUp = 17,
        VolumeDown = 18,
        Mute = 19,
        Unmute = 20,
        ToggleMute = 21,
        SetVolume = 22,
        SetAudioStreamIndex = 23,
        SetSubtitleStreamIndex = 24,
        ToggleFullscreen = 25,
        DisplayContent = 26,
        GoToSearch = 27,
        DisplayMessage = 28,
        SetRepeatMode = 29,
        ChannelUp = 30,
        ChannelDown = 31,
        SetMaxStreamingBitrate = 31,
        Guide = 32,
        ToggleStats = 33,
        PlayMediaSource = 34,
        PlayTrailers = 35
    }
}
