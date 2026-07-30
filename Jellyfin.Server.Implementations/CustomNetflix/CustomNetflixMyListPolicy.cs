#pragma warning disable CS1591

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixMyListPolicy
{
    public const int DefaultLimit = 50;
    public const int MinLimit = 1;
    public const int MaxLimit = 100;

    public static int NormalizeLimit(int limit)
        => Math.Clamp(limit <= 0 ? DefaultLimit : limit, MinLimit, MaxLimit);

    public static bool SupportsItemType(string itemType)
        => string.Equals(itemType, "Movie", StringComparison.Ordinal)
            || string.Equals(itemType, "Series", StringComparison.Ordinal);
}
