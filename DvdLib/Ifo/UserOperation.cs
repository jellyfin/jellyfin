#pragma warning disable CS1591

using System;

namespace DvdLib.Ifo
{
    [Flags]
    public enum UserOperation
    {
        None = 0,
        TitleOrTimePlay = 1,
        ChapterSearchOrPlay = 2,
        TitlePlay = 4,
        Stop = 8,
        GoUp = 16,
        TimeOrChapterSearch = 32,
        PrevOrTopProgramSearch = 64,
        NextProgramSearch = 128,
        ForwardScan = 256,
        BackwardScan = 512,
        TitleMenuCall = 1024,
        RootMenuCall = 2048,
        SubpictureMenuCall = 4096,
        AudioMenuCall = 8192,
        AngleMenuCall = 16384,
        ChapterMenuCall = 32768,
        Resume = 65536,
        ButtonSelectOrActive = 131072,
        StillOff = 262144,
        PauseOn = 524288,
        AudioStreamChange = 1048576,
        SubpictureStreamChange = 2097152,
        AngleChange = 4194304,
        KaraokeAudioPresentationModeChange = 8388608,
        VideoPresentationModeChange = 16777216,
    }
}
