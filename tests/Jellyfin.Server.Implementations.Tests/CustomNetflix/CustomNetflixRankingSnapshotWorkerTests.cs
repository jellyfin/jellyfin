using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.CustomNetflix;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixRankingSnapshotWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_BoundsRetentionPurgeToTenBatches()
    {
        var purge = new TaskCompletionSource<(DateTime Cutoff, int BatchSize)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var purgeCalls = 0;
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(mock => mock.IsEnabled).Returns(true);
        repository
            .Setup(mock => mock.GetTrendingItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RankedItemRow>());
        repository
            .Setup(mock => mock.GetTopTenItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RankedItemRow>());
        repository
            .Setup(mock => mock.SaveRankingSnapshotAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<RankedItemRow>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(mock => mock.PurgeWatchEventsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns((DateTime cutoff, int batchSize, CancellationToken _) =>
            {
                if (Interlocked.Increment(ref purgeCalls) == 10)
                {
                    purge.TrySetResult((cutoff, batchSize));
                }

                return Task.FromResult(batchSize);
            });
        var cache = new Mock<ICustomNetflixCacheService>();
        cache
            .Setup(mock => mock.SetStringAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var schemaState = new CustomNetflixSchemaState();
        schemaState.MarkReady();
        using var worker = new CustomNetflixRankingSnapshotWorker(
            repository.Object,
            cache.Object,
            schemaState,
            NullLogger<CustomNetflixRankingSnapshotWorker>.Instance);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var earliestCutoff = DateTime.UtcNow.AddDays(-31);

        await worker.StartAsync(timeout.Token);
        var result = await purge.Task.WaitAsync(timeout.Token);
        await worker.StopAsync(timeout.Token);

        Assert.Equal(1000, result.BatchSize);
        Assert.InRange(result.Cutoff, earliestCutoff, DateTime.UtcNow.AddDays(-31));
        repository.Verify(
            mock => mock.PurgeWatchEventsAsync(
                It.IsAny<DateTime>(),
                1000,
                It.IsAny<CancellationToken>()),
            Times.Exactly(10));
    }
}
