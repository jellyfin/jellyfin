// File: src/Jellyfin.Database/Jellyfin.Database.Implementations/DesignTimeLockingBehavior.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore; // Added for DbContextOptionsBuilder

namespace Jellyfin.Database.Implementations
{
    /// <summary>
    /// Minimal stub for IEntityFrameworkCoreLockingBehavior for design-time EF Core tools.
    /// </summary>
    internal sealed class DesignTimeLockingBehavior : IEntityFrameworkCoreLockingBehavior
    {
        /// <inheritdoc />
        public bool IsTransactionOwned { get; set; }

        /// <inheritdoc />
        public bool AcquireWriteLock(TimeSpan timeout) => true;

        /// <inheritdoc />
        public Task<bool> AcquireWriteLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => Task.FromResult(true);

        /// <inheritdoc />
        public void ReleaseWriteLock()
        {
            // No-op
        }

        /// <inheritdoc />
        public void EnterTransaction()
        {
            // No-op
        }

        /// <inheritdoc />
        public void ExitTransaction()
        {
            // No-op
        }

        /// <inheritdoc />
        public TResult ExecuteRead<TResult>(Func<TResult> action) => action();

        /// <inheritdoc />
        public Task<TResult> ExecuteReadAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default) => action();

        /// <inheritdoc />
        public void ExecuteWrite(Action action, TimeSpan timeout) => action();

        /// <inheritdoc />
        public Task ExecuteWriteAsync(Func<Task> action, TimeSpan timeout, CancellationToken cancellationToken = default) => action();

        /// <inheritdoc />
        public TResult ExecuteWrite<TResult>(Func<TResult> action, TimeSpan timeout) => action();

        /// <inheritdoc />
        public Task<TResult> ExecuteWriteAsync<TResult>(Func<Task<TResult>> action, TimeSpan timeout, CancellationToken cancellationToken = default) => action();

        /// <inheritdoc />
        public void Dispose()
        {
            // No-op
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        /// <inheritdoc />
        public void Initialise(DbContextOptionsBuilder optionsBuilder)
        {
            // No-op for design time
        }

        /// <inheritdoc />
        public void OnSaveChanges(JellyfinDbContext dbContext, Action baseSaveChanges) => baseSaveChanges();

        /// <inheritdoc />
        public Task OnSaveChangesAsync(JellyfinDbContext dbContext, Func<Task> baseSaveChangesAsync) => baseSaveChangesAsync();
    }
}
