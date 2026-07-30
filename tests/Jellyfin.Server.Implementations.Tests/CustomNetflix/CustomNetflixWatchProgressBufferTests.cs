using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.CustomNetflix;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixWatchProgressBufferTests
{
    [Fact]
    public async Task Flush_RetriesSameBatchWhenUpsertFails()
    {
        var attempts = 0;
        var batches = new ConcurrentQueue<Guid[]>();
        var firstAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(mock => mock.IsEnabled).Returns(true);
        repository
            .Setup(mock => mock.UpsertProgressRowsAsync(
                It.IsAny<IReadOnlyList<WatchProgressRow>>(),
                It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyList<WatchProgressRow> rows, CancellationToken _) =>
            {
                batches.Enqueue(rows.Select(row => row.ItemId).Order().ToArray());
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    firstAttempt.TrySetResult();
                    return Task.FromException(new InvalidOperationException("PostgreSQL unavailable."));
                }

                secondAttempt.TrySetResult();
                return Task.CompletedTask;
            });
        var schemaState = new CustomNetflixSchemaState();
        using var buffer = new CustomNetflixWatchProgressBuffer(
            repository.Object,
            schemaState,
            NullLogger<CustomNetflixWatchProgressBuffer>.Instance);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var profileId = Guid.NewGuid();

        await buffer.StartAsync(timeout.Token);
        for (var index = 0; index < 250; index++)
        {
            await buffer.EnqueueAsync(CreateProgress(profileId, Guid.NewGuid()), timeout.Token);
        }

        schemaState.MarkReady();
        await firstAttempt.Task.WaitAsync(timeout.Token);
        await Task.Delay(250, timeout.Token);
        Assert.Equal(1, Volatile.Read(ref attempts));
        await secondAttempt.Task.WaitAsync(timeout.Token);
        await buffer.StopAsync(timeout.Token);

        var flushedBatches = batches.ToArray();
        Assert.Equal(2, flushedBatches.Length);
        Assert.Equal(flushedBatches[0], flushedBatches[1]);
        repository.Verify(
            mock => mock.DeleteHomeSnapshotsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static WatchProgressRow CreateProgress(Guid profileId, Guid itemId)
        => new(
            profileId,
            itemId,
            null,
            60,
            1200,
            5,
            false,
            0,
            DateTime.UtcNow);
}
