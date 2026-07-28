using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// Spawns ffmpeg and ffprobe. Every run redirects all three pipes, drains stdout and stderr,
/// bounds itself with a wall clock and an idle watchdog, and kills the process tree on
/// cancellation.
/// </summary>
public interface IFFRunner
{
    /// <summary>
    /// Runs an operation and waits for it to finish. Equivalent to <see cref="StartAsync"/>
    /// followed by awaiting <see cref="IFFSession.Completion"/>, and implemented that way, so the
    /// two entry points cannot drift apart.
    /// </summary>
    /// <param name="request">The operation and its parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The outcome.</returns>
    Task<FFResult> RunAsync(FFRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Starts an operation and returns once the process is running, leaving the caller to decide
    /// when to stop it. For work that outlives the call — recordings and transcodes — where
    /// <see cref="RunAsync(FFRequest, CancellationToken)"/> would block until exit.
    /// <para>
    /// Pair with <see cref="FFOutputSink.ToStream"/> so the log is readable while the process runs.
    /// </para>
    /// </summary>
    /// <param name="request">The operation and its parameters.</param>
    /// <param name="cancellationToken">Cancels the start, and thereafter stops the process.</param>
    /// <returns>A handle on the running process.</returns>
    Task<IFFSession> StartAsync(FFRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Renders the command line this request would run, binary included, without running anything.
    /// <para>
    /// For callers that write the command into a user-facing log. Since the runner supplies the
    /// global flags — <c>-hide_banner</c>, the derived <c>-loglevel</c>, <c>-stats</c> and the
    /// overwrite flag — a caller logging only its own <c>Arguments</c> records something that does
    /// not reproduce when pasted into a shell. Ask for this instead.
    /// </para>
    /// </summary>
    /// <param name="request">The operation and its parameters.</param>
    /// <returns>The full command line, exactly as <see cref="StartAsync"/> would launch it.</returns>
    string GetCommandLine(FFRequest request);
}
