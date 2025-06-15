using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// Concurrent database locking behavior.
/// </summary>
public class ConcurrentDatabaseLockingBehavior : IEntityFrameworkDatabaseLockingBehavior
{
    private static readonly Disposable _disposable = new();

    /// <inheritdoc />
    public IDisposable AcquireWriterLock(JellyfinDbContext context)
    {
        return _disposable;
    }

    /// <inheritdoc />
    public Task<IDisposable> AcquireWriterLockAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<IDisposable>(_disposable);
    }

    /// <inheritdoc />
    public IDisposable AcquireReaderLock(JellyfinDbContext context)
    {
        return _disposable;
    }

    /// <inheritdoc />
    public Task<IDisposable> AcquireReaderLockAsync(JellyfinDbContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IDisposable>(_disposable);
    }

    private sealed class Disposable : IDisposable
    {
        public void Dispose()
        {
            // no-op
        }
    }
}
