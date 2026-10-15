using System;
using System.Linq;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Testing;
using Microsoft.Data.Sqlite;
using Xunit;
using Xunit.Sdk;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Holds the table of engine traits to what each engine is, written out here apart from the table.
/// </summary>
public class EngineTraitsTests
{
    [Fact]
    public void For_EveryEngine_HasTheRowOfThatEngine()
    {
        Assert.All(Enum.GetValues<TestDbProvider>(), provider => Assert.Equal(provider, EngineTraits.For(provider).Provider));
    }

    [Fact]
    public void For_UnknownEngine_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EngineTraits.For((TestDbProvider)(-1)));
    }

    [Fact]
    public void Selected_IsTheRowOfTheSelectedEngine()
    {
        Assert.Equal(TestDatabaseEnvironment.Provider, EngineTraits.Selected.Provider);
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite, typeof(SqliteDatabaseProvider), "Jellyfin-SQLite")]
    [InlineData(TestDbProvider.Postgres, typeof(PostgresDatabaseProvider), "Jellyfin-PostgreSQL")]
    [InlineData(TestDbProvider.MySql, typeof(MySqlDatabaseProvider), "Jellyfin-MySQL")]
    [InlineData(TestDbProvider.MariaDb, typeof(MySqlDatabaseProvider), "Jellyfin-MySQL")]
    public void ProviderTypeAndKey_AreThoseOfTheProviderOfTheEngine(TestDbProvider provider, Type providerType, string key)
    {
        var traits = EngineTraits.For(provider);

        Assert.Equal(providerType, traits.ProviderType);
        Assert.Equal(providerType.Assembly, traits.MigrationsAssembly);
        Assert.Equal(key, traits.ProviderKey);
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite, null)]
    [InlineData(TestDbProvider.Postgres, PostgresMigrationBaseline.CoversThroughSqliteMigration)]
    [InlineData(TestDbProvider.MySql, MySqlMigrationBaseline.CoversThroughSqliteMigration)]
    [InlineData(TestDbProvider.MariaDb, MySqlMigrationBaseline.CoversThroughSqliteMigration)]
    public void CoversThroughSqliteMigration_IsTheOneOfTheBaselineOfTheEngine(TestDbProvider provider, string? expected)
    {
        Assert.Equal(expected, EngineTraits.For(provider).CoversThroughSqliteMigration);
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite, true, true)]
    [InlineData(TestDbProvider.Postgres, false, false)]
    [InlineData(TestDbProvider.MySql, false, true)]
    [InlineData(TestDbProvider.MariaDb, false, true)]
    public void EmbeddedAndPooled_AreTheEnginesThatAreSo(TestDbProvider provider, bool embedded, bool pooled)
    {
        var traits = EngineTraits.For(provider);

        Assert.Equal(embedded, traits.IsEmbedded);
        Assert.Equal(pooled, traits.Pool is not null);
    }

    [Fact]
    public void AssertUniqueViolation_AnotherErrorOrNone_FailsOnEveryEngine()
    {
        Exception?[] others = [null, new InvalidOperationException("duplicate"), new SqliteException("constraint failed", 19, 787)];

        Assert.All(
            Enum.GetValues<TestDbProvider>().Select(EngineTraits.For),
            traits => Assert.All(others, other => Assert.ThrowsAny<XunitException>(() => traits.AssertUniqueViolation(other))));
    }

    [Fact]
    public void AssertUniqueViolation_UniqueConstraintOfSqlite_PassesOnSqliteOnly()
    {
        var violation = new SqliteException("UNIQUE constraint failed", 19, 2067);

        EngineTraits.For(TestDbProvider.Sqlite).AssertUniqueViolation(violation);
        Assert.ThrowsAny<XunitException>(() => EngineTraits.For(TestDbProvider.Postgres).AssertUniqueViolation(violation));
        Assert.ThrowsAny<XunitException>(() => EngineTraits.For(TestDbProvider.MySql).AssertUniqueViolation(violation));
    }

    [Fact]
    public void AssertUniqueViolation_ExceptionOfAnotherTypeDerivedFromThatOfTheClient_Fails()
    {
        var violation = new DerivedSqliteException("UNIQUE constraint failed", 19, 2067);

        var ex = Assert.ThrowsAny<XunitException>(() => EngineTraits.For(TestDbProvider.Sqlite).AssertUniqueViolation(violation));

        Assert.Contains(nameof(DerivedSqliteException), ex.Message, StringComparison.Ordinal);
    }

    private sealed class DerivedSqliteException(string message, int errorCode, int extendedErrorCode)
        : SqliteException(message, errorCode, extendedErrorCode);
}
