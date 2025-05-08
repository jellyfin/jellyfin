#pragma warning disable MT1013 // Releasing lock without guarantee of execution
#pragma warning disable MT1012 // Acquiring lock without guarantee of releasing

using System;
using System.Data.Common;
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

    private static ReaderWriterLockSlim DatabaseLock { get; } = new();

    /// <inheritdoc/>
    public void OnSaveChanges(JellyfinDbContext context, Action saveChanges)
    {
        // using (DbLock.EnterWrite(_logger))
        // {
        // }
        saveChanges();
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
        await saveChanges().ConfigureAwait(false);
        // using (DbLock.EnterWrite(_logger))
        // {
        // }
    }

    private sealed class TransactionLockingInterceptor : DbTransactionInterceptor
    {
        private static Guid? _lockInitiator;
        private readonly ILogger _logger;

        public TransactionLockingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            WriteLock(eventData);

            return base.TransactionStarting(connection, eventData, result);
        }

        private void WriteLock(TransactionStartingEventData eventData)
        {
            _logger.LogTrace("Write Lock");
            if (!DatabaseLock.IsWriteLockHeld)
            {
                _logger.LogTrace("Aquire Write Lock for {Connection}", eventData.ConnectionId);
                DatabaseLock.EnterWriteLock();
                _logger.LogTrace("Write Lock Aquired {Connection}", eventData.ConnectionId);
                _lockInitiator = eventData.ConnectionId;
            }
            else
            {
                _logger.LogTrace("Write Lock already aquired {CurrentConnection}", _lockInitiator.ToString());
            }
        }

        private void HandleWriteLock(TransactionEndEventData eventData)
        {
            _logger.LogTrace("End Write Lock for {Connection} from {CurrentLock}", eventData.ConnectionId, _lockInitiator);
            if (DatabaseLock.IsWriteLockHeld && _lockInitiator.Equals(eventData.ConnectionId))
            {
                _logger.LogTrace("Finish Write Lock {Connection}", _lockInitiator.Value);
                DatabaseLock.ExitWriteLock();
                _lockInitiator = null;
            }
            else
            {
                _logger.LogTrace("Not Initiator, skip handling.");
            }
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
        {
            WriteLock(eventData);

            return base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
        }

        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            HandleWriteLock(eventData);

            base.TransactionCommitted(transaction, eventData);
        }

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            HandleWriteLock(eventData);

            return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
        {
            HandleWriteLock(eventData);

            base.TransactionFailed(transaction, eventData);
        }

        public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            HandleWriteLock(eventData);

            return base.TransactionFailedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        {
            HandleWriteLock(eventData);

            base.TransactionRolledBack(transaction, eventData);
        }

        public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            HandleWriteLock(eventData);

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
            using (DbLock.EnterWrite(_logger))
            {
                return InterceptionResult<int>.SuppressWithResult(command.ExecuteNonQuery());
            }
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            using (DbLock.EnterWrite(_logger))
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

        public DbLock(Action? action = null)
        {
            _action = action;
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static IDisposable EnterWrite(ILogger logger)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            logger.LogTrace("Enter Write");
            if (DatabaseLock.IsWriteLockHeld)
            {
                logger.LogTrace("Write Held");
                return _noLock;
            }

            logger.LogTrace("Aquire Write");
            DatabaseLock.EnterWriteLock();
            return new DbLock(() =>
            {
                logger.LogTrace("Release Write");
                DatabaseLock.ExitWriteLock();
            });
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static IDisposable EnterRead(ILogger logger)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            logger.LogTrace("Enter Read");
            if (DatabaseLock.IsWriteLockHeld)
            {
                logger.LogTrace("Write Held");
                return _noLock;
            }

            logger.LogTrace("Aquire Write");
            DatabaseLock.EnterReadLock();
            return new DbLock(() =>
            {
                logger.LogTrace("Release Read");
                DatabaseLock.ExitReadLock();
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
    }
}
