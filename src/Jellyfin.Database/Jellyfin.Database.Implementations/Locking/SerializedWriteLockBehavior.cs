using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// A locking behavior that serializes writers in-process while leaving readers unsynchronized.
/// </summary>
/// <remarks>
/// <para>
/// SQLite permits one writer at a time, so queueing writers in-process lets each take the database
/// lock uncontended instead of racing for it and generating SQLITE_BUSY retries.
/// </para>
/// <para>
/// Reads are left unsynchronized: in WAL mode they neither block nor are blocked by writers.
/// </para>
/// <para>
/// Uses <see cref="SemaphoreSlim"/> rather than <see cref="ReaderWriterLockSlim"/> so the lock can
/// be held across an <see langword="await"/>.
/// </para>
/// </remarks>
public sealed class SerializedWriteLockBehavior : IEntityFrameworkCoreLockingBehavior, IDisposable
{
    /// <summary>
    /// How long to queue for the in-process write lock before giving up and letting SQLite's own
    /// busy handler arbitrate instead. This is a deadlock backstop, not a normal code path.
    /// </summary>
    private static readonly TimeSpan _acquireTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Set while this instance owns the permit. Propagates into the guarded call's EF internals so
    /// the nested interceptors skip re-acquiring.
    /// </summary>
    private readonly AsyncLocal<bool> _holdsWriteLock = new();

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// The explicit transaction owning the write lock, mapped to the connection it was opened on.
    /// Keyed by transaction because a transaction's lifetime spans separate async flows, and to make
    /// releasing idempotent across the several end-of-transaction callbacks EF raises. Holds at most
    /// one entry, since the semaphore admits one writer.
    /// </summary>
    private readonly ConcurrentDictionary<DbTransaction, DbConnection> _lockedTransactions = new();

    private readonly ILogger<SerializedWriteLockBehavior> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedWriteLockBehavior"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    public SerializedWriteLockBehavior(ILogger<SerializedWriteLockBehavior> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder optionsBuilder)
    {
        _logger.LogInformation("The database locking mode has been set to: SerializedWrites.");
        optionsBuilder.AddInterceptors(new WriteSerializingCommandInterceptor(this));
        optionsBuilder.AddInterceptors(new WriteSerializingTransactionInterceptor(this));
        optionsBuilder.AddInterceptors(new WriteLockReleasingConnectionInterceptor(this));
    }

    /// <inheritdoc/>
    public void OnSaveChanges(JellyfinDbContext context, Action saveChanges)
    {
        if (AlreadyHoldsLock(context))
        {
            saveChanges();
            return;
        }

        var acquired = Acquire();
        _holdsWriteLock.Value = true;
        try
        {
            saveChanges();
        }
        finally
        {
            _holdsWriteLock.Value = false;
            Release(acquired);
        }
    }

