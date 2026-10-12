using System;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class TestDatabaseEnvironmentTests
{
    [Theory]
    [InlineData(null, TestDbProvider.Sqlite)]
    [InlineData("", TestDbProvider.Sqlite)]
    [InlineData("  ", TestDbProvider.Sqlite)]
    [InlineData("Sqlite", TestDbProvider.Sqlite)]
    [InlineData("sqlite", TestDbProvider.Sqlite)]
    [InlineData("POSTGRES", TestDbProvider.Postgres)]
    public void ParseProvider_KnownValue_ReturnsProvider(string? value, TestDbProvider expected)
    {
        Assert.Equal(expected, TestDatabaseEnvironment.ParseProvider(value));
    }

    [Theory]
    [InlineData("oracle")]
    [InlineData("42")]
    [InlineData("Sqlite,Postgres")]
    public void ParseProvider_UnknownValue_Throws(string value)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TestDatabaseEnvironment.ParseProvider(value));

        Assert.Contains(TestDatabaseEnvironment.ProviderVariable, ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TestDbProvider.Sqlite), ex.Message, StringComparison.Ordinal);
    }
}
