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
            DatabaseLock.ExitWriteLock();
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
            DatabaseLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Adds strict read/write locking.
    /// </summary>
    private sealed class LockingInterceptor : DbCommandInterceptor
    {
        /// <inheritdoc/>
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
#pragma warning disable MT1012 // Acquiring lock without guarantee of releasing
            if (!DatabaseLock.IsWriteLockHeld) // enter a read lock only if not already a write lock has been issued by the savechanges invocation
            {
                DatabaseLock.EnterReadLock();
            }

#pragma warning restore MT1012 // Acquiring lock without guarantee of releasing
            return base.ReaderExecuting(command, eventData, result);
        }

        /// <inheritdoc/>
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
#pragma warning disable MT1013 // Releasing lock without guarantee of execution
            if (!DatabaseLock.IsWriteLockHeld) // if the current thread has no write lock, it will have entered a read lock. As the write lock is managed by saveChanges only handle readlock here
            {
                DatabaseLock.ExitReadLock();
            }
#pragma warning restore MT1013 // Releasing lock without guarantee of execution
            return base.ReaderExecuted(command, eventData, result);
        }

        /// <inheritdoc/>
        public override void CommandCanceled(DbCommand command, CommandEndEventData eventData)
        {
#pragma warning disable MT1013 // Releasing lock without guarantee of execution
            if (!DatabaseLock.IsWriteLockHeld) // if the current thread has no write lock, it will have entered a read lock. As the write lock is managed by saveChanges only handle readlock here
            {
                DatabaseLock.ExitReadLock();
            }
#pragma warning restore MT1013 // Releasing lock without guarantee of execution
            base.CommandCanceled(command, eventData);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
#pragma warning disable MT1013 // Releasing lock without guarantee of execution
            if (!DatabaseLock.IsWriteLockHeld) // if the current thread has no write lock, it will have entered a read lock. As the write lock is managed by saveChanges only handle readlock here
            {
                DatabaseLock.ExitReadLock();
            }
#pragma warning restore MT1013 // Releasing lock without guarantee of execution
            base.CommandFailed(command, eventData);
        }
    }
}
