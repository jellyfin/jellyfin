using System;
using System.Data.Common;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// A locking behavior that will always block any operation while a write is requested. Mimikes the old SqliteRepository behavior.
/// </summary>
public class PessimisticLockBehavior : IEntityFrameworkCoreLockingBehavior
{
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
        optionsBuilder.AddInterceptors(new LockingInterceptor());
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
            if (!DatabaseLock.IsWriteLockHeld)
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
            if (DatabaseLock.IsWriteLockHeld)
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
            if (DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.ExitReadLock();
            }
#pragma warning restore MT1013 // Releasing lock without guarantee of execution
            base.CommandCanceled(command, eventData);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
#pragma warning disable MT1013 // Releasing lock without guarantee of execution
            if (DatabaseLock.IsWriteLockHeld)
            {
                DatabaseLock.ExitReadLock();
            }
#pragma warning restore MT1013 // Releasing lock without guarantee of execution
            base.CommandFailed(command, eventData);
        }
    }
}
