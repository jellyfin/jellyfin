using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds the sessions of the SQL Server provider to read committed and to the settings the queries rely on, and its
/// startup check to warning about a database that does not read from row versions or has another default collation. The
/// tests run on databases that read from row versions, as the provider recommends, and on databases that read with locks,
/// as SQL Server creates them unless told otherwise.
/// </summary>
/// <remarks>
/// SqlClient hands out a pooled connection with the isolation level its last user left it at. Each test leaves a pooled
/// connection at serializable first, and checks by its session id that EF Core gets that connection again. The level is
/// read from the session of the command itself, inside the same batch or the same transaction.
/// </remarks>
public abstract class SqlServerSessionTestsBase : IDisposable
{
    private const short ReadCommitted = 2;
    private const short Serializable = 4;

    private const string SessionQuery = "SELECT CONVERT(int, @@SPID) AS [Spid], transaction_isolation_level AS [Level] FROM sys.dm_exec_sessions WHERE session_id = @@SPID";

    private readonly LevelProbe _probe = new();
    private readonly TestDatabase _database;
    private readonly bool _readsFromRowVersions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSessionTestsBase"/> class.
    /// </summary>
    /// <param name="backend">The backend of the variant, on a SQL Server run.</param>
    /// <param name="readsFromRowVersions">Whether the databases of the backend read from row versions.</param>
    protected SqlServerSessionTestsBase(ITestDatabaseBackend backend, bool readsFromRowVersions)
    {
        _readsFromRowVersions = readsFromRowVersions;
        _database = TestDatabaseEnvironment.Provider == TestDbProvider.SqlServer
            ? backend.Create(TestSchemaSource.Model, [_probe])
            : null!;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _database?.Dispose();
        }
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void Query_OutsideATransactionOnAPooledConnectionLeftAtSerializable_RunsAtReadCommitted()
    {
        var spid = LeavePooledConnectionAtSerializable();
        using var context = CreateContext();

        var session = context.Database.SqlQueryRaw<SessionState>(SessionQuery).AsEnumerable().Single();

        Assert.Equal(spid, session.Spid);
        Assert.Equal(ReadCommitted, session.Level);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task QueryAsync_OutsideATransactionOnAPooledConnectionLeftAtSerializable_RunsAtReadCommitted()
    {
        var spid = LeavePooledConnectionAtSerializable();
        await using var context = CreateContext();

        var session = (await context.Database.SqlQueryRaw<SessionState>(SessionQuery).ToListAsync(TestContext.Current.CancellationToken)).Single();

        Assert.Equal(spid, session.Spid);
        Assert.Equal(ReadCommitted, session.Level);
    }

    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData(true)]
    [InlineData(false)]
    public void Transaction_WithoutALevelOnAPooledConnectionLeftAtSerializable_RunsAtReadCommittedAndLeavesTheSessionThere(bool commit)
    {
        var spid = LeavePooledConnectionAtSerializable();
        using var context = CreateContext();
        context.Database.OpenConnection();

        using (var transaction = context.Database.BeginTransaction())
        {
            var inside = context.Database.SqlQueryRaw<SessionState>(SessionQuery).AsEnumerable().Single();
            Assert.Equal(spid, inside.Spid);
            Assert.Equal(ReadCommitted, inside.Level);
            Assert.Equal(IsolationLevel.ReadCommitted, transaction.GetDbTransaction().IsolationLevel);
            if (commit)
            {
                transaction.Commit();
            }
            else
            {
                transaction.Rollback();
            }
        }

        var after = context.Database.SqlQueryRaw<SessionState>(SessionQuery).AsEnumerable().Single();
        Assert.Equal(ReadCommitted, after.Level);
        Assert.Equal([ReadCommitted], _probe.Levels);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task SaveChanges_OnAPooledConnectionLeftAtSerializable_RunsItsTransactionAtReadCommitted()
    {
        LeavePooledConnectionAtSerializable();
        await using var context = CreateContext();
        context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
        context.ActivityLogs.Add(new ActivityLog("name", "type", Guid.NewGuid()));

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal([ReadCommitted], _probe.Levels);
    }

    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData(true)]
    [InlineData(false)]
    public void Transaction_WithoutALevelAfterOneThatNamedSerializableOnTheOpenConnection_RunsAtReadCommitted(bool commitFirst)
    {
        // Nothing in the provider chooses the level of a transaction that names none: SqlClient names read committed
        // for it, although the session was left at serializable. A SqlClient that started it at the level of the
        // session would fail this test and the one that saves.
        using var context = CreateContext();
        context.Database.OpenConnection();
        EndATransactionThatNamedSerializable(context, commitFirst);

        using (var transaction = context.Database.BeginTransaction())
        {
            var inside = context.Database.SqlQueryRaw<SessionState>(SessionQuery).AsEnumerable().Single();
            Assert.Equal(ReadCommitted, inside.Level);
            Assert.Equal(IsolationLevel.ReadCommitted, transaction.GetDbTransaction().IsolationLevel);
            transaction.Commit();
        }

        var after = context.Database.SqlQueryRaw<SessionState>(SessionQuery).AsEnumerable().Single();
        Assert.Equal(ReadCommitted, after.Level);
        Assert.Equal([Serializable, ReadCommitted], _probe.Levels);
    }

    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveChanges_AfterATransactionThatNamedSerializableOnTheOpenConnection_RunsItsTransactionAtReadCommitted(bool commitFirst)
    {
        await using var context = CreateContext();
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        EndATransactionThatNamedSerializable(context, commitFirst);
        context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
        context.ActivityLogs.Add(new ActivityLog("name", "type", Guid.NewGuid()));

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal([Serializable, ReadCommitted], _probe.Levels);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void Transaction_ThatNamesALevel_KeepsIt()
    {
        using var context = CreateContext();
        context.Database.OpenConnection();

        using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
        var inside = context.Database.SqlQueryRaw<SessionState>(SessionQuery).AsEnumerable().Single();

        Assert.Equal(Serializable, inside.Level);
        Assert.Equal([Serializable], _probe.Levels);
    }

    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData("QUOTED_IDENTIFIER")]
    [InlineData("ANSI_WARNINGS")]
    [InlineData("ANSI_NULLS")]
    [InlineData("CONCAT_NULL_YIELDS_NULL")]
    public void Session_HasTheSettingsTheQueriesRelyOn(string setting)
    {
        using var context = CreateContext();

        // A filtered index in double quotes needs QUOTED_IDENTIFIER, and a value too long for its column is refused,
        // not cut, only under ANSI_WARNINGS.
        var query = "SELECT CONVERT(int, SESSIONPROPERTY('" + setting + "')) AS [Value]";
        var value = context.Database.SqlQueryRaw<int>(query).AsEnumerable().Single();

        Assert.Equal(1, value);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task ApplyDatabaseSettings_DatabaseOfTheVariant_WarnsOnlyWhenItReadsWithLocks()
    {
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        await using var context = CreateContext(new SqlServerDatabaseProvider(logger));

        await new SqlServerDatabaseProvider(logger).ApplyDatabaseSettingsAsync(context, TestContext.Current.CancellationToken);

        // The outcome differs by design: only a database that reads with locks is warned of, and only of that.
        var warnings = logger.Entries.Where(entry => entry.Level >= LogLevel.Warning).ToList();
        if (_readsFromRowVersions)
        {
            Assert.Empty(warnings);
        }
        else
        {
            Assert.Contains("READ_COMMITTED_SNAPSHOT is off", Assert.Single(warnings).Message, StringComparison.Ordinal);
        }
    }

    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData(nameof(NoLockBehavior))]
    [InlineData(nameof(OptimisticLockBehavior))]
    [InlineData(nameof(PessimisticLockBehavior))]
    public void SaveChanges_WithEachLockingBehaviour_RunsAtReadCommittedAndIsReadBack(string behaviourName)
    {
        // Synchronous, as the pessimistic behaviour holds a lock that belongs to a thread and is unsafe with asynchronous
        // transactions.
        IEntityFrameworkCoreLockingBehavior behaviour = behaviourName switch
        {
            nameof(NoLockBehavior) => new NoLockBehavior(NullLogger<NoLockBehavior>.Instance),
            nameof(OptimisticLockBehavior) => new OptimisticLockBehavior(NullLogger<OptimisticLockBehavior>.Instance),
            _ => new PessimisticLockBehavior(NullLogger<PessimisticLockBehavior>.Instance, NullLoggerFactory.Instance)
        };
        var options = new DbContextOptionsBuilder<JellyfinDbContext>(_database.Options);
        behaviour.Initialise(options);
        var log = new ActivityLog("name", "type", Guid.NewGuid());

        using (var context = new JellyfinDbContext(options.Options, NullLogger<JellyfinDbContext>.Instance, _database.CreateProvider(), behaviour))
        {
            context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Always;
            context.ActivityLogs.Add(log);
            context.SaveChanges();
        }

        using var check = CreateContext();
        Assert.True(check.ActivityLogs.Any(e => e.Id == log.Id));
        Assert.Equal([ReadCommitted], _probe.Levels);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task ApplyDatabaseSettings_ReadCommittedSnapshotOff_WarnsWhatToRun()
    {
        using var database = TestDatabaseEnvironment.Backend.CreateEmpty();
        AlterDatabase(database, "SET READ_COMMITTED_SNAPSHOT OFF WITH ROLLBACK IMMEDIATE");
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        var provider = new SqlServerDatabaseProvider(logger);
        await using var context = new JellyfinDbContext(database.Options, NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        await provider.ApplyDatabaseSettingsAsync(context, TestContext.Current.CancellationToken);

        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("READ_COMMITTED_SNAPSHOT is off", warning.Message, StringComparison.Ordinal);
        Assert.Contains("SET READ_COMMITTED_SNAPSHOT ON", warning.Message, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task ApplyDatabaseSettings_DefaultCollationOfTheServer_WarnsAndNamesIt()
    {
        using var database = TestDatabaseEnvironment.Backend.CreateEmpty();
        AlterDatabase(database, "SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
        AlterDatabase(database, "COLLATE SQL_Latin1_General_CP1_CI_AS");
        AlterDatabase(database, "SET MULTI_USER");
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        var provider = new SqlServerDatabaseProvider(logger);
        await using var context = new JellyfinDbContext(database.Options, NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        await provider.ApplyDatabaseSettingsAsync(context, TestContext.Current.CancellationToken);

        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("SQL_Latin1_General_CP1_CI_AS", warning.Message, StringComparison.Ordinal);
        Assert.Contains(SqlServerDatabaseProvider.Collation, warning.Message, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task ApplyDatabaseSettings_DatabaseCannotBeRead_WarnsAndDoesNotThrow()
    {
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        var provider = new SqlServerDatabaseProvider(logger);
        var builder = new SqlConnectionStringBuilder(ConnectionString(_database)) { InitialCatalog = "jf_" + Guid.NewGuid().ToString("N"), Pooling = false };
        var options = new DbContextOptionsBuilder<JellyfinDbContext>();
        provider.Initialise(options, new Jellyfin.Database.Implementations.DbConfiguration.DatabaseConfigurationOptions
        {
            DatabaseType = "Jellyfin-SqlServer",
            CustomProviderOptions = new Jellyfin.Database.Implementations.DbConfiguration.CustomDatabaseOptions
            {
                PluginName = string.Empty,
                PluginAssembly = string.Empty,
                ConnectionString = builder.ConnectionString
            }
        });
        await using var context = new JellyfinDbContext(options.Options, NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        await provider.ApplyDatabaseSettingsAsync(context, TestContext.Current.CancellationToken);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("Could not read the settings", StringComparison.Ordinal));
    }

    private static string ConnectionString(TestDatabase database) => RelationalOptionsExtension.Extract(database.Options).ConnectionString!;

    private static void AlterDatabase(TestDatabase database, string change)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString(database));
        var name = builder.InitialCatalog;
        SqlConnection.ClearAllPools();
        builder.InitialCatalog = "master";
        builder.Pooling = false;
        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The name is that of a database the backend created, and the changes are constants.
        command.CommandText = $"ALTER DATABASE [{name}] {change}";
#pragma warning restore CA2100
        command.ExecuteNonQuery();
    }

    // Leaves one connection of the pool of the database at serializable, alone in its pool, and returns its session id.
    private int LeavePooledConnectionAtSerializable()
    {
        using (var context = CreateContext())
        {
            var connection = (SqlConnection)context.Database.GetDbConnection();
            SqlConnection.ClearPool(connection);
        }

        using var raw = new SqlConnection(ConnectionStringOfTheContexts());
        raw.Open();
        using var command = raw.CreateCommand();
        command.CommandText = "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; " + SessionQuery;
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(Serializable, reader.GetInt16(1));
        return reader.GetInt32(0);
    }

    // Runs and ends a transaction that names serializable on the open connection of the context, and checks that the
    // session of that connection is left there, as the next transaction of the connection would find it.
    private static void EndATransactionThatNamedSerializable(JellyfinDbContext context, bool commit)
    {
        using (var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable))
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

        var left = context.Database.SqlQueryRaw<SessionState>(SessionQuery).AsEnumerable().Single();
        Assert.Equal(Serializable, left.Level);
    }

    // The provider adds settings to the connection string, and connections are pooled per connection string.
    private string ConnectionStringOfTheContexts()
    {
        using var context = CreateContext();
        return context.Database.GetDbConnection().ConnectionString;
    }

    private JellyfinDbContext CreateContext(IJellyfinDatabaseProvider? provider = null)
        => new(_database.Options, NullLogger<JellyfinDbContext>.Instance, provider ?? _database.CreateProvider(), new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private sealed class SessionState
    {
        public int Spid { get; set; }

        public short Level { get; set; }
    }

    // Reads the level of every transaction EF Core starts, on its own connection and in the transaction, as it starts.
    private sealed class LevelProbe : DbTransactionInterceptor
    {
        public ConcurrentQueue<short> Queue { get; } = new();

        public short[] Levels => [.. Queue];

        public override DbTransaction TransactionStarted(DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
        {
            Queue.Enqueue(Read(connection, result));
            return result;
        }

        public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
        {
            Queue.Enqueue(Read(connection, result));
            return ValueTask.FromResult(result);
        }

        private static short Read(DbConnection connection, DbTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT transaction_isolation_level FROM sys.dm_exec_sessions WHERE session_id = @@SPID";
            return Convert.ToInt16(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
