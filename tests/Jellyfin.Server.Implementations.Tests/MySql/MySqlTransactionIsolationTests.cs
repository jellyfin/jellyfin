using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

/// <summary>
/// Jellyfin runs MySQL and MariaDB at read committed: every transaction it starts runs at read committed, and the
/// session is still at read committed when it ends. MySqlConnector starts a transaction that names no level at
/// repeatable read, and does so by changing the level of the session, so neither holds without the provider. The probe
/// interceptor reads the level the server reports for each transaction EF Core starts, right after it started.
/// </summary>
public sealed class MySqlTransactionIsolationTests : DatabaseTestFixture
{
    private const string ReadCommitted = "READ-COMMITTED";

    private readonly IsolationProbe _probe;

    public MySqlTransactionIsolationTests()
        : this(new IsolationProbe())
    {
    }

    private MySqlTransactionIsolationTests(IsolationProbe probe)
        : base(probe)
    {
        _probe = probe;
    }

    [ProviderTheory(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    [InlineData(true)]
    [InlineData(false)]
    public void BeginTransaction_WithoutALevel_RunsAtReadCommittedAndLeavesTheSessionThere(bool commit)
    {
        using var context = CreateDbContext();
        context.Database.OpenConnection();

        using (var transaction = context.Database.BeginTransaction())
        {
            AssertReadCommitted(context);
            AssertBegunAtReadCommitted(transaction);
            End(transaction, commit);
        }

        AssertReadCommitted(context);
        AssertProbedReadCommitted(1);
    }

    [ProviderTheory(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BeginTransactionAsync_WithoutALevel_RunsAtReadCommittedAndLeavesTheSessionThere(bool commit)
    {
        await using var context = CreateDbContext();
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);

        await using (var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken))
        {
            await AssertReadCommittedAsync(context);
            AssertBegunAtReadCommitted(transaction);
            await EndAsync(transaction, commit);
        }

        await AssertReadCommittedAsync(context);
        AssertProbedReadCommitted(1);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void BeginTransaction_WithoutALevel_SeesARowAnotherConnectionCommitted()
    {
        using var setup = CreateDbContext();
        CreateProbeTable(setup);
        try
        {
            using var context = CreateDbContext();
            using var transaction = context.Database.BeginTransaction();
            AssertSeesRowsOthersCommit(context);
            transaction.Commit();
        }
        finally
        {
            DropProbeTable(setup);
        }
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task BeginTransactionAsync_WithoutALevel_SeesARowAnotherConnectionCommitted()
    {
        await using var setup = CreateDbContext();
        CreateProbeTable(setup);
        try
        {
            await using var context = CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
            await AssertSeesRowsOthersCommitAsync(context);
            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            DropProbeTable(setup);
        }
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void PooledConnection_ReusedAfterReset_StartsItsTransactionsAtReadCommitted()
    {
        long firstSession;
        using (var first = CreateDbContext())
        {
            first.Database.OpenConnection();
            firstSession = SessionId(first);
            using var transaction = first.Database.BeginTransaction();
            transaction.Commit();
        }

        // The connection went back to the pool when the context was disposed and comes out of it reset.
        using var second = CreateDbContext();
        second.Database.OpenConnection();
        Assert.Equal(firstSession, SessionId(second));
        AssertReadCommitted(second);

        using (var transaction = second.Database.BeginTransaction())
        {
            AssertReadCommitted(second);
            AssertBegunAtReadCommitted(transaction);
            transaction.Commit();
        }

        AssertReadCommitted(second);
        AssertProbedReadCommitted(2);
    }

    // The locking behaviours are added after the provider, as the server does it. The optimistic one starts the
    // transaction itself, with the level it is given.
    [ProviderTheory(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    [InlineData(DatabaseLockingBehaviorTypes.NoLock)]
    [InlineData(DatabaseLockingBehaviorTypes.Optimistic)]
    public void BeginTransaction_UnderEachLockingBehaviour_RunsAtReadCommitted(DatabaseLockingBehaviorTypes lockingBehaviour)
    {
        using var setup = CreateDbContext();
        CreateProbeTable(setup);
        try
        {
            var probe = new IsolationProbe();
            using (var context = CreateContextWithLocking(setup, lockingBehaviour, probe))
            {
                context.Database.OpenConnection();
                using (var transaction = context.Database.BeginTransaction())
                {
                    AssertReadCommitted(context);
                    AssertBegunAtReadCommitted(transaction);
                    AssertSeesRowsOthersCommit(context);
                    transaction.Commit();
                }

                AssertReadCommitted(context);
            }

            Assert.All(probe.Seen, AssertProbe);
            Assert.Single(probe.Seen);
        }
        finally
        {
            DropProbeTable(setup);
        }
    }

    [ProviderTheory(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    [InlineData(DatabaseLockingBehaviorTypes.NoLock)]
    [InlineData(DatabaseLockingBehaviorTypes.Optimistic)]
    public async Task BeginTransactionAsync_UnderEachLockingBehaviour_RunsAtReadCommitted(DatabaseLockingBehaviorTypes lockingBehaviour)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var setup = CreateDbContext();
        CreateProbeTable(setup);
        try
        {
            var probe = new IsolationProbe();
            await using (var context = CreateContextWithLocking(setup, lockingBehaviour, probe))
            {
                await context.Database.OpenConnectionAsync(cancellationToken);
                await using (var transaction = await context.Database.BeginTransactionAsync(cancellationToken))
                {
                    await AssertReadCommittedAsync(context);
                    AssertBegunAtReadCommitted(transaction);
                    await AssertSeesRowsOthersCommitAsync(context);
                    await transaction.CommitAsync(cancellationToken);
                }

                await AssertReadCommittedAsync(context);
            }

            Assert.All(probe.Seen, AssertProbe);
            Assert.Single(probe.Seen);
        }
        finally
        {
            DropProbeTable(setup);
        }
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void SaveChanges_WithoutAUserTransaction_RunsTheTransactionOfEfCoreAtReadCommitted()
    {
        using var context = CreateDbContext();
        context.ActivityLogs.Add(new ActivityLog("first", "type", Guid.Empty));
        context.ActivityLogs.Add(new ActivityLog("second", "type", Guid.Empty));

        context.SaveChanges();

        // Two inserts need a transaction, which EF Core starts without a level.
        AssertProbedReadCommitted(1);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task SaveChangesAsync_WithoutAUserTransaction_RunsTheTransactionOfEfCoreAtReadCommitted()
    {
        await using var context = CreateDbContext();
        context.ActivityLogs.Add(new ActivityLog("first", "type", Guid.Empty));
        context.ActivityLogs.Add(new ActivityLog("second", "type", Guid.Empty));

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        AssertProbedReadCommitted(1);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task PurgeDatabase_RunsItsTransactionAtReadCommitted()
    {
        await using (var seed = CreateDbContext())
        {
            seed.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        _probe.Clear();
        await using var context = CreateDbContext();
        await DatabaseProvider.PurgeDatabase(context, [context.Model.FindEntityType(typeof(ActivityLog))!.GetTableName()!]);

        AssertProbedReadCommitted(1);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Migrate_EmptyDatabase_RunsEveryTransactionAtReadCommitted()
    {
        using var empty = TestDatabaseEnvironment.Backend.CreateEmpty();
        var probe = new IsolationProbe();
        var options = new DbContextOptionsBuilder<JellyfinDbContext>(empty.Options).AddInterceptors(probe).Options;
        using var context = new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            empty.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        // The connection is opened first so that the migration runs on it and leaves its session to be checked.
        context.Database.OpenConnection();
        context.Database.Migrate();

        Assert.NotEmpty(probe.Seen);
        Assert.All(probe.Seen, AssertProbe);
        AssertReadCommitted(context);
    }

    private static void End(IDbContextTransaction transaction, bool commit)
    {
        if (commit)
        {
            transaction.Commit();
        }
        else
        {
            transaction.Rollback();
        }
    }

    private static Task EndAsync(IDbContextTransaction transaction, bool commit)
        => commit
            ? transaction.CommitAsync(TestContext.Current.CancellationToken)
            : transaction.RollbackAsync(TestContext.Current.CancellationToken);

    private static void AssertBegunAtReadCommitted(IDbContextTransaction transaction)
    {
        // The level MySqlConnector was asked for, which it sets for the session before it starts the transaction.
        Assert.Equal(IsolationLevel.ReadCommitted, transaction.GetDbTransaction().IsolationLevel);
    }

    private static void AssertProbe(IsolationProbe.Observation observation)
    {
        Assert.All(observation.Server, level => Assert.Equal(ReadCommitted, level));
        Assert.Equal(IsolationLevel.ReadCommitted, observation.Begun);
    }

    private static long SessionId(JellyfinDbContext context)
        => context.Database.SqlQueryRaw<long>("SELECT CAST(CONNECTION_ID() AS SIGNED) AS `Value`").AsEnumerable().Single();

    private static string TransactionIsolation(JellyfinDbContext context)
        => context.Database.SqlQueryRaw<string>("SELECT @@transaction_isolation AS `Value`").AsEnumerable().Single();

    private static string TxIsolation(JellyfinDbContext context)
        => context.Database.SqlQueryRaw<string>("SELECT @@tx_isolation AS `Value`").AsEnumerable().Single();

    private static async Task<string> TransactionIsolationAsync(JellyfinDbContext context)
        => (await context.Database.SqlQueryRaw<string>("SELECT @@transaction_isolation AS `Value`").ToListAsync(TestContext.Current.CancellationToken)).Single();

    private static async Task<string> TxIsolationAsync(JellyfinDbContext context)
        => (await context.Database.SqlQueryRaw<string>("SELECT @@tx_isolation AS `Value`").ToListAsync(TestContext.Current.CancellationToken)).Single();

    private static int ReadProbe(JellyfinDbContext context)
        => context.Database.SqlQueryRaw<int>("SELECT `V` AS `Value` FROM `IsolationProbe` WHERE `Id` = 1").AsEnumerable().Single();

    private static async Task<int> ReadProbeAsync(JellyfinDbContext context)
        => (await context.Database.SqlQueryRaw<int>("SELECT `V` AS `Value` FROM `IsolationProbe` WHERE `Id` = 1").ToListAsync(TestContext.Current.CancellationToken)).Single();

    private static void CreateProbeTable(JellyfinDbContext context)
    {
        DropProbeTable(context);
        context.Database.ExecuteSqlRaw("CREATE TABLE `IsolationProbe` (`Id` INT NOT NULL PRIMARY KEY, `V` INT NOT NULL) ENGINE=InnoDB");
        context.Database.ExecuteSqlRaw("INSERT INTO `IsolationProbe` (`Id`, `V`) VALUES (1, 0)");
    }

    private static void DropProbeTable(JellyfinDbContext context)
        => context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS `IsolationProbe`");

    private JellyfinDbContext CreateContextWithLocking(
        JellyfinDbContext context,
        DatabaseLockingBehaviorTypes lockingBehaviour,
        IInterceptor probe)
    {
        var connectionString = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>()).ConnectionString!;
        IEntityFrameworkCoreLockingBehavior locking = lockingBehaviour switch
        {
            DatabaseLockingBehaviorTypes.NoLock => new NoLockBehavior(NullLogger<NoLockBehavior>.Instance),
            DatabaseLockingBehaviorTypes.Optimistic => new OptimisticLockBehavior(NullLogger<OptimisticLockBehavior>.Instance),
            _ => throw new ArgumentOutOfRangeException(nameof(lockingBehaviour))
        };

        // The order of ServiceCollectionExtensions: the provider, then the locking behaviour.
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        new MySqlDatabaseProvider(NullLogger<MySqlDatabaseProvider>.Instance).Initialise(
            builder,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-MySQL",
                LockingBehavior = lockingBehaviour,
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = connectionString,
                    Options = [new CustomDatabaseOption { Key = "server-version", Value = DbProvider == TestDbProvider.MariaDb ? "11.4.7-mariadb" : "8.4-mysql" }]
                }
            });
        locking.Initialise(builder);
        builder.AddInterceptors(probe);
        return new JellyfinDbContext(builder.Options, NullLogger<JellyfinDbContext>.Instance, DatabaseProvider, locking);
    }

    private void AssertReadCommitted(JellyfinDbContext context)
    {
        Assert.Equal(ReadCommitted, TransactionIsolation(context));
        if (DbProvider == TestDbProvider.MariaDb)
        {
            Assert.Equal(ReadCommitted, TxIsolation(context));
        }
    }

    private async Task AssertReadCommittedAsync(JellyfinDbContext context)
    {
        Assert.Equal(ReadCommitted, await TransactionIsolationAsync(context));
        if (DbProvider == TestDbProvider.MariaDb)
        {
            Assert.Equal(ReadCommitted, await TxIsolationAsync(context));
        }
    }

    /// <summary>
    /// Reads a row, lets another connection change it and commit, and reads it again and updates it in the transaction
    /// of the context. At read committed the second read sees the change. At repeatable read it does not, and MariaDB
    /// 11.8 and later then refuse the update with error 1020, as innodb_snapshot_isolation is on by default.
    /// </summary>
    private void AssertSeesRowsOthersCommit(JellyfinDbContext context)
    {
        Assert.Equal(0, ReadProbe(context));
        using (var other = CreateDbContext())
        {
            other.Database.ExecuteSqlRaw("UPDATE `IsolationProbe` SET `V` = 1 WHERE `Id` = 1");
        }

        Assert.Equal(1, ReadProbe(context));
        context.Database.ExecuteSqlRaw("UPDATE `IsolationProbe` SET `V` = `V` + 10 WHERE `Id` = 1");
        Assert.Equal(11, ReadProbe(context));
    }

    private async Task AssertSeesRowsOthersCommitAsync(JellyfinDbContext context)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        Assert.Equal(0, await ReadProbeAsync(context));
        await using (var other = CreateDbContext())
        {
            await other.Database.ExecuteSqlRawAsync("UPDATE `IsolationProbe` SET `V` = 1 WHERE `Id` = 1", cancellationToken);
        }

        Assert.Equal(1, await ReadProbeAsync(context));
        await context.Database.ExecuteSqlRawAsync("UPDATE `IsolationProbe` SET `V` = `V` + 10 WHERE `Id` = 1", cancellationToken);
        Assert.Equal(11, await ReadProbeAsync(context));
    }

    private void AssertProbedReadCommitted(int expectedTransactions)
    {
        var seen = _probe.Seen;
        Assert.All(seen, AssertProbe);
        Assert.Equal(expectedTransactions, seen.Count);
    }

    /// <summary>
    /// Reads the level of each transaction EF Core starts, from the server, right after it started.
    /// </summary>
    private sealed class IsolationProbe : DbTransactionInterceptor
    {
        private readonly ConcurrentQueue<Observation> _seen = new();

        public IReadOnlyList<Observation> Seen => _seen.ToArray();

        public void Clear() => _seen.Clear();

        public override DbTransaction TransactionStarted(DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
        {
            _seen.Enqueue(new Observation(result.IsolationLevel, Read(connection, result)));
            return result;
        }

        public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
        {
            _seen.Enqueue(new Observation(result.IsolationLevel, Read(connection, result)));
            return ValueTask.FromResult(result);
        }

        private static string[] Read(DbConnection connection, DbTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT @@transaction_isolation";
            var levels = new[] { Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture)! };
            if (connection.ServerVersion.Contains("MariaDB", StringComparison.OrdinalIgnoreCase))
            {
                command.CommandText = "SELECT @@tx_isolation";
                levels = [.. levels, Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture)!];
            }

            return levels;
        }

        public sealed record Observation(IsolationLevel Begun, string[] Server);
    }
}
