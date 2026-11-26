using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Starts a test process without holding up the test. On Unix, starting a process may block while waiting for the
/// program file on storage that does not respond.
/// </summary>
internal static class TranscodeTestLauncher
{
    /// <summary>
    /// Runs <paramref name="start"/> on the thread pool and waits for it up to <paramref name="timeout"/>. A process
    /// that starts after timeout or cancellation is handed to <paramref name="abandon"/> for cleanup.
    /// </summary>
    /// <typeparam name="T">What <paramref name="start"/> returns, such as the started process.</typeparam>
    /// <param name="start">Starts the process off the caller's thread.</param>
    /// <param name="timeout">How long to wait for it.</param>
    /// <param name="abandon">Stops and releases a process that started after the caller stopped waiting.</param>
    /// <param name="cancellationToken">Stops the wait.</param>
    /// <returns>The started process, or null when it did not start in time.</returns>
    /// <exception cref="OperationCanceledException">The test was cancelled before the process started.</exception>
    public static async Task<T?> StartAsync<T>(Func<T> start, TimeSpan timeout, Action<T> abandon, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(abandon);
        var launch = Task.Run(start, CancellationToken.None);
        using (var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            var delay = Task.Delay(timeout, wait.Token);
            if (await Task.WhenAny(launch, delay).ConfigureAwait(false) == launch)
            {
                await wait.CancelAsync().ConfigureAwait(false);
                return await launch.ConfigureAwait(false);
            }
        }

        _ = AbandonWhenStartedAsync(launch, abandon);
        cancellationToken.ThrowIfCancellationRequested();
        return null;
    }

    private static async Task AbandonWhenStartedAsync<T>(Task<T> launch, Action<T> abandon)
    {
        await ((Task)launch).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (launch.IsCompletedSuccessfully)
        {
            abandon(await launch.ConfigureAwait(false));
        }
    }
}
