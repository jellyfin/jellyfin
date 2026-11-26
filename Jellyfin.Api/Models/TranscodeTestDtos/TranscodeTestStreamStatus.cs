namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// The status of one stream of the capacity test.
/// </summary>
public enum TranscodeTestStreamStatus
{
    /// <summary>
    /// It is running.
    /// </summary>
    Running,

    /// <summary>
    /// The test stopped it.
    /// </summary>
    Stopped,

    /// <summary>
    /// ffmpeg exited with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// It ended without producing a frame.
    /// </summary>
    NotStarted
}
