using System;
using System.Linq;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixWatchProgressBatchPolicyTests
{
    [Fact]
    public void NormalizeItemIds_DropsEmptyAndDuplicates()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var result = CustomNetflixWatchProgressBatchPolicy.NormalizeItemIds([
            Guid.Empty,
            first,
            first,
            second
        ]);

        Assert.Equal([first, second], result);
    }

    [Fact]
    public void NormalizeItemIds_LimitsBatchSize()
    {
        var itemIds = Enumerable.Range(0, CustomNetflixWatchProgressBatchPolicy.MaxItemIds + 10)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var result = CustomNetflixWatchProgressBatchPolicy.NormalizeItemIds(itemIds);

        Assert.Equal(CustomNetflixWatchProgressBatchPolicy.MaxItemIds, result.Count);
        Assert.Equal(itemIds.Take(CustomNetflixWatchProgressBatchPolicy.MaxItemIds), result);
    }
}
