using System;
using System.Linq;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Database.Testing;
using Jellyfin.Database.Testing.Conformance;
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
    [InlineData(TestDbProvider.SqlServer, typeof(SqlServerDatabaseProvider), "Jellyfin-SqlServer")]
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
    [InlineData(TestDbProvider.SqlServer, SqlServerMigrationBaseline.CoversThroughSqliteMigration)]
    public void CoversThroughSqliteMigration_IsTheOneOfTheBaselineOfTheEngine(TestDbProvider provider, string? expected)
    {
        Assert.Equal(expected, EngineTraits.For(provider).CoversThroughSqliteMigration);
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite, true, true)]
    [InlineData(TestDbProvider.Postgres, false, false)]
    [InlineData(TestDbProvider.MySql, false, true)]
    [InlineData(TestDbProvider.MariaDb, false, true)]
    [InlineData(TestDbProvider.SqlServer, false, true)]
    public void EmbeddedAndPooled_AreTheEnginesThatAreSo(TestDbProvider provider, bool embedded, bool pooled)
    {
        var traits = EngineTraits.For(provider);

        Assert.Equal(embedded, traits.IsEmbedded);
        Assert.Equal(pooled, traits.Pool is not null);
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite, null)]
    [InlineData(TestDbProvider.Postgres, "PostgreSQL")]
    [InlineData(TestDbProvider.MySql, null)]
    [InlineData(TestDbProvider.MariaDb, "MariaDB")]
    [InlineData(TestDbProvider.SqlServer, "Microsoft SQL Server")]
    public void ServerMarker_IsWhatTheVersionOfTheServerNames(TestDbProvider provider, string? marker)
    {
        Assert.Equal(marker, EngineTraits.For(provider).ServerMarker);
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite)]
    [InlineData(TestDbProvider.Postgres)]
    public void UnexpressibleIndexFilters_AreNoneOnTheEnginesWithPartialIndexes(TestDbProvider provider)
    {
        Assert.Empty(EngineTraits.For(provider).UnexpressibleIndexFilters);
    }

    [Theory]
    [InlineData(TestDbProvider.MySql)]
    [InlineData(TestDbProvider.MariaDb)]
    public void UnexpressibleIndexFilters_AreEveryFilterOnTheEnginesWithoutPartialIndexes(TestDbProvider provider)
    {
        string[] every =
        [
            "\"OwnerId\" IS NOT NULL",
            "\"ExtraType\" IS NOT NULL",
            "\"PrimaryVersionId\" IS NOT NULL",
            "\"PrimaryVersionId\" IS NULL AND (\"OwnerId\" IS NULL OR \"ExtraType\" IS NOT NULL)"
        ];

        Assert.Equal(every.Order(StringComparer.Ordinal), EngineTraits.For(provider).UnexpressibleIndexFilters.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite)]
    [InlineData(TestDbProvider.Postgres)]
    [InlineData(TestDbProvider.MySql)]
    [InlineData(TestDbProvider.MariaDb)]
    public void NoActionCascades_AreNoneOnTheEnginesThatCascadeEveryKey(TestDbProvider provider)
    {
        Assert.Empty(EngineTraits.For(provider).NoActionCascades);
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite)]
    [InlineData(TestDbProvider.Postgres)]
    [InlineData(TestDbProvider.MySql)]
    [InlineData(TestDbProvider.MariaDb)]
    public void NullFilteredUniqueIndexes_AreNoneOnTheEnginesWhoseUniqueIndexesTakeManyNulls(TestDbProvider provider)
    {
        Assert.Empty(EngineTraits.For(provider).NullFilteredUniqueIndexes);
    }

    [Fact]
    public void ModelAllowances_OfSqlServer_AreTheOrFilterTheTwoCascadePathsAndTheUniqueIndexOnANullableUser()
    {
        var traits = EngineTraits.For(TestDbProvider.SqlServer);

        Assert.Equal(["\"PrimaryVersionId\" IS NULL AND (\"OwnerId\" IS NULL OR \"ExtraType\" IS NOT NULL)"], traits.UnexpressibleIndexFilters);
        Assert.Equal([("AncestorIds", "ParentItemId"), ("BaseItems", "ParentId")], traits.NoActionCascades.Order());
        Assert.Equal([("ImageInfos", "UserId", "[UserId] IS NOT NULL")], traits.NullFilteredUniqueIndexes);
    }

    [Fact]
    public void ModelAllowances_AreTheThreeSetsOfTheRow()
    {
        Assert.All(Enum.GetValues<TestDbProvider>().Select(EngineTraits.For), traits =>
        {
            Assert.Same(traits.UnexpressibleIndexFilters, traits.ModelAllowances.UnexpressibleIndexFilters);
            Assert.Same(traits.NoActionCascades, traits.ModelAllowances.NoActionCascades);
            Assert.Same(traits.NullFilteredUniqueIndexes, traits.ModelAllowances.NullFilteredUniqueIndexes);
        });
    }

    [Fact]
    public void NoActionCascades_OfEveryEngine_AreCoveredByTheFolderDeleteRowOfTheKit()
    {
        // The server deletes the rows of such a key itself, which only the folder delete row holds an engine to.
        Assert.All(
            Enum.GetValues<TestDbProvider>().Select(EngineTraits.For).Where(traits => traits.NoActionCascades.Count > 0),
            traits => Assert.Contains(traits.Provider, ConformanceKit.Engines));
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite, true)]
    [InlineData(TestDbProvider.Postgres, true)]
    [InlineData(TestDbProvider.MySql, false)]
    [InlineData(TestDbProvider.MariaDb, false)]
    [InlineData(TestDbProvider.SqlServer, true)]
    public void PurgeResetsIdentityInATransaction_IsFalseOnlyOnTheEnginesThatCommitToChangeACounter(TestDbProvider provider, bool resets)
    {
        Assert.Equal(resets, EngineTraits.For(provider).PurgeResetsIdentityInATransaction);
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
        Assert.ThrowsAny<XunitException>(() => EngineTraits.For(TestDbProvider.SqlServer).AssertUniqueViolation(violation));
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite, true, true, false)]
    [InlineData(TestDbProvider.Postgres, true, true, false)]
    [InlineData(TestDbProvider.MySql, true, true, true)]
    [InlineData(TestDbProvider.MariaDb, true, true, true)]
    [InlineData(TestDbProvider.SqlServer, true, true, true)]
    public void RegistrationMigrationsAndBounds_AreThoseOfTheEngine(TestDbProvider provider, bool registered, bool migrations, bool bounds)
    {
        var traits = EngineTraits.For(provider);

        Assert.Equal(registered, traits.IsRegistered);
        Assert.Equal(migrations, traits.HasSchemaMigrations);
        Assert.Equal(bounds, traits.BoundsIndexedStrings);
        Assert.Equal(migrations ? TestSchemaSource.Migrations : TestSchemaSource.Model, traits.DefaultSchemaSource);
    }

    [Fact]
    public void HasSchemaMigrations_IsWhetherTheMigrationsAssemblyHasMigrations()
    {
        // The trait of an engine whose provider gains its migrations fails here until it is changed.
        Assert.All(
            EngineTraits.All,
            traits => Assert.True(
                traits.HasSchemaMigrations == ProviderRequirement.MigrationIds(traits.Provider).Count > 0,
                $"{traits.Provider}: HasSchemaMigrations is {traits.HasSchemaMigrations}, the assembly has {ProviderRequirement.MigrationIds(traits.Provider).Count} migrations"));
    }

    [Fact]
    public void All_HasARowForEveryEngine()
    {
        Assert.Equal(Enum.GetValues<TestDbProvider>().Order(), EngineTraits.All.Select(traits => traits.Provider).Order());
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
