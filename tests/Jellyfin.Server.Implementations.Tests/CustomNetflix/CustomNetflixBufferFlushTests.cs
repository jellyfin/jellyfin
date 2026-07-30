using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.CustomNetflix.Tests;

public sealed class CustomNetflixBufferFlushTests
{
    [Fact]
    public async Task ProgressFlushAsync_PersistsEntriesQueuedBeforeBarrier()
    {
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(value => value.IsEnabled).Returns(true);
        repository
            .Setup(value => value.UpsertProgressRowsAsync(
                It.IsAny<IReadOnlyList<WatchProgressRow>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var schemaState = new CustomNetflixSchemaState();
        schemaState.MarkReady();
        var buffer = new CustomNetflixWatchProgressBuffer(
            repository.Object,
            schemaState,
            NullLogger<CustomNetflixWatchProgressBuffer>.Instance);
        await buffer.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var progress = new WatchProgressRow(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                10,
                100,
                10,
                false,
                0,
                DateTime.UtcNow);
            await buffer.EnqueueAsync(progress, TestContext.Current.CancellationToken);

            await buffer.FlushAsync(TestContext.Current.CancellationToken);

            repository.Verify(
                value => value.UpsertProgressRowsAsync(
                    It.Is<IReadOnlyList<WatchProgressRow>>(rows => rows.Count == 1 && rows[0] == progress),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await buffer.StopAsync(CancellationToken.None);
            buffer.Dispose();
        }
    }

    [Fact]
    public async Task EventFlushAsync_PersistsEntriesQueuedBeforeBarrier()
    {
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(value => value.IsEnabled).Returns(true);
        repository
            .Setup(value => value.InsertWatchEventsAsync(
                It.IsAny<IReadOnlyList<WatchEventRow>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var schemaState = new CustomNetflixSchemaState();
        schemaState.MarkReady();
        var buffer = new CustomNetflixWatchEventBuffer(
            repository.Object,
            schemaState,
            NullLogger<CustomNetflixWatchEventBuffer>.Instance);
        await buffer.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var watchEvent = new WatchEventRow(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Movie",
                "play",
                10,
                100,
                null,
                null);
            await buffer.EnqueueAsync(watchEvent, TestContext.Current.CancellationToken);

            await buffer.FlushAsync(TestContext.Current.CancellationToken);

            repository.Verify(
                value => value.InsertWatchEventsAsync(
                    It.Is<IReadOnlyList<WatchEventRow>>(events => events.Count == 1 && events[0] == watchEvent),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await buffer.StopAsync(CancellationToken.None);
            buffer.Dispose();
        }
    }
}
