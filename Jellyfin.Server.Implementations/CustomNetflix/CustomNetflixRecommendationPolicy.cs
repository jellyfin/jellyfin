#pragma warning disable CS1591, SA1402, SA1649

using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed record CustomNetflixRecommendationSignal(
    Guid ItemId,
    string ItemType,
    IReadOnlyList<string> Genres,
    DateTime LastPlayedAt,
    bool Completed,
    int PlayCount);

internal sealed record CustomNetflixRecommendationCandidate(
    Guid ItemId,
    string ItemType,
    IReadOnlyList<string> Genres,
    double? CommunityRating,
    DateTime? PremiereDate,
    DateTime DateCreated);

internal static class CustomNetflixRecommendationPolicy
{
    public const int DefaultLimit = 20;
    public const int MinLimit = 1;
    public const int MaxLimit = 50;
    public const int HistoryLimit = 100;
    public const string LikedItemsReason = "because_you_liked";
    public const string WatchHistoryReason = "based_on_watch_history";
    public const string PopularFallbackReason = "popular_in_library";

    public static int NormalizeLimit(int limit)
        => limit <= 0 ? DefaultLimit : Math.Clamp(limit, MinLimit, MaxLimit);

    public static int GetCandidatePoolSize(int limit)
        => Math.Clamp(NormalizeLimit(limit) * 15, 100, 500);

    public static bool HasPersonalizationSignals(IReadOnlyList<CustomNetflixRecommendationSignal> signals)
        => signals.Any(signal => signal.Genres.Any(genre => !string.IsNullOrWhiteSpace(genre)));

    public static IReadOnlyList<string> GetTopGenres(
        IReadOnlyList<CustomNetflixRecommendationSignal> signals,
        DateTime utcNow,
        int limit = 5)
        => BuildGenreAffinity(signals, utcNow)
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(limit, 1, 10))
            .Select(entry => entry.Key)
            .ToArray();

    public static IReadOnlyList<Guid> RankCandidates(
        IReadOnlyList<CustomNetflixRecommendationSignal> signals,
        IReadOnlyList<CustomNetflixRecommendationCandidate> candidates,
        DateTime utcNow,
        int limit,
        IReadOnlySet<Guid>? excludedItemIds = null)
    {
        limit = NormalizeLimit(limit);
        var watchedItemIds = signals.Select(signal => signal.ItemId).ToHashSet();
        var genreAffinity = BuildGenreAffinity(signals, utcNow);
        var typeAffinity = BuildTypeAffinity(signals, utcNow);
        var maxGenreAffinity = genreAffinity.Count == 0 ? 1 : genreAffinity.Values.Max();
        var maxTypeAffinity = typeAffinity.Count == 0 ? 1 : typeAffinity.Values.Max();
        var ranked = new List<RecommendationRank>(candidates.Count);
        var seen = new HashSet<Guid>();

        foreach (var candidate in candidates)
        {
            if (candidate.ItemId.Equals(Guid.Empty)
                || watchedItemIds.Contains(candidate.ItemId)
                || excludedItemIds?.Contains(candidate.ItemId) == true
                || !seen.Add(candidate.ItemId))
            {
                continue;
            }

            var matchingGenreScores = candidate.Genres
                .Where(genre => !string.IsNullOrWhiteSpace(genre))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(genre => genreAffinity.GetValueOrDefault(genre) / maxGenreAffinity)
                .Where(score => score > 0)
                .OrderByDescending(score => score)
                .ToArray();
            var genreScore = matchingGenreScores.Length == 0
                ? 0
                : matchingGenreScores[0] + (matchingGenreScores.Skip(1).Sum() * 0.3);
            var typeScore = typeAffinity.GetValueOrDefault(candidate.ItemType) / maxTypeAffinity;
            var ratingScore = Math.Clamp((candidate.CommunityRating ?? 0) / 10, 0, 1);
            var freshnessScore = GetFreshnessScore(candidate.PremiereDate ?? candidate.DateCreated, utcNow);
            var score = (genreScore * 0.72)
                + (typeScore * 0.12)
                + (ratingScore * 0.10)
                + (freshnessScore * 0.06);

            ranked.Add(new RecommendationRank(candidate.ItemId, score, ratingScore, candidate.PremiereDate ?? candidate.DateCreated));
        }

        return ranked
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.RatingScore)
            .ThenByDescending(candidate => candidate.ReleaseDate)
            .ThenBy(candidate => candidate.ItemId)
            .Take(limit)
            .Select(candidate => candidate.ItemId)
            .ToArray();
    }

    public static string GetStableReason(bool hasLikedItems, bool personalized)
        => hasLikedItems
            ? LikedItemsReason
            : personalized
                ? WatchHistoryReason
                : PopularFallbackReason;

    private static Dictionary<string, double> BuildGenreAffinity(
        IReadOnlyList<CustomNetflixRecommendationSignal> signals,
        DateTime utcNow)
    {
        var affinity = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var signal in signals)
        {
            var weight = GetSignalWeight(signal, utcNow);
            foreach (var genre in signal.Genres
                         .Where(genre => !string.IsNullOrWhiteSpace(genre))
                         .Select(genre => genre.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                affinity[genre] = affinity.GetValueOrDefault(genre) + weight;
            }
        }

        return affinity;
    }

    private static Dictionary<string, double> BuildTypeAffinity(
        IReadOnlyList<CustomNetflixRecommendationSignal> signals,
        DateTime utcNow)
    {
        var affinity = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var signal in signals.Where(signal => !string.IsNullOrWhiteSpace(signal.ItemType)))
        {
            affinity[signal.ItemType] = affinity.GetValueOrDefault(signal.ItemType) + GetSignalWeight(signal, utcNow);
        }

        return affinity;
    }

    private static double GetSignalWeight(CustomNetflixRecommendationSignal signal, DateTime utcNow)
    {
        var ageDays = Math.Max(0, (utcNow - signal.LastPlayedAt).TotalDays);
        var recencyWeight = Math.Pow(0.5, ageDays / 60);
        var completionWeight = signal.Completed ? 1.2 : 1;
        var repeatWeight = 1 + (Math.Clamp(signal.PlayCount - 1, 0, 4) * 0.15);
        return recencyWeight * completionWeight * repeatWeight;
    }

    private static double GetFreshnessScore(DateTime releaseDate, DateTime utcNow)
    {
        var ageDays = Math.Max(0, (utcNow - releaseDate).TotalDays);
        return Math.Pow(0.5, ageDays / (365.25 * 5));
    }

    private sealed record RecommendationRank(Guid ItemId, double Score, double RatingScore, DateTime ReleaseDate);
}
