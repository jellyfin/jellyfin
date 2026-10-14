using System;
using System.Collections.Generic;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class ProviderAttributeTests
{
    private const string NormalisedMatchColumns = "20261013120000_AddNormalisedMatchColumns";

    [Fact]
    public void ProviderFact_SelectedProviderListed_IsNotSkipped()
    {
        var attribute = new ProviderFactAttribute(TestDatabaseEnvironment.Provider);

        Assert.Null(attribute.Skip);
    }

    [Fact]
    public void ProviderFact_SelectedProviderNotListed_IsSkippedWithTheReason()
    {
        var attribute = new ProviderFactAttribute();

        Assert.NotNull(attribute.Skip);
        Assert.Contains(TestDatabaseEnvironment.ProviderVariable, attribute.Skip, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderFact_ListedProviders_AreExposedAsTraits()
    {
        var attribute = new ProviderFactAttribute(TestDbProvider.Sqlite);

        Assert.Equal(
            new KeyValuePair<string, string>("Provider", nameof(TestDbProvider.Sqlite)),
            Assert.Single(attribute.GetTraits()));
    }

    [Fact]
    public void ProviderTheory_SelectedProviderListed_IsNotSkipped()
    {
        var attribute = new ProviderTheoryAttribute(TestDatabaseEnvironment.Provider);

        Assert.Null(attribute.Skip);
    }

    [Fact]
    public void ProviderTheory_SelectedProviderNotListed_IsSkippedWithTheReason()
    {
        var attribute = new ProviderTheoryAttribute();

        Assert.NotNull(attribute.Skip);
        Assert.Contains(TestDatabaseEnvironment.ProviderVariable, attribute.Skip, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderTheory_ListedProviders_AreExposedAsTraits()
    {
        var attribute = new ProviderTheoryAttribute(TestDbProvider.Sqlite);

        Assert.Equal(
            new KeyValuePair<string, string>("Provider", nameof(TestDbProvider.Sqlite)),
            Assert.Single(attribute.GetTraits()));
    }

    [Fact]
    public void MigrationFact_MigrationOfTheSelectedProvider_IsNotSkipped()
    {
        var attribute = new MigrationFactAttribute(ProviderRequirement.MigrationIds(TestDatabaseEnvironment.Provider)[0]);

        Assert.Null(attribute.Skip);
    }

    [Theory]
    [InlineData(TestDbProvider.Postgres)]
    [InlineData(TestDbProvider.MySql)]
    [InlineData(TestDbProvider.MariaDb)]
    public void MigrationSkipReason_MissingMigrationAtOrBeforeTheBaselineCoverage_IsSkippedWithAReasonThatNamesTheBaseline(TestDbProvider provider)
    {
        var covers = ProviderRequirement.CoversThroughSqliteMigration(provider)!;
        var migrationIds = ProviderRequirement.MigrationIds(provider);
        const string Before = "20200601000000_SomethingTheBaselineIncludes";

        var atCoverage = ProviderRequirement.MigrationSkipReason(provider, covers, migrationIds, covers);
        var beforeCoverage = ProviderRequirement.MigrationSkipReason(provider, Before, migrationIds, covers);

        Assert.NotNull(atCoverage);
        Assert.NotNull(beforeCoverage);
        Assert.Contains(covers, atCoverage, StringComparison.Ordinal);
        Assert.Contains(Before, beforeCoverage, StringComparison.Ordinal);
        Assert.Contains(migrationIds[0], beforeCoverage, StringComparison.Ordinal);
        Assert.Contains("already includes the change", beforeCoverage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TestDbProvider.Postgres)]
    [InlineData(TestDbProvider.MySql)]
    [InlineData(TestDbProvider.MariaDb)]
    public void MigrationSkipReason_MissingMigrationAfterTheBaselineCoverage_IsNotSkipped(TestDbProvider provider)
    {
        const string After = "29990101000000_NotAMigration";
        var covers = ProviderRequirement.CoversThroughSqliteMigration(provider)!;
        var migrationIds = ProviderRequirement.MigrationIds(provider);

        Assert.DoesNotContain(After, migrationIds);
        Assert.Null(ProviderRequirement.MigrationSkipReason(provider, After, migrationIds, covers));
    }

    [Fact]
    public void MigrationSkipReason_ProviderWithoutABaseline_IsNeverSkipped()
    {
        var migrationIds = ProviderRequirement.MigrationIds(TestDbProvider.Sqlite);

        Assert.Null(ProviderRequirement.CoversThroughSqliteMigration(TestDbProvider.Sqlite));
        Assert.Null(ProviderRequirement.MigrationSkipReason(TestDbProvider.Sqlite, "20200601000000_NotAMigration", migrationIds, null));
    }

    [Fact]
    public void MigrationSkipReason_MigrationThatTheProviderHas_IsNotSkipped()
    {
        var migrationIds = ProviderRequirement.MigrationIds(TestDbProvider.MySql);
        var covers = ProviderRequirement.CoversThroughSqliteMigration(TestDbProvider.MySql);

        Assert.All(migrationIds, id => Assert.Null(ProviderRequirement.MigrationSkipReason(TestDbProvider.MySql, id, migrationIds, covers)));
    }

    [Fact]
    public void MigrationFact_MigrationThatTheSelectedProviderLacksAfterItsBaselineCoverage_IsNotSkipped()
    {
        var attribute = new MigrationFactAttribute("29990101000000_NotAMigration");

        Assert.Null(attribute.Skip);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void MigrationFact_NormalisedMatchColumnsOnMySqlAndMariaDb_IsSkippedBecauseTheBaselineCoversIt()
    {
        var attribute = new MigrationFactAttribute(NormalisedMatchColumns);

        Assert.NotNull(attribute.Skip);
        Assert.Contains("baseline", attribute.Skip, StringComparison.Ordinal);
        Assert.DoesNotContain("yet", attribute.Skip, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Sqlite, TestDbProvider.Postgres)]
    public void MigrationFact_NormalisedMatchColumnsOnSqliteAndPostgres_IsNotSkipped()
    {
        var attribute = new MigrationFactAttribute(NormalisedMatchColumns);

        Assert.Null(attribute.Skip);
    }
}
