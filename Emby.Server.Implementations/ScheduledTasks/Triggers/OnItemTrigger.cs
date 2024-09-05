using System;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Triggers;

/// <summary>
/// Trigger for running a task after base items where changed.
/// </summary>
public class OnItemTrigger : ITaskTrigger
{
    /// <inheritdoc/>
    public event EventHandler<EventArgs>? Triggered;

    /// <inheritdoc/>
    public TaskOptions TaskOptions => new TaskOptions();

    /// <inheritdoc/>
    public void Start(TaskResult? lastResult, ILogger logger, string taskName, bool isApplicationStartup)
    {
        Triggered?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Stop()
    {
    }
}
