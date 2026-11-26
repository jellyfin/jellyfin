namespace Jellyfin.Api.Helpers;

/// <summary>
/// How a stress-test stream ended the run.
/// </summary>
internal enum TranscodeTestEndKind
{
    /// <summary>
    /// ffmpeg exited with a non-zero code.
    /// </summary>
    Exited,

    /// <summary>
    /// The stream produced no frames before the start timeout.
    /// </summary>
    Stalled
}
