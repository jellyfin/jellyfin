namespace Mono.Nat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="AsyncExtensions" />.
    /// </summary>
    internal static class AsyncExtensions
    {
        /// <summary>
        /// The DisposableWaitAsync.
        /// </summary>
        /// <param name="semaphore">The semaphore<see cref="SemaphoreSlim"/>.</param>
        /// <param name="token">The token<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task{IDisposable}"/>.</returns>
        public static async Task<IDisposable> DisposableWaitAsync(this SemaphoreSlim semaphore, CancellationToken token)
        {
            await semaphore.WaitAsync(token).ConfigureAwait(false);
            return new SemaphoreSlimDisposable(semaphore);
        }

        /// <summary>
        /// The CatchExceptions.
        /// </summary>
        /// <param name="task">The task<see cref="Task"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public static async Task CatchExceptions(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // If we cancel the task then we don't need to log anything.
            }
            catch (Exception ex)
            {
                NatUtility.Log("Unhandled exception: {0}{1}", Environment.NewLine, ex);
            }
        }

        /// <summary>
        /// The FireAndForget.
        /// </summary>
        /// <param name="task">The task<see cref="Task"/>.</param>
        /// <returns>A Task.</returns>
        public static async Task FireAndForget(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // If we cancel the task then we don't need to log anything.
            }
            catch (Exception ex)
            {
                NatUtility.Log("Unhandled exception: {0}{1}", Environment.NewLine, ex);
            }
        }

        /// <summary>
        /// The WaitAndForget.
        /// </summary>
        /// <param name="task">The task<see cref="Task"/>.</param>
        public static void WaitAndForget(this Task task)
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // If we cancel the task then we don't need to log anything.
            }
            catch (Exception ex)
            {
                NatUtility.Log("Unhandled exception: {0}{1}", Environment.NewLine, ex);
            }
        }

        /// <summary>
        /// Defines the <see cref="SemaphoreSlimDisposable" />.
        /// </summary>
        private class SemaphoreSlimDisposable : IDisposable
        {
            /// <summary>
            /// Defines the semaphore.
            /// </summary>
            private SemaphoreSlim semaphore;

            /// <summary>
            /// Initializes a new instance of the <see cref="SemaphoreSlimDisposable"/> class.
            /// </summary>
            /// <param name="semaphore">The semaphore<see cref="SemaphoreSlim"/>.</param>
            public SemaphoreSlimDisposable(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            /// <summary>
            /// The Dispose.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// The Dispose.
            /// </summary>
            /// <param name="disposing">The disposing<see cref="bool"/>.</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    semaphore?.Release();
                    semaphore = null;
                }
            }
        }
    }
}
