using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.v3;

namespace Jellyfin.Database.Testing;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ProviderFactAttribute : FactAttribute, ITraitAttribute
{
    public ProviderFactAttribute(params TestDbProvider[] providers)
    {
        Providers = providers;
        Skip = ProviderRequirement.SkipReason(providers);
    }

    public IReadOnlyList<TestDbProvider> Providers { get; }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => ProviderRequirement.Traits(Providers);
}
