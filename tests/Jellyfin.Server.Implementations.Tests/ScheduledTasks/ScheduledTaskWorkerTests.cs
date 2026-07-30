using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.ScheduledTasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ScheduledTasks;

public sealed class ScheduledTaskWorkerTests
{
    [Fact]
    public async Task Execute_PreventsConcurrentRunAndAllowsQueuedRun()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "jellyfin-scheduled-task-tests", Guid.NewGuid().ToString("N"));
        var applicationPaths = new Mock<IApplicationPaths>();
        applicationPaths.SetupGet(paths => paths.ConfigurationDirectoryPath).Returns(testDirectory);
        applicationPaths.SetupGet(paths => paths.DataPath).Returns(testDirectory);

        var scheduledTask = new BlockingScheduledTask();
        using var taskManager = new TaskManager(applicationPaths.Object, NullLogger<TaskManager>.Instance);
        taskManager.AddTasks([scheduledTask]);
        var worker = Assert.IsType<ScheduledTaskWorker>(Assert.Single(taskManager.ScheduledTasks));
        var completionCount = 0;
        var queuedExecutionCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        taskManager.TaskCompleted += (_, _) =>
        {
            if (Interlocked.Increment(ref completionCount) == 2)
            {
                queuedExecutionCompleted.TrySetResult(true);
            }
        };

        try
        {
            var firstExecution = worker.Execute(new TaskOptions());
            var secondExecution = worker.Execute(new TaskOptions());

            try
            {
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => secondExecution.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
                await scheduledTask.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                Assert.Equal(1, scheduledTask.ExecutionCount);
                taskManager.QueueScheduledTask(scheduledTask, new TaskOptions());
            }
            finally
            {
                scheduledTask.Release.TrySetResult(true);
                await firstExecution;
            }

            await scheduledTask.QueuedExecutionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await queuedExecutionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(2, scheduledTask.ExecutionCount);
        }
        finally
        {
            scheduledTask.Release.TrySetResult(true);
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    private sealed class BlockingScheduledTask : IScheduledTask
    {
        private int _executionCount;

        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> QueuedExecutionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ExecutionCount => Volatile.Read(ref _executionCount);

        public string Name => nameof(BlockingScheduledTask);

        public string Key => nameof(BlockingScheduledTask);

        public string Description => string.Empty;

        public string Category => string.Empty;

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _executionCount) == 1)
            {
                Started.TrySetResult(true);
            }
            else
            {
                QueuedExecutionStarted.TrySetResult(true);
            }

            await Release.Task.WaitAsync(cancellationToken);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
    }
}
