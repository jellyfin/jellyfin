namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// The outcome of one hardware check.
/// </summary>
public enum TranscodeTestOutcome
{
    /// <summary>
    /// It ran on the hardware.
    /// </summary>
    Passed,

    /// <summary>
    /// ffmpeg failed.
    /// </summary>
    Failed,

    /// <summary>
    /// It ran, but ffmpeg fell back to the CPU.
    /// </summary>
    Software,

    /// <summary>
    /// Jellyfin has no hardware path for this with the selected acceleration.
    /// </summary>
    NotSupported,

    /// <summary>
    /// Not tested.
    /// </summary>
    Skipped
}
