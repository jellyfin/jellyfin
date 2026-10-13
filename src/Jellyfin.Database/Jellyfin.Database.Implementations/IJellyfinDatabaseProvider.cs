using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.DbConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Defines the type and extension points for multi database support.
/// </summary>
public interface IJellyfinDatabaseProvider
{
    /// <summary>
    /// Gets or Sets the Database Factory when initialisaition is done.
    /// </summary>
    IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

    /// <summary>
    /// Gets a value indicating whether the engine undoes a schema migration that fails part way, so that the database
    /// is left as it was before the migration. Engines that commit DDL statements implicitly, such as MySQL and
    /// MariaDB, cannot: a failed migration leaves the schema partly changed and without a history row. For those, the
    /// server records the migration it is about to apply in a file in the data directory, and refuses to migrate
    /// again until an operator has restored the database and deleted that file. The default is <see langword="true"/>.
    /// </summary>
    bool SupportsTransactionalMigrations => true;

    /// <summary>
    /// Initialises jellyfins EFCore database access.
    /// </summary>
    /// <param name="options">The EFCore database options.</param>
    /// <param name="databaseConfiguration">The Jellyfin database options.</param>
    void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration);

    /// <summary>
    /// Will be invoked when EFCore wants to build its model.
    /// </summary>
    /// <param name="modelBuilder">The ModelBuilder from EFCore.</param>
    void OnModelCreating(ModelBuilder modelBuilder);

    /// <summary>
    /// Will be invoked when EFCore wants to configure its model.
    /// </summary>
    /// <param name="configurationBuilder">The ModelConfigurationBuilder from EFCore.</param>
    void ConfigureConventions(ModelConfigurationBuilder configurationBuilder);

    /// <summary>
    /// If supported this should run any periodic maintaince tasks, reclaiming unused space and refreshing the query
    /// planner statistics. Also used after migrations have modified the database.
    /// </summary>
    /// <param name="cancellationToken">The token to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RunScheduledOptimisation(CancellationToken cancellationToken);

    /// <summary>
    /// If supported this should refresh the query planner statistics, e.g. after a library scan changed the data.
    /// Unlike <see cref="RunScheduledOptimisation(CancellationToken)"/> it should not reclaim space, so that it stays
    /// cheap enough to run after every scan.
    /// </summary>
    /// <param name="cancellationToken">The token to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RefreshStatistics(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// If supported this should perform any actions that are required on stopping the jellyfin server. This runs
    /// against a deadline imposed by the service manager, so unlike
    /// <see cref="RunScheduledOptimisation(CancellationToken)"/> it should only do work whose cost does not grow with
    /// the size of the database.
    /// </summary>
    /// <param name="cancellationToken">The token that will be used to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RunShutdownTask(CancellationToken cancellationToken);

    /// <summary>
    /// Runs a full Database backup that can later be restored to.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A key to identify the backup, or an empty string when no snapshot was taken, as the database has nothing that
    /// the provider can snapshot. <see cref="RestoreBackupFast(string, CancellationToken)"/> and
    /// <see cref="DeleteBackup(string)"/> are called with that empty key all the same.
    /// </returns>
    /// <exception cref="NotImplementedException">May throw an NotImplementException if this operation is not supported for this database.</exception>
    Task<string> MigrationBackupFast(CancellationToken cancellationToken);

    /// <summary>
    /// Restores a backup that has been previously created by <see cref="MigrationBackupFast(CancellationToken)"/>.
    /// </summary>
    /// <param name="key">
    /// The key to the backup from which the current database should be restored from, which is empty when
    /// <see cref="MigrationBackupFast(CancellationToken)"/> took no snapshot.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task RestoreBackupFast(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a backup that has been previously created by <see cref="MigrationBackupFast(CancellationToken)"/>.
    /// </summary>
    /// <param name="key">
    /// The key to the backup which should be cleaned up, which is empty when
    /// <see cref="MigrationBackupFast(CancellationToken)"/> took no snapshot.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task DeleteBackup(string key);

    /// <summary>
    /// Removes all contents from the database.
    /// </summary>
    /// <param name="dbContext">The Database context.</param>
    /// <param name="tableNames">The names of the tables to purge or null for all tables to be purged.</param>
    /// <returns>A Task.</returns>
    Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames);

    /// <summary>
    /// Determines whether a write failed because it lost a race with another writer, such as a unique constraint
    /// violation or a deadlock, so that running the same unit of work again against fresh state can succeed.
    /// Constraint violations that would fail every time, such as a foreign key or a not null violation, are not
    /// retryable. Neither are errors about a busy or locked database, which say nothing about rows another writer
    /// inserted and which the provider already waits out for each command.
    /// The exception may be wrapped, for example in a <see cref="DbUpdateException"/>.
    /// </summary>
    /// <param name="exception">The exception thrown by the failed write.</param>
    /// <returns><c>true</c> if repeating the write can succeed; otherwise, <c>false</c>.</returns>
    bool IsRetryableWriteConflict(Exception exception) => false;

    /// <summary>
    /// Prepares the database for inserting rows of <paramref name="entityType"/> with explicit values in its
    /// generated key columns, as a restore does. Engines that refuse such values by default can allow them here, for
    /// example SQL Server with <c>SET IDENTITY_INSERT</c>, which applies to a single table of a session at a time.
    /// The caller inserts the rows of one table, saves them, and then disposes the returned object before it begins
    /// the next table. The default does nothing.
    /// </summary>
    /// <param name="dbContext">The context that inserts the rows.</param>
    /// <param name="entityType">The entity type whose rows are inserted.</param>
    /// <param name="cancellationToken">The token to abort the operation.</param>
    /// <returns>An object that ends the preparation when disposed. It is never <see langword="null"/>.</returns>
    ValueTask<IAsyncDisposable> BeginExplicitIdentityInsertAsync(JellyfinDbContext dbContext, IEntityType entityType, CancellationToken cancellationToken)
        => ValueTask.FromResult<IAsyncDisposable>(NoOpAsyncDisposable.Instance);

    /// <summary>
    /// Brings the database into a usable state after a restore inserted rows with explicit generated key values, for
    /// example by moving the sequences of an engine behind the highest restored value. It runs inside the restore
    /// transaction, after all tables are inserted. The default does nothing.
    /// </summary>
    /// <param name="dbContext">The context of the restore.</param>
    /// <param name="cancellationToken">The token to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CompleteDatabaseRestoreAsync(JellyfinDbContext dbContext, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Applies the settings the provider needs on the database server that persist across sessions, so that they also
    /// reach connections that are pooled or opened later. It is called at every startup, after the schema migrations,
    /// and may run again after a restore, so implementations must be idempotent. Implementations must not throw when
    /// the server refuses a change, for example because of missing privileges: they log it and continue, because
    /// startup must not fail over a setting. The default does nothing.
    /// </summary>
    /// <param name="dbContext">A context on the database that is in use.</param>
    /// <param name="cancellationToken">The token to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ApplyDatabaseSettingsAsync(JellyfinDbContext dbContext, CancellationToken cancellationToken) => Task.CompletedTask;
}
