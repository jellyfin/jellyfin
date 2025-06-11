using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.LibraryTaskScheduler;

/// <summary>
/// Provides Parallel action interface to process tasks with a set concurrency level.
/// </summary>
public sealed class LimitedConcurrencyLibraryScheduler : ILimitedConcurrencyLibraryScheduler, IDisposable
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<LimitedConcurrencyLibraryScheduler> _logger;
    private readonly Dictionary<CancellationTokenSource, Task> _taskRunners = new();

    private static readonly AsyncLocal<CancellationTokenSource> _deadlockDetector = new();

    /// <summary>
    /// Gets used to lock all operations on the Tasks queue and creating workers.
    /// </summary>
    private readonly Lock _taskLock = new();

    private readonly BlockingCollection<TaskQueueItem> _tasks = new();

    private Task? _cleanupTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LimitedConcurrencyLibraryScheduler"/> class.
    /// </summary>
    /// <param name="hostApplicationLifetime">The hosting lifetime.</param>
    /// <param name="logger">The logger.</param>
    public LimitedConcurrencyLibraryScheduler(IHostApplicationLifetime hostApplicationLifetime, ILogger<LimitedConcurrencyLibraryScheduler> logger)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    private void ScheduleTaskCleanup()
    {
        lock (_taskLock)
        {
            if (_cleanupTask is not null)
            {
                // cleanup task is already running.
                return;
            }

            RunCleanupTask();
        }

        void RunCleanupTask()
        {
            _cleanupTask = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(
                t =>
                {
                    lock (_taskLock)
                    {
                        if (_tasks.Count > 0)
                        {
                            // tasks are still there so its still in use. Reschedule cleanup task.
                            // we cannot just exit here and rely on the other invoker because there is a considerable timeframe where it could have already ended.
                            RunCleanupTask();
                            return;
                        }

                        foreach (var item in _taskRunners.ToArray())
                        {
                            item.Key.Cancel();
                            _taskRunners.Remove(item.Key);
                        }
                    }
                },
                TaskScheduler.Default);
        }
    }

    private void Worker()
    {
        lock (_taskLock)
        {
            var fanoutConcurrency = BaseItem.ConfigurationManager.Configuration.LibraryScanFanoutConcurrency;
            var parallelism = (fanoutConcurrency > 0 ? fanoutConcurrency : Environment.ProcessorCount) - _taskRunners.Count;
            for (int i = 0; i < parallelism; i++)
            {
                var stopToken = new CancellationTokenSource();
                var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken.Token, _hostApplicationLifetime.ApplicationStopping);
                _taskRunners.Add(
                    combinedSource,
                    Task.Factory.StartNew(
                        ItemWorker,
                        (combinedSource, stopToken),
                        combinedSource.Token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                        TaskScheduler.Default));
            }
        }
    }

    private async Task ItemWorker(object? obj)
    {
        var stopToken = ((CancellationTokenSource TaskStop, CancellationTokenSource GlobalStop))obj!;
        _deadlockDetector.Value = stopToken.TaskStop;
        try
        {
            foreach (var item in _tasks.GetConsumingEnumerable(stopToken.GlobalStop.Token))
            {
                await ProcessItem(item).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stopToken.TaskStop.IsCancellationRequested)
        {
            // thats how you do it, interupt the waiter thread. There is nothing to do here when it was on purpose.
        }
        finally
        {
            _deadlockDetector.Value = default!;
            _taskRunners.Remove(stopToken.TaskStop);
            stopToken.GlobalStop.Dispose();
            stopToken.TaskStop.Dispose();
        }
    }

    private async Task ProcessItem(TaskQueueItem item)
    {
        try
        {
            if (item.CancellationToken.IsCancellationRequested)
            {
                // if item is cancled, just skip it
                return;
            }

            await item.Worker(item.Data).ConfigureAwait(true);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error while performing a library operation");
        }
        finally
        {
            item.Done.SetResult();
            item.Progress.Report(100);
        }
    }

    /// <inheritdoc/>
    public async Task Enqueue<T>(T[] data, Func<T, IProgress<double>, Task> worker, IProgress<double> progress, CancellationToken cancellationToken)
    {
        TaskQueueItem[] workItems = null!;

        void UpdateProgress()
        {
            progress.Report(workItems.Select(e => e.ProgressValue).Average());
        }

        workItems = data.Select(item =>
        {
            TaskQueueItem queueItem = null!;
            return queueItem = new TaskQueueItem()
            {
                Data = item!,
                Progress = new Progress<double>(innerPercent =>
                    {
                        // round the percent and only update progress if it changed to prevent excessive UpdateProgress calls
                        var innerPercentRounded = Math.Round(innerPercent);
                        if (queueItem.ProgressValue != innerPercentRounded)
                        {
                            queueItem.ProgressValue = innerPercentRounded;
                            UpdateProgress();
                        }
                    }),
                Worker = (val) => worker((T)val, queueItem.Progress),
                CancellationToken = cancellationToken
            };
        }).ToArray();

        for (var i = 0; i < workItems.Length; i++)
        {
            var item = workItems[i]!;
            _tasks.Add(item, CancellationToken.None);
        }

        if (_deadlockDetector.Value is not null)
        {
            // we are in a nested loop. There is no reason to spawn a task here as that would just lead to deadlocks and no additional concurrency is achieved
            while (workItems.Any(e => !e.Done.Task.IsCompleted) && _tasks.TryTake(out var item, 0, _deadlockDetector.Value.Token))
            {
                await ProcessItem(item).ConfigureAwait(false);
            }
        }
        else
        {
            Worker();
            await Task.WhenAll([.. workItems.Select(f => f.Done.Task)]).ConfigureAwait(false);
            ScheduleTaskCleanup();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _tasks.Dispose();
        _cleanupTask?.Dispose();
    }

    private class TaskQueueItem
    {
        public required object Data { get; init; }

        public double ProgressValue { get; set; }

        public required Func<object, Task> Worker { get; init; }

        public required IProgress<double> Progress { get; init; }

        public TaskCompletionSource Done { get; } = new();

        public CancellationToken CancellationToken { get; init; }
    }
}
