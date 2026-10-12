using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// A user is saved with its row version as a concurrency token. A save made from a copy of the user that
/// another context has changed in the meantime is rejected instead of overwriting that change, and each
/// successful save raises the row version by one.
/// </summary>
public sealed class UserConcurrencyTests : JellyfinDbTestFixture
{
    [Fact]
    public async Task SaveChanges_StaleUser_ThrowsConcurrencyErrorAndRowVersionRisesOnce()
    {
        var userId = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("user", "auth", "reset") { Id = userId });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        using var first = CreateDbContext();
        using var second = CreateDbContext();
        var firstCopy = await first.Users.SingleAsync(e => e.Id.Equals(userId), TestContext.Current.CancellationToken).ConfigureAwait(true);
        var secondCopy = await second.Users.SingleAsync(e => e.Id.Equals(userId), TestContext.Current.CancellationToken).ConfigureAwait(true);
        var initialRowVersion = firstCopy.RowVersion;
        Assert.Equal(initialRowVersion, secondCopy.RowVersion);

        firstCopy.CastReceiverId = "first";
        await first.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        secondCopy.CastReceiverId = "second";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => second.SaveChangesAsync(TestContext.Current.CancellationToken)).ConfigureAwait(true);

        using var verify = CreateDbContext();
        var stored = await verify.Users.AsNoTracking().SingleAsync(e => e.Id.Equals(userId), TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("first", stored.CastReceiverId);
        Assert.Equal(initialRowVersion + 1, stored.RowVersion);
    }
}
