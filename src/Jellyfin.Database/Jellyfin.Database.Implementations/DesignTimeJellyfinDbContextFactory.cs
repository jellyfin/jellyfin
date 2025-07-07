// File: src/Jellyfin.Database/Jellyfin.Database.Implementations/DesignTimeJellyfinDbContextFactory.cs
using System; // For EventArgs or other basic types if stubs need them
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Database.Implementations
{
    // Minimal stub for IJellyfinDatabaseProvider
    internal class DesignTimeDatabaseProvider : IJellyfinDatabaseProvider
    {
        public string Name => "DesignTimeDefault";
        public string Description => "DesignTimeDefault";
        public bool IsSqlite => true; // Assuming SQLite for design time as per UseSqlite
        public bool IsPostgres => false;
        public bool IsCaseSensitive => false; // Typical for SQLite default
        public string GroupConcatSeparator => ","; // Typical for SQLite

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
        public string GetLastInsertedIdExpression(string tableName) => $"last_insert_rowid()"; // Specific to SQLite
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
    }

    // Minimal stub for IEntityFrameworkCoreLockingBehavior
    internal class DesignTimeLockingBehavior : IEntityFrameworkCoreLockingBehavior
    {
        public bool IsTransactionOwned { get; set; }
        public bool AcquireWriteLock(TimeSpan timeout) => true;
        public Task<bool> AcquireWriteLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void ReleaseWriteLock() { }
        public void EnterTransaction() { }
        public void ExitTransaction() { }
        public TResult ExecuteRead<TResult>(Func<TResult> action) => action();
        public Task<TResult> ExecuteReadAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default) => action();
        public void ExecuteWrite(Action action, TimeSpan timeout) => action();
        public Task ExecuteWriteAsync(Func<Task> action, TimeSpan timeout, CancellationToken cancellationToken = default) => action();
        public TResult ExecuteWrite<TResult>(Func<TResult> action, TimeSpan timeout) => action();
        public Task<TResult> ExecuteWriteAsync<TResult>(Func<Task<TResult>> action, TimeSpan timeout, CancellationToken cancellationToken = default) => action();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public class DesignTimeJellyfinDbContextFactory : IDesignTimeDbContextFactory<JellyfinDbContext>
    {
        public JellyfinDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();

            optionsBuilder.UseSqlite(
                "Data Source=design_time_temp.db",
                sqliteOptionsAction => sqliteOptionsAction.MigrationsAssembly("Jellyfin.Database.Providers.Sqlite")
            );

            var logger = new NullLogger<JellyfinDbContext>();
            var dummyProvider = new DesignTimeDatabaseProvider();
            var dummyLockingBehavior = new DesignTimeLockingBehavior();

            return new JellyfinDbContext(optionsBuilder.Options, logger, dummyProvider, dummyLockingBehavior);
        }
    }
}
