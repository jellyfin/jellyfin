using System;
using System.Linq;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public sealed class DefaultSchemaTests : DatabaseTestFixture
{
    [Fact]
    public void DefaultDatabase_HasEveryMigrationApplied_OrTheSchemaOfTheModelWithoutMigrations()
    {
        using var context = CreateDbContext();

        var migrations = context.Database.GetMigrations().ToList();

        if (EngineTraits.Selected.HasSchemaMigrations)
        {
            Assert.NotEmpty(migrations);
            Assert.Equal(migrations, context.Database.GetAppliedMigrations());
        }
        else
        {
            // A provider without migrations: the default database has the schema of the model, and no history.
            Assert.Empty(migrations);
            Assert.Equal(TestSchemaSource.Model, EngineTraits.Selected.DefaultSchemaSource);
            Assert.Empty(context.Database.GetAppliedMigrations());
        }
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void DefaultDatabase_IndexesThePeopleByLowercasedNameOnSqlite()
    {
        using var context = CreateDbContext();

        var definition = context.Database
            .SqlQueryRaw<string>("""SELECT sql AS "Value" FROM sqlite_master WHERE type = 'index' AND name = 'IX_Peoples_NameLower'""")
            .Single();

        Assert.Contains("(\"NameLower\")", definition, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void DefaultDatabase_IndexesThePeopleByLowercasedNameOnPostgres()
    {
        using var context = CreateDbContext();

        var definition = context.Database
            .SqlQueryRaw<string>("""SELECT indexdef AS "Value" FROM pg_indexes WHERE schemaname = current_schema() AND indexname = 'IX_Peoples_NameLower'""")
            .Single();

        Assert.EndsWith("(\"NameLower\")", definition, StringComparison.Ordinal);
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
