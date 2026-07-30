#pragma warning disable CS1591

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixRankingSnapshots
{
    public const string TrendingId = "trending";

    public const string TopTenId = "top10";

    public const int MaxTrendingLimit = 100;

    public const int MaxTopTenLimit = 10;

    public static readonly TimeSpan SnapshotTtl = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    public static int NormalizeLimit(string rankingId, int limit)
        => string.Equals(rankingId, TopTenId, StringComparison.Ordinal)
            ? Math.Clamp(limit, 1, MaxTopTenLimit)
            : Math.Clamp(limit, 1, MaxTrendingLimit);
}
