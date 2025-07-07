// File: src/Jellyfin.Database/Jellyfin.Database.Implementations/DesignTimeLockingBehavior.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Implementations
{
    /// <summary>
    /// Minimal stub for IEntityFrameworkCoreLockingBehavior for design-time EF Core tools.
    /// </summary>
    internal sealed class DesignTimeLockingBehavior : IEntityFrameworkCoreLockingBehavior
    {
        public bool IsTransactionOwned { get; set; }

        public bool AcquireWriteLock(TimeSpan timeout) => true;

        public Task<bool> AcquireWriteLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void ReleaseWriteLock()
        {
            // No-op
        }

        public void EnterTransaction()
        {
            // No-op
        }

        public void ExitTransaction()
        {
            // No-op
        }

        public TResult ExecuteRead<TResult>(Func<TResult> action) => action();

        public Task<TResult> ExecuteReadAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default) => action();

        public void ExecuteWrite(Action action, TimeSpan timeout) => action();

        public Task ExecuteWriteAsync(Func<Task> action, TimeSpan timeout, CancellationToken cancellationToken = default) => action();

        public TResult ExecuteWrite<TResult>(Func<TResult> action, TimeSpan timeout) => action();

        public Task<TResult> ExecuteWriteAsync<TResult>(Func<Task<TResult>> action, TimeSpan timeout, CancellationToken cancellationToken = default) => action();

        public void Initialise(DbContextOptionsBuilder optionsBuilder)
        {
            // No-op for design time
        }

        public void OnSaveChanges(JellyfinDbContext dbContext, Action baseSaveChanges) => baseSaveChanges();

        public Task OnSaveChangesAsync(JellyfinDbContext dbContext, Func<Task> baseSaveChangesAsync) => baseSaveChangesAsync();

        public void Dispose()
        {
            // No-op for design time.
            // GC.SuppressFinalize(this) removed as per IDISP024 when type is sealed and has no finalizer.
        }

        public ValueTask DisposeAsync()
        {
            // No-op for design time.
            // GC.SuppressFinalize(this) removed as per IDISP024 when type is sealed and has no finalizer.
            return ValueTask.CompletedTask;
        }
    }
}
