using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Locking;

public sealed partial class PessimisticLockBehaviorTests
{
    [Fact]
    public async Task OnSaveChangesAsync_WhenSaveChangesContinuationThreadHops_CompletesWithoutThreadAffineLockFailure()
    {
        var exception = await InvokeWithDedicatedThreadHopAsync(static (behavior, context, hopTask) =>
            behavior.OnSaveChangesAsync(context, () => hopTask));

        Assert.Null(exception);
    }

    [Fact]
    public async Task OnSaveChangesAsync_WhenNestedSaveChangesContinuationThreadHops_CompletesWithoutThreadAffineLockFailure()
    {
        var exception = await InvokeWithDedicatedThreadHopAsync(static (behavior, context, hopTask) =>
            behavior.OnSaveChangesAsync(context, () =>
                behavior.OnSaveChangesAsync(context, () => hopTask)));

        Assert.Null(exception);
    }

    private static JellyfinDbContext CreateJellyfinContext(PessimisticLockBehavior behavior)
    {
        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            behavior);
    }

    private static async Task<Exception?> InvokeWithDedicatedThreadHopAsync(
        Func<PessimisticLockBehavior, JellyfinDbContext, Task, Task> invokeSaveChanges)
    {
        var result = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownerThread = new Thread(() =>
        {
            Exception? observedException = null;
            var setupException = Record.Exception(() =>
            {
                observedException = InvokeOnLockOwnerThread(invokeSaveChanges);
            });

            result.SetResult(setupException ?? observedException);
        })
        {
            IsBackground = true,
            Name = "PessimisticLockBehaviorTests lock owner"
        };

        ownerThread.Start();
        return await result.Task;
    }

    private static Exception? InvokeOnLockOwnerThread(
        Func<PessimisticLockBehavior, JellyfinDbContext, Task, Task> invokeSaveChanges)
    {
        Thread? continuationThread = null;
        Exception? completionException = null;

        try
        {
            var behavior = CreateBehavior();
            using var context = CreateJellyfinContext(behavior);
            var threadHop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var saveChangesTask = invokeSaveChanges(behavior, context, threadHop.Task);

            if (saveChangesTask.IsCompleted)
            {
                return new InvalidOperationException("The save changes task completed before the test could force a thread hop.");
            }

            continuationThread = new Thread(() =>
            {
                completionException = Record.Exception(threadHop.SetResult);
            })
            {
                IsBackground = true,
                Name = "PessimisticLockBehaviorTests continuation"
            };

            continuationThread.Start();

            var saveChangesException = Record.Exception(saveChangesTask.GetAwaiter().GetResult);
            return saveChangesException ?? completionException;
        }
        finally
        {
            continuationThread?.Join();
        }
    }
}
