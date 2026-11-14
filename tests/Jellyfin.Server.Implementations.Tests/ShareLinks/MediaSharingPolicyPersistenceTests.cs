using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ShareLinks;

/// <summary>
/// Persistence tests for the per-user media sharing policy column. The column is additive
/// with a default of <see cref="UserMediaSharingPolicy.Default"/>, so legacy rows read as
/// "not explicitly set".
/// </summary>
public sealed class MediaSharingPolicyPersistenceTests : ShareLinkDbTestFixture
{
    private static readonly Guid _userId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void NewUser_DefaultsToUnsetPolicy()
    {
        var user = new User("alice", "auth-provider", "reset-provider");

        Assert.Equal(UserMediaSharingPolicy.Default, user.MediaSharingPolicy);
    }

    [Fact]
    public async Task Policy_RoundTripsThroughDatabase()
    {
        using (var writeContext = CreateDbContext())
        {
            writeContext.Users.Add(new User("alice", "auth-provider", "reset-provider")
            {
                Id = _userId,
                MediaSharingPolicy = UserMediaSharingPolicy.Enabled
            });
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var readContext = CreateDbContext();
        var stored = await readContext.Users
            .SingleAsync(user => user.Id.Equals(_userId), TestContext.Current.CancellationToken);

        Assert.Equal(UserMediaSharingPolicy.Enabled, stored.MediaSharingPolicy);
    }

    [Fact]
    public async Task RowWithoutExplicitPolicy_ReadsAsDefault()
    {
        using (var writeContext = CreateDbContext())
        {
            writeContext.Users.Add(new User("bob", "auth-provider", "reset-provider") { Id = _userId });
            await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var readContext = CreateDbContext();
        var stored = await readContext.Users
            .SingleAsync(user => user.Id.Equals(_userId), TestContext.Current.CancellationToken);

        Assert.Equal(UserMediaSharingPolicy.Default, stored.MediaSharingPolicy);
    }
}
