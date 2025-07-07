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

        public string Name => "DesignTimeDefaultSqlite";

        public string Description => "DesignTimeDefault SQLite Provider";

        public bool IsSqlite => true;

        public bool IsPostgres => false;

        public bool IsCaseSensitive => false;

        public string GroupConcatSeparator => ",";

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

        public string GetConnectionString(string path) => $"Data Source={path}";

        public string GetFindInSetExpression(string column, string value) => $"instr({column}, {value}) > 0";

        public string GetGuidExpression() => "lower(hex(randomblob(16)))";

        public string GetRandomExpression() => "RANDOM()";

        public string GetCaseSensitiveLikeExpression(string column, string value, char escapeChar = '\\') => $"{column} LIKE {value} ESCAPE '{escapeChar}'";

        public string GetCaseInsensitiveLikeExpression(string column, string value, char escapeChar = '\\') => $"{column} LIKE {value} ESCAPE '{escapeChar}'";

        public string GetConcatExpression(params string[] values) => string.Join(" || ", values);

        public string GetUtcNowExpression() => "strftime('%Y-%m-%d %H:%M:%f', 'now')";

        public string GetLengthExpression(string value) => $"LENGTH({value})";

        public string GetIntegerTrueExpression() => "1";

        public string GetIntegerFalseExpression() => "0";

        public string GetCoalesceExpression(params string[] values) => $"COALESCE({string.Join(", ", values)})";

        public string GetRegexpExpression(string column, string pattern) => $"{column} REGEXP {pattern}";

        public string GetLowerExpression(string value) => $"LOWER({value})";

        public string GetUpperExpression(string value) => $"UPPER({value})";

        public string GetLastInsertedIdExpression(string tableName) => "last_insert_rowid()";

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

        public void Initialise(DbContextOptionsBuilder optionsBuilder)
        {
            // No-op for design time
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            // No-op for design time
        }

        public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // No-op for design time
        }

        public Task RunScheduledOptimisation(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RunShutdownTask(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string> MigrationBackupFast(CancellationToken cancellationToken) => Task.FromResult("design_time_backup.db");

        public Task RestoreBackupFast(string backupFilePath, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteBackup(string backupFilePath) => Task.CompletedTask;

        public Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? excludedTables = null) => Task.CompletedTask;
    }
}
