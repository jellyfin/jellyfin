#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.CustomNetflix;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixSegmentCoveragePolicy
{
    public static readonly MediaSegmentType[] SegmentTypes =
    [
        MediaSegmentType.Intro,
        MediaSegmentType.Recap,
        MediaSegmentType.Outro
    ];

    public static CustomNetflixMediaSegmentCoverageDto Build(
        int eligibleItems,
        IReadOnlyDictionary<MediaSegmentType, int> coveredItems,
        DateTime generatedAt)
    {
        eligibleItems = Math.Max(0, eligibleItems);
        var types = new CustomNetflixMediaSegmentTypeCoverageDto[SegmentTypes.Length];
        for (var index = 0; index < SegmentTypes.Length; index++)
        {
            var type = SegmentTypes[index];
            var covered = Math.Clamp(coveredItems.GetValueOrDefault(type), 0, eligibleItems);
            types[index] = new CustomNetflixMediaSegmentTypeCoverageDto
            {
                Type = type switch
                {
                    MediaSegmentType.Intro => "intro",
                    MediaSegmentType.Recap => "recap",
                    _ => "outro"
                },
                CoveredItems = covered,
                CoveragePercent = eligibleItems == 0
                    ? 0
                    : Math.Round(covered * 100D / eligibleItems, 2)
            };
        }

        return new CustomNetflixMediaSegmentCoverageDto
        {
            GeneratedAt = generatedAt,
            EligibleItems = eligibleItems,
            Types = types
        };
    }
}
