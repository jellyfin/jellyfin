using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds the SQL Server provider to working on a database whose default collation is the case-insensitive one SQL Server
/// gives a database unless told otherwise: the schema is created, every text column keeps the binary collation, the
/// startup check names the default, and the queries and members that compare text or read the catalog meet no conflict of
/// collations (Msg 468 or 451).
/// </summary>
public sealed class SqlServerCaseInsensitiveDatabaseTests
{
    private const string CaseInsensitive = "SQL_Latin1_General_CP1_CI_AS";

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task Database_WithTheDefaultCollationOfTheServer_WorksAndIsWarnedOf()
    {
        using var database = TestDatabaseEnvironment.Backend.CreateEmpty();
        ChangeCollation(database);
        var logger = new CollectingLogger<SqlServerDatabaseProvider>();
        var provider = new SqlServerDatabaseProvider(logger);
        await using var context = new JellyfinDbContext(database.Options, Microsoft.Extensions.Logging.Abstractions.NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(Microsoft.Extensions.Logging.Abstractions.NullLogger<NoLockBehavior>.Instance));

        Assert.True(await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken));
        await provider.ApplyDatabaseSettingsAsync(context, TestContext.Current.CancellationToken);

        // The startup check names the default and the collation it should be.
        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains(CaseInsensitive, StringComparison.Ordinal));
        Assert.Contains(SqlServerDatabaseProvider.Collation, warning.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("Could not read the settings", StringComparison.Ordinal));

        // Every text column keeps the binary collation; only the default differs.
        var otherCollations = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.is_ms_shipped = 0 AND c.collation_name IS NOT NULL AND c.collation_name <> N'" + SqlServerDatabaseProvider.Collation + "'")
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, otherCollations);
        Assert.Equal(CaseInsensitive, await context.Database.SqlQueryRaw<string>("SELECT CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation')) AS [Value]").SingleAsync(TestContext.Current.CancellationToken));

        // Text that is not a column meets a column without a conflict, and still compares exactly.
        var id = Guid.NewGuid();
        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = "Test", Name = "Abc", CleanName = "Abc", SortName = "Abc" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal([id], await context.BaseItems.Where(e => e.Name == "Abc").Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await context.BaseItems.Where(e => e.Name == "ABC").Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal([id], await context.BaseItems.Where(e => JellyfinDbFunctions.LikeIgnoreAsciiCase(e.CleanName, "ab%")).Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal([id], await context.BaseItems.Where(e => e.SortName!.StartsWith("Ab")).Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));

        // The purge reads the catalog, and the history repository compares its own column with a text that is not one.
        var tables = context.Model.GetEntityTypes().Select(e => e.GetTableName()).OfType<string>().Distinct().ToList();
        await provider.PurgeDatabase(context, tables);
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await context.BaseItems.CountAsync(TestContext.Current.CancellationToken));
    }

    private static void ChangeCollation(TestDatabase database)
    {
        var builder = new SqlConnectionStringBuilder(RelationalOptionsExtension.Extract(database.Options).ConnectionString);
        var name = builder.InitialCatalog;
        SqlConnection.ClearAllPools();
        builder.InitialCatalog = "master";
        builder.Pooling = false;
        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();
        foreach (var change in new[] { "SET SINGLE_USER WITH ROLLBACK IMMEDIATE", "COLLATE " + CaseInsensitive, "SET MULTI_USER" })
        {
            using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The name is that of a database the backend created, and the changes are constants.
            command.CommandText = $"ALTER DATABASE [{name}] {change}";
#pragma warning restore CA2100
            command.ExecuteNonQuery();
        }
    }
}
