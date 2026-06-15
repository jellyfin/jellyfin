using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Locking;

public sealed partial class PessimisticLockBehaviorTests
{
    [Fact]
    public async Task TransactionCommands_WhenTransactionLockIsHeld_DoNotReenterGlobalLock()
    {
        var behavior = CreateBehavior();
        await using var database = await SqliteTestDatabase.CreateAsync(behavior);
        await using var context = database.CreateContext(behavior);
        await using var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await context.Database
            .ExecuteSqlRawAsync("INSERT INTO Rows (Name) VALUES ('inside transaction')", TestContext.Current.CancellationToken)
            .WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        var rowCount = await context.Rows.CountAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, rowCount);
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenAnotherTransactionIsOpen_BlocksUntilFirstTransactionDisposes()
    {
        var behavior = CreateBehavior();
        await using var database = await SqliteTestDatabase.CreateAsync(behavior);
        await using var firstContext = database.CreateContext(behavior);
        await using var secondContext = database.CreateContext(behavior);
        var firstTransaction = await firstContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        IDbContextTransaction? secondTransaction = null;
        using var secondTransactionCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        secondTransactionCancellation.CancelAfter(TestTimeout);
        var secondTransactionTask = secondContext.Database.BeginTransactionAsync(secondTransactionCancellation.Token);

        try
        {
            Assert.False(await CompletesWithinAsync(secondTransactionTask, LockProbeDelay));

            await firstTransaction.DisposeAsync();
            secondTransaction = await secondTransactionTask.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            if (secondTransaction is not null)
            {
                await secondTransaction.DisposeAsync();
            }

            await firstTransaction.DisposeAsync();
            if (secondTransaction is null && !secondTransactionTask.IsCompleted)
            {
                await secondTransactionCancellation.CancelAsync();
                await IgnoreCancellationAsync(secondTransactionTask);
            }
        }
    }

    [Fact]
    public async Task DisposeOnlyTransaction_WhenNoExplicitRollbackOrCommit_ReleasesPessimisticLock()
    {
        var behavior = CreateBehavior();
        await using var database = await SqliteTestDatabase.CreateAsync(behavior);
        await using var transactionContext = database.CreateContext(behavior);
        await using var commandContext = database.CreateContext(behavior);
        var transaction = await transactionContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        using var commandCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        commandCancellation.CancelAfter(TestTimeout);

        var blockedCommand = commandContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO Rows (Name) VALUES ('after dispose')",
            [],
            commandCancellation.Token);

        try
        {
            Assert.False(await CompletesWithinAsync(blockedCommand, LockProbeDelay));

            await transaction.DisposeAsync();
            await blockedCommand.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            if (!blockedCommand.IsCompleted)
            {
                await commandCancellation.CancelAsync();
                await IgnoreCancellationAsync(blockedCommand);
            }
        }
    }
}
