using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.ScheduledTasks.Triggers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks;

/// <summary>
/// Worker for BaseItem aware tasks.
/// </summary>
public class BaseItemScheduledTaskWorker : ScheduledTaskWorker
{
    private readonly ILibraryManager _libraryManager;
    private ConcurrentBag<(BaseItem, TaskActionTypes)> _baseItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItemScheduledTaskWorker"/> class.
    /// </summary>
    /// <param name="scheduledTask">Scheduled Task.</param>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="taskManager">Task manager.</param>
    /// <param name="logger">logger.</param>
    /// <param name="libraryManager">Library manager.</param>
    public BaseItemScheduledTaskWorker(IScheduledTask scheduledTask, IApplicationPaths applicationPaths, ITaskManager taskManager, ILogger logger, ILibraryManager libraryManager)
        : base(scheduledTask, applicationPaths, taskManager, logger)
    {
        _baseItems = [];
        _libraryManager = libraryManager;
        _libraryManager.ItemAdded += ItemChanged;
        _libraryManager.ItemRemoved += ItemDeleted;
        _libraryManager.ItemUpdated += ItemChanged;
    }

    private void ItemChanged(object? sender, ItemChangeEventArgs e)
    {
        EnqueueRun(e.Item, e.UpdateReason switch
            {
                ItemUpdateType.None => TaskActionTypes.Added,
                _ => TaskActionTypes.Changed
            });
    }

    private void ItemDeleted(object? sender, ItemChangeEventArgs e)
    {
        EnqueueRun(e.Item, TaskActionTypes.Removed);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool dispose)
    {
        _libraryManager.ItemAdded -= ItemChanged;
        _libraryManager.ItemRemoved -= ItemChanged;
        _libraryManager.ItemUpdated -= ItemChanged;
        base.Dispose(dispose);
    }

    /// <summary>
    /// Enqueues a changed <see cref="BaseItem"/> to be processed by all scheduled tasks that are registered for it.
    /// </summary>
    /// <param name="baseItem">The base Item to Process.</param>
    /// <param name="taskActionType">The type of change.</param>
    public void EnqueueRun(BaseItem baseItem, TaskActionTypes taskActionType)
    {
        var trigger = InternalTriggers.FirstOrDefault(e => e.Item2 is OnItemTrigger);

        if (trigger is null)
        {
            return;
        }

        var task = ScheduledTask as IBaseItemScheduledTask;

        if (task is null)
        {
            return;
        }

        _baseItems.Add((baseItem, taskActionType));
        if (_baseItems.Count == 1)
        {
            var changeCounter = -1;
            Task.Run(async () =>
            {
                do
                {
                    changeCounter = _baseItems.Count;
                    await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                } while (changeCounter != _baseItems.Count);

                await Execute(trigger.Item2.TaskOptions).ConfigureAwait(false);
            });
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteTask(Progress<double> progress, CancellationTokenSource cancellationTokenSource)
    {
        var items = Interlocked.Exchange(ref _baseItems, new ConcurrentBag<(BaseItem, TaskActionTypes)>())
            .ToImmutableArray();
        await (ScheduledTask as IBaseItemScheduledTask)!
            .ExecuteAsync(progress, items, cancellationTokenSource.Token).ConfigureAwait(false);
    }
}
