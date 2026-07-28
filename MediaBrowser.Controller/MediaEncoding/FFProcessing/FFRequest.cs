using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// Base for every FFmpeg operation. Concrete requests carry their own parameters; this base holds
/// only what applies to every operation.
/// </summary>
public abstract record FFRequest
{
    /// <summary>
    /// Gets the executable to launch, when the operation names one rather than using the resolved
    /// encoder or prober. Empty for everything except validating a candidate path, which runs
    /// before any path has been accepted.
    /// </summary>
    public virtual string BinaryPathOverride => string.Empty;

    /// <summary>Gets the operation this request performs.</summary>
    public abstract FFAction Action { get; }

    /// <summary>
    /// Gets the wall-clock deadline. <see cref="TimeSpan.Zero"/> means the action's own deadline.
    /// Set this only where the timeout is user-configurable.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Gets the idle watchdog window. <see cref="TimeSpan.Zero"/> means the action's own; a zero
    /// window would kill the process before it could report progress. Only meaningful alongside
    /// <see cref="ProgressProbe"/>.
    /// </summary>
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Gets the scheduling priority. <see cref="FFDefaults.InheritPriority"/> means the action's
    /// own.
    /// </summary>
    public ProcessPriorityClass Priority { get; init; } = FFDefaults.InheritPriority;

    /// <summary>
    /// Gets the working directory. Empty means inherit, which is
    /// <see cref="ProcessStartInfo.WorkingDirectory"/>'s contract.
    /// </summary>
    public string WorkingDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Gets a reader that drains standard output and discards it. Draining is not optional even
    /// when the bytes are unwanted: an unread pipe fills, which blocks the child, which then
    /// never exits.
    /// </summary>
    public static Func<Stream, CancellationToken, Task> DiscardStdout { get; }
        = static (stdout, cancellationToken) => stdout.CopyToAsync(Stream.Null, cancellationToken);

    /// <summary>
    /// Gets the reader for standard output. Defaults to <see cref="DiscardStdout"/>; supply one to
    /// parse the output, as the probe and keyframe scans do. It receives the <em>caller's</em>
    /// cancellation token rather than the wall-clock deadline, since a consumer still parsing after
    /// the process has exited is not overrunning anything.
    /// </summary>
    public Func<Stream, CancellationToken, Task> Stdout { get; init; } = DiscardStdout;

    /// <summary>
    /// Gets where standard error goes. Defaults to a bounded in-memory buffer surfaced on
    /// <see cref="FFResult.Stderr"/>; use <see cref="FFOutputSink.Complete"/> when the output is
    /// the answer rather than a diagnostic, or <see cref="FFOutputSink.ToStream"/> to follow a
    /// long-running process live.
    /// </summary>
    public FFOutputSink Stderr { get; init; } = FFOutputSink.Diagnostic();

    /// <summary>
    /// Gets an optional liveness counter, such as files produced so far. The watchdog also tracks
    /// the child's CPU time; either advancing counts as alive.
    /// </summary>
    public Func<long> ProgressProbe { get; init; } = FFDefaults.NoProgressProbe;

    /// <summary>
    /// Gets a value indicating whether <see cref="ProgressProbe"/> reports real progress. Without
    /// one there is nothing to watch, so the idle watchdog does not run and the wall clock is the
    /// only bound.
    /// </summary>
    public bool HasProgressSignal => !ReferenceEquals(ProgressProbe, FFDefaults.NoProgressProbe);

    /// <summary>
    /// Resolves the process policy for this request: the action's policy, with this request's
    /// deadline and priority applied where it supplied them.
    /// </summary>
    /// <returns>The effective policy.</returns>
    public virtual FFActionPolicy ResolvePolicy()
    {
        var policy = FFActionPolicy.For(Action);

        if (Timeout != TimeSpan.Zero)
        {
            policy = policy with { Timeout = Timeout };
        }

        if (IdleTimeout != TimeSpan.Zero)
        {
            policy = policy with { IdleTimeout = IdleTimeout };
        }

        if (Priority != FFDefaults.InheritPriority)
        {
            policy = policy with { Priority = Priority };
        }

        return policy;
    }

    /// <summary>
    /// Appends this operation's arguments to <paramref name="builder"/>. The global flags implied
    /// by <see cref="FFAction"/> and the server log level have already been written, this appends
    /// a fragment for the action for the rest of the command line.
    /// </summary>
    /// <param name="builder">The buffer to append to.</param>
    public abstract void BuildArguments(StringBuilder builder);
}
