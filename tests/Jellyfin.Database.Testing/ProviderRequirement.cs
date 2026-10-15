using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Jellyfin.Database.Testing;

internal static class ProviderRequirement
{
    public static string? SkipReason(IReadOnlyCollection<TestDbProvider> providers)
        => providers.Contains(TestDatabaseEnvironment.Provider)
            ? null
            : $"Requires provider(s) {string.Join(", ", providers)} (set {TestDatabaseEnvironment.ProviderVariable})";

    public static string? MigrationSkipReason(string migrationId)
    {
        var provider = TestDatabaseEnvironment.Provider;
        return MigrationSkipReason(provider, migrationId, MigrationIds(provider), CoversThroughSqliteMigration(provider));
    }

    /// <summary>
    /// Decides whether a test of a single migration is skipped on a provider. It is skipped only for a migration that the
    /// provider does not have because its baseline migration already includes the change, that is one that sorts at or
    /// before the last SQLite migration the baseline covers. A migration that the provider lacks for any other reason is
    /// a gap, so the test runs and fails.
    /// </summary>
    /// <param name="provider">The provider under test.</param>
    /// <param name="migrationId">The id of the migration the test is about.</param>
    /// <param name="migrationIds">The ids of the migrations of the provider, oldest first.</param>
    /// <param name="coversThroughSqliteMigration">The last SQLite migration that the baseline covers, or <c>null</c> for a provider without a baseline.</param>
    /// <returns>The reason to skip the test, or <c>null</c> to run it.</returns>
    internal static string? MigrationSkipReason(
        TestDbProvider provider,
        string migrationId,
        IReadOnlyList<string> migrationIds,
        string? coversThroughSqliteMigration)
    {
        if (migrationIds.Contains(migrationId, StringComparer.Ordinal)
            || migrationIds.Count == 0
            || coversThroughSqliteMigration is null
            || string.CompareOrdinal(migrationId, coversThroughSqliteMigration) > 0)
        {
            return null;
        }

        return $"The migrations of {provider} do not include {migrationId}: its baseline migration {migrationIds[0]} already includes the change";
    }

    /// <summary>
    /// Gets the last SQLite migration that the baseline migration of a provider covers.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The id of the migration, or <c>null</c> for the SQLite provider, which has no baseline.</returns>
    public static string? CoversThroughSqliteMigration(TestDbProvider provider)
        => EngineTraits.For(provider).CoversThroughSqliteMigration;

    /// <summary>
    /// Lists the ids of the migrations of a provider, oldest first, without a server.
    /// </summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The ids of the migrations.</returns>
    public static IReadOnlyList<string> MigrationIds(TestDbProvider provider)
        => [.. EngineTraits.For(provider).MigrationsAssembly.GetTypes()
            .Select(type => type.GetCustomAttribute<MigrationAttribute>()?.Id)
            .OfType<string>()
            .Order(StringComparer.Ordinal)];

    public static IReadOnlyCollection<KeyValuePair<string, string>> Traits(IEnumerable<TestDbProvider> providers)
        => providers.Select(provider => new KeyValuePair<string, string>("Provider", provider.ToString())).ToArray();
}
