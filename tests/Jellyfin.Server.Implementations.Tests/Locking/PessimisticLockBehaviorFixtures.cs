using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Locking;

public sealed partial class PessimisticLockBehaviorTests
{
    private sealed class SqliteTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _keepAliveConnection;
        private readonly string _connectionString;

        private SqliteTestDatabase(SqliteConnection keepAliveConnection, string connectionString)
        {
            _keepAliveConnection = keepAliveConnection;
            _connectionString = connectionString;
        }

        public static async Task<SqliteTestDatabase> CreateAsync(PessimisticLockBehavior behavior)
        {
            var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var keepAliveConnection = new SqliteConnection(connectionString);
            await keepAliveConnection.OpenAsync(TestContext.Current.CancellationToken);

            var database = new SqliteTestDatabase(keepAliveConnection, connectionString);
            await using var context = database.CreateContext(behavior);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            return database;
        }

        public LockingDbContext CreateContext(
            PessimisticLockBehavior behavior,
            bool enableDelayFunction = false,
            TaskCompletionSource? delayStarted = null,
            TaskCompletionSource? commandStarted = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LockingDbContext>()
                .UseSqlite(_connectionString);
            if (enableDelayFunction)
            {
                optionsBuilder.AddInterceptors(new DelayFunctionConnectionInterceptor(delayStarted));
            }

            if (commandStarted is not null)
            {
                optionsBuilder.AddInterceptors(new CommandStartedInterceptor(commandStarted));
            }

            behavior.Initialise(optionsBuilder);

            return new LockingDbContext(optionsBuilder.Options);
        }

        public async ValueTask DisposeAsync()
        {
            await _keepAliveConnection.DisposeAsync();
        }
    }

    private sealed class LockingDbContext(DbContextOptions<LockingDbContext> options) : DbContext(options)
    {
        public DbSet<Row> Rows => Set<Row>();
    }

    private sealed class Row
    {
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    private sealed class DelayFunctionConnectionInterceptor : DbConnectionInterceptor
    {
        private readonly TaskCompletionSource? _delayStarted;

        public DelayFunctionConnectionInterceptor(TaskCompletionSource? delayStarted)
        {
            _delayStarted = delayStarted;
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            AddDelayFunction(connection);
        }

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            AddDelayFunction(connection);
            return Task.CompletedTask;
        }

        private void AddDelayFunction(DbConnection connection)
        {
            if (connection is SqliteConnection sqliteConnection)
            {
                sqliteConnection.CreateFunction(
                    "delay",
                    (int milliseconds) =>
                    {
                        _delayStarted?.TrySetResult();
                        Thread.Sleep(milliseconds);
                        return milliseconds;
                    });
            }
        }
    }

    private sealed class CommandStartedInterceptor(TaskCompletionSource commandStarted) : DbCommandInterceptor
    {
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            commandStarted.TrySetResult();
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            commandStarted.TrySetResult();
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class CongestionLoggerFactory(Action onQueryCongestionDetected) : ILoggerFactory
    {
        private int _notified;

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CongestionLogger(() =>
            {
                if (Interlocked.Exchange(ref _notified, 1) == 0)
                {
                    onQueryCongestionDetected();
                }
            });
        }

        public void Dispose()
        {
        }
    }

    private sealed class CongestionLogger(Action onQueryCongestionDetected) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter(state, exception).Contains("Query congestion detected", StringComparison.Ordinal))
            {
                onQueryCongestionDetected();
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
