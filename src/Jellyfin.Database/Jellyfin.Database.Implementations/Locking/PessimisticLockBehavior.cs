#pragma warning disable MT1013 // Releasing lock without guarantee of execution
#pragma warning disable MT1012 // Acquiring lock without guarantee of releasing
#pragma warning disable CA1873

using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// A locking behavior that will always block any operation while a write is requested. Mimicks the old SqliteRepository behavior.
/// </summary>
public class PessimisticLockBehavior : IEntityFrameworkCoreLockingBehavior
{
    private readonly ILogger<PessimisticLockBehavior> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PessimisticLockBehavior"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public PessimisticLockBehavior(ILogger<PessimisticLockBehavior> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    private static ReaderWriterLockSlim DatabaseLock { get; } = new(LockRecursionPolicy.SupportsRecursion);

    /// <inheritdoc/>
    public void OnSaveChanges(JellyfinDbContext context, Action saveChanges)
    {
        using (DbLock.EnterWrite(_logger))
        {
            saveChanges();
        }
    }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder optionsBuilder)
    {
        _logger.LogInformation("The database locking mode has been set to: Pessimistic.");
        optionsBuilder.AddInterceptors(new CommandLockingInterceptor(_loggerFactory.CreateLogger<CommandLockingInterceptor>()));
        optionsBuilder.AddInterceptors(new TransactionLockingInterceptor(_loggerFactory.CreateLogger<TransactionLockingInterceptor>()));
    }

    /// <inheritdoc/>
    public async Task OnSaveChangesAsync(JellyfinDbContext context, Func<Task> saveChanges)
    {
        using (DbLock.EnterWrite(_logger))
        {
            await saveChanges().ConfigureAwait(false);
        }
    }

    private sealed class TransactionLockingInterceptor : DbTransactionInterceptor
    {
        private readonly ILogger _logger;

        public TransactionLockingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            DbLock.BeginWriteLock(_logger);

            return base.TransactionStarting(connection, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
        {
            DbLock.BeginWriteLock(_logger);

            return base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
        }

        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            DbLock.EndWriteLock(_logger);

            base.TransactionCommitted(transaction, eventData);
        }

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            DbLock.EndWriteLock(_logger);

            return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
        {
            DbLock.EndWriteLock(_logger);

            base.TransactionFailed(transaction, eventData);
        }

        public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            DbLock.EndWriteLock(_logger);

            return base.TransactionFailedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        {
            DbLock.EndWriteLock(_logger);

            base.TransactionRolledBack(transaction, eventData);
        }

        public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            DbLock.EndWriteLock(_logger);

            return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
        }
    }

    /// <summary>
    /// Adds strict read/write locking.
    /// </summary>
    private sealed class CommandLockingInterceptor : DbCommandInterceptor
    {
        private readonly ILogger _logger;

        public CommandLockingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            using (DbLock.EnterWrite(_logger, command))
            {
                return InterceptionResult<int>.SuppressWithResult(command.ExecuteNonQuery());
            }
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            using (DbLock.EnterWrite(_logger, command))
            {
                return InterceptionResult<int>.SuppressWithResult(await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false));
            }
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            using (DbLock.EnterRead(_logger))
            {
                return InterceptionResult<object>.SuppressWithResult(command.ExecuteScalar()!);
            }
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            using (DbLock.EnterRead(_logger))
            {
                return InterceptionResult<object>.SuppressWithResult((await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!);
            }
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            using (DbLock.EnterRead(_logger))
            {
                return InterceptionResult<DbDataReader>.SuppressWithResult(command.ExecuteReader()!);
            }
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            using (DbLock.EnterRead(_logger))
            {
                return InterceptionResult<DbDataReader>.SuppressWithResult(await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false));
            }
        }
    }

    private sealed class DbLock : IDisposable
    {
        private readonly Action? _action;
        private bool _disposed;

        private static readonly IDisposable _noLock = new DbLock(null) { _disposed = true };
        private static (string Command, Guid Id, DateTimeOffset QueryDate, bool Printed) _blockQuery;

        public DbLock(Action? action = null)
        {
            _action = action;
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static IDisposable EnterWrite(ILogger logger, IDbCommand? command = null, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            logger.LogTrace("Enter Write for {Caller}:{Line}", callerMemberName, callerNo);
            if (DatabaseLock.IsWriteLockHeld)
            {
                logger.LogTrace("Write Held {Caller}:{Line}", callerMemberName, callerNo);
                return _noLock;
            }

            BeginWriteLock(logger, command, callerMemberName, callerNo);
            return new DbLock(() =>
            {
                EndWriteLock(logger, callerMemberName, callerNo);
            });
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static IDisposable EnterRead(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            logger.LogTrace("Enter Read {Caller}:{Line}", callerMemberName, callerNo);
            if (DatabaseLock.IsWriteLockHeld)
            {
                logger.LogTrace("Write Held {Caller}:{Line}", callerMemberName, callerNo);
                return _noLock;
            }

            BeginReadLock(logger, callerMemberName, callerNo);
            return new DbLock(() =>
            {
                ExitReadLock(logger, callerMemberName, callerNo);
            });
        }

        public static void BeginWriteLock(ILogger logger, IDbCommand? command = null, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Aquire Write {Caller}:{Line}", callerMemberName, callerNo);
            if (!DatabaseLock.TryEnterWriteLock(TimeSpan.FromMilliseconds(1000)))
            {
                var blockingQuery = _blockQuery;
                if (!blockingQuery.Printed)
                {
                    _blockQuery = (blockingQuery.Command, blockingQuery.Id, blockingQuery.QueryDate, true);
                    logger.LogInformation("QueryLock: {Id} --- {Query}", blockingQuery.Id, blockingQuery.Command);
                }

                logger.LogInformation("Query congestion detected: '{Id}' since '{Date}'", blockingQuery.Id, blockingQuery.QueryDate);

                DatabaseLock.EnterWriteLock();

                logger.LogInformation("Query congestion cleared: '{Id}' for '{Date}'", blockingQuery.Id, DateTimeOffset.Now - blockingQuery.QueryDate);
            }

            _blockQuery = (command?.CommandText ?? "Transaction", Guid.NewGuid(), DateTimeOffset.Now, false);

            logger.LogTrace("Write Aquired {Caller}:{Line}", callerMemberName, callerNo);
        }

        public static void BeginReadLock(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Aquire Write {Caller}:{Line}", callerMemberName, callerNo);
            DatabaseLock.EnterReadLock();
            logger.LogTrace("Read Aquired {Caller}:{Line}", callerMemberName, callerNo);
        }

        public static void EndWriteLock(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Release Write {Caller}:{Line}", callerMemberName, callerNo);
            DatabaseLock.ExitWriteLock();
        }

        public static void ExitReadLock(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Release Read {Caller}:{Line}", callerMemberName, callerNo);
            DatabaseLock.ExitReadLock();
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
    }
}
