using System.Linq;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public sealed class DefaultSchemaTests : DatabaseTestFixture
{
    [Fact]
    public void DefaultDatabase_HasEveryMigrationApplied()
    {
        using var context = CreateDbContext();

        var migrations = context.Database.GetMigrations().ToList();

        Assert.NotEmpty(migrations);
        Assert.Equal(migrations, context.Database.GetAppliedMigrations());
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void DefaultDatabase_IndexesThePeopleByLowercasedNameOnSqlite()
    {
        using var context = CreateDbContext();

        var count = context.Database
            .SqlQueryRaw<long>("""SELECT count(*) AS "Value" FROM sqlite_master WHERE type = 'index' AND name = 'IX_Peoples_NameLower'""")
            .Single();

        Assert.Equal(1, count);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void DefaultDatabase_IndexesThePeopleByLowercasedNameOnPostgres()
    {
        using var context = CreateDbContext();

        var count = context.Database
            .SqlQueryRaw<long>("""SELECT count(*) AS "Value" FROM pg_indexes WHERE schemaname = current_schema() AND indexname = 'IX_Peoples_NameLower'""")
            .Single();

        Assert.Equal(1, count);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void DefaultDatabase_CanTakeTheMinimumOfAnItemId()
    {
        using var context = CreateDbContext();

        var rows = context.Database
            .SqlQueryRaw<long>("""SELECT count(*) AS "Value" FROM (SELECT min("Id") FROM "BaseItems") AS minimum""")
            .Single();

        Assert.Equal(1, rows);
    }
}
