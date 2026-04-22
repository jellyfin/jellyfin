using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using Emby.Server.Implementations.ScheduledTasks;
using Jellyfin.Data.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Class RefreshMediaLibraryTask.
/// </summary>
public class RefreshMediaLibraryTask : IScheduledTask
{
    private readonly ILocalizationManager _localization;
    private readonly ITaskManager _taskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshMediaLibraryTask" /> class.
    /// </summary>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="taskManager">Instance of the <see cref="ITaskManager"/> interface.</param>
    public RefreshMediaLibraryTask(ILocalizationManager localization, ITaskManager taskManager)
    {
        _localization = localization;
        _taskManager = taskManager;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskRefreshLibrary");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskRefreshLibraryDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public string Key => "RefreshLibrary";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(12).Ticks
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        progress.Report(0);

        // Cancel any running phase tasks before starting full scan
        await CancelAndWaitForPhaseTaskAsync<RefreshMediaLibraryPhase1Task>(cancellationToken).ConfigureAwait(false);
        await CancelAndWaitForPhaseTaskAsync<RefreshMediaLibraryPhase2Task>(cancellationToken).ConfigureAwait(false);

        // Phase 1: discovery and local NFO metadata (0% - 40% of full scan)
        var phase1Worker = GetTaskWorker<RefreshMediaLibraryPhase1Task>();
        void OnPhase1Progress(object? sender, GenericEventArgs<double> e) => progress.Report(e.Argument * 0.4);
        phase1Worker.TaskProgress += OnPhase1Progress;
        using (cancellationToken.Register(() => ((ScheduledTaskWorker)phase1Worker).CancelIfRunning()))
        {
            try
            {
                await _taskManager.Execute(phase1Worker, new TaskOptions()).ConfigureAwait(false);
            }
            finally
            {
                phase1Worker.TaskProgress -= OnPhase1Progress;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Phase 2: external metadata refresh (40% - 100% of full scan)
        var phase2Worker = GetTaskWorker<RefreshMediaLibraryPhase2Task>();
        void OnPhase2Progress(object? sender, GenericEventArgs<double> e) => progress.Report(0.4 + (e.Argument * 0.6));
        phase2Worker.TaskProgress += OnPhase2Progress;
        using (cancellationToken.Register(() => ((ScheduledTaskWorker)phase2Worker).CancelIfRunning()))
        {
            try
            {
                await _taskManager.Execute(phase2Worker, new TaskOptions()).ConfigureAwait(false);
            }
            finally
            {
                phase2Worker.TaskProgress -= OnPhase2Progress;
            }
        }
    }

    private async Task CancelAndWaitForPhaseTaskAsync<T>(CancellationToken cancellationToken)
        where T : IScheduledTask
    {
        var worker = _taskManager.ScheduledTasks.FirstOrDefault(t => t.ScheduledTask is T);
        if (worker is ScheduledTaskWorker scheduledTaskWorker)
        {
            scheduledTaskWorker.CancelIfRunning();
            while (worker.State != TaskState.Idle)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private IScheduledTaskWorker GetTaskWorker<T>()
        where T : IScheduledTask
    {
        return _taskManager.ScheduledTasks.First(t => t.ScheduledTask is T);
    }
}
