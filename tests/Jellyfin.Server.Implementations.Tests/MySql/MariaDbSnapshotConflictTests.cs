using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

/// <summary>
/// MariaDB raises error 1020 when a transaction under snapshot isolation writes a row that another transaction changed
/// after it read it. Jellyfin's transactions run at read committed, where that cannot happen, but the error is retryable
/// in case one runs at repeatable read. MariaDB turns <c>innodb_snapshot_isolation</c> on by default from 11.8; the
/// test turns it on for its session, so it runs on 11.4 too.
/// </summary>
public sealed class MariaDbSnapshotConflictTests : DatabaseTestFixture
{
    [ProviderFact(TestDbProvider.MariaDb)]
    public async Task RowChangedAfterARepeatableReadTransactionReadIt_Error1020_IsClassifiedAsRetryable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var setup = CreateDbContext();
        await CreateProbeTableAsync(setup);
        try
        {
            await using var context = CreateDbContext();
            await context.Database.OpenConnectionAsync(cancellationToken);
            await ExecuteAsync(context, "SET SESSION innodb_snapshot_isolation = ON");
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
            Assert.Equal([0], await ReadAsync(context));

            await using (var other = CreateDbContext())
            {
                await ExecuteAsync(other, "UPDATE `SnapshotProbe` SET `V` = 1 WHERE `Id` = 1");
            }

            var ex = await Assert.ThrowsAnyAsync<Exception>(() => ExecuteAsync(context, "UPDATE `SnapshotProbe` SET `V` = `V` + 10 WHERE `Id` = 1"));

            Assert.Contains(Chain(ex), e => e is MySqlException { ErrorCode: MySqlErrorCode.CheckRead });
            Assert.True(DatabaseProvider.IsRetryableWriteConflict(ex), ex.ToString());
        }
        finally
        {
            await setup.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS `SnapshotProbe`", CancellationToken.None);
        }
    }

    private static Task<int> ExecuteAsync(JellyfinDbContext context, string sql)
        => context.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);

    private static Task<List<int>> ReadAsync(JellyfinDbContext context)
        => context.Database.SqlQueryRaw<int>("SELECT `V` AS `Value` FROM `SnapshotProbe` WHERE `Id` = 1").ToListAsync(TestContext.Current.CancellationToken);

    private static IEnumerable<Exception> Chain(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    private static async Task CreateProbeTableAsync(JellyfinDbContext context)
    {
        await ExecuteAsync(context, "DROP TABLE IF EXISTS `SnapshotProbe`");
        await ExecuteAsync(context, "CREATE TABLE `SnapshotProbe` (`Id` INT NOT NULL PRIMARY KEY, `V` INT NOT NULL) ENGINE=InnoDB");
        await ExecuteAsync(context, "INSERT INTO `SnapshotProbe` (`Id`, `V`) VALUES (1, 0)");
    }
}
