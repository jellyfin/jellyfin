#pragma warning disable CS1591

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixRankingPolicy
{
    public const double TrendingDistinctProfileWeight = 2.0;
    public const double TopTenDistinctProfileWeight = 3.0;
    public const double TrendingEventWeight = 0.08;
    public const double TopTenEventWeight = 0.04;
    public const double TrendingMaxEventBonusPerProfile = 0.8;
    public const double TopTenMaxEventBonusPerProfile = 0.6;
    public const double TrendingRecencyHalfLifeDays = 2.0;
    public const double TopTenRecencyHalfLifeDays = 7.0;

    public static double EventWeight(string eventType, bool topTen)
        => eventType switch
        {
            "complete" => topTen ? 8.0 : 5.0,
            "mark_played" => topTen ? 7.0 : 4.0,
            "progress" => 1.0,
            "pause" => topTen ? 0.5 : 0.5,
            _ => topTen ? 0.5 : 1.0
        };

    public static double RecencyMultiplier(double ageDays, double halfLifeDays)
    {
        if (ageDays <= 0)
        {
            return 1.0;
        }

        return Math.Pow(0.5, ageDays / halfLifeDays);
    }

    public static double ProfileScore(
        double strongestEventScore,
        int eventCount,
        double lastEventAgeDays,
        bool topTen)
    {
        var eventWeight = topTen ? TopTenEventWeight : TrendingEventWeight;
        var maxEventBonus = topTen ? TopTenMaxEventBonusPerProfile : TrendingMaxEventBonusPerProfile;
        var halfLife = topTen ? TopTenRecencyHalfLifeDays : TrendingRecencyHalfLifeDays;
        var recency = RecencyMultiplier(lastEventAgeDays, halfLife);
        return (strongestEventScore + Math.Min(eventCount * eventWeight, maxEventBonus)) * recency;
    }

    public static double ItemScore(double profileScoreSum, int distinctProfileCount, bool topTen)
        => profileScoreSum + (distinctProfileCount * (topTen ? TopTenDistinctProfileWeight : TrendingDistinctProfileWeight));
}
