namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// Why a process stopped.
/// </summary>
public enum FFStopReason
{
    /// <summary>
    /// Not a settled outcome.
    /// </summary>
    Unknown,

    /// <summary>The process exited on its own. Consult the exit code.</summary>
    Exited,

    /// <summary>The caller's cancellation token fired; the process was killed.</summary>
    Cancelled,

    /// <summary>The wall-clock deadline elapsed; the process was killed.</summary>
    TimedOut,

    /// <summary>
    /// The process was alive but made no forward progress within the idle window, measured by its
    /// own CPU time and by the action's optional domain probe. It was killed.
    /// </summary>
    Stalled
}
