using System;
using System.Linq;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlMigrationBaselineTests
{
    [Fact]
    public void MySqlMigrations_StartWithTheBaseline()
    {
        var ids = MySqlMigrationIds();

        Assert.Equal(MySqlMigrationBaseline.MigrationId, ids[0]);
    }

    [Fact]
    public void Baseline_SortsBeforeEverySqliteMigration()
    {
        Assert.All(SqliteMigrationIds(), id => Assert.True(string.CompareOrdinal(MySqlMigrationBaseline.MigrationId, id) < 0, id));
    }

    [Fact]
    public void CoversThroughSqliteMigration_IsAnExistingSqliteMigration()
    {
        Assert.Contains(MySqlMigrationBaseline.CoversThroughSqliteMigration, SqliteMigrationIds());
    }

    [Fact]
    public void MySqlMigrationsAfterTheBaseline_ArePairedWithTheSqliteMigrationsAfterWhatItCovers()
    {
        var sqliteAfterBaseline = SqliteMigrationIds()
            .Where(id => string.CompareOrdinal(id, MySqlMigrationBaseline.CoversThroughSqliteMigration) > 0)
            .ToList();
        var mySqlAfterBaseline = MySqlMigrationIds().Where(id => id != MySqlMigrationBaseline.MigrationId).ToList();

        Assert.Equal(sqliteAfterBaseline, mySqlAfterBaseline);
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.7-mariadb")]
    public void Model_HasNoPendingChanges(string server)
    {
        using var context = OfflineContexts.MySql(server);

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.7-mariadb")]
    public void Migrations_AreTheSameForBothServers(string server)
    {
        using var context = OfflineContexts.MySql(server);

        var ids = context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal).ToList();

        Assert.NotEmpty(ids);
        Assert.Equal(MySqlMigrationBaseline.MigrationId, ids[0]);
        Assert.Equal(MySqlMigrationIds(), ids);
    }

    private static string[] MySqlMigrationIds()
    {
        using var context = OfflineContexts.MySql();
        return [.. context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal)];
    }

    private static string[] SqliteMigrationIds()
    {
        using var context = OfflineContexts.Sqlite();
        return [.. context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal)];
    }
}
