using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.CustomNetflix.Tests;

public sealed class CustomNetflixUserDeletedConsumerTests
{
    [Fact]
    public async Task UserDeletion_PurgesDatabaseAndRelatedCacheKeys()
    {
        var user = new User("deleted-user", "auth", "reset");
        var profileIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var tokenHash = "TOKEN-HASH";
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(mock => mock.IsEnabled).Returns(true);
        repository
            .Setup(mock => mock.PurgeUserDataAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomNetflixUserDataKeys(profileIds, new[] { tokenHash }));
        IReadOnlyList<string>? removedKeys = null;
        var cache = new Mock<ICustomNetflixCacheService>();
        cache
            .Setup(mock => mock.RemoveAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Callback((IReadOnlyList<string> keys, CancellationToken _) => removedKeys = keys)
            .Returns(Task.CompletedTask);
        var consumer = new CustomNetflixUserDeletedConsumer(
            repository.Object,
            cache.Object,
            NullLogger<CustomNetflixUserDeletedConsumer>.Instance);

        await consumer.OnEvent(new UserDeletedEventArgs(user));

        Assert.NotNull(removedKeys);
        Assert.Contains(RedisKeyBuilder.ActiveProfile(user.Id, tokenHash), removedKeys);
        Assert.Contains(
            RedisKeyBuilder.RankingSnapshot(CustomNetflixRankingSnapshots.TrendingId),
            removedKeys);
        Assert.All(
            profileIds,
            profileId => Assert.Contains(CustomNetflixHomeSnapshots.CacheKeys(profileId)[0], removedKeys));
        Assert.Equal(removedKeys.Count, removedKeys.Distinct().Count());
        repository.VerifyAll();
        cache.VerifyAll();
    }
}
