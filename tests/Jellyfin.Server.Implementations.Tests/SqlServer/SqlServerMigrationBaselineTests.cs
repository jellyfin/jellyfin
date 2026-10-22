using System;
using System.Linq;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

public class SqlServerMigrationBaselineTests
{
    [Fact]
    public void SqlServerMigrations_StartWithTheBaseline()
    {
        var ids = SqlServerMigrationIds();

        Assert.Equal(SqlServerMigrationBaseline.MigrationId, ids[0]);
    }

    [Fact]
    public void Baseline_SortsBeforeEverySqliteMigration()
    {
        Assert.All(SqliteMigrationIds(), id => Assert.True(string.CompareOrdinal(SqlServerMigrationBaseline.MigrationId, id) < 0, id));
    }

    [Fact]
    public void CoversThroughSqliteMigration_IsAnExistingSqliteMigration()
    {
        Assert.Contains(SqlServerMigrationBaseline.CoversThroughSqliteMigration, SqliteMigrationIds());
    }

    [Fact]
    public void SqlServerMigrationsAfterTheBaseline_ArePairedWithTheSqliteMigrationsAfterWhatItCovers()
    {
        var sqliteAfterBaseline = SqliteMigrationIds()
            .Where(id => string.CompareOrdinal(id, SqlServerMigrationBaseline.CoversThroughSqliteMigration) > 0)
            .ToList();
        var sqlServerAfterBaseline = SqlServerMigrationIds().Where(id => id != SqlServerMigrationBaseline.MigrationId).ToList();

        Assert.Equal(sqliteAfterBaseline, sqlServerAfterBaseline);
    }

    [Fact]
    public void Model_HasNoPendingChanges()
    {
        using var context = OfflineContexts.SqlServer();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static string[] SqlServerMigrationIds()
    {
        using var context = OfflineContexts.SqlServer();
        return [.. context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal)];
    }

    private static string[] SqliteMigrationIds()
    {
        using var context = OfflineContexts.Sqlite();
        return [.. context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal)];
    }
}
