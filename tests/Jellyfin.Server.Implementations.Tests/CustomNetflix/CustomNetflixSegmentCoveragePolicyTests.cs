using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixSegmentCoveragePolicyTests
{
    [Fact]
    public void Build_ReturnsStableTypesAndDistinctItemPercentages()
    {
        var generatedAt = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

        var result = CustomNetflixSegmentCoveragePolicy.Build(
            80,
            new Dictionary<MediaSegmentType, int>
            {
                [MediaSegmentType.Intro] = 40,
                [MediaSegmentType.Recap] = 20,
                [MediaSegmentType.Outro] = 60
            },
            generatedAt);

        Assert.Equal(generatedAt, result.GeneratedAt);
        Assert.Equal(80, result.EligibleItems);
        Assert.Equal(["intro", "recap", "outro"], result.Types.Select(type => type.Type));
        Assert.Equal([50D, 25D, 75D], result.Types.Select(type => type.CoveragePercent));
    }
}
