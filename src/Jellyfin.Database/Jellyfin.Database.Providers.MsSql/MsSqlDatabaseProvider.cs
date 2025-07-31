using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.MsSql
{
    /// <summary>
    /// Configures jellyfin to use an MsSql database.
    /// </summary>
    [JellyfinDatabaseProviderKey("Jellyfin-MsSql")]
    public class MsSqlDatabaseProvider : IJellyfinDatabaseProvider
    {
        private readonly ILogger<MsSqlDatabaseProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsSqlDatabaseProvider"/> class.
        /// </summary>
        /// <param name="logger">A logger.</param>
        public MsSqlDatabaseProvider(ILogger<MsSqlDatabaseProvider> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

        /// <inheritdoc/>
        public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
        }

        /// <inheritdoc/>
        public Task DeleteBackup(string key)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
        {
            var sqlConnectionBuilder = new SqlConnectionStringBuilder(databaseConfiguration?.CustomProviderOptions?.ConnectionString);

            options
                .UseSqlServer(
                    sqlConnectionBuilder.ToString(),
                    sqlOptions => sqlOptions.MigrationsAssembly(GetType().Assembly));
        }

        /// <inheritdoc/>
        public Task<string> MigrationBackupFast(CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }

        /// <inheritdoc/>
        public void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        /// <inheritdoc/>
        public async Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
        {
            ArgumentNullException.ThrowIfNull(tableNames);

            var deleteQueries = new List<string>();
            foreach (var tableName in tableNames)
            {
                deleteQueries.Add($"DELETE FROM [{tableName}];");
            }

            var deleteAllQuery =
            $"""
                BEGIN TRY
                    BEGIN TRANSACTION;
                    EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
                    {string.Join('\n', deleteQueries)}
                    EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';
                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0
                        ROLLBACK TRANSACTION;
                    THROW;
                END CATCH
                """;

            await dbContext.Database.ExecuteSqlRawAsync(deleteAllQuery).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task RestoreBackupFast(string key, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RunScheduledOptimisation(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RunShutdownTask(CancellationToken cancellationToken)
        {
            SqlConnection.ClearAllPools();
            return Task.CompletedTask;
        }
    }
}
