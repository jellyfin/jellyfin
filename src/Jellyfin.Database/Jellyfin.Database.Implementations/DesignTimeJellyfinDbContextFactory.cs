// File: src/Jellyfin.Database/Jellyfin.Database.Implementations/DesignTimeJellyfinDbContextFactory.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Database.Implementations
{
    // Minimal stub for IDbContextFactory<JellyfinDbContext>
    internal class DesignTimeDbContextFactoryInstance : IDbContextFactory<JellyfinDbContext>
    {
        private readonly IDesignTimeDbContextFactory<JellyfinDbContext> _factory;

        public DesignTimeDbContextFactoryInstance(IDesignTimeDbContextFactory<JellyfinDbContext> factory)
        {
            _factory = factory;
        }

        public JellyfinDbContext CreateDbContext()
        {
            // Pass empty args, as the main factory's CreateDbContext(string[] args) will handle defaults
            return _factory.CreateDbContext(Array.Empty<string>());
        }
    }

    // Minimal stub for IJellyfinDatabaseProvider
    internal class DesignTimeDatabaseProvider : IJellyfinDatabaseProvider
    {
        public string Name => "DesignTimeDefaultSqlite";
        public string Description => "DesignTimeDefault SQLite Provider";
        public bool IsSqlite => true;
        public bool IsPostgres => false;
        public bool IsCaseSensitive => false;
        public string GroupConcatSeparator => ",";

        // This factory is for design-time, not runtime DI.
        // It now returns an IDbContextFactory<JellyfinDbContext>
        private IDbContextFactory<JellyfinDbContext> _dbContextFactory;
        public IDbContextFactory<JellyfinDbContext> DbContextFactory
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

        public void Initialise(DbContextOptionsBuilder optionsBuilder) { /* No-op for design time */ }
        public void OnModelCreating(ModelBuilder modelBuilder) { /* No-op for design time */ }
        public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) { /* No-op for design time */ }
        public Task RunScheduledOptimisation(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RunShutdownTask(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<string> MigrationBackupFast(CancellationToken cancellationToken) => Task.FromResult("design_time_backup.db");
        public Task RestoreBackupFast(string backupFilePath, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteBackup(string backupFilePath) => Task.CompletedTask; // Changed to return Task
        public Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? excludedTables = null) => Task.CompletedTask;
    }

    // Minimal stub for IEntityFrameworkCoreLockingBehavior
    internal class DesignTimeLockingBehavior : IEntityFrameworkCoreLockingBehavior
    {
        public bool IsTransactionOwned { get; set; }
        public bool AcquireWriteLock(TimeSpan timeout) => true;
        public Task<bool> AcquireWriteLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void ReleaseWriteLock() { /* No-op */ }
        public void EnterTransaction() { /* No-op */ }
        public void ExitTransaction() { /* No-op */ }
        public TResult ExecuteRead<TResult>(Func<TResult> action) => action();
        public Task<TResult> ExecuteReadAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default) => action();
        public void ExecuteWrite(Action action, TimeSpan timeout) => action();
        public Task ExecuteWriteAsync(Func<Task> action, TimeSpan timeout, CancellationToken cancellationToken = default) => action();
        public TResult ExecuteWrite<TResult>(Func<TResult> action, TimeSpan timeout) => action();
        public Task<TResult> ExecuteWriteAsync<TResult>(Func<Task<TResult>> action, TimeSpan timeout, CancellationToken cancellationToken = default) => action();
        public void Dispose() { /* No-op */ }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Initialise(DbContextOptionsBuilder optionsBuilder) { /* No-op for design time */ }
        public void OnSaveChanges(JellyfinDbContext dbContext, Action baseSaveChanges) => baseSaveChanges();
        public Task OnSaveChangesAsync(JellyfinDbContext dbContext, Func<Task> baseSaveChangesAsync) => baseSaveChangesAsync();
    }

    public class DesignTimeJellyfinDbContextFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
    {
        public JellyfinDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();

            optionsBuilder.UseSqlite(
                "Data Source=design_time_temp.db", // Dummy connection string for design-time
                sqliteOptionsAction => sqliteOptionsAction.MigrationsAssembly("Jellyfin.Database.Providers.Sqlite")
            );

            var logger = new NullLogger<JellyfinDbContext>();
            var dummyProvider = new DesignTimeDatabaseProvider();
            var dummyLockingBehavior = new DesignTimeLockingBehavior();

            return new JellyfinDbContext(optionsBuilder.Options, logger, dummyProvider, dummyLockingBehavior);
        }
    }
}
