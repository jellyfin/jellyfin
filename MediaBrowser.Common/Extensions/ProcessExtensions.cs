using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Process"/>.
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the associated process has been terminated using
        /// <see cref="Process.HasExited"/>. This is safe to call even if there is no operating system process
        /// associated with the <see cref="Process"/>.
        /// </summary>
        /// <param name="process">The process to check the exit status for.</param>
        /// <returns>
        /// True if the operating system process referenced by the <see cref="Process"/> component has
        /// terminated, or if there is no associated operating system process; otherwise, false.
        /// </returns>
        public static bool HasExitedSafe(this Process process)
        {
            try
            {
                return process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        /// <summary>
        /// Asynchronously wait for the process to exit.
        /// </summary>
        /// <param name="process">The process to wait for.</param>
        /// <param name="timeMs">A timeout, in milliseconds, after which to stop waiting for the task.</param>
        /// <returns>True if the task exited normally, false if the timeout elapsed before the process exited.</returns>
        public static async Task<bool> WaitForExitAsync(this Process process, int timeMs)
        {
            if (!process.EnableRaisingEvents)
            {
                throw new InvalidOperationException("EnableRisingEvents must be enabled to async wait for a task to exit.");
            }

            // Add an event handler for the process exit event
            var tcs = new TaskCompletionSource<bool>();
            process.Exited += (sender, args) => tcs.TrySetResult(true);

            // Return immediately if the process has already exited
            if (process.HasExitedSafe())
            {
                return true;
            }

            // Add an additional timeout then await
            using (var cancelTokenSource = new CancellationTokenSource(Math.Max(0, timeMs)))
            using (var cancelRegistration = cancelTokenSource.Token.Register(() => tcs.TrySetResult(process.HasExitedSafe())))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}
