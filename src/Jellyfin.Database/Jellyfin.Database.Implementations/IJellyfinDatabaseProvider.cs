using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
    /// Initialises jellyfins EFCore database access.
    /// </summary>
    /// <param name="options">The EFCore database options.</param>
    void Initialise(DbContextOptionsBuilder options);

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
    /// If supported this should run any periodic maintaince tasks.
    /// </summary>
    /// <param name="cancellationToken">The token to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RunScheduledOptimisation(CancellationToken cancellationToken);

    /// <summary>
    /// If supported this should perform any actions that are required on stopping the jellyfin server.
    /// </summary>
    /// <param name="cancellationToken">The token that will be used to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RunShutdownTask(CancellationToken cancellationToken);

    /// <summary>
    /// Runs a full Database backup that can later be restored to.
    /// </summary>
    /// <param name="cancellationToken">A cancelation token.</param>
    /// <returns>A key to identify the backup.</returns>
    /// <exception cref="NotImplementedException">May throw an NotImplementException if this operation is not supported for this database.</exception>
    Task<string> MigrationBackupFast(CancellationToken cancellationToken);

    /// <summary>
    /// Restores a backup that has been previously created by <see cref="MigrationBackupFast(CancellationToken)"/>.
    /// </summary>
    /// <param name="key">The key to the backup from which the current database should be restored from.</param>
    /// <param name="cancellationToken">A cancelation token.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task RestoreBackupFast(string key, CancellationToken cancellationToken);
}
