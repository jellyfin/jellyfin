using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Polly;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// Defines a locking mechanism that will retry any write operation for a few times.
/// </summary>
public class OptimisticLockBehavior : IEntityFrameworkCoreLockingBehavior
{
    private readonly Policy _writePolicy;
    private readonly AsyncPolicy _writeAsyncPolicy;
    private readonly ILogger<OptimisticLockBehavior> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimisticLockBehavior"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    public OptimisticLockBehavior(ILogger<OptimisticLockBehavior> logger)
    {
        System.Collections.Generic.IEnumerable<TimeSpan> sleepDurations = [
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(150),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(3)
        ];
        _logger = logger;
        _writePolicy = Policy.Handle<DbUpdateException>().WaitAndRetry(sleepDurations, RetryHandle);
        _writeAsyncPolicy = Policy.Handle<DbUpdateException>().WaitAndRetryAsync(sleepDurations, RetryHandle);

        void RetryHandle(Exception exception, TimeSpan timespan, int retryNo, Context context)
        {
            _logger.LogWarning(exception, "Operation failed retry {RetryNo}", retryNo);
        }
    }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder optionsBuilder)
    {
        _logger.LogInformation("The database locking mode has been set to: Optimistic.");
    }

    /// <inheritdoc/>
    public void OnSaveChanges(JellyfinDbContext context, Action saveChanges)
    {
        _writePolicy.ExecuteAndCapture(saveChanges);
    }

    /// <inheritdoc/>
    public async Task OnSaveChangesAsync(JellyfinDbContext context, Func<Task> saveChanges)
    {
        await _writeAsyncPolicy.ExecuteAndCaptureAsync(saveChanges).ConfigureAwait(false);
    }
}
