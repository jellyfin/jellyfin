// File: src/Jellyfin.Database/Jellyfin.Database.Implementations/DesignTimeDatabaseProvider.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design; // Required for IDbContextFactory

namespace Jellyfin.Database.Implementations
{
    /// <summary>
    /// Minimal stub for IJellyfinDatabaseProvider for design-time EF Core tools.
    /// </summary>
    internal sealed class DesignTimeDatabaseProvider : IJellyfinDatabaseProvider
    {
        private IDbContextFactory<JellyfinDbContext>? _dbContextFactory;

        /// <inheritdoc />
        public string Name => "DesignTimeDefaultSqlite";

        /// <inheritdoc />
        public string Description => "DesignTimeDefault SQLite Provider";

        /// <inheritdoc />
        public bool IsSqlite => true;

        /// <inheritdoc />
        public bool IsPostgres => false;

        /// <inheritdoc />
        public bool IsCaseSensitive => false;

        /// <inheritdoc />
        public string GroupConcatSeparator => ",";

        /// <inheritdoc />
        public IDbContextFactory<JellyfinDbContext>? DbContextFactory
        {
            get
            {
                _dbContextFactory ??= new DesignTimeDbContextFactoryInstance(new DesignTimeJellyfinDbContextFactory());
                return _dbContextFactory;
            }
            set
            {
                _dbContextFactory = value;
            }
        }

        /// <inheritdoc />
        public string GetConnectionString(string path) => $"Data Source={path}";

        /// <inheritdoc />
        public string GetFindInSetExpression(string column, string value) => $"instr({column}, {value}) > 0";

        /// <inheritdoc />
        public string GetGuidExpression() => "lower(hex(randomblob(16)))";

        /// <inheritdoc />
        public string GetRandomExpression() => "RANDOM()";

        /// <inheritdoc />
        public string GetCaseSensitiveLikeExpression(string column, string value, char escapeChar = '\\') => $"{column} LIKE {value} ESCAPE '{escapeChar}'";

        /// <inheritdoc />
        public string GetCaseInsensitiveLikeExpression(string column, string value, char escapeChar = '\\') => $"{column} LIKE {value} ESCAPE '{escapeChar}'";

        /// <inheritdoc />
        public string GetConcatExpression(params string[] values) => string.Join(" || ", values);

        /// <inheritdoc />
        public string GetUtcNowExpression() => "strftime('%Y-%m-%d %H:%M:%f', 'now')";

        /// <inheritdoc />
        public string GetLengthExpression(string value) => $"LENGTH({value})";

        /// <inheritdoc />
        public string GetIntegerTrueExpression() => "1";

        /// <inheritdoc />
        public string GetIntegerFalseExpression() => "0";

        /// <inheritdoc />
        public string GetCoalesceExpression(params string[] values) => $"COALESCE({string.Join(", ", values)})";

        /// <inheritdoc />
        public string GetRegexpExpression(string column, string pattern) => $"{column} REGEXP {pattern}";

        /// <inheritdoc />
        public string GetLowerExpression(string value) => $"LOWER({value})";

        /// <inheritdoc />
        public string GetUpperExpression(string value) => $"UPPER({value})";

        /// <inheritdoc />
        public string GetLastInsertedIdExpression(string tableName) => "last_insert_rowid()";

        /// <inheritdoc />
        public DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder optionsBuilder, string connectionString, string? migrationsAssembly = null)
        {
            return optionsBuilder.UseSqlite(connectionString, sqliteOptions =>
            {
                if (!string.IsNullOrEmpty(migrationsAssembly))
                {
                    sqliteOptions.MigrationsAssembly(migrationsAssembly);
                }
            });
        }

        /// <inheritdoc />
        public void Initialise(DbContextOptionsBuilder optionsBuilder)
        {
            // No-op for design time
        }

        /// <inheritdoc />
        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            // No-op for design time
        }

        /// <inheritdoc />
        public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // No-op for design time
        }

        /// <inheritdoc />
        public Task RunScheduledOptimisation(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public Task RunShutdownTask(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public Task<string> MigrationBackupFast(CancellationToken cancellationToken) => Task.FromResult("design_time_backup.db");

        /// <inheritdoc />
        public Task RestoreBackupFast(string backupFilePath, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public Task DeleteBackup(string backupFilePath) => Task.CompletedTask;

        /// <inheritdoc />
        public Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? excludedTables = null) => Task.CompletedTask;
    }
}
