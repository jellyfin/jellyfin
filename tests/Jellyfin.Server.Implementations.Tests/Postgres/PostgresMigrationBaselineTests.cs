using System;
using System.Linq;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public class PostgresMigrationBaselineTests
{
    [Fact]
    public void PostgresMigrations_StartWithTheBaseline()
    {
        var ids = PostgresMigrationIds();

        Assert.Equal(PostgresMigrationBaseline.MigrationId, ids[0]);
    }

    [Fact]
    public void Baseline_SortsBeforeEverySqliteMigration()
    {
        Assert.All(SqliteMigrationIds(), id => Assert.True(string.CompareOrdinal(PostgresMigrationBaseline.MigrationId, id) < 0, id));
    }

    [Fact]
    public void CoversThroughSqliteMigration_IsAnExistingSqliteMigration()
    {
        Assert.Contains(PostgresMigrationBaseline.CoversThroughSqliteMigration, SqliteMigrationIds());
    }

    private static string[] PostgresMigrationIds()
    {
        using var context = OfflineContexts.Postgres();
        return [.. context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal)];
    }

    private static string[] SqliteMigrationIds()
    {
        using var context = OfflineContexts.Sqlite();
        return [.. context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal)];
    }
}
