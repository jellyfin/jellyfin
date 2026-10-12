using System;
using System.Linq;
using Jellyfin.Database.Providers.Postgres;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public void PostgresMigrationsAfterTheBaseline_ArePairedWithTheSqliteMigrationsAfterWhatItCovers()
    {
        var sqliteAfterBaseline = SqliteMigrationIds()
            .Where(id => string.CompareOrdinal(id, PostgresMigrationBaseline.CoversThroughSqliteMigration) > 0)
            .ToList();
        var postgresAfterBaseline = PostgresMigrationIds().Where(id => id != PostgresMigrationBaseline.MigrationId).ToList();

        Assert.Equal(sqliteAfterBaseline, postgresAfterBaseline);
    }

    [Fact]
    public void PostgresModel_HasNoPendingChanges()
    {
        using var context = OfflineContexts.Postgres();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void SqliteModel_HasNoPendingChanges()
    {
        using var context = OfflineContexts.Sqlite();

        Assert.False(context.Database.HasPendingModelChanges());
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
