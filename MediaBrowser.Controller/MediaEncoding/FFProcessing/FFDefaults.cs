using System;
using System.Diagnostics;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing;

/// <summary>
/// Shared defaults for <see cref="FFRequest"/>.
/// </summary>
public static class FFDefaults
{
    /// <summary>
    /// Gets the "use the action's priority" value. <see cref="ProcessPriorityClass"/> has no zero
    /// member, so zero cannot collide with a real priority.
    /// </summary>
    public const ProcessPriorityClass InheritPriority = 0;

    /// <summary>Gets a progress probe that contributes no liveness signal.</summary>
    public static Func<long> NoProgressProbe { get; } = static () => 0;

    /// <summary>
    /// Gets the "no deadline" value, which
    /// <see cref="System.Threading.CancellationTokenSource.CancelAfter(TimeSpan)"/> treats as never
    /// firing.
    /// </summary>
    public static TimeSpan Unbounded => System.Threading.Timeout.InfiniteTimeSpan;
}
