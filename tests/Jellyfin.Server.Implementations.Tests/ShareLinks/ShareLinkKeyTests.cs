using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ShareLinks;

/// <summary>
/// Tests for the per-user share link key: generation, backfill and rotation on password change.
/// </summary>
public sealed class ShareLinkKeyTests : ShareLinkDbTestFixture
{
    private static readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _userId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void NewUser_GetsUniqueDecodableKey()
    {
        var alice = new User("alice", "auth-provider", "reset-provider") { Id = _userId };
        var bob = new User("bob", "auth-provider", "reset-provider") { Id = _userId2 };

        Assert.NotEqual(alice.ShareLinkKey, bob.ShareLinkKey);
        Assert.False(ShareLinkKeyHelper.IsNullOrBlank(alice.ShareLinkKey));
        Assert.True(ShareLinkKeyHelper.TryDecode(alice.ShareLinkKey, out var aliceKey));
        Assert.Equal(ShareLinkKeyHelper.KeyLengthBytes, aliceKey!.Length);
    }

    [Fact]
    public async Task LegacyBlankKey_IsDetectedAndCanBePersisted()
    {
        using var context = CreateDbContext();
        context.Users.Add(new User("alice", "auth-provider", "reset-provider") { Id = _userId });
        context.Users.Add(new User("bob", "auth-provider", "reset-provider") { Id = _userId2 });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Simulate rows that predate the column: SQLite has no base64() function, so the
        // AddShareLinks migration carries no SQL backfill and legacy rows stay blank.
        await context.Users
            .Where(u => u.Id.Equals(_userId) || u.Id.Equals(_userId2))
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.ShareLinkKey, string.Empty), TestContext.Current.CancellationToken);

        var blankKeys = await context.Users
            .Where(u => u.Id.Equals(_userId) || u.Id.Equals(_userId2))
            .Select(u => u.ShareLinkKey)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, blankKeys.Count);
        Assert.All(blankKeys, key =>
        {
            Assert.True(ShareLinkKeyHelper.IsNullOrBlank(key), "Legacy row must be reported as blank");
            Assert.False(ShareLinkKeyHelper.TryDecode(key, out _), "A blank key must not decode");
        });

        // The sanity guard: generate and persist, exactly what the token service does before minting.
        var legacyUser = await context.Users.SingleAsync(u => u.Id.Equals(_userId), TestContext.Current.CancellationToken);
        legacyUser.ShareLinkKey = ShareLinkKeyHelper.Generate();
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var persisted = await context.Users
            .Where(u => u.Id.Equals(_userId))
            .Select(u => u.ShareLinkKey)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.False(ShareLinkKeyHelper.IsNullOrBlank(persisted));
        Assert.True(ShareLinkKeyHelper.TryDecode(persisted, out var bytes));
        Assert.Equal(ShareLinkKeyHelper.KeyLengthBytes, bytes!.Length);
    }

    [Fact]
    public async Task ChangePassword_RotatesShareLinkKey()
    {
        var user = new User("alice", "auth-provider", "reset-provider") { Id = _userId };
        using (var context = CreateDbContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var originalKey = user.ShareLinkKey;
        var provider = new DefaultAuthenticationProvider(
            NullLogger<DefaultAuthenticationProvider>.Instance,
            new Mock<MediaBrowser.Model.Cryptography.ICryptoProvider>().Object);

        await provider.ChangePassword(user, string.Empty);

        Assert.NotEqual(originalKey, user.ShareLinkKey);
        Assert.True(ShareLinkKeyHelper.TryDecode(user.ShareLinkKey, out var rotated));
        Assert.Equal(ShareLinkKeyHelper.KeyLengthBytes, rotated!.Length);
    }
}
