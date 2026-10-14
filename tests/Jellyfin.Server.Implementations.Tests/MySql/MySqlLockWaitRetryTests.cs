using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

/// <summary>
/// A lock wait timeout (error 1205) is retryable because it only rolls back the statement that timed out. The
/// transaction it happened in stays open and keeps the locks it already has, so it cannot be reused, and
/// <see cref="WriteConflictRetry"/> never reuses it: every attempt creates its own context and transaction, and the
/// transaction of a failed attempt is rolled back when its context is disposed.
/// </summary>
public sealed class MySqlLockWaitRetryTests : DatabaseTestFixture
{
    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task LockWaitTimeout_RollsBackOnlyTheStatement_AndIsClassifiedAsRetryable()
    {
        await using var setup = CreateDbContext();
        await CreateProbeTableAsync(setup);
        try
        {
            await using var holder = CreateDbContext();
            await using var holderTransaction = await BeginAsync(holder);
            await ExecuteAsync(holder, "UPDATE `LockProbe` SET `V` = 1 WHERE `Id` = 1");

            await using var contender = CreateDbContext();
            await using var contenderTransaction = await BeginAsync(contender);
            await ExecuteAsync(contender, "UPDATE `LockProbe` SET `V` = 7 WHERE `Id` = 2");

            var ex = await Assert.ThrowsAnyAsync<Exception>(() => ExecuteAsync(contender, "UPDATE `LockProbe` SET `V` = 7 WHERE `Id` = 1"));

            Assert.True(DatabaseProvider.IsRetryableWriteConflict(ex), ex.ToString());
            Assert.Contains(Chain(ex), e => e is MySqlException { ErrorCode: MySqlErrorCode.LockWaitTimeout });

            // Only the statement was rolled back, the earlier update of the same transaction is still there and still locked.
            var values = await contender.Database
                .SqlQueryRaw<int>("SELECT `V` AS `Value` FROM `LockProbe` WHERE `Id` = 2")
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal([7], values);
        }
        finally
        {
            await DropProbeTableAsync(setup);
        }
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task WriteConflictRetry_LockWaitTimeout_RunsTheWorkAgainInAFreshTransaction()
    {
        await using var setup = CreateDbContext();
        await CreateProbeTableAsync(setup);
        try
        {
            await using var holder = CreateDbContext();
            await using var holderTransaction = await BeginAsync(holder);
            await ExecuteAsync(holder, "UPDATE `LockProbe` SET `V` = 1 WHERE `Id` = 1");

            var attempts = 0;
            var retry = new WriteConflictRetry(
                DatabaseProvider,
                NullLogger.Instance,
                async (_, ct) => await holderTransaction.RollbackAsync(ct),
                (_, _) => throw new InvalidOperationException("The asynchronous pause is expected."));

            await retry.ExecuteAsync(
                async ct =>
                {
                    attempts++;
                    await using var context = CreateDbContext();
                    await context.Database.OpenConnectionAsync(ct);
                    await context.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 1", ct);
                    await using var transaction = await context.Database.BeginTransactionAsync(ct);

                    // Counts the attempts that kept this update, the one of a failed attempt must be gone.
                    await context.Database.ExecuteSqlRawAsync("UPDATE `LockProbe` SET `V` = `V` + 1 WHERE `Id` = 2", ct);
                    await context.Database.ExecuteSqlRawAsync("UPDATE `LockProbe` SET `V` = 100 WHERE `Id` = 1", ct);
                    await transaction.CommitAsync(ct);
                },
                TestContext.Current.CancellationToken);

            Assert.Equal(2, attempts);

            await using var verify = CreateDbContext();
            var values = await verify.Database
                .SqlQueryRaw<int>("SELECT `V` AS `Value` FROM `LockProbe` ORDER BY `Id`")
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.Equal([100, 1], values);
        }
        finally
        {
            await DropProbeTableAsync(setup);
        }
    }

    private static async Task<IDbContextTransaction> BeginAsync(JellyfinDbContext context)
    {
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 1", TestContext.Current.CancellationToken);
        return await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
    }

    private static Task<int> ExecuteAsync(JellyfinDbContext context, string sql)
        => context.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);

    private static IEnumerable<Exception> Chain(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    private static async Task CreateProbeTableAsync(JellyfinDbContext context)
    {
        await DropProbeTableAsync(context);
        await ExecuteAsync(context, "CREATE TABLE `LockProbe` (`Id` INT NOT NULL PRIMARY KEY, `V` INT NOT NULL) ENGINE=InnoDB");
        await ExecuteAsync(context, "INSERT INTO `LockProbe` (`Id`, `V`) VALUES (1, 0), (2, 0)");
    }

    private static Task DropProbeTableAsync(JellyfinDbContext context)
        => context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS `LockProbe`", CancellationToken.None);
}
