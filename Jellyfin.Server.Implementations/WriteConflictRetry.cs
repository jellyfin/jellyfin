using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations;

/// <summary>
/// Repeats a unit of work that failed because it lost a race with another writer, as classified by
/// <see cref="IJellyfinDatabaseProvider.IsRetryableWriteConflict(Exception)"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every unit of work passed in must be idempotent and must read the rows it depends on again each time it runs:
/// the point of repeating it is that the other writer's rows are now visible. The unit of work owns its
/// <see cref="JellyfinDbContext"/> and transaction, and this class only decides whether to run it again, so each
/// run has to create its own context. A context that has failed to save still tracks the rejected rows, and its
/// transaction cannot be reused.
/// </para>
/// <para>
/// A unit of work runs at most <see cref="MaxAttempts"/> times, with a random pause between
/// <see cref="MinDelay"/> and <see cref="MaxDelay"/> before each repetition. An exception that is not retryable, or the
/// one thrown by the last attempt, is rethrown as it is.
/// </para>
/// </remarks>
internal sealed class WriteConflictRetry
{
    internal const int MaxAttempts = 3;

    internal static readonly TimeSpan MinDelay = TimeSpan.FromMilliseconds(50);
    internal static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(250);

    private readonly IJellyfinDatabaseProvider _databaseProvider;
    private readonly ILogger _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly Action<TimeSpan, CancellationToken> _delay;

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteConflictRetry"/> class.
    /// </summary>
    /// <param name="databaseProvider">The provider that classifies the exceptions.</param>
    /// <param name="logger">The logger the retries are reported to.</param>
    public WriteConflictRetry(IJellyfinDatabaseProvider databaseProvider, ILogger logger)
        : this(databaseProvider, logger, Task.Delay, WaitForDelay)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteConflictRetry"/> class with its own way of pausing.
    /// </summary>
    /// <param name="databaseProvider">The provider that classifies the exceptions.</param>
    /// <param name="logger">The logger the retries are reported to.</param>
    /// <param name="delayAsync">Pauses asynchronously between two attempts.</param>
    /// <param name="delay">Pauses synchronously between two attempts.</param>
    internal WriteConflictRetry(
        IJellyfinDatabaseProvider databaseProvider,
        ILogger logger,
        Func<TimeSpan, CancellationToken, Task> delayAsync,
        Action<TimeSpan, CancellationToken> delay)
    {
        _databaseProvider = databaseProvider;
        _logger = logger;
        _delayAsync = delayAsync;
        _delay = delay;
    }

    /// <summary>
    /// Runs <paramref name="work"/>, and runs it again when it fails with a retryable write conflict.
    /// </summary>
    /// <param name="work">The idempotent unit of work.</param>
    /// <param name="cancellationToken">The token that stops further attempts.</param>
    public void Execute(Action work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                work();
                return;
            }
            catch (Exception ex)
            {
                if (!ShouldRetry(ex, attempt))
                {
                    throw;
                }
            }

            _delay(NextDelay(), cancellationToken);
        }
    }

    /// <summary>
    /// Runs <paramref name="work"/>, and runs it again when it fails with a retryable write conflict.
    /// </summary>
    /// <param name="work">The idempotent unit of work.</param>
    /// <param name="cancellationToken">The token that stops further attempts.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await work(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                if (!ShouldRetry(ex, attempt))
                {
                    throw;
                }
            }

            await _delayAsync(NextDelay(), cancellationToken).ConfigureAwait(false);
        }
    }

    internal static void WaitForDelay(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (cancellationToken.WaitHandle.WaitOne(delay))
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static TimeSpan NextDelay()
        => TimeSpan.FromMilliseconds(Random.Shared.Next((int)MinDelay.TotalMilliseconds, (int)MaxDelay.TotalMilliseconds + 1));

    private bool ShouldRetry(Exception exception, int attempt)
    {
        if (attempt >= MaxAttempts || !_databaseProvider.IsRetryableWriteConflict(exception))
        {
            return false;
        }

        // The exception itself is not logged: its message can carry the values that conflicted.
        _logger.LogWarning(
            "Write conflict ({ExceptionType}) on attempt {Attempt} of {MaxAttempts}, trying again",
            exception.GetType().FullName,
            attempt,
            MaxAttempts);
        return true;
    }
}
