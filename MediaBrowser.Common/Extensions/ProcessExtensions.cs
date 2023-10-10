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
        /// Asynchronously wait for the process to exit.
        /// </summary>
        /// <param name="process">The process to wait for.</param>
        /// <param name="timeout">The duration to wait before cancelling waiting for the task.</param>
        /// <returns>A task that will complete when the process has exited, cancellation has been requested, or an error occurs.</returns>
        /// <exception cref="OperationCanceledException">The timeout ended.</exception>
        public static async Task WaitForExitAsync(this Process process, TimeSpan timeout)
        {
            using (var cancelTokenSource = new CancellationTokenSource(timeout))
            {
                await process.WaitForExitAsync(cancelTokenSource.Token).ConfigureAwait(false);
            }
        }
    }
}
