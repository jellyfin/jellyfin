using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// A running FFmpeg process the caller holds onto, for operations that outlive the call that
/// started them. Obtained from <see cref="IFFRunner.StartAsync"/>, which returns once the process
/// is running rather than once it has finished.
/// </summary>
public interface IFFSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the process id, for correlating with external logs.
    /// </summary>
    int ProcessId { get; }

    /// <summary>
    /// Gets a task that completes when the process has exited and its output has been drained.
    /// Carries the same outcome <see cref="IFFRunner.RunAsync(FFRequest, CancellationToken)"/>
    /// would have returned.
    /// </summary>
    Task<FFResult> Completion { get; }

    /// <summary>
    /// Sends one of FFmpeg's runtime keys, such as pause or resume. Requires the action to use
    /// <see cref="FFStdinMode.ControlChannel"/>. Do not send the quit key here — use
    /// <see cref="StopAsync"/>, which also handles the escalation to a kill.
    /// </summary>
    /// <param name="key">The key to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the write.</returns>
    Task SendKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Asks the process to stop, escalating to a kill of the whole tree if it does not exit within
    /// the action's grace period.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The outcome, once it has stopped.</returns>
    Task<FFResult> StopAsync(CancellationToken cancellationToken);
}
