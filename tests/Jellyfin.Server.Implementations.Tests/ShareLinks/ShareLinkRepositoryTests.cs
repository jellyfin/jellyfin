using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.ShareLinks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ShareLinks;

/// <summary>
/// TC-300 persistence tests for the share link repository.
/// </summary>
public sealed class ShareLinkRepositoryTests : ShareLinkDbTestFixture
{
    private static readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _userId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _itemId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid _itemId2 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly ShareLinkRepository _repository;

    public ShareLinkRepositoryTests()
    {
        _repository = new ShareLinkRepository(CreateDbContextFactory());
        using var context = CreateDbContext();
        context.Users.Add(new User("alice", "auth-provider", "reset-provider") { Id = _userId });
        context.Users.Add(new User("bob", "auth-provider", "reset-provider") { Id = _userId2 });
        context.SaveChanges();
    }

    [Fact]
    public async Task CreateAndGetById_RoundTrip()
    {
        const string jti = "0123456789abcdef0123456789abcdef";
        var link = new ShareLink(jti, _userId, _itemId, 1_700_000_000)
        {
            ExpiresAt = 1_700_360_000,
            DurationSeconds = null
        };

        await _repository.CreateAsync(link, TestContext.Current.CancellationToken);

        var stored = await _repository.GetByIdAsync(jti, TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.Equal(jti, stored.Id);
        Assert.Equal(_userId, stored.UserId);
        Assert.Equal(_itemId, stored.ItemId);
        Assert.Equal(1_700_000_000, stored.CreatedAt);
        Assert.Equal(1_700_360_000, stored.ExpiresAt);
        Assert.Null(stored.FirstAccessedAt);
        Assert.False(stored.IsRevoked);

        Assert.Null(await _repository.GetByIdAsync("ffffffffffffffffffffffffffffffff", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TrySetFirstAccess_SetsExactlyOnce()
    {
        const string jti = "0123456789abcdef0123456789abcdef";
        const long firstAccess = 1_700_000_100;
        await _repository.CreateAsync(new ShareLink(jti, _userId, _itemId, 1_700_000_000), TestContext.Current.CancellationToken);

        Assert.True(await _repository.TrySetFirstAccessAsync(jti, firstAccess, TestContext.Current.CancellationToken));
        Assert.False(await _repository.TrySetFirstAccessAsync(jti, firstAccess + 100, TestContext.Current.CancellationToken));
        Assert.False(await _repository.TrySetFirstAccessAsync("ffffffffffffffffffffffffffffffff", firstAccess, TestContext.Current.CancellationToken));

        var stored = await _repository.GetByIdAsync(jti, TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.Equal(firstAccess, stored.FirstAccessedAt);
    }

    [Fact]
    public async Task TrySetFirstAccess_IsAtomic_ExactlyOneParallelWinner()
    {
        const string jti = "0123456789abcdef0123456789abcdef";
        const long firstAccess = 1_700_000_100;
        const int parallelCalls = 16;
        await _repository.CreateAsync(new ShareLink(jti, _userId, _itemId, 1_700_000_000), TestContext.Current.CancellationToken);

        var results = await Task.WhenAll(Enumerable.Range(0, parallelCalls)
            .Select(_ => Task.Run(() => _repository.TrySetFirstAccessAsync(jti, firstAccess, TestContext.Current.CancellationToken))));

        Assert.Equal(1, results.Count(result => result));

        using var context = CreateDbContext();
        Assert.Equal(1, await context.ShareLinks.CountAsync(e => e.Id == jti && e.FirstAccessedAt != null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Revoke_PersistsAndIsIdempotent()
    {
        const string jti = "0123456789abcdef0123456789abcdef";
        await _repository.CreateAsync(new ShareLink(jti, _userId, _itemId, 1_700_000_000), TestContext.Current.CancellationToken);

        Assert.True(await _repository.RevokeAsync(jti, TestContext.Current.CancellationToken));
        Assert.False(await _repository.RevokeAsync(jti, TestContext.Current.CancellationToken));
        Assert.False(await _repository.RevokeAsync("ffffffffffffffffffffffffffffffff", TestContext.Current.CancellationToken));

        var stored = await _repository.GetByIdAsync(jti, TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.True(stored.IsRevoked);
    }

    [Fact]
    public async Task ListByUser_ReturnsOnlyThatUsersLinksOrderedByCreationDesc()
    {
        const string jtiA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string jtiB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        const string jtiC = "cccccccccccccccccccccccccccccccc";
        await _repository.CreateAsync(new ShareLink(jtiA, _userId, _itemId, 100), TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(jtiB, _userId, _itemId2, 300), TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(jtiC, _userId2, _itemId, 200), TestContext.Current.CancellationToken);

        var aliceLinks = await _repository.ListByUserAsync(_userId, TestContext.Current.CancellationToken);
        Assert.Equal(2, aliceLinks.Count);
        Assert.Equal(new[] { jtiB, jtiA }, aliceLinks.Select(l => l.Id).ToArray());

        var bobLinks = await _repository.ListByUserAsync(_userId2, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { jtiC }, bobLinks.Select(l => l.Id).ToArray());
    }

    [Fact]
    public async Task ListByItem_FiltersByUserAndItem()
    {
        const string jtiA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string jtiB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        await _repository.CreateAsync(new ShareLink(jtiA, _userId, _itemId, 100), TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(jtiB, _userId, _itemId2, 200), TestContext.Current.CancellationToken);

        var links = await _repository.ListByItemAsync(_userId, _itemId, TestContext.Current.CancellationToken);
        var link = Assert.Single(links);
        Assert.Equal(jtiA, link.Id);
    }

    [Fact]
    public async Task DeleteUserLinks_RemovesOnlyThatUsersLinks()
    {
        const string jtiA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string jtiB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        await _repository.CreateAsync(new ShareLink(jtiA, _userId, _itemId, 100), TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(jtiB, _userId2, _itemId, 200), TestContext.Current.CancellationToken);

        await _repository.DeleteUserLinksAsync(_userId, TestContext.Current.CancellationToken);

        Assert.Null(await _repository.GetByIdAsync(jtiA, TestContext.Current.CancellationToken));
        Assert.NotNull(await _repository.GetByIdAsync(jtiB, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeletingUser_CascadesToShareLinks()
    {
        const string jtiA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string jtiB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        const string jtiC = "cccccccccccccccccccccccccccccccc";
        await _repository.CreateAsync(new ShareLink(jtiA, _userId, _itemId, 100), TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(jtiB, _userId, _itemId2, 200), TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(jtiC, _userId2, _itemId, 300), TestContext.Current.CancellationToken);

        using var context = CreateDbContext();
        await context.Users.Where(u => u.Id.Equals(_userId)).ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        Assert.Null(await _repository.GetByIdAsync(jtiA, TestContext.Current.CancellationToken));
        Assert.Null(await _repository.GetByIdAsync(jtiB, TestContext.Current.CancellationToken));
        Assert.NotNull(await _repository.GetByIdAsync(jtiC, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PurgeExpired_RemovesExpiredRevokedAndElapsedDuration_KeepsValid()
    {
        const string expired = "11111111111111111111111111111111";
        const string revoked = "22222222222222222222222222222222";
        const string durationElapsed = "33333333333333333333333333333333";
        const string validAbsolute = "44444444444444444444444444444444";
        const string validDurationUnused = "55555555555555555555555555555555";
        const string validPermanent = "66666666666666666666666666666666";
        long now = 10_000;

        await _repository.CreateAsync(new ShareLink(expired, _userId, _itemId, 1) { ExpiresAt = now - 1 }, TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(revoked, _userId, _itemId, 2) { IsRevoked = true }, TestContext.Current.CancellationToken);
        await _repository.CreateAsync(
            new ShareLink(durationElapsed, _userId, _itemId, 3)
            {
                DurationSeconds = 100,
                FirstAccessedAt = now - 200
            },
            TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(validAbsolute, _userId, _itemId, 4) { ExpiresAt = now + 100 }, TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(validDurationUnused, _userId, _itemId, 5) { DurationSeconds = 100 }, TestContext.Current.CancellationToken);
        await _repository.CreateAsync(new ShareLink(validPermanent, _userId, _itemId, 6), TestContext.Current.CancellationToken);

        var removed = await _repository.PurgeExpiredAsync(now, TestContext.Current.CancellationToken);
        Assert.Equal(3, removed);

        Assert.Null(await _repository.GetByIdAsync(expired, TestContext.Current.CancellationToken));
        Assert.Null(await _repository.GetByIdAsync(revoked, TestContext.Current.CancellationToken));
        Assert.Null(await _repository.GetByIdAsync(durationElapsed, TestContext.Current.CancellationToken));
        Assert.NotNull(await _repository.GetByIdAsync(validAbsolute, TestContext.Current.CancellationToken));
        Assert.NotNull(await _repository.GetByIdAsync(validDurationUnused, TestContext.Current.CancellationToken));
        Assert.NotNull(await _repository.GetByIdAsync(validPermanent, TestContext.Current.CancellationToken));

        // Idempotent: a second run removes nothing further.
        Assert.Equal(0, await _repository.PurgeExpiredAsync(now, TestContext.Current.CancellationToken));
    }
}
