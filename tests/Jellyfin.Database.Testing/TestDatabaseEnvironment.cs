using System;
using System.Collections.Generic;

namespace Jellyfin.Database.Testing;

public static class TestDatabaseEnvironment
{
    private static readonly Dictionary<TestDbProvider, Func<ITestDatabaseBackend>> _backends = new()
    {
        [TestDbProvider.Sqlite] = () => SqliteTestBackend.Instance,
        [TestDbProvider.Postgres] = () => PostgresTestBackend.Instance,
        [TestDbProvider.MySql] = () => MySqlTestBackend.Instance,
        [TestDbProvider.MariaDb] = () => MariaDbTestBackend.Instance
    };

    public const string ProviderVariable = "JELLYFIN_TEST_DB_PROVIDER";

    public static TestDbProvider Provider => ParseProvider(Environment.GetEnvironmentVariable(ProviderVariable));

    public static ITestDatabaseBackend Backend => _backends.TryGetValue(Provider, out var factory)
        ? factory()
        : throw new NotSupportedException($"There is no test database backend for {Provider}.");

    /// <summary>
    /// Gets the schema source that a test database uses unless the test asks for another: the migrations of the provider,
    /// or the model for a provider that has no migrations.
    /// </summary>
    public static TestSchemaSource DefaultSchemaSource => Provider is TestDbProvider.MySql or TestDbProvider.MariaDb
        ? TestSchemaSource.Model
        : TestSchemaSource.Migrations;

    public static string? GetProviderSetting(TestDbProvider provider, string name)
    {
        var value = Environment.GetEnvironmentVariable($"JELLYFIN_TEST_{provider.ToString().ToUpperInvariant()}_{name}");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static TestDbProvider ParseProvider(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TestDbProvider.Sqlite;
        }

        foreach (var provider in Enum.GetValues<TestDbProvider>())
        {
            if (string.Equals(provider.ToString(), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return provider;
            }
        }

        throw new InvalidOperationException(
            $"{ProviderVariable} is set to '{value}', expected one of: {string.Join(", ", Enum.GetNames<TestDbProvider>())}.");
    }
}
