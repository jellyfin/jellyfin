using System;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// The outcome of a completed FFmpeg or ffprobe run.
/// </summary>
/// <param name="ExitCode">The exit code, or <c>-1</c> if the process was killed.</param>
/// <param name="Stderr">The retained tail of standard error.</param>
/// <param name="Elapsed">Wall-clock duration from start to exit.</param>
/// <param name="StopReason">Why the process stopped.</param>
public readonly record struct FFResult(
    int ExitCode,
    string Stderr,
    TimeSpan Elapsed,
    FFStopReason StopReason)
{
    /// <summary>
    /// Gets a value indicating whether the process ran to completion and reported success.
    /// </summary>
    public bool Succeeded => StopReason == FFStopReason.Exited && ExitCode == 0;
}
