#pragma warning disable CS1591

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixFeedbackPolicy
{
    public const string Like = "like";
    public const string Dislike = "dislike";
    public const string NotInterested = "not-interested";
    public const int RecommendationFeedbackLimit = 200;

    public static string Normalize(string? feedback)
        => feedback?.Trim().ToLowerInvariant() switch
        {
            Like => Like,
            Dislike => Dislike,
            NotInterested or "not_interested" => NotInterested,
            _ => throw new ArgumentException(
                "Feedback must be like, dislike, or not-interested.",
                nameof(feedback))
        };

    public static bool IsNegative(string feedback)
        => feedback is Dislike or NotInterested;
}
