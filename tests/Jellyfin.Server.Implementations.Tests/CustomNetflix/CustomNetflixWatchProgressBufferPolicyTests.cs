using System;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixWatchProgressBufferPolicyTests
{
    [Fact]
    public void GetKey_UsesProfileAndItem()
    {
        var profileId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var row = CreateProgress(profileId, itemId, 60, DateTime.UtcNow);

        var key = CustomNetflixWatchProgressBufferPolicy.GetKey(row);

        Assert.Equal(profileId, key.ProfileId);
        Assert.Equal(itemId, key.ItemId);
    }

    [Fact]
    public void Coalesce_UsesNewerProgressEvenWhenPositionMovesBackward()
    {
        var now = DateTime.UtcNow;
        var current = CreateProgress(Guid.NewGuid(), Guid.NewGuid(), 120, now);
        var incoming = current with
        {
            PositionSeconds = 45,
            PercentViewed = 4.5,
            LastPlayedAt = now.AddSeconds(1)
        };

        var result = CustomNetflixWatchProgressBufferPolicy.Coalesce(current, incoming);

        Assert.Equal(45, result.PositionSeconds);
        Assert.Equal(incoming.LastPlayedAt, result.LastPlayedAt);
    }

    [Fact]
    public void Coalesce_IgnoresOlderProgress()
    {
        var now = DateTime.UtcNow;
        var current = CreateProgress(Guid.NewGuid(), Guid.NewGuid(), 120, now);
        var incoming = current with
        {
            PositionSeconds = 180,
            PercentViewed = 18,
            LastPlayedAt = now.AddSeconds(-1)
        };

        var result = CustomNetflixWatchProgressBufferPolicy.Coalesce(current, incoming);

        Assert.Equal(120, result.PositionSeconds);
        Assert.Equal(current.LastPlayedAt, result.LastPlayedAt);
    }

    private static WatchProgressRow CreateProgress(Guid profileId, Guid itemId, double positionSeconds, DateTime lastPlayedAt)
        => new(
            profileId,
            itemId,
            null,
            positionSeconds,
            1000,
            positionSeconds / 10,
            false,
            0,
            lastPlayedAt);
}
