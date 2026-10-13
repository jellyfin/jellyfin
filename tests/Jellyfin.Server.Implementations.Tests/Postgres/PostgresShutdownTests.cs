using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

[Collection(PostgresPoolIsolation.Name)]
public sealed class PostgresShutdownTests : DatabaseTestFixture
{
    private static readonly TimeSpan _closeTimeout = TimeSpan.FromSeconds(30);

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RunShutdownTask_ConnectionsArePooled_ClosesThem()
    {
        await using (var context = CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync("SELECT 1", TestContext.Current.CancellationToken);
        }

        Assert.True(await CountOtherConnectionsAsync() > 0, "The released connection should be idle in the pool.");

        await DatabaseProvider.RunShutdownTask(TestContext.Current.CancellationToken);

        var remaining = await CountOtherConnectionsAsync();
        var stopwatch = Stopwatch.StartNew();
        while (remaining > 0 && stopwatch.Elapsed < _closeTimeout)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
            remaining = await CountOtherConnectionsAsync();
        }

        Assert.Equal(0, remaining);
    }

    private async Task<long> CountOtherConnectionsAsync()
    {
        string connectionString;
        await using (var context = CreateDbContext())
        {
            connectionString = RelationalOptionsExtension.Extract(context.GetService<IDbContextOptions>()).ConnectionString!;
        }

        // Unpooled, so that counting does not leave a connection of its own in the pool that is being checked.
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid()",
            connection);
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
