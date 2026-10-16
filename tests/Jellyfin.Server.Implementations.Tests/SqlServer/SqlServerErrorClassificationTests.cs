using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Database.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds the classifiers of the SQL Server provider to the errors the server really raises: each refusal is provoked on
/// a database of the backend, and the exception it gives is classified as a write conflict, a data error, both or
/// neither. Every wait is bounded by a lock or command time-out.
/// </summary>
public sealed class SqlServerErrorClassificationTests
{
    private readonly SqlServerDatabaseProvider _provider = new(NullLogger<SqlServerDatabaseProvider>.Instance);

    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData("INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (1, 'b', N'x', 1)", 2627)]
    [InlineData("INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (2, 'a', N'x', 1)", 2601)]
    [InlineData("INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (2, 'b', N'abcdef', 1)", 2628)]
    [InlineData("INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (2, 'b', NULL, 1)", 515)]
    [InlineData("INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (2, 'b', N'x', -1)", 547)]
    [InlineData("INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (CONVERT(int, 2147483647) + 1, 'b', N'x', 1)", 8115)]
    [InlineData("INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES ('abc', 'b', N'x', 1)", 245)]
    [InlineData("INSERT INTO [Probe] ([Id], [Code], [Name], [Ratio]) VALUES (2, 'b', N'x', N'abc')", 8114)]
    [InlineData("INSERT INTO [Thrown] ([Id]) VALUES (1)", 50001)]
    public void Refusal_OfTheValues_IsADataError(string statement, int number)
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        CreateProbeTables(database);

        var exception = Assert.Throws<SqlException>(() => Execute(database, statement));

        Assert.Contains(number, Numbers(exception));
        Assert.True(_provider.IsDataError(new DbUpdateException("save", exception)), Describe(exception));
        Assert.Equal(number is 2601 or 2627, _provider.IsRetryableWriteConflict(exception));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void Refusal_OfATruncationWithoutVerboseWarnings_IsADataError()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        CreateProbeTables(database);
        Execute(database, "ALTER DATABASE SCOPED CONFIGURATION SET VERBOSE_TRUNCATION_WARNINGS = OFF");

        var exception = Assert.Throws<SqlException>(() => Execute(database, "INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (2, 'b', N'abcdef', 1)"));

        Assert.Contains(8152, Numbers(exception));
        Assert.True(_provider.IsDataError(exception), Describe(exception));
        Assert.False(_provider.IsRetryableWriteConflict(exception));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void Refusal_OfAKeyOverTheLimitOfItsIndex_IsADataError()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        using var context = CreateContext(database);
        context.BaseItems.Add(new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", Path = new string('p', 851) });

        var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.Contains(1946, Numbers(Assert.IsType<SqlException>(exception.InnerException)));
        Assert.True(_provider.IsDataError(exception));
        Assert.False(_provider.IsRetryableWriteConflict(exception));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void Refusal_OfAMissingParent_IsADataError()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        using var context = CreateContext(database);
        context.BaseItems.Add(new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", ParentId = Guid.NewGuid() });

        var exception = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

        Assert.Contains(547, Numbers(Assert.IsType<SqlException>(exception.InnerException)));
        Assert.True(_provider.IsDataError(exception));
        Assert.False(_provider.IsRetryableWriteConflict(exception));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void LockRequestTimeOut_IsAWriteConflictAndNotADataError()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        CreateProbeTables(database);
        using var holder = Open(database);
        using var hold = holder.BeginTransaction();
        Run(holder, hold, "UPDATE [Probe] SET [Amount] = 2 WHERE [Id] = 1");

        var exception = Assert.Throws<SqlException>(() => Execute(database, "SET LOCK_TIMEOUT 0; UPDATE [Probe] SET [Amount] = 3 WHERE [Id] = 1"));

        Assert.Contains(1222, Numbers(exception));
        Assert.True(_provider.IsRetryableWriteConflict(exception));
        Assert.False(_provider.IsDataError(exception));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task Deadlock_IsAWriteConflictAndNotADataError()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        CreateProbeTables(database);
        Execute(database, "INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (2, 'b', N'y', 1)");
        using var first = Open(database);
        using var second = Open(database);
        await using var firstTransaction = (SqlTransaction)await first.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using var secondTransaction = (SqlTransaction)await second.BeginTransactionAsync(TestContext.Current.CancellationToken);
        Run(first, firstTransaction, "UPDATE [Probe] SET [Amount] = 5 WHERE [Id] = 1");
        Run(second, secondTransaction, "UPDATE [Probe] SET [Amount] = 5 WHERE [Id] = 2");

        // Each now waits for the row the other holds; the server chooses one of them as the victim within seconds.
        var waits = new[]
        {
            Task.Run(() => Run(first, firstTransaction, "UPDATE [Probe] SET [Amount] = 6 WHERE [Id] = 2"), TestContext.Current.CancellationToken),
            Task.Run(() => Run(second, secondTransaction, "UPDATE [Probe] SET [Amount] = 6 WHERE [Id] = 1"), TestContext.Current.CancellationToken)
        };
        var all = Task.WhenAll(waits);
        await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken));

        var exception = Assert.Single(waits.Where(t => t.IsFaulted).Select(t => t.Exception!.GetBaseException()));
        var sqlException = Assert.IsType<SqlException>(exception);
        Assert.Contains(1205, Numbers(sqlException));
        Assert.True(_provider.IsRetryableWriteConflict(sqlException));
        Assert.False(_provider.IsDataError(sqlException));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void SnapshotUpdateConflict_IsAWriteConflictAndNotADataError()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        CreateProbeTables(database);
        Execute(database, $"ALTER DATABASE [{Name(database)}] SET ALLOW_SNAPSHOT_ISOLATION ON");
        using var first = Open(database);
        using var second = Open(database);
        using var firstTransaction = first.BeginTransaction(System.Data.IsolationLevel.Snapshot);
        using var secondTransaction = second.BeginTransaction(System.Data.IsolationLevel.Snapshot);
        Run(first, firstTransaction, "SELECT [Amount] FROM [Probe] WHERE [Id] = 1");
        Run(second, secondTransaction, "SELECT [Amount] FROM [Probe] WHERE [Id] = 1");
        Run(first, firstTransaction, "UPDATE [Probe] SET [Amount] = 7 WHERE [Id] = 1");
        firstTransaction.Commit();

        var exception = Assert.Throws<SqlException>(() => Run(second, secondTransaction, "UPDATE [Probe] SET [Amount] = 8 WHERE [Id] = 1"));

        Assert.Contains(3960, Numbers(exception));
        Assert.True(_provider.IsRetryableWriteConflict(exception));
        Assert.False(_provider.IsDataError(exception));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void CommandTimeOut_IsNeitherAConflictNorADataError()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, []);
        using var connection = Open(database);
        using var command = connection.CreateCommand();
        command.CommandText = "WAITFOR DELAY '00:00:05'";
        command.CommandTimeout = 1;

        var exception = Assert.Throws<SqlException>(() => command.ExecuteNonQuery());

        Assert.Contains(-2, Numbers(exception));
        Assert.False(_provider.IsRetryableWriteConflict(exception));
        Assert.False(_provider.IsDataError(exception));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void BrokenConnection_IsNeitherAConflictNorADataError()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, []);
        using var victim = Open(database);
        int session;
        using (var command = victim.CreateCommand())
        {
            command.CommandText = "SELECT CONVERT(int, @@SPID)";
            session = (int)command.ExecuteScalar()!;
        }

        Execute(database, $"KILL {session}");

        var exception = Assert.ThrowsAny<SqlException>(() => Run(victim, null, "SELECT 1"));
        Assert.False(_provider.IsRetryableWriteConflict(exception), Describe(exception));
        Assert.False(_provider.IsDataError(exception), Describe(exception));
    }

    private static int[] Numbers(SqlException exception) => [.. exception.Errors.Cast<SqlError>().Select(error => error.Number)];

    private static string Describe(SqlException exception) => string.Join(", ", exception.Errors.Cast<SqlError>().Select(error => $"{error.Number}: {error.Message}"));

    private static string Name(TestDatabase database) => new SqlConnectionStringBuilder(RelationalOptionsExtension.Extract(database.Options).ConnectionString).InitialCatalog;

    private static JellyfinDbContext CreateContext(TestDatabase database)
        => new(database.Options, NullLogger<JellyfinDbContext>.Instance, database.CreateProvider(), new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    // A connection of its own, outside the pool, so that a lock it holds or a session that is killed touches nothing else.
    private static SqlConnection Open(TestDatabase database)
    {
        // Without the retries of an idle connection, which would open a new session in place of one that was killed.
        var builder = new SqlConnectionStringBuilder(RelationalOptionsExtension.Extract(database.Options).ConnectionString) { Pooling = false, CommandTimeout = 30, ConnectRetryCount = 0 };
        var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();
        return connection;
    }

    private static void Execute(TestDatabase database, string statement)
    {
        using var connection = Open(database);
        Run(connection, null, statement);
    }

    private static void Run(SqlConnection connection, SqlTransaction? transaction, string statement)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
#pragma warning disable CA2100 // The statements are constants of the test.
        command.CommandText = statement;
#pragma warning restore CA2100
        command.ExecuteNonQuery();
    }

    // A table whose columns refuse each kind of value, with one row, and a table whose trigger raises an error of its own.
    private static void CreateProbeTables(TestDatabase database)
    {
        Execute(database, "CREATE TABLE [Probe] ([Id] int NOT NULL PRIMARY KEY, [Code] varchar(4) NOT NULL, [Name] nvarchar(4) NOT NULL, [Amount] int NULL CHECK ([Amount] >= 0), [Ratio] float NULL)");
        Execute(database, "CREATE UNIQUE INDEX [IX_Probe_Code] ON [Probe] ([Code])");
        Execute(database, "INSERT INTO [Probe] ([Id], [Code], [Name], [Amount]) VALUES (1, 'a', N'x', 1)");
        Execute(database, "CREATE TABLE [Thrown] ([Id] int NOT NULL)");
        Execute(database, "CREATE TRIGGER [Thrown_Insert] ON [Thrown] AFTER INSERT AS THROW 50001, N'Refused by a trigger', 1");
    }
}
