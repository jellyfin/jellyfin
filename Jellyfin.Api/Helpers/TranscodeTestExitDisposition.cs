namespace Jellyfin.Api.Helpers;

/// <summary>
/// An exited stress process's disposition, with real failures ordered ahead of source restart budgets.
/// </summary>
internal enum TranscodeTestExitDisposition
{
    /// <summary>
    /// The process or input failed, or its final diagnostics could not be verified.
    /// </summary>
    Failure,

    /// <summary>
    /// The input ended normally and may restart.
    /// </summary>
    Restart,

    /// <summary>
    /// The input ended normally but exhausted its restart budget, without establishing a capacity limit.
    /// </summary>
    RestartLimit
}
