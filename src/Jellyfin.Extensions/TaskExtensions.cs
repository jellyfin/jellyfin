using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Extensions;

/// <summary>
/// Task extensions for timeout handling.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Wrapper for a task that will throw a TimeoutException if the task does not complete within the specified timeout.
    /// </summary>
    /// <param name="task">task.</param>
    /// <param name="timeout">timeout.</param>
    /// <returns>Wrapped Task.</returns>
    /// <exception cref="TimeoutException">throws when timeout is exceeded.</exception>
    public static async Task WithTimeout(this Task task, TimeSpan timeout)
    {
        if (await TimeoutTask(task, timeout).ConfigureAwait(false))
        {
            throw new TimeoutException($"Timeout {timeout} exceeded");
        }

        await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Wrapper for a task that will throw a TimeoutException if the task does not complete within the specified timeout.
    /// </summary>
    /// <param name="task">task.</param>
    /// <param name="timeout">timeout.</param>
    /// <typeparam name="T">result type.</typeparam>
    /// <returns>Wrapped Task of T.</returns>
    /// <exception cref="TimeoutException">throws when timeout is exceeded.</exception>
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        if (await TimeoutTask(task, timeout).ConfigureAwait(false))
        {
            throw new TimeoutException($"Timeout {timeout} exceeded");
        }

        return await task.ConfigureAwait(false);
    }

    private static async Task<bool> TimeoutTask(Task task, TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            await task.ConfigureAwait(false);
            return false;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationToken = new CancellationTokenSource(timeout);
        await using (cancellationToken.Token.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
        {
            return tcs.Task == await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
        }
    }
}
