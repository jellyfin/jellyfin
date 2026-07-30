#pragma warning disable CS1591, SA1402, SA1649

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixRankingSnapshotSerializer
{
    public static string Serialize(RankingSnapshotRow snapshot)
        => JsonSerializer.Serialize(new RankingSnapshotPayload(
            snapshot.RankingId,
            snapshot.GeneratedAt,
            snapshot.ExpiresAt,
            snapshot.Items
                .OrderBy(item => item.Rank)
                .Select(item => new RankingSnapshotItem(item.ItemId, item.Score, item.Rank))
                .ToArray()));

    public static RankingSnapshotRow? Deserialize(string json, int limit, DateTime utcNow)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<RankingSnapshotPayload>(json);
            if (payload is null || payload.ExpiresAt <= utcNow)
            {
                return null;
            }

            return new RankingSnapshotRow(
                payload.RankingId,
                payload.Items
                    .OrderBy(item => item.Rank)
                    .Take(Math.Clamp(limit, 1, 100))
                    .Select(item => new RankedItemRow(item.ItemId, item.Score, item.Rank))
                    .ToArray(),
                payload.GeneratedAt,
                payload.ExpiresAt);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record RankingSnapshotPayload(
        string RankingId,
        DateTime GeneratedAt,
        DateTime ExpiresAt,
        IReadOnlyList<RankingSnapshotItem> Items);

    private sealed record RankingSnapshotItem(Guid ItemId, double Score, int Rank);
}
