using System;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using DotNext.Threading.Tasks;

namespace Jellyfin.Database.Implementations.Locking;

internal enum State
{
    None,
    ReaderLock,
    WriterLock
}

/// <summary>
/// Serialized database write behavior.
/// </summary>
public sealed class SerializedDatabaseLockingBehavior : IEntityFrameworkDatabaseLockingBehavior, IAsyncDisposable
{
#pragma warning disable MT1016 // Replace ReaderWriterLock with ReaderWriterLockSlim
    private readonly AsyncReaderWriterLock _lock = new();
#pragma warning restore MT1016 // Replace ReaderWriterLock with ReaderWriterLockSlim

    private static readonly AsyncLocal<State> _state = new();

    /// <inheritdoc />
    public IDisposable AcquireWriterLock(JellyfinDbContext context)
    {
        switch (_state.Value)
        {
            case State.None:
                // Ensure that only one thread can write access the database at a time.
                _lock.EnterWriteLockAsync().Wait();
                _state.Value = State.WriterLock;
                return new LockHolder(this);
            case State.ReaderLock:
                _lock.UpgradeToWriteLockAsync().Wait();
                _state.Value = State.WriterLock;
                return new UpgradeLockHolder(this);
            case State.WriterLock:
                // Already in a writer lock, no need to acquire again.
                return new Empty();
            default:
                throw new NotSupportedException($"Bad SerializedDatabaseLockingBehavior State {_state.Value}");
        }
    }

    /// <inheritdoc />
    public async Task<IDisposable> AcquireWriterLockAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        switch (_state.Value)
        {
            case State.None:
                // Ensure that only one thread can write access the database at a time.
                await _lock.EnterWriteLockAsync(cancellationToken).ConfigureAwait(true);
                _state.Value = State.WriterLock;
                return new LockHolder(this);
            case State.ReaderLock:
                await _lock.UpgradeToWriteLockAsync(cancellationToken).ConfigureAwait(false);
                _state.Value = State.WriterLock;
                return new UpgradeLockHolder(this);
            case State.WriterLock:
                // Already in a writer lock, no need to acquire again.
                return new Empty();
            default:
                throw new NotSupportedException($"Bad SerializedDatabaseLockingBehavior State {_state.Value}");
        }
    }

    /// <inheritdoc />
    public IDisposable AcquireReaderLock(JellyfinDbContext context)
    {
        switch (_state.Value)
        {
            case State.None:
                // Ensure that only readers can read access the database at a time.
                _lock.EnterReadLockAsync().Wait();
                _state.Value = State.ReaderLock;
                return new LockHolder(this);
            case State.ReaderLock:
            case State.WriterLock:
                // Already in a reader or writer lock, no need to acquire again.
                return new Empty();
            default:
                throw new NotSupportedException($"Bad SerializedDatabaseLockingBehavior State {_state.Value}");
        }
    }

    /// <inheritdoc />
    public async Task<IDisposable> AcquireReaderLockAsync(JellyfinDbContext context, CancellationToken cancellationToken = default)
    {
        switch (_state.Value)
        {
            case State.None:
                // Ensure that only readers can read access the database at a time.
                await _lock.EnterReadLockAsync(cancellationToken).ConfigureAwait(true);
                return new LockHolder(this);
            case State.ReaderLock:
            case State.WriterLock:
                // Already in a reader or writer lock, no need to acquire again.
                return new Empty();
            default:
                throw new NotSupportedException($"Bad SerializedDatabaseLockingBehavior State {_state.Value}");
        }
    }

    private void Release()
    {
        _state.Value = State.None;
        _lock.Release();
    }

    private void DowngradeFromWriteLock()
    {
        _state.Value = State.ReaderLock;
        _lock.DowngradeFromWriteLock();
    }

    /// <summary>
    /// Disposes the lock when no longer needed.
    /// </summary>
    /// <returns>The task.</returns>
    public async ValueTask DisposeAsync()
    {
        await _lock.DisposeAsync().ConfigureAwait(false);
    }

    private sealed record LockHolder(SerializedDatabaseLockingBehavior LockData) : IDisposable
    {
        public void Dispose()
        {
            LockData.Release();
        }
    }

    private sealed record UpgradeLockHolder(SerializedDatabaseLockingBehavior LockData) : IDisposable
    {
        public void Dispose()
        {
            LockData.DowngradeFromWriteLock();
        }
    }

    private sealed record Empty : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
