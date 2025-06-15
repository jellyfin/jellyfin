using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// Defines a jellyfin database write locking behavior.
/// </summary>
public interface IEntityFrameworkDatabaseLockingBehavior
{
    /// <summary>
    /// Acquires the database writer lock.
    /// </summary>
    /// <param name="context">JellyfinDbContext instance.</param>
    /// <returns>Lock scope.</returns>
    IDisposable AcquireWriterLock(JellyfinDbContext context);

    /// <summary>
    /// Acquires the database writer lock.
    /// </summary>
    /// <param name="context">JellyfinDbContext instance.</param>
    /// <param name="cancellationToken">Instance of CancellationToken.</param>
    /// <returns>Lock scope.</returns>
    Task<IDisposable> AcquireWriterLockAsync(JellyfinDbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires the database reader lock.
    /// </summary>
    /// <param name="context">JellyfinDbContext instance.</param>
    /// <returns>Lock scope.</returns>
    IDisposable AcquireReaderLock(JellyfinDbContext context);

    /// <summary>
    /// Acquires the database reader lock.
    /// </summary>
    /// <param name="context">JellyfinDbContext instance.</param>
    /// <param name="cancellationToken">Instance of CancellationToken.</param>
    /// <returns>Lock scope.</returns>
    Task<IDisposable> AcquireReaderLockAsync(JellyfinDbContext context, CancellationToken cancellationToken = default);
}
