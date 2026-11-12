using System;
using System.Threading.Tasks;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.ShareLinks;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ShareLinks;

/// <summary>
/// TC-700 purge task tests: the scheduled task removes expired/revoked rows, keeps valid rows and is
/// idempotent across daily runs.
/// </summary>
public sealed class ShareLinkExpiryTaskTests : ShareLinkDbTestFixture
{
    private static readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _itemId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public ShareLinkExpiryTaskTests()
    {
        using var context = CreateDbContext();
        context.Users.Add(new User("alice", "auth-provider", "reset-provider") { Id = _userId });
        context.SaveChanges();
    }

    [Fact]
    public async Task ExecuteAsync_RemovesExpiredAndRevoked_KeepsValid()
    {
        var repository = new ShareLinkRepository(CreateDbContextFactory());
        var task = new ShareLinkExpiryTask(repository);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const string expired = "11111111111111111111111111111111";
        const string revoked = "22222222222222222222222222222222";
        const string elapsedDuration = "33333333333333333333333333333333";
        const string validAbsolute = "44444444444444444444444444444444";
        const string validDuration = "55555555555555555555555555555555";

        await repository.CreateAsync(new ShareLink(expired, _userId, _itemId, now - 100) { ExpiresAt = now - 1 }, TestContext.Current.CancellationToken);
        await repository.CreateAsync(new ShareLink(revoked, _userId, _itemId, now - 100) { IsRevoked = true }, TestContext.Current.CancellationToken);
        await repository.CreateAsync(
            new ShareLink(elapsedDuration, _userId, _itemId, now - 100)
            {
                DurationSeconds = 60,
                FirstAccessedAt = now - 200
            },
            TestContext.Current.CancellationToken);
        await repository.CreateAsync(new ShareLink(validAbsolute, _userId, _itemId, now - 100) { ExpiresAt = now + 3600 }, TestContext.Current.CancellationToken);
        await repository.CreateAsync(new ShareLink(validDuration, _userId, _itemId, now - 100) { DurationSeconds = 3600 }, TestContext.Current.CancellationToken);

        await task.ExecuteAsync(new System.Progress<double>(), TestContext.Current.CancellationToken);

        Assert.Null(await repository.GetByIdAsync(expired, TestContext.Current.CancellationToken));
        Assert.Null(await repository.GetByIdAsync(revoked, TestContext.Current.CancellationToken));
        Assert.Null(await repository.GetByIdAsync(elapsedDuration, TestContext.Current.CancellationToken));
        Assert.NotNull(await repository.GetByIdAsync(validAbsolute, TestContext.Current.CancellationToken));
        Assert.NotNull(await repository.GetByIdAsync(validDuration, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExecuteAsync_IsIdempotent()
    {
        var repository = new ShareLinkRepository(CreateDbContextFactory());
        var task = new ShareLinkExpiryTask(repository);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const string expired = "11111111111111111111111111111111";
        const string valid = "44444444444444444444444444444444";

        await repository.CreateAsync(new ShareLink(expired, _userId, _itemId, now - 100) { ExpiresAt = now - 1 }, TestContext.Current.CancellationToken);
        await repository.CreateAsync(new ShareLink(valid, _userId, _itemId, now - 100) { ExpiresAt = now + 3600 }, TestContext.Current.CancellationToken);

        await task.ExecuteAsync(new System.Progress<double>(), TestContext.Current.CancellationToken);
        await task.ExecuteAsync(new System.Progress<double>(), TestContext.Current.CancellationToken);

        Assert.Null(await repository.GetByIdAsync(expired, TestContext.Current.CancellationToken));
        Assert.NotNull(await repository.GetByIdAsync(valid, TestContext.Current.CancellationToken));
    }
}
