using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Database.Testing;

internal static class ProviderRequirement
{
    public static string? SkipReason(IReadOnlyCollection<TestDbProvider> providers)
        => providers.Contains(TestDatabaseEnvironment.Provider)
            ? null
            : $"Requires provider(s) {string.Join(", ", providers)} (set {TestDatabaseEnvironment.ProviderVariable})";

    public static IReadOnlyCollection<KeyValuePair<string, string>> Traits(IEnumerable<TestDbProvider> providers)
        => providers.Select(provider => new KeyValuePair<string, string>("Provider", provider.ToString())).ToArray();
}
