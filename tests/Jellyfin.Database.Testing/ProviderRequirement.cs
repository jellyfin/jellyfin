using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Database.Testing;

internal static class ProviderRequirement
{
    public static string? SkipReason(IReadOnlyCollection<TestDbProvider> providers)
        => providers.Contains(TestDatabaseEnvironment.Provider)
            ? null
            : $"Requires provider(s) {string.Join(", ", providers)} (set {TestDatabaseEnvironment.ProviderVariable})";

    public static string? MigrationsSkipReason()
        => TestDatabaseEnvironment.Provider switch
        {
            TestDbProvider.MySql => "no MySQL migrations yet",
            TestDbProvider.MariaDb => "no MariaDB migrations yet",
            _ => null
        };

    public static IReadOnlyCollection<KeyValuePair<string, string>> Traits(IEnumerable<TestDbProvider> providers)
        => providers.Select(provider => new KeyValuePair<string, string>("Provider", provider.ToString())).ToArray();
}
