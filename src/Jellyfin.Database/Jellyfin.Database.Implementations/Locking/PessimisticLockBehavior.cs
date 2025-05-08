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
        try
        {
            DatabaseLock.EnterWriteLock();
            saveChanges();
        }
        finally
        {
            if (DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.ExitWriteLock();
            }
        }
    }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder optionsBuilder)
    {
        _logger.LogInformation("The database locking mode has been set to: Pessimistic.");
        optionsBuilder.AddInterceptors(new LockingInterceptor());
    }

    /// <inheritdoc/>
    public async Task OnSaveChangesAsync(JellyfinDbContext context, Func<Task> saveChanges)
    {
        try
        {
            DatabaseLock.EnterWriteLock();
            await saveChanges().ConfigureAwait(false);
        }
        finally
        {
            if (DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Adds strict read/write locking.
    /// </summary>
    private sealed class LockingInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            if (!DatabaseLock.IsWriteLockHeld) // enter a write lock as NonQueries are used to manipulate data only if not already held
            {
                DatabaseLock.EnterWriteLock();
            }

            return base.NonQueryExecuting(command, eventData, result);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            if (DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.ExitWriteLock();
            }

            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!DatabaseLock.IsWriteLockHeld) // enter a write lock as NonQueries are used to manipulate data only if not already held
            {
                DatabaseLock.EnterWriteLock();
            }

            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.ExitWriteLock();
            }

            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <inheritdoc/>
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            if (!DatabaseLock.IsWriteLockHeld) // enter a read lock only if not already a write lock has been issued by the savechanges invocation
            {
                DatabaseLock.EnterReadLock();
            }

            return base.ReaderExecuting(command, eventData, result);
        }

        /// <inheritdoc/>
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            if (!DatabaseLock.IsWriteLockHeld && DatabaseLock.IsReadLockHeld) // if the current thread has no write lock, it will have entered a read lock. As the write lock is managed by saveChanges only handle readlock here
            {
                DatabaseLock.ExitReadLock();
            }

            return base.ReaderExecuted(command, eventData, result);
        }

        /// <inheritdoc/>
        public override void CommandCanceled(DbCommand command, CommandEndEventData eventData)
        {
            if (!DatabaseLock.IsWriteLockHeld && DatabaseLock.IsReadLockHeld) // if the current thread has no write lock, it will have entered a read lock. As the write lock is managed by saveChanges only handle readlock here
            {
                DatabaseLock.ExitReadLock();
            }
            else if (DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.ExitWriteLock();
            }

            base.CommandCanceled(command, eventData);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            if (!DatabaseLock.IsWriteLockHeld && DatabaseLock.IsReadLockHeld) // if the current thread has no write lock, it will have entered a read lock. As the write lock is managed by saveChanges only handle readlock here
            {
                DatabaseLock.ExitReadLock();
            }
            else if (DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.ExitWriteLock();
            }

            base.CommandFailed(command, eventData);
        }
    }
}
