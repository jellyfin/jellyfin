using System;
using System.Linq;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixRankingSnapshotSerializerTests
{
    [Fact]
    public void Deserialize_ReturnsNullForExpiredSnapshot()
    {
        var now = DateTime.UtcNow;
        var snapshot = new RankingSnapshotRow(
            CustomNetflixRankingSnapshots.TrendingId,
            new[] { new RankedItemRow(Guid.NewGuid(), 1, 1) },
            now.AddMinutes(-20),
            now.AddMinutes(-1));

        var result = CustomNetflixRankingSnapshotSerializer.Deserialize(
            CustomNetflixRankingSnapshotSerializer.Serialize(snapshot),
            10,
            now);

        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_AppliesLimitAndOrdersByRank()
    {
        var now = DateTime.UtcNow;
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var snapshot = new RankingSnapshotRow(
            CustomNetflixRankingSnapshots.TrendingId,
            new[]
            {
                new RankedItemRow(second, 5, 2),
                new RankedItemRow(first, 10, 1)
            },
            now,
            now.AddMinutes(15));

        var result = CustomNetflixRankingSnapshotSerializer.Deserialize(
            CustomNetflixRankingSnapshotSerializer.Serialize(snapshot),
            1,
            now);

        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal(first, result.Items.Single().ItemId);
    }

    [Theory]
    [InlineData("trending", 150, CustomNetflixRankingSnapshots.MaxTrendingLimit)]
    [InlineData("trending", 0, 1)]
    [InlineData("top10", 50, CustomNetflixRankingSnapshots.MaxTopTenLimit)]
    [InlineData("top10", 0, 1)]
    public void NormalizeLimit_ClampsByRankingType(string rankingId, int limit, int expected)
    {
        var result = CustomNetflixRankingSnapshots.NormalizeLimit(rankingId, limit);

        Assert.Equal(expected, result);
    }
}
