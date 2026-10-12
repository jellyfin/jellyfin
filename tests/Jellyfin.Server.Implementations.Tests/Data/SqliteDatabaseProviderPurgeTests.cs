using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public sealed class SqliteDatabaseProviderPurgeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly SqliteDatabaseProvider _provider = new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance);

    public SqliteDatabaseProviderPurgeTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection, sqliteOptions => sqliteOptions.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly))
            .Options;

        using var ctx = CreateDbContext();
        ctx.Database.Migrate();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task PurgeDatabase_ForTableThatHeldRows_RestartsIdentityAtOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var ctx = CreateDbContext();
        for (var i = 0; i < 3; i++)
        {
            ctx.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
        }

        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

        await _provider.PurgeDatabase(ctx, [ctx.Model.FindEntityType(typeof(ActivityLog))!.GetTableName()!]).ConfigureAwait(true);
        ctx.ChangeTracker.Clear();

        var added = new ActivityLog("entry", "type", Guid.Empty);
        ctx.ActivityLogs.Add(added);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(1, added.Id);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task PurgeDatabase_ForTableThatNeverHeldRows_StartsIdentityAtOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var ctx = CreateDbContext();
        ctx.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

        await _provider.PurgeDatabase(
            ctx,
            [ctx.Model.FindEntityType(typeof(ActivityLog))!.GetTableName()!, ctx.Model.FindEntityType(typeof(ApiKey))!.GetTableName()!]).ConfigureAwait(true);

        var added = new ApiKey("key");
        ctx.ApiKeys.Add(added);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(1, added.Id);
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            _provider,
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
