#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixSegmentTypeMapper
{
    public static IReadOnlyList<string> NormalizeRequestedTypes(IReadOnlyList<string>? requestedTypes)
    {
        if (requestedTypes is null || requestedTypes.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalizedTypes = requestedTypes
            .SelectMany(type => type.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(NormalizeType)
            .Where(type => type is "intro" or "recap" or "credits")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return normalizedTypes.Length > 0
            ? normalizedTypes
            : throw new ArgumentException("No valid segment type was provided.", nameof(requestedTypes));
    }

    public static MediaSegmentType ToNativeSegmentType(string segmentType)
        => segmentType switch
        {
            "intro" => MediaSegmentType.Intro,
            "recap" => MediaSegmentType.Recap,
            "credits" => MediaSegmentType.Outro,
            _ => MediaSegmentType.Unknown
        };

    public static string NormalizeType(string segmentType)
        => segmentType.ToLowerInvariant() switch
        {
            "intro" => "intro",
            "recap" => "recap",
            "outro" => "credits",
            "credit" => "credits",
            "credits" => "credits",
            _ => string.Empty
        };
}
