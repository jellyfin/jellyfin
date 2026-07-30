using System;
using System.Linq;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Controller.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixManualSegmentPolicyTests
{
    [Fact]
    public void BuildManualRows_NormalizesTypesAndUsesManualSource()
    {
        var itemId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var rows = CustomNetflixManualSegmentPolicy.BuildManualRows(
            itemId,
            new[]
            {
                new CustomNetflixManualMediaSegmentRequest { Type = "outro", StartSeconds = 90, EndSeconds = 100 },
                new CustomNetflixManualMediaSegmentRequest { Type = "Intro", StartSeconds = 10, EndSeconds = 20 }
            },
            now);

        Assert.Equal(new[] { "intro", "credits" }, rows.Select(row => row.SegmentType));
        Assert.All(rows, row => Assert.Equal(CustomNetflixManualSegmentPolicy.ManualSource, row.Source));
        Assert.All(rows, row => Assert.Equal(itemId, row.ItemId));
        Assert.All(rows, row => Assert.Equal(now, row.UpdatedAt));
    }

    [Fact]
    public void BuildManualRows_RejectsInvalidSegments()
    {
        var itemId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => CustomNetflixManualSegmentPolicy.BuildManualRows(
            itemId,
            new[] { new CustomNetflixManualMediaSegmentRequest { Type = "unknown", StartSeconds = 0, EndSeconds = 10 } },
            DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => CustomNetflixManualSegmentPolicy.BuildManualRows(
            itemId,
            new[] { new CustomNetflixManualMediaSegmentRequest { Type = "intro", StartSeconds = -1, EndSeconds = 10 } },
            DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => CustomNetflixManualSegmentPolicy.BuildManualRows(
            itemId,
            new[] { new CustomNetflixManualMediaSegmentRequest { Type = "intro", StartSeconds = 10, EndSeconds = 10 } },
            DateTime.UtcNow));
    }
}
