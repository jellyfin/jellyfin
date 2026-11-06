namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// The state of a transcoding test.
/// </summary>
public enum TranscodeTestState
{
    /// <summary>
    /// No test has run since the server started.
    /// </summary>
    Idle,

    /// <summary>
    /// A test is running.
    /// </summary>
    Running,

    /// <summary>
    /// The test finished.
    /// </summary>
    Completed,

    /// <summary>
    /// The test could not run.
    /// </summary>
    Failed,

    /// <summary>
    /// The test was stopped.
    /// </summary>
    Cancelled
}