    /// <inheritdoc/>
    public async Task OnSaveChangesAsync(JellyfinDbContext context, Func<Task> saveChanges)
    {
        if (AlreadyHoldsLock(context))
        {
            await saveChanges().ConfigureAwait(false);
            return;
        }

        var acquired = await AcquireAsync(CancellationToken.None).ConfigureAwait(false);
        _holdsWriteLock.Value = true;
        try
        {
            await saveChanges().ConfigureAwait(false);
        }
        finally
        {
            _holdsWriteLock.Value = false;
            Release(acquired);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writeLock.Dispose();
    }

    /// <summary>
    /// Whether an enclosing operation or an explicit transaction on this context holds the lock.
    /// </summary>
    private bool AlreadyHoldsLock(JellyfinDbContext context)
    {
        if (_holdsWriteLock.Value)
        {
            return true;
        }

        var current = context.Database.CurrentTransaction?.GetDbTransaction();
        return current is not null && _lockedTransactions.ContainsKey(current);
    }

    private bool Acquire()
    {
        if (_writeLock.Wait(_acquireTimeout))
        {
            return true;
        }

        LogAcquireTimeout();
        return false;
    }

    private async ValueTask<bool> AcquireAsync(CancellationToken cancellationToken)
    {
        if (await _writeLock.WaitAsync(_acquireTimeout, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        LogAcquireTimeout();
        return false;
    }

    private void LogAcquireTimeout()
    {
        _logger.LogWarning(
            "Timed out after {Timeout}s waiting for the in-process database write lock; proceeding without it. This means some write is holding the lock far too long, or that writes are nested across separate connections.",
            _acquireTimeout.TotalSeconds);
    }

    private void Release(bool acquired)
    {
        if (acquired)
        {
            _writeLock.Release();
        }
    }

    private void TrackTransaction(DbTransaction transaction, DbConnection connection)
    {
        _lockedTransactions[transaction] = connection;
    }

    private void ReleaseTransaction(DbTransaction transaction)
    {
        if (_lockedTransactions.TryRemove(transaction, out _))
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Releases a lock still attributed to a transaction on a connection being closed.
    /// EF disposes the underlying transaction directly, without raising a commit, rollback or
    /// failure callback, so a transaction abandoned by an exception has no other release point.
    /// </summary>
    private void ReleaseTransactionsOn(DbConnection connection)
    {
        foreach (var (transaction, owner) in _lockedTransactions)
        {
            if (ReferenceEquals(owner, connection))
            {
                ReleaseTransaction(transaction);
            }
        }
    }

    /// <summary>
    /// Serializes writes issued outside <c>SaveChanges</c> and outside an explicit transaction:
    /// ExecuteDelete, ExecuteUpdate, raw SQL and migrations. All execute as non-queries; reads pass
    /// straight through.
    /// </summary>
    private sealed class WriteSerializingCommandInterceptor : DbCommandInterceptor
    {
        private readonly SerializedWriteLockBehavior _owner;

        public WriteSerializingCommandInterceptor(SerializedWriteLockBehavior owner)
        {
            _owner = owner;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            if (!NeedsLock(command, eventData))
            {
                return base.NonQueryExecuting(command, eventData, result);
            }

            var acquired = _owner.Acquire();
            try
            {
                return InterceptionResult<int>.SuppressWithResult(command.ExecuteNonQuery());
            }
            finally
            {
                _owner.Release(acquired);
            }
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!NeedsLock(command, eventData))
            {
                return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
            }

            var acquired = await _owner.AcquireAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return InterceptionResult<int>.SuppressWithResult(await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false));
            }
            finally
            {
                _owner.Release(acquired);
            }
        }

        private bool NeedsLock(DbCommand command, CommandEventData eventData)
        {
            if (!IsWrite(eventData.CommandSource))
            {
                return false;
            }

            // The semaphore is not reentrant; taking it again under an owning operation deadlocks.
            if (_owner._holdsWriteLock.Value)
            {
                return false;
            }

            return command.Transaction is null || !_owner._lockedTransactions.ContainsKey(command.Transaction);
        }

        private static bool IsWrite(CommandSource source) => source switch
        {
            CommandSource.SaveChanges => true,
            CommandSource.Migrations => true,
            CommandSource.ExecuteSqlRaw => true,
            CommandSource.ExecuteUpdate => true,
            CommandSource.ExecuteDelete => true,
            _ => false,
        };
    }

    /// <summary>
    /// Holds the write lock for the lifetime of an explicit transaction. Acquires on
    /// <c>TransactionStarted</c>, where the transaction object exists, so the lock is never held
    /// without a key to release it by.
    /// </summary>
    private sealed class WriteSerializingTransactionInterceptor : DbTransactionInterceptor
    {
        private readonly SerializedWriteLockBehavior _owner;

        public WriteSerializingTransactionInterceptor(SerializedWriteLockBehavior owner)
        {
            _owner = owner;
        }

        public override DbTransaction TransactionStarted(DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
        {
            if (!_owner._holdsWriteLock.Value && _owner.Acquire())
            {
                _owner.TrackTransaction(result, connection);
            }

            return base.TransactionStarted(connection, eventData, result);
        }

        public override async ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
        {
            if (!_owner._holdsWriteLock.Value && await _owner.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                _owner.TrackTransaction(result, connection);
            }

            return await base.TransactionStartedAsync(connection, eventData, result, cancellationToken).ConfigureAwait(false);
        }

        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            _owner.ReleaseTransaction(transaction);
            base.TransactionCommitted(transaction, eventData);
        }

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            _owner.ReleaseTransaction(transaction);
            return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        {
            _owner.ReleaseTransaction(transaction);
            base.TransactionRolledBack(transaction, eventData);
        }

        public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            _owner.ReleaseTransaction(transaction);
            return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
        {
            _owner.ReleaseTransaction(transaction);
            base.TransactionFailed(transaction, eventData);
        }

        public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _owner.ReleaseTransaction(transaction);
            return base.TransactionFailedAsync(transaction, eventData, cancellationToken);
        }
    }

    /// <summary>
    /// Backstop release for transactions abandoned without a commit or rollback callback.
    /// </summary>
    private sealed class WriteLockReleasingConnectionInterceptor : DbConnectionInterceptor
    {
        private readonly SerializedWriteLockBehavior _owner;

        public WriteLockReleasingConnectionInterceptor(SerializedWriteLockBehavior owner)
        {
            _owner = owner;
        }

        public override void ConnectionClosed(DbConnection connection, ConnectionEndEventData eventData)
        {
            _owner.ReleaseTransactionsOn(connection);
            base.ConnectionClosed(connection, eventData);
        }

        public override Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData eventData)
        {
            _owner.ReleaseTransactionsOn(connection);
            return base.ConnectionClosedAsync(connection, eventData);
        }
    }
}
