#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixWatchProgressBatchPolicy
{
    public const int MaxItemIds = 200;

    public static IReadOnlyList<Guid> NormalizeItemIds(IReadOnlyList<Guid>? itemIds)
    {
        if (itemIds is null || itemIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var normalized = new List<Guid>(Math.Min(itemIds.Count, MaxItemIds));
        var seen = new HashSet<Guid>();
        foreach (var itemId in itemIds)
        {
            if (itemId.Equals(Guid.Empty) || !seen.Add(itemId))
            {
                continue;
            }

            normalized.Add(itemId);
            if (normalized.Count >= MaxItemIds)
            {
                break;
            }
        }

        return normalized;
    }
}
