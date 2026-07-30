#pragma warning disable CS1591

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixPlaybackPolicy
{
    public static bool IsCompleted(string itemType, double positionSeconds, double durationSeconds, double percentViewed)
    {
        if (durationSeconds <= 0)
        {
            return false;
        }

        var remainingSeconds = Math.Max(0, durationSeconds - positionSeconds);
        var remainingThreshold = string.Equals(itemType, "Movie", StringComparison.OrdinalIgnoreCase) ? 300 : 180;
        return percentViewed >= 90 || remainingSeconds <= remainingThreshold;
    }
}
