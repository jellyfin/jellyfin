#pragma warning disable CS1591, SA1402, SA1649

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal readonly record struct WatchEventBufferKey(
    Guid ProfileId,
    Guid ItemId,
    string EventType,
    string? PlaySessionId);

internal readonly record struct WatchEventSamplingKey(
    Guid ProfileId,
    Guid ItemId,
    string? PlaySessionId);

internal static class CustomNetflixWatchEventBufferPolicy
{
    private const int ProgressSampleSeconds = 300;

    public static WatchEventBufferKey GetKey(WatchEventRow watchEvent)
        => new(watchEvent.ProfileId, watchEvent.ItemId, watchEvent.EventType, watchEvent.PlaySessionId);

    public static WatchEventRow Coalesce(WatchEventRow? current, WatchEventRow incoming)
    {
        if (current is null)
        {
            return incoming;
        }

        if (string.Equals(incoming.EventType, "progress", StringComparison.Ordinal)
            && incoming.PositionSeconds < current.PositionSeconds)
        {
            return current;
        }

        return incoming;
    }

    public static bool IsProgress(WatchEventRow watchEvent)
        => string.Equals(watchEvent.EventType, "progress", StringComparison.Ordinal);

    public static WatchEventSamplingKey GetSamplingKey(WatchEventRow watchEvent)
        => new(watchEvent.ProfileId, watchEvent.ItemId, watchEvent.PlaySessionId);

    public static long GetProgressSampleBucket(WatchEventRow watchEvent)
        => Math.Max(0, (long)Math.Floor(watchEvent.PositionSeconds / ProgressSampleSeconds));
}
