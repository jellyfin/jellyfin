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

public class CustomNetflixWatchEventBufferTests
{
    [Fact]
    public async Task Flush_RetainsAndRetriesBatchAfterRepositoryFailure()
    {
        var attempts = 0;
        var batches = new ConcurrentQueue<Guid[]>();
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(mock => mock.IsEnabled).Returns(true);
        repository
            .Setup(mock => mock.InsertWatchEventsAsync(
                It.IsAny<IReadOnlyList<WatchEventRow>>(),
                It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyList<WatchEventRow> events, CancellationToken _) =>
            {
                batches.Enqueue(events.Select(row => row.Id).Order().ToArray());
                return Interlocked.Increment(ref attempts) == 1
                    ? Task.FromException(new InvalidOperationException("PostgreSQL unavailable."))
                    : Task.CompletedTask;
            });
        var schemaState = new CustomNetflixSchemaState();
        using var buffer = new CustomNetflixWatchEventBuffer(
            repository.Object,
            schemaState,
            NullLogger<CustomNetflixWatchEventBuffer>.Instance);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        await buffer.StartAsync(timeout.Token);
        for (var index = 0; index < 250; index++)
        {
            await buffer.EnqueueAsync(CreateEvent(Guid.NewGuid()), timeout.Token);
        }

        repository.Verify(
            mock => mock.InsertWatchEventsAsync(
                It.IsAny<IReadOnlyList<WatchEventRow>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        schemaState.MarkReady();
        while (Volatile.Read(ref attempts) < 2)
        {
            await Task.Delay(25, timeout.Token);
        }

        await buffer.StopAsync(timeout.Token);

        var flushedBatches = batches.ToArray();
        Assert.Equal(2, flushedBatches.Length);
        Assert.DoesNotContain(Guid.Empty, flushedBatches[0]);
        Assert.Equal(flushedBatches[0], flushedBatches[1]);
    }

    [Fact]
    public async Task Enqueue_SamplesProgressButKeepsPauseAndCompleteEvents()
    {
        var inserted = new ConcurrentQueue<WatchEventRow>();
        var firstFlush = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(mock => mock.IsEnabled).Returns(true);
        repository
            .Setup(mock => mock.InsertWatchEventsAsync(
                It.IsAny<IReadOnlyList<WatchEventRow>>(),
                It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyList<WatchEventRow> events, CancellationToken _) =>
            {
                foreach (var watchEvent in events)
                {
                    inserted.Enqueue(watchEvent);
                }

                firstFlush.TrySetResult();
                return Task.CompletedTask;
            });
        var schemaState = new CustomNetflixSchemaState();
        using var buffer = new CustomNetflixWatchEventBuffer(
            repository.Object,
            schemaState,
            NullLogger<CustomNetflixWatchEventBuffer>.Instance);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var profileId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        await buffer.StartAsync(timeout.Token);
        await buffer.EnqueueAsync(CreateEvent(profileId, itemId, "progress", 60, "session"), timeout.Token);
        for (var index = 1; index < 250; index++)
        {
            await buffer.EnqueueAsync(CreateEvent(Guid.NewGuid()), timeout.Token);
        }

        schemaState.MarkReady();
        await firstFlush.Task.WaitAsync(timeout.Token);
        await buffer.EnqueueAsync(CreateEvent(profileId, itemId, "progress", 120, "session"), timeout.Token);
        await buffer.EnqueueAsync(CreateEvent(profileId, itemId, "pause", 120, "session"), timeout.Token);
        await buffer.EnqueueAsync(CreateEvent(profileId, itemId, "complete", 1200, "session"), timeout.Token);
        await buffer.StopAsync(timeout.Token);

        var sessionEvents = inserted
            .Where(row => row.ProfileId.Equals(profileId) && row.ItemId.Equals(itemId))
            .OrderBy(row => row.EventType)
            .ToArray();
        Assert.Equal(3, sessionEvents.Length);
        Assert.Equal(["complete", "pause", "progress"], sessionEvents.Select(row => row.EventType));
        Assert.Equal(60, sessionEvents.Single(row => row.EventType == "progress").PositionSeconds);
    }

    private static WatchEventRow CreateEvent(Guid itemId)
        => CreateEvent(Guid.NewGuid(), itemId, "progress", 60, "session");

    private static WatchEventRow CreateEvent(
        Guid profileId,
        Guid itemId,
        string eventType,
        double positionSeconds,
        string playSessionId)
        => new(
            Guid.NewGuid(),
            profileId,
            Guid.NewGuid(),
            itemId,
            "Episode",
            eventType,
            positionSeconds,
            1200,
            playSessionId,
            null);
}
