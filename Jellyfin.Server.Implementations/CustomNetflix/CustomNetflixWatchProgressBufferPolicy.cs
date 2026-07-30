#pragma warning disable CS1591, SA1402, SA1649

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal readonly record struct WatchProgressBufferKey(
    Guid ProfileId,
    Guid ItemId);

internal static class CustomNetflixWatchProgressBufferPolicy
{
    public static WatchProgressBufferKey GetKey(WatchProgressRow progress)
        => new(progress.ProfileId, progress.ItemId);

    public static WatchProgressRow Coalesce(WatchProgressRow? current, WatchProgressRow incoming)
    {
        if (current is null || incoming.LastPlayedAt >= current.LastPlayedAt)
        {
            return incoming;
        }

        return current;
    }
}
