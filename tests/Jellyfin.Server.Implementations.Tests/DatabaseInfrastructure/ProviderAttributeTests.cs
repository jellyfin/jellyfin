using System;
using System.Collections.Generic;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class ProviderAttributeTests
{
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
}
