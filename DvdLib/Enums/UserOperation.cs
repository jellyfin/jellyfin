using System;

namespace DvdLib.Enums;

/// <summary>
/// The User Operation Flags (Uops).
/// </summary>
[Flags]
public enum UserOperation
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Time play or search.
    /// </summary>
    TimePlayOrSearch = 1,

    /// <summary>
    /// PTT play or search.
    /// </summary>
    PTTPlayOrSearch = 2,

    /// <summary>
    /// Title play.
    /// </summary>
    TitlePlay = 4,

    /// <summary>
    /// Stop.
    /// </summary>
    Stop = 8,

    /// <summary>
    /// Go up.
    /// </summary>
    GoUp = 16,

    /// <summary>
    /// Time or PTT search.
    /// </summary>
    TimeOrPTTSearch = 32,

    /// <summary>
    /// Top or previous program search.
    /// </summary>
    TopOrPrevProgramSearch = 64,

    /// <summary>
    /// Next program search.
    /// </summary>
    NextProgramSearch = 128,

    /// <summary>
    /// Forward scan.
    /// </summary>
    ForwardScan = 256,

    /// <summary>
    /// Backward scan.
    /// </summary>
    BackwardScan = 512,

    /// <summary>
    /// Menu call - Title.
    /// </summary>
    MenuCallTitle = 1024,

    /// <summary>
    /// Menu call - Root.
    /// </summary>
    MenuCallRoot = 2048,

    /// <summary>
    /// Menu call - Subpicture.
    /// </summary>
    MenuCallSubpicture = 4096,

    /// <summary>
    ///Menu call -  Audio menu.
    /// </summary>
    MenuCallAudio = 8192,

    /// <summary>
    /// Menu call - Angle menu.
    /// </summary>
    MenuCallAngle = 16384,

    /// <summary>
    /// Menu call - Chapter.
    /// </summary>
    MenuCallChapter = 32768,

    /// <summary>
    /// Resume.
    /// </summary>
    Resume = 65536,

    /// <summary>
    /// Button select or activate.
    /// </summary>
    ButtonSelectOrActivate = 131072,

    /// <summary>
    /// Still off.
    /// </summary>
    StillOff = 262144,

    /// <summary>
    /// Pause on.
    /// </summary>
    PauseOn = 524288,

    /// <summary>
    /// Audio stream change.
    /// </summary>
    AudioStreamChange = 1048576,

    /// <summary>
    /// Subpicture stream change.
    /// </summary>
    SubpictureStreamChange = 2097152,

    /// <summary>
    /// Angle change.
    /// </summary>
    AngleChange = 4194304,

    /// <summary>
    /// Karaoke audio mix change.
    /// </summary>
    KaraokeAudioMixChange = 8388608,

    /// <summary>
    /// Video presentation mode change.
    /// </summary>
    VideoPresentationModeChange = 16777216
}
