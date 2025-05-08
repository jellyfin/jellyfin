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

    /// <summary>
    /// Initializes a new instance of the <see cref="PessimisticLockBehavior"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    public PessimisticLockBehavior(ILogger<PessimisticLockBehavior> logger)
    {
        _logger = logger;
    }

    private static ReaderWriterLockSlim DatabaseLock { get; } = new();

    /// <inheritdoc/>
    public void OnSaveChanges(JellyfinDbContext context, Action saveChanges)
    {
        using (DbLock.EnterWrite())
        {
            saveChanges();
        }
    }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder optionsBuilder)
    {
        _logger.LogInformation("The database locking mode has been set to: Pessimistic.");
        optionsBuilder.AddInterceptors(new CommandLockingInterceptor());
        optionsBuilder.AddInterceptors(new TransactionLockingInterceptor());
    }

    /// <inheritdoc/>
    public async Task OnSaveChangesAsync(JellyfinDbContext context, Func<Task> saveChanges)
    {
        using (DbLock.EnterWrite())
        {
            await saveChanges().ConfigureAwait(false);
        }
    }

    private sealed class TransactionLockingInterceptor : DbTransactionInterceptor
    {
        private static AsyncLocal<Guid> _lockInitiator = new();

        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            if (!DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.EnterWriteLock();
                _lockInitiator.Value = eventData.ConnectionId;
            }

            return base.TransactionStarting(connection, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
        {
            if (!DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.EnterWriteLock();
                _lockInitiator.Value = eventData.ConnectionId;
            }

            return base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
        }

        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            if (DatabaseLock.IsWriteLockHeld && _lockInitiator.Value.Equals(eventData.ConnectionId))
            {
                DatabaseLock.ExitWriteLock();
            }

            base.TransactionCommitted(transaction, eventData);
        }

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (DatabaseLock.IsWriteLockHeld && _lockInitiator.Value.Equals(eventData.ConnectionId))
            {
                DatabaseLock.ExitWriteLock();
            }

            return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
        {
            if (DatabaseLock.IsWriteLockHeld && _lockInitiator.Value.Equals(eventData.ConnectionId))
            {
                DatabaseLock.ExitWriteLock();
            }

            base.TransactionFailed(transaction, eventData);
        }

        public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (DatabaseLock.IsWriteLockHeld && _lockInitiator.Value.Equals(eventData.ConnectionId))
            {
                DatabaseLock.ExitWriteLock();
            }

            return base.TransactionFailedAsync(transaction, eventData, cancellationToken);
        }

        public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        {
            if (DatabaseLock.IsWriteLockHeld && _lockInitiator.Value.Equals(eventData.ConnectionId))
            {
                DatabaseLock.ExitWriteLock();
            }

            base.TransactionRolledBack(transaction, eventData);
        }

        public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (DatabaseLock.IsWriteLockHeld && _lockInitiator.Value.Equals(eventData.ConnectionId))
            {
                DatabaseLock.ExitWriteLock();
            }

            return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
        }
    }

    /// <summary>
    /// Adds strict read/write locking.
    /// </summary>
    private sealed class CommandLockingInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            using (DbLock.EnterWrite())
            {
                return InterceptionResult<int>.SuppressWithResult(command.ExecuteNonQuery());
            }
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            using (DbLock.EnterWrite())
            {
                return InterceptionResult<int>.SuppressWithResult(await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false));
            }
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            using (DbLock.EnterRead())
            {
                return InterceptionResult<object>.SuppressWithResult(command.ExecuteScalar()!);
            }
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            using (DbLock.EnterRead())
            {
                return InterceptionResult<object>.SuppressWithResult((await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!);
            }
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            using (DbLock.EnterRead())
            {
                return InterceptionResult<DbDataReader>.SuppressWithResult(command.ExecuteReader()!);
            }
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            using (DbLock.EnterRead())
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
        public static IDisposable EnterWrite()
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            if (DatabaseLock.IsWriteLockHeld)
            {
                return _noLock;
            }

            DatabaseLock.EnterWriteLock();
            return new DbLock(() => DatabaseLock.ExitWriteLock());
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static IDisposable EnterRead()
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            if (DatabaseLock.IsWriteLockHeld)
            {
                return _noLock;
            }

            DatabaseLock.EnterReadLock();
            return new DbLock(() => DatabaseLock.ExitReadLock());
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
