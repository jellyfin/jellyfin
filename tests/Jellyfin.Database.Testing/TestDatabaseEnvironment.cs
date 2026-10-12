using System;
using System.Collections.Generic;

namespace Jellyfin.Database.Testing;

public static class TestDatabaseEnvironment
{
    private static readonly Dictionary<TestDbProvider, Func<ITestDatabaseBackend>> _backends = new()
    {
        [TestDbProvider.Sqlite] = () => SqliteTestBackend.Instance
    };

    public const string ProviderVariable = "JELLYFIN_TEST_DB_PROVIDER";

    public static TestDbProvider Provider => ParseProvider(Environment.GetEnvironmentVariable(ProviderVariable));

    public static ITestDatabaseBackend Backend => _backends.TryGetValue(Provider, out var factory)
        ? factory()
        : throw new NotSupportedException($"There is no test database backend for {Provider}.");

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
