using System;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixWatchEventBufferPolicyTests
{
    [Fact]
    public void GetKey_UsesProfileItemEventTypeAndSession()
    {
        var profileId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var row = CreateEvent(profileId, itemId, "progress", 60, "session");

        var key = CustomNetflixWatchEventBufferPolicy.GetKey(row);

        Assert.Equal(profileId, key.ProfileId);
        Assert.Equal(itemId, key.ItemId);
        Assert.Equal("progress", key.EventType);
        Assert.Equal("session", key.PlaySessionId);
    }

    [Fact]
    public void Coalesce_KeepsFurthestProgressPosition()
    {
        var current = CreateEvent(Guid.NewGuid(), Guid.NewGuid(), "progress", 120, "session");
        var incoming = current with { PositionSeconds = 90 };

        var result = CustomNetflixWatchEventBufferPolicy.Coalesce(current, incoming);

        Assert.Equal(120, result.PositionSeconds);
    }

    [Fact]
    public void Coalesce_ReplacesNonProgressEventsWithLatest()
    {
        var current = CreateEvent(Guid.NewGuid(), Guid.NewGuid(), "pause", 120, "session");
        var incoming = current with { PositionSeconds = 90 };

        var result = CustomNetflixWatchEventBufferPolicy.Coalesce(current, incoming);

        Assert.Equal(90, result.PositionSeconds);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(299.9, 0)]
    [InlineData(300, 1)]
    [InlineData(900, 3)]
    public void GetProgressSampleBucket_UsesFiveMinutePlaybackWindows(double positionSeconds, long expectedBucket)
    {
        var watchEvent = CreateEvent(Guid.NewGuid(), Guid.NewGuid(), "progress", positionSeconds, "session");

        var result = CustomNetflixWatchEventBufferPolicy.GetProgressSampleBucket(watchEvent);

        Assert.Equal(expectedBucket, result);
    }

    private static WatchEventRow CreateEvent(Guid profileId, Guid itemId, string eventType, double positionSeconds, string playSessionId)
        => new(
            Guid.NewGuid(),
            profileId,
            Guid.NewGuid(),
            itemId,
            "Episode",
            eventType,
            positionSeconds,
            1200,
            playSessionId,
            null);
}
