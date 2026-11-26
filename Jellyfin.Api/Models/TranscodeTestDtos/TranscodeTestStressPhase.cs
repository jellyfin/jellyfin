namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// Where a stress test is.
/// </summary>
public enum TranscodeTestStressPhase
{
    /// <summary>
    /// The first stream is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// A stream was just added; the streams are not judged yet.
    /// </summary>
    WarmingUp,

    /// <summary>
    /// Every stream is keeping up with real time.
    /// </summary>
    KeepingUp,

    /// <summary>
    /// At least one stream is below the threshold; the test ends unless it catches up in time.
    /// </summary>
    FallingBehind,

    /// <summary>
    /// The test has ended.
    /// </summary>
    Finished
}
