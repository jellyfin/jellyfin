using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds the identity columns of SQL Server to what the purge and a restore need. <c>DBCC CHECKIDENT … RESEED, 0</c>
/// makes the next value 1 on a table that has had rows, but 0 on one that never had any, so the purge reseeds only the
/// identities that have handed out a value. IDENTITY_INSERT is allowed for one table of a session at a time, and the scope
/// turns it off again.
/// </summary>
public sealed class SqlServerIdentityTests
{
    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Purge_TableThatNeverHadARow_HandsOutOneFirst(bool inATransaction)
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        await using var context = CreateContext(database);

        await Purge(context, database, inATransaction);
        var id = await AddActivityLog(context);

        Assert.Equal(1, id);
    }

    [ProviderTheory(TestDbProvider.SqlServer)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Purge_TableThatHadRows_HandsOutOneFirst(bool inATransaction)
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        await using var context = CreateContext(database);
        for (var i = 0; i < 3; i++)
        {
            await AddActivityLog(context);
        }

        await Purge(context, database, inATransaction);
        var id = await AddActivityLog(context);

        Assert.Equal(1, id);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task ExplicitIdentityInsert_EndedScope_RefusesExplicitValuesAgain()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        await using var context = CreateContext(database);
        var provider = database.CreateProvider();
        var activityLogs = context.Model.FindEntityType(typeof(ActivityLog))!;

        var scope = await provider.BeginExplicitIdentityInsertAsync(context, activityLogs, TestContext.Current.CancellationToken);
        await using (scope)
        {
            AddWithId(context, new ActivityLog("name", "type", Guid.NewGuid()), 10);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();
        }

        AddWithId(context, new ActivityLog("name", "type", Guid.NewGuid()), 20);
        var refused = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        context.ChangeTracker.Clear();

        Assert.Equal(544, Assert.IsType<SqlException>(refused.InnerException).Number);
        Assert.Equal(11, await AddActivityLog(context));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task ExplicitIdentityInsert_OneTableAfterAnotherInOneTransaction_EachKeepsItsValues()
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        await using var context = CreateContext(database);
        var provider = database.CreateProvider();
        var userId = Guid.NewGuid();

        await using var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var scope = await provider.BeginExplicitIdentityInsertAsync(context, context.Model.FindEntityType(typeof(ActivityLog))!, TestContext.Current.CancellationToken);
        await using (scope)
        {
            AddWithId(context, new ActivityLog("name", "type", userId), 7);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();
        }

        // A second table in the same session is allowed only once the first is off again (Msg 8107 otherwise).
        scope = await provider.BeginExplicitIdentityInsertAsync(context, context.Model.FindEntityType(typeof(ImageInfo))!, TestContext.Current.CancellationToken);
        await using (scope)
        {
            var image = new ImageInfo("/images/a.jpg");
            context.ImageInfos.Add(image);
            context.Entry(image).Property(e => e.Id).CurrentValue = 42;
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            context.ChangeTracker.Clear();
        }

        await transaction.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Equal([7], await context.ActivityLogs.Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal([42], await context.ImageInfos.Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(8, await AddActivityLog(context));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task Purge_IdentifierContainingSqlSyntax_IsOneQuotedIdentifier()
    {
        const string table = "we]ird; DROP TABLE [ActivityLogs] --";
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        await using var context = CreateContext(database);

        await context.Database.ExecuteSqlRawAsync("CREATE TABLE [we]]ird; DROP TABLE [ActivityLogs]] --] ([Id] int IDENTITY(1, 1) PRIMARY KEY)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO [we]]ird; DROP TABLE [ActivityLogs]] --] DEFAULT VALUES");
        var activityLogId = await AddActivityLog(context);

        await database.CreateProvider().PurgeDatabase(context, [table]);

        Assert.Equal(0, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [we]]ird; DROP TABLE [ActivityLogs]] --]").SingleAsync());
        await context.Database.ExecuteSqlRawAsync("INSERT INTO [we]]ird; DROP TABLE [ActivityLogs]] --] DEFAULT VALUES");
        Assert.Equal(1, await context.Database.SqlQueryRaw<int>("SELECT [Id] AS [Value] FROM [we]]ird; DROP TABLE [ActivityLogs]] --]").SingleAsync());
        Assert.Equal([activityLogId], await context.ActivityLogs.Select(e => e.Id).ToListAsync());
    }

    // The id of an activity log has no setter, so the explicit value is given through its entry.
    private static void AddWithId(JellyfinDbContext context, ActivityLog log, int id)
    {
        context.ActivityLogs.Add(log);
        context.Entry(log).Property(e => e.Id).CurrentValue = id;
    }

    private static JellyfinDbContext CreateContext(TestDatabase database)
        => new(database.Options, NullLogger<JellyfinDbContext>.Instance, database.CreateProvider(), new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private static async Task Purge(JellyfinDbContext context, TestDatabase database, bool inATransaction)
    {
        var tables = context.Model.GetEntityTypes().Select(e => e.GetTableName()).OfType<string>().Distinct().ToList();
        var provider = database.CreateProvider();
        if (!inATransaction)
        {
            await provider.PurgeDatabase(context, tables);
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await provider.PurgeDatabase(context, tables);
        await transaction.CommitAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<int> AddActivityLog(JellyfinDbContext context)
    {
        var log = new ActivityLog("name", "type", Guid.NewGuid());
        context.ActivityLogs.Add(log);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();
        return log.Id;
    }
}
