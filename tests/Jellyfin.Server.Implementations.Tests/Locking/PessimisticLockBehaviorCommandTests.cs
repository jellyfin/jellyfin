using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Locking;

public sealed partial class PessimisticLockBehaviorTests
{
    [Fact]
    public async Task ReadCommands_WhenNoWriterIsHeld_RunConcurrently()
    {
        var behavior = CreateBehavior();
        await using var database = await SqliteTestDatabase.CreateAsync(behavior);
        await using var firstContext = database.CreateContext(behavior, enableDelayFunction: true);
        await using var secondContext = database.CreateContext(behavior, enableDelayFunction: true);
        await firstContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await secondContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        var stopwatch = Stopwatch.StartNew();

        var firstRead = Task.Run(async () => await firstContext.Database.SqlQueryRaw<int>("SELECT delay(1000) AS Value").SingleAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        var secondRead = Task.Run(async () => await secondContext.Database.SqlQueryRaw<int>("SELECT delay(1000) AS Value").SingleAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

        await Task.WhenAll(firstRead, secondRead).WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(1700), $"Concurrent reads took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task ReadCommands_WhenWriterIsWaiting_BlockBehindWriter()
    {
        Task? secondReader = null;
        LockingDbContext? secondReaderContext = null;
        var congestionObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReaderCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReaderStarted = 0;
        var behavior = CreateBehavior(() =>
        {
            congestionObserved.TrySetResult();
            if (Interlocked.Exchange(ref secondReaderStarted, 1) != 0)
            {
                return;
            }

            secondReader = Task.Run(
                async () =>
                {
                    await secondReaderContext!.Database.SqlQueryRaw<int>("SELECT delay(10) AS Value").SingleAsync(TestContext.Current.CancellationToken);
                    secondReaderCompleted.SetResult();
                },
                TestContext.Current.CancellationToken);
        });
        await using var database = await SqliteTestDatabase.CreateAsync(behavior);
        var firstReaderStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var firstReaderContext = database.CreateContext(behavior, enableDelayFunction: true, delayStarted: firstReaderStarted);
        await using var writerContext = database.CreateContext(behavior);
        await using var lateReaderContext = database.CreateContext(behavior, enableDelayFunction: true);
        secondReaderContext = lateReaderContext;
        await firstReaderContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await lateReaderContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        var firstReader = Task.Run(async () => await firstReaderContext.Database.SqlQueryRaw<int>("SELECT delay(2000) AS Value").SingleAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        await firstReaderStarted.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        var writer = Task.Run(
            async () =>
            {
                await writerContext.Database.ExecuteSqlRawAsync("INSERT INTO Rows (Name) VALUES ('writer')", TestContext.Current.CancellationToken);
            },
            TestContext.Current.CancellationToken);
        await congestionObserved.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        Assert.False(await CompletesWithinAsync(secondReaderCompleted.Task, LockProbeDelay));

        await firstReader.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await writer.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await secondReader!.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenWriterIsWaiting_BlocksReadersBehindWriter()
    {
        Task? secondReader = null;
        LockingDbContext? secondReaderContext = null;
        var congestionObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReaderCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReaderStarted = 0;
        var behavior = CreateBehavior(() =>
        {
            congestionObserved.TrySetResult();
            if (Interlocked.Exchange(ref secondReaderStarted, 1) != 0)
            {
                return;
            }

            secondReader = Task.Run(
                async () =>
                {
                    await secondReaderContext!.Database.SqlQueryRaw<int>("SELECT delay(10) AS Value").SingleAsync(TestContext.Current.CancellationToken);
                    secondReaderCompleted.SetResult();
                },
                TestContext.Current.CancellationToken);
        });
        await using var database = await SqliteTestDatabase.CreateAsync(behavior);
        var firstReaderStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var firstReaderContext = database.CreateContext(behavior, enableDelayFunction: true, delayStarted: firstReaderStarted);
        await using var writerContext = database.CreateContext(behavior);
        await using var lateReaderContext = database.CreateContext(behavior, enableDelayFunction: true);
        secondReaderContext = lateReaderContext;
        await firstReaderContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await lateReaderContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        var firstReader = Task.Run(async () => await firstReaderContext.Database.SqlQueryRaw<int>("SELECT delay(2000) AS Value").SingleAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        await firstReaderStarted.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        writerContext.Rows.Add(new Row { Name = "save changes writer" });
        var writer = Task.Run(
            async () =>
            {
                await writerContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            },
            TestContext.Current.CancellationToken);
        await congestionObserved.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        Assert.False(await CompletesWithinAsync(secondReaderCompleted.Task, LockProbeDelay));

        await firstReader.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await writer.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await secondReader!.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WriteCommandAsync_WhenCanceledWhileWaitingForReader_ReleasesWriterTurnstile()
    {
        var behavior = CreateBehavior();
        await using var database = await SqliteTestDatabase.CreateAsync(behavior);
        var firstReaderStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writerReachedCommandPipeline = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var firstReaderContext = database.CreateContext(behavior, enableDelayFunction: true, delayStarted: firstReaderStarted);
        await using var writerContext = database.CreateContext(behavior, commandStarted: writerReachedCommandPipeline);
        await using var probeReaderContext = database.CreateContext(behavior);
        await firstReaderContext.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        using var writerCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        writerCancellation.CancelAfter(TestTimeout);
        var firstReader = Task.Run(async () => await firstReaderContext.Database.SqlQueryRaw<int>("SELECT delay(2000) AS Value").SingleAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        await firstReaderStarted.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        var writer = writerContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO Rows (Name) VALUES ('canceled writer')",
            [],
            writerCancellation.Token);
        await writerReachedCommandPipeline.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        await Task.Delay(LockProbeDelay, TestContext.Current.CancellationToken);
        await writerCancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await writer);

        await firstReader.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await probeReaderContext.Database.SqlQueryRaw<int>("SELECT 1 AS Value").SingleAsync(TestContext.Current.CancellationToken).WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
    }
}
