using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.Locking;

public partial class PessimisticLockBehavior
{
    private sealed partial class DbLock : IDisposable, IAsyncDisposable
    {
        private const string WriteHeldMessage = "Write Held {Caller}:{Line}";

        // This must not use a thread-affine reader/writer lock: async EF work may resume and
        // dispose on a different thread than the one that acquired the lock.
        private static readonly ReaderWriterSemaphore _databaseLock = new();
        // A DbTransaction owns the global write lock until the transaction ends. Commands inside
        // that transaction check this map to avoid reacquiring the same global lock and deadlocking.
        private static readonly ConcurrentDictionary<DbTransaction, DbLock> _transactionLocks = new();
        private static readonly DbLock _noLock = new(null) { _disposed = true };
        private static readonly TransactionDisposedDiagnosticObserver _transactionDisposedDiagnosticObserver = new();
        private static readonly object _diagnosticListenerLock = new();
        private static (string Command, Guid Id, DateTimeOffset QueryDate, bool Printed) _blockQuery;
        private static IDisposable? _diagnosticListenerSubscription;

        private readonly Action? _action;
        private bool _disposed;

        private DbLock(Action? action)
        {
            _action = action;
        }

        public static void EnsureTransactionDisposedListener()
        {
            lock (_diagnosticListenerLock)
            {
                if (_diagnosticListenerSubscription is not null)
                {
                    return;
                }

                _diagnosticListenerSubscription = DiagnosticListener.AllListeners.Subscribe(_transactionDisposedDiagnosticObserver);
            }
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static DbLock EnterWrite(ILogger logger, DbTransaction? transaction = null, IDbCommand? command = null, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            logger.LogTrace("Enter Write for {Caller}:{Line}", callerMemberName, callerNo);
            if (IsLockHeld(transaction))
            {
                logger.LogTrace(WriteHeldMessage, callerMemberName, callerNo);
                return _noLock;
            }

            return EnterWriteCore(logger, command, callerMemberName, callerNo);
        }

        public static ValueTask<DbLock> EnterWriteAsync(ILogger logger, DbTransaction? transaction = null, IDbCommand? command = null, CancellationToken cancellationToken = default, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Enter Write for {Caller}:{Line}", callerMemberName, callerNo);
            if (IsLockHeld(transaction))
            {
                logger.LogTrace(WriteHeldMessage, callerMemberName, callerNo);
                return new ValueTask<DbLock>(_noLock);
            }

            return EnterWriteCoreAsync(logger, command, cancellationToken, callerMemberName, callerNo);
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static DbLock EnterRead(ILogger logger, DbTransaction? transaction = null, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            logger.LogTrace("Enter Read {Caller}:{Line}", callerMemberName, callerNo);
            if (IsLockHeld(transaction))
            {
                logger.LogTrace(WriteHeldMessage, callerMemberName, callerNo);
                return _noLock;
            }

            return EnterReadCore(logger, callerMemberName, callerNo);
        }

        public static ValueTask<DbLock> EnterReadAsync(ILogger logger, DbTransaction? transaction = null, CancellationToken cancellationToken = default, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Enter Read {Caller}:{Line}", callerMemberName, callerNo);
            if (IsLockHeld(transaction))
            {
                logger.LogTrace(WriteHeldMessage, callerMemberName, callerNo);
                return new ValueTask<DbLock>(_noLock);
            }

            return EnterReadCoreAsync(logger, cancellationToken, callerMemberName, callerNo);
        }

        public static InterceptionResult<DbTransaction> BeginTransaction(ILogger logger, DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            var transactionLock = EnterWriteCore(logger, command: null, callerMemberName, callerNo);
            try
            {
                // Create the transaction while the write lock is held so the transaction object and
                // its lock enter _transactionLocks as one atomic ownership handoff.
                var transaction = result.HasResult
                    ? result.Result
                    : connection.BeginTransaction(eventData.IsolationLevel);
                TrackTransactionLock(transaction, transactionLock);

                return InterceptionResult<DbTransaction>.SuppressWithResult(transaction);
            }
            catch
            {
                transactionLock.Dispose();
                throw;
            }
        }

        public static async ValueTask<InterceptionResult<DbTransaction>> BeginTransactionAsync(ILogger logger, DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            var transactionLock = await EnterWriteCoreAsync(logger, command: null, cancellationToken, callerMemberName, callerNo).ConfigureAwait(false);
            try
            {
                var transaction = result.HasResult
                    ? result.Result
                    : await connection.BeginTransactionAsync(eventData.IsolationLevel, cancellationToken).ConfigureAwait(false);
                TrackTransactionLock(transaction, transactionLock);

                return InterceptionResult<DbTransaction>.SuppressWithResult(transaction);
            }
            catch
            {
                await transactionLock.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        public static void EndTransactionLock(DbTransaction transaction)
        {
            if (_transactionLocks.TryRemove(transaction, out var transactionLock))
            {
                transactionLock.Dispose();
            }
        }

        public static async ValueTask EndTransactionLockAsync(DbTransaction transaction)
        {
            if (_transactionLocks.TryRemove(transaction, out var transactionLock))
            {
                await transactionLock.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static bool IsLockHeld(DbTransaction? transaction)
        {
            return transaction is not null && _transactionLocks.ContainsKey(transaction);
        }

        private static void TrackTransactionLock(DbTransaction transaction, DbLock transactionLock)
        {
            if (!_transactionLocks.TryAdd(transaction, transactionLock))
            {
                transactionLock.Dispose();
            }
        }

        private static DbLock EnterWriteCore(ILogger logger, IDbCommand? command, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Acquire Write {Caller}:{Line}", callerMemberName, callerNo);
            if (!_databaseLock.AcquireWrite(TimeSpan.FromMilliseconds(1000), () => LogQueryCongestionDetected(logger)))
            {
                var blockingQuery = _blockQuery;
                logger.LogInformation("Query congestion cleared: '{Id}' for '{Date}'", blockingQuery.Id, DateTimeOffset.Now - blockingQuery.QueryDate);
            }

            _blockQuery = (command?.CommandText ?? "Transaction", Guid.NewGuid(), DateTimeOffset.Now, false);

            logger.LogTrace("Write Acquired {Caller}:{Line}", callerMemberName, callerNo);
            return new DbLock(
                static () =>
                {
                    _databaseLock.ExitWrite();
                });
        }

        private static async ValueTask<DbLock> EnterWriteCoreAsync(ILogger logger, IDbCommand? command, CancellationToken cancellationToken, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Acquire Write {Caller}:{Line}", callerMemberName, callerNo);
            if (!await _databaseLock.AcquireWriteAsync(TimeSpan.FromMilliseconds(1000), () => LogQueryCongestionDetected(logger), cancellationToken).ConfigureAwait(false))
            {
                var blockingQuery = _blockQuery;
                logger.LogInformation("Query congestion cleared: '{Id}' for '{Date}'", blockingQuery.Id, DateTimeOffset.Now - blockingQuery.QueryDate);
            }

            _blockQuery = (command?.CommandText ?? "Transaction", Guid.NewGuid(), DateTimeOffset.Now, false);

            logger.LogTrace("Write Acquired {Caller}:{Line}", callerMemberName, callerNo);
            return new DbLock(
                static () =>
                {
                    _databaseLock.ExitWrite();
                });
        }

        private static void LogQueryCongestionDetected(ILogger logger)
        {
            var blockingQuery = _blockQuery;
            if (!blockingQuery.Printed)
            {
                _blockQuery = (blockingQuery.Command, blockingQuery.Id, blockingQuery.QueryDate, true);
                logger.LogInformation("QueryLock: {Id} --- {Query}", blockingQuery.Id, blockingQuery.Command);
            }

            logger.LogInformation("Query congestion detected: '{Id}' since '{Date}'", blockingQuery.Id, blockingQuery.QueryDate);
        }

        private static DbLock EnterReadCore(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Acquire Read {Caller}:{Line}", callerMemberName, callerNo);
            _databaseLock.AcquireRead();
            logger.LogTrace("Read Acquired {Caller}:{Line}", callerMemberName, callerNo);
            return new DbLock(
                static () =>
                {
                    _databaseLock.ExitRead();
                });
        }

        private static async ValueTask<DbLock> EnterReadCoreAsync(ILogger logger, CancellationToken cancellationToken, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Acquire Read {Caller}:{Line}", callerMemberName, callerNo);
            await _databaseLock.AcquireReadAsync(cancellationToken).ConfigureAwait(false);
            logger.LogTrace("Read Acquired {Caller}:{Line}", callerMemberName, callerNo);
            return new DbLock(
                static () =>
                {
                    _databaseLock.ExitRead();
                });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_action is not null)
            {
                _action();
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
