using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Helper methods for running tasks concurrently.
    /// </summary>
    public static class TaskMethods
    {
        private static readonly int _processorCount = Environment.ProcessorCount;

        private static readonly ConcurrentDictionary<SharedThrottleId, SemaphoreSlim> _sharedThrottlers = new ConcurrentDictionary<SharedThrottleId, SemaphoreSlim>();

        /// <summary>
        /// Throttle id for sharing a concurrency limit.
        /// </summary>
        public enum SharedThrottleId
        {
            /// <summary>
            /// Library scan fan out
            /// </summary>
            ScanFanout,

            /// <summary>
            /// Refresh metadata
            /// </summary>
            RefreshMetadata,
        }

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        public static IServerConfigurationManager ConfigurationManager { get; set; }

        /// <summary>
        /// Similiar to Task.WhenAll but only allows running a certain amount of tasks at the same time.
        /// </summary>
        /// <param name="throttleId">The throttle id. Multiple calls to this method with the same throttle id will share a concurrency limit.</param>
        /// <param name="actions">List of actions to run.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public static async Task WhenAllThrottled(SharedThrottleId throttleId, IEnumerable<Func<Task>> actions, CancellationToken cancellationToken)
        {
            var taskThrottler = throttleId == SharedThrottleId.ScanFanout ?
                new SemaphoreSlim(GetConcurrencyLimit(throttleId)) :
                _sharedThrottlers.GetOrAdd(throttleId, id => new SemaphoreSlim(GetConcurrencyLimit(id)));

            try
            {
                var tasks = new List<Task>();

                foreach (var action in actions)
                {
                    await taskThrottler.WaitAsync(cancellationToken).ConfigureAwait(false);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await action().ConfigureAwait(false);
                        }
                        finally
                        {
                            taskThrottler.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                if (throttleId == SharedThrottleId.ScanFanout)
                {
                    taskThrottler.Dispose();
                }
            }
        }

        /// <summary>
        /// Runs a task within a given throttler.
        /// </summary>
        /// <param name="throttleId">The throttle id. Multiple calls to this method with the same throttle id will share a concurrency limit.</param>
        /// <param name="action">The action to run.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public static async Task RunThrottled(SharedThrottleId throttleId, Func<Task> action, CancellationToken cancellationToken)
        {
            if (throttleId == SharedThrottleId.ScanFanout)
            {
                // just await the task instead
                throw new InvalidOperationException("Invalid throttle id");
            }

            var taskThrottler = _sharedThrottlers.GetOrAdd(throttleId, id => new SemaphoreSlim(GetConcurrencyLimit(id)));

            await taskThrottler.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                taskThrottler.Release();
            }
        }

        /// <summary>
        /// Get the concurrency limit for the given throttle id.
        /// </summary>
        /// <param name="throttleId">The throttle id.</param>
        /// <returns>The concurrency limit.</returns>
        private static int GetConcurrencyLimit(SharedThrottleId throttleId)
        {
            var concurrency = throttleId == SharedThrottleId.RefreshMetadata ?
                ConfigurationManager.Configuration.LibraryMetadataRefreshConcurrency :
                ConfigurationManager.Configuration.LibraryScanFanoutConcurrency;

            if (concurrency <= 0)
            {
                concurrency = _processorCount;
            }

            return concurrency;
        }
    }
}
