#pragma warning disable CS1591

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixRetryPolicy
{
    public static TimeSpan GetDelay(int failureCount, TimeSpan maximumDelay)
        => TimeSpan.FromSeconds(Math.Min(
            maximumDelay.TotalSeconds,
            Math.Pow(2, Math.Clamp(failureCount - 1, 0, 20))));
}
