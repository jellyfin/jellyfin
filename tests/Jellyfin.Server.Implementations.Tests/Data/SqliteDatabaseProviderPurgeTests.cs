using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public sealed class SqliteDatabaseProviderPurgeTests : DatabaseTestFixture
{
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

        await DatabaseProvider.PurgeDatabase(ctx, [ctx.Model.FindEntityType(typeof(ActivityLog))!.GetTableName()!]).ConfigureAwait(true);
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

        await DatabaseProvider.PurgeDatabase(
            ctx,
            [ctx.Model.FindEntityType(typeof(ActivityLog))!.GetTableName()!, ctx.Model.FindEntityType(typeof(ApiKey))!.GetTableName()!]).ConfigureAwait(true);

        var added = new ApiKey("key");
        ctx.ApiKeys.Add(added);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(1, added.Id);
    }
}
