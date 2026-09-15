using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediaBrowser.Common.Telemetry;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Controller.Telemetry;

/// <summary>
/// Scheduled task instruments published on <see cref="JellyfinTelemetry.Meter"/>.
/// </summary>
public static class TaskMetrics
{
    /// <summary>
    /// The name of the histogram recording how long scheduled tasks run.
    /// </summary>
    public const string TaskDurationName = "jellyfin.task.duration";

    private const string TaskTag = "jellyfin.task.key";
    private const string StatusTag = "jellyfin.task.status";

    private static readonly Counter<long> _executions = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.task.executions",
        "{execution}",
        "Scheduled task executions that ended, by status.");

    private static readonly Histogram<double> _duration = JellyfinTelemetry.Meter.CreateHistogram<double>(
        TaskDurationName,
        "s",
        "Wall clock time a scheduled task ran for.");

    /// <summary>
    /// Records that a scheduled task finished running.
    /// </summary>
    /// <param name="key">The key identifying the task.</param>
    /// <param name="status">How the execution ended.</param>
    /// <param name="duration">How long the execution took.</param>
    public static void OnTaskCompleted(string? key, TaskCompletionStatus status, TimeSpan duration)
    {
        var tags = new TagList
        {
            { TaskTag, TelemetryTags.Normalize(key) },
            { StatusTag, status.ToString() }
        };

        _executions.Add(1, tags);
        _duration.Record(duration.TotalSeconds, tags);
    }
}
