using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Locking;

public sealed partial class PessimisticLockBehaviorTests
{
    private static readonly TimeSpan LockProbeDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    private static PessimisticLockBehavior CreateBehavior(Action? onQueryCongestionDetected = null)
    {
        ILoggerFactory loggerFactory = onQueryCongestionDetected is null
            ? NullLoggerFactory.Instance
            : new CongestionLoggerFactory(onQueryCongestionDetected);

        return new PessimisticLockBehavior(
            NullLogger<PessimisticLockBehavior>.Instance,
            loggerFactory);
    }

    private static async Task<bool> CompletesWithinAsync(Task task, TimeSpan timeout)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, TestContext.Current.CancellationToken));
        if (!ReferenceEquals(completedTask, task))
        {
            return false;
        }

        await task;
        return true;
    }

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
